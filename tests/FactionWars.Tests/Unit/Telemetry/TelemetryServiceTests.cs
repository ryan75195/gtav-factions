using System;
using System.Collections.Generic;
using FactionWars.AI.Interfaces;
using FactionWars.AI.Models;
using FactionWars.Combat.Events;
using FactionWars.Combat.Interfaces;
using FactionWars.Combat.Models;
using FactionWars.Core.Interfaces;
using FactionWars.Core.Models;
using FactionWars.Economy.Interfaces;
using FactionWars.Factions.Interfaces;
using FactionWars.Factions.Models;
using FactionWars.ScriptHookV.Managers;
using FactionWars.ScriptHookV.Persistence;
using FactionWars.Telemetry.Interfaces;
using FactionWars.Telemetry.Models;
using FactionWars.Telemetry.Services;
using FactionWars.Territory.Events;
using FactionWars.Territory.Interfaces;
using Moq;
using Newtonsoft.Json.Linq;
using Xunit;

namespace FactionWars.Tests.Unit.Telemetry
{
    public class TelemetryServiceTests
    {
        private readonly Mock<ITelemetrySink> _sink = new();
        private readonly Mock<IFactionService> _factionService = new();
        private readonly Mock<IZoneService> _zoneService = new();
        private readonly Mock<IGameStateManager> _gameStateManager = new();

        public TelemetryServiceTests()
        {
            _factionService.Setup(s => s.GetAllFactions()).Returns(System.Array.Empty<Faction>());
            _gameStateManager.Setup(g => g.TotalPlayTimeSeconds).Returns(0);
        }

        [Fact]
        public void Tick_BuildsSnapshotAndWritesToSink()
        {
            var michael = new Faction("michael", "Michael's Crew");
            _factionService.Setup(s => s.GetAllFactions()).Returns([michael]);
            _factionService.Setup(s => s.GetFactionState("michael"))
                .Returns(new FactionState("michael") { Cash = 500 });
            _zoneService.Setup(z => z.GetZoneCount("michael")).Returns(8);
            _gameStateManager.Setup(g => g.TotalPlayTimeSeconds).Returns(123L);

            using var svc = new TelemetryService(_sink.Object, _factionService.Object,
                _zoneService.Object, _gameStateManager.Object);

            svc.Tick(); // public test entry that bypasses the timer

            _sink.Verify(s => s.WriteSnapshot(It.Is<IReadOnlyList<FactionSnapshot>>(rows =>
                rows.Count == 1 && rows[0].FactionId == "michael" && rows[0].Cash == 500
                && rows[0].PlayTimeSeconds == 123L)), Times.Once);
        }

        [Fact]
        public void Tick_EmptyFactionList_DoesNotCallSink()
        {
            using var svc = new TelemetryService(_sink.Object, _factionService.Object,
                _zoneService.Object, _gameStateManager.Object);

            svc.Tick();

            _sink.Verify(s => s.WriteSnapshot(It.IsAny<IReadOnlyList<FactionSnapshot>>()), Times.Never);
        }

        [Fact]
        public void Dispose_StopsTimerAndIsIdempotent()
        {
            var svc = new TelemetryService(_sink.Object, _factionService.Object,
                _zoneService.Object, _gameStateManager.Object);
            svc.Dispose();
            svc.Dispose(); // must not throw
        }

        [Fact]
        public void TickAfterDispose_DoesNotCallSink()
        {
            var michael = new Faction("michael", "Michael's Crew");
            _factionService.Setup(s => s.GetAllFactions()).Returns([michael]);
            _factionService.Setup(s => s.GetFactionState("michael"))
                .Returns(new FactionState("michael") { Cash = 500 });

            var svc = new TelemetryService(_sink.Object, _factionService.Object,
                _zoneService.Object, _gameStateManager.Object);
            svc.Dispose();
            svc.Tick();

            _sink.Verify(s => s.WriteSnapshot(It.IsAny<IReadOnlyList<FactionSnapshot>>()), Times.Never);
        }

        [Fact]
        public void Tick_BuilderThrows_DoesNotPropagate()
        {
            // Cause the snapshot builder to throw via the underlying faction service.
            _factionService.Setup(s => s.GetAllFactions()).Throws(new InvalidOperationException("boom"));

            using var svc = new TelemetryService(_sink.Object, _factionService.Object,
                _zoneService.Object, _gameStateManager.Object);

            // Must not propagate the exception out of Tick.
            svc.Tick();

            _sink.Verify(s => s.WriteSnapshot(It.IsAny<IReadOnlyList<FactionSnapshot>>()), Times.Never);
        }

        [Fact]
        public void Update_AccumulatesUnderInterval_DoesNotTrigger()
        {
            var michael = new Faction("michael", "Michael's Crew");
            _factionService.Setup(s => s.GetAllFactions()).Returns([michael]);
            _factionService.Setup(s => s.GetFactionState("michael"))
                .Returns(new FactionState("michael") { Cash = 500 });

            using var svc = new TelemetryService(_sink.Object, _factionService.Object,
                _zoneService.Object, _gameStateManager.Object);

            svc.Update(30f);
            svc.Update(29f); // 59s total, still under 60

            _sink.Verify(s => s.WriteSnapshot(It.IsAny<IReadOnlyList<FactionSnapshot>>()), Times.Never);
        }

        [Fact]
        public void Update_AccumulatesAtInterval_TriggersOnce()
        {
            var michael = new Faction("michael", "Michael's Crew");
            _factionService.Setup(s => s.GetAllFactions()).Returns([michael]);
            _factionService.Setup(s => s.GetFactionState("michael"))
                .Returns(new FactionState("michael") { Cash = 500 });

            using var svc = new TelemetryService(_sink.Object, _factionService.Object,
                _zoneService.Object, _gameStateManager.Object);

            svc.Update(30f);
            svc.Update(30f); // 60s total

            _sink.Verify(s => s.WriteSnapshot(It.IsAny<IReadOnlyList<FactionSnapshot>>()), Times.Once);
        }

        [Fact]
        public void Update_AfterTrigger_ResetsAccumulator()
        {
            var michael = new Faction("michael", "Michael's Crew");
            _factionService.Setup(s => s.GetAllFactions()).Returns([michael]);
            _factionService.Setup(s => s.GetFactionState("michael"))
                .Returns(new FactionState("michael") { Cash = 500 });

            using var svc = new TelemetryService(_sink.Object, _factionService.Object,
                _zoneService.Object, _gameStateManager.Object);

            svc.Update(60f); // first trigger
            svc.Update(30f); // accumulator now 30 (post-reset), not 90
            _sink.Verify(s => s.WriteSnapshot(It.IsAny<IReadOnlyList<FactionSnapshot>>()), Times.Once);

            svc.Update(30f); // total 60 after reset → second trigger
            _sink.Verify(s => s.WriteSnapshot(It.IsAny<IReadOnlyList<FactionSnapshot>>()), Times.Exactly(2));
        }

        [Fact]
        public void Update_PlayerDies_WritesDeathWithZoneAndPosition()
        {
            var dead = false;
            _gameStateManager.Setup(g => g.TotalPlayTimeSeconds).Returns(3723L);
            using var svc = new TelemetryService(_sink.Object, _factionService.Object,
                _zoneService.Object, _gameStateManager.Object,
                new TelemetryServiceOptions
                {
                    IsPlayerDead = () => dead,
                    GetCurrentZoneId = () => "morningwood",
                    GetPlayerPosition = () => new Vector3(1.5f, 2.5f, 3.5f),
                });

            dead = true;
            svc.Update(0.1f);

            _sink.Verify(s => s.WritePlayerEvent(It.Is<PlayerEventRow>(r =>
                r.Type == PlayerEventType.Death
                && r.ZoneId == "morningwood"
                && r.PlayTimeSeconds == 3723L
                && r.Details.Contains("\"x\":1.5")
                && r.Details.Contains("\"y\":2.5")
                && r.Details.Contains("\"z\":3.5"))), Times.Once);
        }

        [Fact]
        public void Update_PlayerRespawns_WritesRespawnAtHospital()
        {
            var dead = true;
            using var svc = new TelemetryService(_sink.Object, _factionService.Object,
                _zoneService.Object, _gameStateManager.Object,
                new TelemetryServiceOptions
                {
                    IsPlayerDead = () => dead,
                    GetCurrentZoneId = () => null,
                    GetPlayerPosition = () => new Vector3(10f, 20f, 30f),
                });

            dead = false;
            svc.Update(0.1f);

            _sink.Verify(s => s.WritePlayerEvent(It.Is<PlayerEventRow>(r =>
                r.Type == PlayerEventType.RespawnAtHospital
                && r.ZoneId == null
                && r.Details.Contains("\"x\":10"))), Times.Once);
        }

        [Fact]
        public void Update_PlayerInBattle_WritesBattleEntered()
        {
            var battle = CreatePlayerAttackBattle();
            var battleManager = new Mock<IZoneBattleManager>();
            battleManager.Setup(b => b.GetPlayerCurrentBattle()).Returns(battle);
            _gameStateManager.Setup(g => g.TotalPlayTimeSeconds).Returns(500L);

            using var svc = new TelemetryService(_sink.Object, _factionService.Object,
                _zoneService.Object, _gameStateManager.Object,
                new TelemetryServiceOptions { ZoneBattleManager = battleManager.Object });

            svc.Update(0.1f);

            _sink.Verify(s => s.WritePlayerEvent(It.Is<PlayerEventRow>(r =>
                r.Type == PlayerEventType.BattleEntered
                && r.ZoneId == "pillbox_hill"
                && r.TargetFaction == "franklin"
                && r.PlayTimeSeconds == 500L
                && r.Details.Contains("\"player_faction\":\"michael\""))), Times.Once);
        }

        [Fact]
        public void Update_PlayerLeavesBattleBeforeResolution_WritesBattleAbandoned()
        {
            ZoneBattle? currentBattle = CreatePlayerAttackBattle();
            var battleManager = new Mock<IZoneBattleManager>();
            battleManager.Setup(b => b.GetPlayerCurrentBattle()).Returns(() => currentBattle);

            using var svc = new TelemetryService(_sink.Object, _factionService.Object,
                _zoneService.Object, _gameStateManager.Object,
                new TelemetryServiceOptions { ZoneBattleManager = battleManager.Object });

            svc.Update(0.1f);
            currentBattle = null;
            svc.Update(0.1f);

            _sink.Verify(s => s.WritePlayerEvent(It.Is<PlayerEventRow>(r =>
                r.Type == PlayerEventType.BattleAbandoned
                && r.ZoneId == "pillbox_hill")), Times.Once);
        }

        [Fact]
        public void Update_PlayerBattleEnds_WritesBattleExitedNotAbandoned()
        {
            ZoneBattle? currentBattle = CreatePlayerAttackBattle();
            var completedBattle = currentBattle;
            var battleManager = new Mock<IZoneBattleManager>();
            battleManager.Setup(b => b.GetPlayerCurrentBattle()).Returns(() => currentBattle);

            using var svc = new TelemetryService(_sink.Object, _factionService.Object,
                _zoneService.Object, _gameStateManager.Object,
                new TelemetryServiceOptions { ZoneBattleManager = battleManager.Object });

            svc.Update(0.1f);
            battleManager.Raise(b => b.BattleEnded += null, completedBattle!, BattleOutcome.AttackersWon);
            currentBattle = null;
            svc.Update(0.1f);

            _sink.Verify(s => s.WritePlayerEvent(It.Is<PlayerEventRow>(r =>
                r.Type == PlayerEventType.BattleExited
                && r.ZoneId == "pillbox_hill")), Times.Once);
            _sink.Verify(s => s.WritePlayerEvent(It.Is<PlayerEventRow>(r =>
                r.Type == PlayerEventType.BattleAbandoned)), Times.Never);
        }

        [Fact]
        public void Update_PlayerDiesInBattle_WritesBattleDeath()
        {
            var dead = false;
            var battle = CreatePlayerAttackBattle();
            var battleManager = new Mock<IZoneBattleManager>();
            battleManager.Setup(b => b.GetPlayerCurrentBattle()).Returns(battle);

            using var svc = new TelemetryService(_sink.Object, _factionService.Object,
                _zoneService.Object, _gameStateManager.Object,
                new TelemetryServiceOptions
                {
                    ZoneBattleManager = battleManager.Object,
                    IsPlayerDead = () => dead,
                    GetCurrentZoneId = () => "pillbox_hill",
                    GetPlayerPosition = () => new Vector3(1f, 2f, 3f)
                });

            dead = true;
            svc.Update(0.1f);

            _sink.Verify(s => s.WritePlayerEvent(It.Is<PlayerEventRow>(r =>
                r.Type == PlayerEventType.Death
                && r.ZoneId == "pillbox_hill")), Times.Once);
            _sink.Verify(s => s.WritePlayerEvent(It.Is<PlayerEventRow>(r =>
                r.Type == PlayerEventType.BattleDeath
                && r.ZoneId == "pillbox_hill"
                && r.TargetFaction == "franklin"
                && r.Details.Contains("\"player_role\":\"Attacker\""))), Times.Once);
        }

        // ---- Domain event subscription tests (Task 11) ----

        [Fact]
        public void ZoneOwnershipChanged_ToOwner_FromExistingOwner_WritesCapturedAndLost()
        {
            using var svc = new TelemetryService(_sink.Object, _factionService.Object,
                _zoneService.Object, _gameStateManager.Object);

            _zoneService.Raise(z => z.ZoneOwnershipChanged += null,
                this, new ZoneOwnershipChangedEventArgs("zone1", "trevor", "michael"));

            _sink.Verify(s => s.WriteZoneEvent(It.Is<ZoneEventRow>(r =>
                r.Type == ZoneEventType.Captured && r.ZoneId == "zone1"
                && r.PreviousOwner == "trevor" && r.NewOwner == "michael")), Times.Once);
            _sink.Verify(s => s.WriteZoneEvent(It.Is<ZoneEventRow>(r =>
                r.Type == ZoneEventType.Lost && r.ZoneId == "zone1"
                && r.PreviousOwner == "trevor" && r.NewOwner == "michael")), Times.Once);
        }

        [Fact]
        public void ZoneOwnershipChanged_ToOwner_FromNeutral_WritesOnlyCaptured()
        {
            using var svc = new TelemetryService(_sink.Object, _factionService.Object,
                _zoneService.Object, _gameStateManager.Object);

            _zoneService.Raise(z => z.ZoneOwnershipChanged += null,
                this, new ZoneOwnershipChangedEventArgs("zone1", null, "michael"));

            _sink.Verify(s => s.WriteZoneEvent(It.Is<ZoneEventRow>(r =>
                r.Type == ZoneEventType.Captured && r.PreviousOwner == null
                && r.NewOwner == "michael")), Times.Once);
            _sink.Verify(s => s.WriteZoneEvent(It.Is<ZoneEventRow>(r =>
                r.Type == ZoneEventType.Lost)), Times.Never);
        }

        [Fact]
        public void ZoneOwnershipChanged_ToNeutral_WritesNeutralized()
        {
            using var svc = new TelemetryService(_sink.Object, _factionService.Object,
                _zoneService.Object, _gameStateManager.Object);

            _zoneService.Raise(z => z.ZoneOwnershipChanged += null,
                this, new ZoneOwnershipChangedEventArgs("zone1", "trevor", null));

            _sink.Verify(s => s.WriteZoneEvent(It.Is<ZoneEventRow>(r =>
                r.Type == ZoneEventType.Neutralized && r.ZoneId == "zone1"
                && r.PreviousOwner == "trevor" && r.NewOwner == null)), Times.Once);
            _sink.Verify(s => s.WriteZoneEvent(It.Is<ZoneEventRow>(r =>
                r.Type == ZoneEventType.Captured)), Times.Never);
            _sink.Verify(s => s.WriteZoneEvent(It.Is<ZoneEventRow>(r =>
                r.Type == ZoneEventType.Lost)), Times.Never);
        }

        [Fact]
        public void BattleStarted_WritesStartedRow()
        {
            var battleManager = new Mock<IZoneBattleManager>();
            using var svc = new TelemetryService(_sink.Object, _factionService.Object,
                _zoneService.Object, _gameStateManager.Object,
                new TelemetryServiceOptions
                {
                    GetPlayerPedHandle = () => 0,
                    ZoneBattleManager = battleManager.Object,
                });

            var attackerTroops = new Dictionary<DefenderRole, int> { { DefenderRole.Grunt, 5 } };
            var defenderTroops = new Dictionary<DefenderRole, int> { { DefenderRole.Grunt, 3 } };
            var battle = new ZoneBattle("trevor", "michael", "zone1", attackerTroops, defenderTroops);

            battleManager.Raise(m => m.BattleStarted += null, battle);

            _sink.Verify(s => s.WriteBattle(It.Is<BattleEventRow>(r =>
                r.Type == BattleEventType.Started
                && r.ZoneId == "zone1"
                && r.AttackerFactionId == "trevor"
                && r.DefenderFactionId == "michael"
                && r.AttackerTroops == 5
                && r.DefenderTroops == 3
                && r.Outcome == null
                && r.AttackerCasualties == 0
                && r.DefenderCasualties == 0)), Times.Once);
        }

        [Fact]
        public void BattleEnded_WritesEndedRowWithOutcomeAndCasualties()
        {
            var battleManager = new Mock<IZoneBattleManager>();
            using var svc = new TelemetryService(_sink.Object, _factionService.Object,
                _zoneService.Object, _gameStateManager.Object,
                new TelemetryServiceOptions
                {
                    GetPlayerPedHandle = () => 0,
                    ZoneBattleManager = battleManager.Object,
                });

            var attackerTroops = new Dictionary<DefenderRole, int> { { DefenderRole.Grunt, 5 } };
            var defenderTroops = new Dictionary<DefenderRole, int> { { DefenderRole.Grunt, 3 } };
            var battle = new ZoneBattle("trevor", "michael", "zone1", attackerTroops, defenderTroops);
            // Simulate casualties: attacker lost 2 troops, defender wiped.
            battle.RemoveAttackerTroop(DefenderRole.Grunt);
            battle.RemoveAttackerTroop(DefenderRole.Grunt);
            battle.RemoveDefenderTroop(DefenderRole.Grunt);
            battle.RemoveDefenderTroop(DefenderRole.Grunt);
            battle.RemoveDefenderTroop(DefenderRole.Grunt);

            battleManager.Raise(m => m.BattleEnded += null, battle, BattleOutcome.AttackersWon);

            _sink.Verify(s => s.WriteBattle(It.Is<BattleEventRow>(r =>
                r.Type == BattleEventType.Ended
                && r.ZoneId == "zone1"
                && r.Outcome == BattleOutcome.AttackersWon
                && r.AttackerCasualties == 2  // initial 5 - remaining 3
                && r.DefenderCasualties == 3  // initial 3 - remaining 0
                )), Times.Once);
        }

        [Fact]
        public void BattleEnded_WhenAttackerParticipantWasRemoved_StillWritesRow()
        {
            var battleManager = new Mock<IZoneBattleManager>();
            using var svc = new TelemetryService(_sink.Object, _factionService.Object,
                _zoneService.Object, _gameStateManager.Object,
                new TelemetryServiceOptions
                {
                    GetPlayerPedHandle = () => 0,
                    ZoneBattleManager = battleManager.Object,
                });

            var attackerTroops = new Dictionary<DefenderRole, int> { { DefenderRole.Grunt, 5 } };
            var defenderTroops = new Dictionary<DefenderRole, int> { { DefenderRole.Grunt, 3 } };
            var battle = new ZoneBattle("trevor", "michael", "zone1", attackerTroops, defenderTroops);
            battle.RemoveParticipant("trevor");

            battleManager.Raise(m => m.BattleEnded += null, battle, BattleOutcome.DefendersWon);

            _sink.Verify(s => s.WriteBattle(It.Is<BattleEventRow>(r =>
                r.Type == BattleEventType.Ended
                && r.ZoneId == "zone1"
                && r.AttackerFactionId == string.Empty
                && r.DefenderFactionId == "michael"
                && r.AttackerTroops == 0
                && r.DefenderTroops == 3)), Times.Once);
        }

        [Fact]
        public void TroopsAllocated_WritesAllocationRowWithSourceAI()
        {
            var allocationService = new Mock<IZoneDefenderAllocationService>();
            using var svc = new TelemetryService(_sink.Object, _factionService.Object,
                _zoneService.Object, _gameStateManager.Object,
                new TelemetryServiceOptions
                {
                    GetPlayerPedHandle = () => 0,
                    AllocationService = allocationService.Object,
                });

            allocationService.Raise(a => a.TroopsAllocated += null,
                this, new TroopsAllocatedEventArgs("trevor", "zone1", DefenderRole.Rifleman, 2));

            _sink.Verify(s => s.WriteAllocation(It.Is<AllocationEventRow>(r =>
                r.FactionId == "trevor"
                && r.ZoneId == "zone1"
                && r.Tier == DefenderRole.Rifleman
                && r.Count == 2
                && r.Source == AllocationSource.AI)), Times.Once);
        }

        [Fact]
        public void OnTroopsRecruited_WritesRecruitmentRow()
        {
            var aiController = new Mock<IAIController>();
            using var svc = new TelemetryService(_sink.Object, _factionService.Object,
                _zoneService.Object, _gameStateManager.Object,
                new TelemetryServiceOptions
                {
                    GetPlayerPedHandle = () => 0,
                    AIController = aiController.Object,
                });

            aiController.Raise(c => c.OnTroopsRecruited += null,
                this, new FactionWars.AI.Events.TroopsRecruitedEventArgs(
                    "trevor", troopsRecruited: 3, cost: 600, cashBefore: 1000, cashAfter: 400));

            _sink.Verify(s => s.WriteRecruitment(It.Is<RecruitmentEventRow>(r =>
                r.FactionId == "trevor"
                && r.TroopsRecruited == 3
                && r.Cost == 600
                && r.CashBefore == 1000
                && r.CashAfter == 400)), Times.Once);
        }

        [Fact]
        public void OnResourceTick_WritesResourceTickRow()
        {
            var resourceTickService = new Mock<IResourceTickService>();
            using var svc = new TelemetryService(_sink.Object, _factionService.Object,
                _zoneService.Object, _gameStateManager.Object,
                new TelemetryServiceOptions
                {
                    GetPlayerPedHandle = () => 0,
                    ResourceTickService = resourceTickService.Object,
                });

            resourceTickService.Raise(r => r.OnResourceTick += null,
                this, new ResourceTickEventArgs("trevor", cash: 250, recruitment: 5, weapons: 2));

            _sink.Verify(s => s.WriteResourceTick(It.Is<ResourceTickEventRow>(r =>
                r.FactionId == "trevor"
                && r.Income == 250
                && r.ZonesContributing == 0)), Times.Once);
        }

        [Fact]
        public void AttackerKilled_WhenKillerIsPlayer_WritesPlayerEventKill()
        {
            var attackerManager = TestHelpers.CreateBattleAttackerManager(out var raiseAttackerKilled);
            using var svc = new TelemetryService(_sink.Object, _factionService.Object,
                _zoneService.Object, _gameStateManager.Object,
                new TelemetryServiceOptions
                {
                    GetPlayerPedHandle = () => 100,
                    BattleAttackerManager = attackerManager,
                });

            raiseAttackerKilled(new AttackerKilledEventArgs(
                "zone1", "trevor", DefenderRole.Grunt, pedHandle: 50, killerPedHandle: 100));

            _sink.Verify(s => s.WritePlayerEvent(It.Is<PlayerEventRow>(r =>
                r.Type == PlayerEventType.Kill
                && r.ZoneId == "zone1"
                && r.TargetFaction == "trevor"
                && r.TargetTier == DefenderRole.Grunt)), Times.Once);
        }

        [Fact]
        public void AttackerKilled_WhenKillerIsNotPlayer_DoesNotWrite()
        {
            var attackerManager = TestHelpers.CreateBattleAttackerManager(out var raiseAttackerKilled);
            using var svc = new TelemetryService(_sink.Object, _factionService.Object,
                _zoneService.Object, _gameStateManager.Object,
                new TelemetryServiceOptions
                {
                    GetPlayerPedHandle = () => 100,
                    BattleAttackerManager = attackerManager,
                });

            raiseAttackerKilled(new AttackerKilledEventArgs(
                "zone1", "trevor", DefenderRole.Grunt, pedHandle: 50, killerPedHandle: 999));

            _sink.Verify(s => s.WritePlayerEvent(It.IsAny<PlayerEventRow>()), Times.Never);
        }

        [Fact]
        public void Ctor_BattleAttackerManagerProvidedWithoutPlayerPedHandle_Throws()
        {
            var attackerManager = TestHelpers.CreateBattleAttackerManager(out _);

            Assert.Throws<ArgumentException>(() => new TelemetryService(
                _sink.Object, _factionService.Object, _zoneService.Object, _gameStateManager.Object,
                new TelemetryServiceOptions { BattleAttackerManager = attackerManager }));
        }

        [Fact]
        public void OnGameLoaded_WhenSuccess_ForwardsSetSaveFile()
        {
            using var svc = new TelemetryService(_sink.Object, _factionService.Object,
                _zoneService.Object, _gameStateManager.Object);

            _gameStateManager.Raise(g => g.OnGameLoaded += null,
                this, new GameStateLoadedEventArgs(slotNumber: 4, saveName: "SGTA0004", success: true));

            _sink.Verify(s => s.SetSaveFile("SGTA0004"), Times.Once);
        }

        [Fact]
        public void OnGameLoaded_WhenFailure_DoesNotForward()
        {
            using var svc = new TelemetryService(_sink.Object, _factionService.Object,
                _zoneService.Object, _gameStateManager.Object);

            _gameStateManager.Raise(g => g.OnGameLoaded += null,
                this, new GameStateLoadedEventArgs(slotNumber: 4, saveName: "SGTA0004", success: false));

            _sink.Verify(s => s.SetSaveFile(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public void OnNativeSaveWritten_ExtractsFilenameAndCallsSetSaveFile()
        {
            // NativeSaveWatcher requires a real directory; create a temp one we control so
            // the watcher initialises cleanly even though we never start it.
            var tempDir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            System.IO.Directory.CreateDirectory(tempDir);
            try
            {
                using var watcher = new NativeSaveWatcher(tempDir);
                using var svc = new TelemetryService(_sink.Object, _factionService.Object,
                    _zoneService.Object, _gameStateManager.Object,
                    new TelemetryServiceOptions
                    {
                        GetPlayerPedHandle = () => 0,
                        NativeSaveWatcher = watcher,
                    });

                // Raise the event via reflection — the event has no public raiser.
                var field = typeof(NativeSaveWatcher).GetField("OnNativeSaveWritten",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                Assert.NotNull(field);
                var handler = (EventHandler<SaveEvent>?)field!.GetValue(watcher);
                Assert.NotNull(handler);
                handler!.Invoke(watcher, new SaveEvent(
                    @"C:/Users/ryan7/Documents/Rockstar Games/GTA V/Profiles/AAA/SGTA0004",
                    DateTime.UtcNow));

                _sink.Verify(s => s.SetSaveFile("SGTA0004"), Times.Once);
            }
            finally
            {
                try { System.IO.Directory.Delete(tempDir, recursive: true); } catch { }
            }
        }

        [Fact]
        public void OnVictory_WritesMatchMetaVictoryRowAsJson()
        {
            var victoryManager = TestHelpers.CreateVictoryManager();
            MatchMetaEventRow? captured = null;
            _sink.Setup(s => s.WriteMatchMeta(It.IsAny<MatchMetaEventRow>()))
                .Callback<MatchMetaEventRow>(r => captured = r);

            using var svc = new TelemetryService(_sink.Object, _factionService.Object,
                _zoneService.Object, _gameStateManager.Object,
                new TelemetryServiceOptions
                {
                    GetPlayerPedHandle = () => 0,
                    VictoryManager = victoryManager,
                });

            // Raise via reflection — no public raise method.
            var field = typeof(VictoryManager).GetField("OnVictory",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            Assert.NotNull(field);
            var handler = (EventHandler<VictoryEventArgs>?)field!.GetValue(victoryManager);
            Assert.NotNull(handler);
            handler!.Invoke(victoryManager, new VictoryEventArgs("michael", "Michael's Crew"));

            Assert.NotNull(captured);
            Assert.Equal(MatchMetaEventType.Victory, captured!.Type);
            // Details must be valid JSON containing both fields.
            var json = JObject.Parse(captured.Details);
            Assert.Equal("michael", (string?)json["factionId"]);
            Assert.Equal("Michael's Crew", (string?)json["name"]);
        }

        [Fact]
        public void DifficultyChanged_WritesMatchMetaDifficultyChangedRow()
        {
            var difficultyService = new Mock<IDifficultyService>();
            using var svc = new TelemetryService(_sink.Object, _factionService.Object,
                _zoneService.Object, _gameStateManager.Object,
                new TelemetryServiceOptions
                {
                    GetPlayerPedHandle = () => 0,
                    DifficultyService = difficultyService.Object,
                });

            difficultyService.Raise(d => d.DifficultyChanged += null,
                this, DifficultySettings.Hard);

            _sink.Verify(s => s.WriteMatchMeta(It.Is<MatchMetaEventRow>(r =>
                r.Type == MatchMetaEventType.DifficultyChanged
                && r.Details == "Hard")), Times.Once);
        }

        [Fact]
        public void Dispose_UnsubscribesFromZoneOwnershipChanged()
        {
            var svc = new TelemetryService(_sink.Object, _factionService.Object,
                _zoneService.Object, _gameStateManager.Object);

            svc.Dispose();

            _zoneService.Raise(z => z.ZoneOwnershipChanged += null,
                this, new ZoneOwnershipChangedEventArgs("zone1", "trevor", "michael"));

            _sink.Verify(s => s.WriteZoneEvent(It.IsAny<ZoneEventRow>()), Times.Never);
        }

        // ---- Lifecycle match-meta emission tests (Task 12) ----

        [Fact]
        public void Constructor_EmitsModSessionStart()
        {
            using var svc = new TelemetryService(_sink.Object, _factionService.Object,
                _zoneService.Object, _gameStateManager.Object);

            _sink.Verify(s => s.WriteMatchMeta(It.Is<MatchMetaEventRow>(r =>
                r.Type == MatchMetaEventType.ModSessionStart)), Times.Once);
        }

        [Fact]
        public void Dispose_EmitsModSessionEnd()
        {
            var svc = new TelemetryService(_sink.Object, _factionService.Object,
                _zoneService.Object, _gameStateManager.Object);

            svc.Dispose();

            _sink.Verify(s => s.WriteMatchMeta(It.Is<MatchMetaEventRow>(r =>
                r.Type == MatchMetaEventType.ModSessionEnd)), Times.Once);
        }

        [Fact]
        public void Dispose_Idempotent_ModSessionEndOnlyOnce()
        {
            var svc = new TelemetryService(_sink.Object, _factionService.Object,
                _zoneService.Object, _gameStateManager.Object);

            svc.Dispose();
            svc.Dispose();

            _sink.Verify(s => s.WriteMatchMeta(It.Is<MatchMetaEventRow>(r =>
                r.Type == MatchMetaEventType.ModSessionEnd)), Times.Once);
        }

        [Fact]
        public void OnGameLoaded_WhenFirstTimeSeen_EmitsMatchStart()
        {
            using var svc = new TelemetryService(_sink.Object, _factionService.Object,
                _zoneService.Object, _gameStateManager.Object,
                new TelemetryServiceOptions
                {
                    IsFirstTimeSeenSave = _ => true,
                });

            _gameStateManager.Raise(g => g.OnGameLoaded += null,
                this, new GameStateLoadedEventArgs(slotNumber: 9, saveName: "SGTA0009", success: true));

            _sink.Verify(s => s.WriteMatchMeta(It.Is<MatchMetaEventRow>(r =>
                r.Type == MatchMetaEventType.MatchStart
                && r.Details.Contains("SGTA0009"))), Times.Once);
        }

        [Fact]
        public void OnGameLoaded_WhenSaveAlreadySeen_DoesNotEmitMatchStart()
        {
            using var svc = new TelemetryService(_sink.Object, _factionService.Object,
                _zoneService.Object, _gameStateManager.Object,
                new TelemetryServiceOptions
                {
                    IsFirstTimeSeenSave = _ => false,
                });

            _gameStateManager.Raise(g => g.OnGameLoaded += null,
                this, new GameStateLoadedEventArgs(slotNumber: 9, saveName: "SGTA0009", success: true));

            _sink.Verify(s => s.SetSaveFile("SGTA0009"), Times.Once);
            _sink.Verify(s => s.WriteMatchMeta(It.Is<MatchMetaEventRow>(r =>
                r.Type == MatchMetaEventType.MatchStart)), Times.Never);
        }

        [Fact]
        public void OnGameLoaded_WhenLoadFailed_DoesNotEmitMatchStart()
        {
            using var svc = new TelemetryService(_sink.Object, _factionService.Object,
                _zoneService.Object, _gameStateManager.Object,
                new TelemetryServiceOptions
                {
                    IsFirstTimeSeenSave = _ => true,
                });

            _gameStateManager.Raise(g => g.OnGameLoaded += null,
                this, new GameStateLoadedEventArgs(slotNumber: 9, saveName: "SGTA0009", success: false));

            _sink.Verify(s => s.WriteMatchMeta(It.Is<MatchMetaEventRow>(r =>
                r.Type == MatchMetaEventType.MatchStart)), Times.Never);
        }

        [Fact]
        public void Constructor_WhenSinkThrowsOnWriteMatchMeta_DoesNotPropagate()
        {
            _sink.Setup(s => s.WriteMatchMeta(It.IsAny<MatchMetaEventRow>()))
                .Throws(new InvalidOperationException("boom"));

            // Construction must not propagate the exception thrown from the
            // ModSessionStart write.
            var svc = new TelemetryService(_sink.Object, _factionService.Object,
                _zoneService.Object, _gameStateManager.Object);
            svc.Dispose();
        }

        private static ZoneBattle CreatePlayerAttackBattle()
        {
            var participants = new List<BattleParticipant>
            {
                BattleParticipant.ForAi("franklin", BattleRole.Defender,
                    new Dictionary<DefenderRole, int> { { DefenderRole.Grunt, 5 } }),
                BattleParticipant.ForPlayer("michael", BattleRole.Attacker, () => 1)
            };

            return new ZoneBattle("pillbox_hill", participants, playerFactionId: "michael");
        }
    }

    /// <summary>
    /// Helpers for constructing concrete-class dependencies whose events can't be raised
    /// via Moq directly (because they're defined on concrete classes).
    /// </summary>
    internal static class TestHelpers
    {
        public static BattleAttackerManager CreateBattleAttackerManager(out Action<AttackerKilledEventArgs> raiser)
        {
            // Construct with all-null dependencies via Mocks of their interfaces. We never
            // call any method that exercises them — we only need the AttackerKilled event
            // to be raisable.
            var bridge = new Mock<FactionWars.Core.Interfaces.IGameBridge>();
            var zoneBattleManager = new Mock<IZoneBattleManager>();
            var pedSpawning = new Mock<FactionWars.Combat.Interfaces.IPedSpawningService>();
            var pedDespawn = new Mock<FactionWars.Combat.Interfaces.IPedDespawnService>();
            var defenderTier = new Mock<FactionWars.Core.Interfaces.IDefenderRoleService>();
            var pedBlip = new Mock<FactionWars.UI.Interfaces.IPedBlipService>();
            var zoneSvc = new Mock<IZoneService>();
            var factionSvc = new Mock<IFactionService>();

            var manager = new BattleAttackerManager(
                bridge.Object, zoneBattleManager.Object, pedSpawning.Object, pedDespawn.Object,
                defenderTier.Object, pedBlip.Object, zoneSvc.Object, factionSvc.Object,
                "michael");

            // Capture the AttackerKilled event via reflection so the test can raise it.
            raiser = (args) =>
            {
                var field = typeof(BattleAttackerManager).GetField("AttackerKilled",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic) ?? throw new InvalidOperationException("AttackerKilled field not found");
                var handler = (EventHandler<AttackerKilledEventArgs>?)field.GetValue(manager);
                handler?.Invoke(manager, args);
            };

            return manager;
        }

        public static VictoryManager CreateVictoryManager()
        {
            var victoryCondition = new Mock<FactionWars.Core.Interfaces.IVictoryConditionService>();
            var factionSvc = new Mock<IFactionService>();
            var notification = new Mock<FactionWars.UI.Interfaces.INotificationService>();
            return new VictoryManager(victoryCondition.Object, factionSvc.Object, notification.Object);
        }
    }
}
