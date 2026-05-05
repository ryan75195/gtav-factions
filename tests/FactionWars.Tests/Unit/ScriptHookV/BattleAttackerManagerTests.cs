using FactionWars.Combat.Events;
using FactionWars.Combat.Interfaces;
using FactionWars.Combat.Models;
using FactionWars.Core.Interfaces;
using FactionWars.Core.Models;
using FactionWars.Factions.Interfaces;
using FactionWars.Factions.Models;
using FactionWars.ScriptHookV.Managers;
using FactionWars.Territory.Interfaces;
using FactionWars.Territory.Models;
using FactionWars.UI.Interfaces;
using Moq;
using System.Collections.Generic;
using Xunit;

namespace FactionWars.Tests.Unit.ScriptHookV
{
    public class BattleAttackerManagerTests
    {
        private readonly Mock<IGameBridge> _gameBridgeMock;
        private readonly Mock<IZoneBattleManager> _battleManagerMock;
        private readonly Mock<IPedSpawningService> _pedSpawningMock;
        private readonly Mock<IPedDespawnService> _pedDespawnMock;
        private readonly Mock<IDefenderTierService> _tierServiceMock;
        private readonly Mock<IPedBlipService> _blipServiceMock;
        private readonly Mock<IZoneService> _zoneServiceMock;
        private readonly Mock<IFactionService> _factionServiceMock;

        public BattleAttackerManagerTests()
        {
            _gameBridgeMock = new Mock<IGameBridge>();
            _battleManagerMock = new Mock<IZoneBattleManager>();
            _pedSpawningMock = new Mock<IPedSpawningService>();
            _pedDespawnMock = new Mock<IPedDespawnService>();
            _tierServiceMock = new Mock<IDefenderTierService>();
            _blipServiceMock = new Mock<IPedBlipService>();
            _zoneServiceMock = new Mock<IZoneService>();
            _factionServiceMock = new Mock<IFactionService>();

            _tierServiceMock.Setup(t => t.GetTierConfig(It.IsAny<DefenderTier>()))
                .Returns(new DefenderTierConfig(DefenderTier.Basic, 100, 100, 0, "weapon_pistol", 50, 1.0f));

            // Default to "ped exists" so Update treats !IsPedAlive as a real death rather
            // than streamed-out culling. Tests that want to simulate streaming override this.
            _gameBridgeMock.Setup(g => g.DoesPedExist(It.IsAny<int>())).Returns(true);
        }

        [Fact]
        public void OnPlayerZoneEntered_WithActiveBattle_AsDefender_ShouldSpawnAttackers()
        {
            // Arrange
            var zone = new Zone("downtown", "Downtown", new Vector3(0, 0, 0), 100f) { OwnerFactionId = "player" };
            var attackerTroops = new Dictionary<DefenderTier, int> { { DefenderTier.Basic, 5 } };
            var defenderTroops = new Dictionary<DefenderTier, int> { { DefenderTier.Basic, 3 } };
            var battle = new ZoneBattle("enemy", "player", "downtown", attackerTroops, defenderTroops, "player");

            _battleManagerMock.Setup(b => b.GetBattleForZone("downtown")).Returns(battle);
            _pedSpawningMock.Setup(p => p.CanSpawn()).Returns(true);
            _pedSpawningMock.Setup(p => p.SpawnPed(It.IsAny<string>(), It.IsAny<Vector3>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(new PedHandle(100));
            _gameBridgeMock.Setup(g => g.GetGroundZ(It.IsAny<float>(), It.IsAny<float>(), It.IsAny<float>())).Returns(0f);

            var manager = CreateManager("player");

            // Act
            manager.OnPlayerZoneEntered(zone);

            // Assert - should spawn up to MaxSpawnedAttackers (or total attackers, whichever is less)
            _pedSpawningMock.Verify(p => p.SpawnPed(It.IsAny<string>(), It.IsAny<Vector3>(), "enemy", "downtown"),
                Times.Exactly(5));
        }

        [Fact]
        public void OnPlayerZoneEntered_NoBattle_ShouldNotSpawnAttackers()
        {
            // Arrange
            var zone = new Zone("downtown", "Downtown", new Vector3(0, 0, 0), 100f) { OwnerFactionId = "player" };
            _battleManagerMock.Setup(b => b.GetBattleForZone("downtown")).Returns((ZoneBattle?)null);

            var manager = CreateManager("player");

            // Act
            manager.OnPlayerZoneEntered(zone);

            // Assert
            _pedSpawningMock.Verify(p => p.SpawnPed(It.IsAny<string>(), It.IsAny<Vector3>(), It.IsAny<string>(), It.IsAny<string>()),
                Times.Never);
        }

        [Fact]
        public void OnPlayerZoneEntered_PlayerIsAttacker_ShouldNotSpawnAttackers()
        {
            // Arrange
            var zone = new Zone("downtown", "Downtown", new Vector3(0, 0, 0), 100f) { OwnerFactionId = "enemy" };
            var attackerTroops = new Dictionary<DefenderTier, int> { { DefenderTier.Basic, 5 } };
            var defenderTroops = new Dictionary<DefenderTier, int> { { DefenderTier.Basic, 3 } };
            var battle = new ZoneBattle("player", "enemy", "downtown", attackerTroops, defenderTroops, "player");

            _battleManagerMock.Setup(b => b.GetBattleForZone("downtown")).Returns(battle);

            var manager = CreateManager("player");

            // Act
            manager.OnPlayerZoneEntered(zone);

            // Assert - should NOT spawn because player is attacker, not defender
            _pedSpawningMock.Verify(p => p.SpawnPed(It.IsAny<string>(), It.IsAny<Vector3>(), It.IsAny<string>(), It.IsAny<string>()),
                Times.Never);
        }

        [Fact]
        public void OnPlayerZoneEntered_PlayerJoinedExistingAiBattle_ShouldSpawnOtherAiAttacker()
        {
            var zone = new Zone("downtown", "Downtown", new Vector3(0, 0, 0), 100f) { OwnerFactionId = "michael" };
            var defender = BattleParticipant.ForAi("michael", BattleRole.Defender, new Dictionary<DefenderTier, int> { { DefenderTier.Basic, 3 } });
            var aiAttacker = BattleParticipant.ForAi("franklin", BattleRole.Attacker, new Dictionary<DefenderTier, int> { { DefenderTier.Basic, 5 } });
            var playerAttacker = BattleParticipant.ForPlayer("trevor", BattleRole.Attacker, () => 1);
            var battle = new ZoneBattle("downtown", new List<BattleParticipant> { defender, aiAttacker, playerAttacker }, "trevor");

            _battleManagerMock.Setup(b => b.GetBattleForZone("downtown")).Returns(battle);
            _pedSpawningMock.Setup(p => p.CanSpawn()).Returns(true);
            _pedSpawningMock.Setup(p => p.GetRelationshipGroup(It.IsAny<string>()))
                .Returns<string>(factionId => factionId.ToUpperInvariant());
            _pedSpawningMock.Setup(p => p.SpawnPed(It.IsAny<string>(), It.IsAny<Vector3>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(new PedHandle(100));

            var manager = CreateManager("trevor");

            manager.OnPlayerZoneEntered(zone);

            _pedSpawningMock.Verify(p => p.SpawnPed(It.IsAny<string>(), It.IsAny<Vector3>(), "franklin", "downtown"),
                Times.Exactly(5));
            _pedSpawningMock.Verify(p => p.SpawnPed(It.IsAny<string>(), It.IsAny<Vector3>(), "trevor", "downtown"),
                Times.Never);
            _gameBridgeMock.Verify(g => g.SetRelationshipBetweenGroups("MICHAEL", "FRANKLIN", 5, true), Times.Once);
            _gameBridgeMock.Verify(g => g.SetRelationshipBetweenGroups("MICHAEL", "TREVOR", 5, true), Times.Once);
            _gameBridgeMock.Verify(g => g.SetRelationshipBetweenGroups("FRANKLIN", "TREVOR", 5, true), Times.Once);
            _gameBridgeMock.Verify(g => g.TaskCombatHatedTargetsAroundPed(100, zone.Radius), Times.AtLeastOnce);
        }

        [Fact]
        public void Update_WhenAttackerDiesAndBattleHasReserveTroops_ShouldSpawnReplacement()
        {
            // Arrange
            // Battle has 15 Basic attackers total, but only 12 are spawned (MaxSpawnedAttackers)
            // When one dies, a replacement should spawn from the remaining 3 reserves
            var zone = new Zone("downtown", "Downtown", new Vector3(0, 0, 0), 100f) { OwnerFactionId = "player" };
            var attackerTroops = new Dictionary<DefenderTier, int> { { DefenderTier.Basic, 15 } };
            var defenderTroops = new Dictionary<DefenderTier, int> { { DefenderTier.Basic, 10 } };
            var battle = new ZoneBattle("enemy", "player", "downtown", attackerTroops, defenderTroops, "player");
            battle.IsPlayerPresent = true;

            _battleManagerMock.Setup(b => b.GetBattleForZone("downtown")).Returns(battle);
            _pedSpawningMock.Setup(p => p.CanSpawn()).Returns(true);
            _zoneServiceMock.Setup(z => z.GetZone("downtown")).Returns(zone);

            // Set up mock to actually decrement battle troops when ReportTroopKilled is called
            _battleManagerMock.Setup(b => b.ReportTroopKilled("downtown", "enemy", It.IsAny<DefenderTier>()))
                .Callback<string, string, DefenderTier>((zoneId, factionId, tier) => battle.RemoveAttackerTroop(tier));

            // Spawn peds with unique handles (100-111)
            int pedHandle = 100;
            _pedSpawningMock.Setup(p => p.SpawnPed(It.IsAny<string>(), It.IsAny<Vector3>(), "enemy", "downtown"))
                .Returns(() => new PedHandle(pedHandle++));
            _gameBridgeMock.Setup(g => g.GetSafeCoordForPed(It.IsAny<Vector3>())).Returns(new Vector3(0, 0, 0));

            var manager = CreateManager("player");
            manager.OnPlayerZoneEntered(zone);

            // 12 attackers should be spawned (MaxSpawnedAttackers)
            Assert.Equal(12, manager.GetSpawnedAttackerCount("downtown"));

            // Simulate one attacker (ped 100) dying
            _gameBridgeMock.Setup(g => g.IsPedAlive(100)).Returns(false);
            _gameBridgeMock.Setup(g => g.IsPedAlive(It.Is<int>(h => h > 100 && h <= 111))).Returns(true);

            // Reset invocation count to track replacement spawn
            _pedSpawningMock.Invocations.Clear();

            // Act - Update should detect death and spawn replacement
            manager.Update();

            // Assert - replacement should spawn (one new SpawnPed call)
            _pedSpawningMock.Verify(p => p.SpawnPed(It.IsAny<string>(), It.IsAny<Vector3>(), "enemy", "downtown"),
                Times.AtLeastOnce,
                "Replacement attacker should spawn when battle has reserve troops");

            // Spawned count should remain at 12 (11 alive + 1 replacement)
            // Note: Actually after death processing it will be 11 (dead removed) + 1 (replacement) = 12
            Assert.Equal(12, manager.GetSpawnedAttackerCount("downtown"));
        }

        [Fact]
        public void Update_WhenAttackerDiesAndBattleHasExactlyMaxTroops_ShouldNotSpawnReplacement()
        {
            // Arrange
            // When battle has exactly 12 Basic attackers (= MaxSpawnedAttackers) and all are spawned,
            // there's no reserve. When one dies, no replacement should spawn.
            var zone = new Zone("downtown", "Downtown", new Vector3(0, 0, 0), 100f) { OwnerFactionId = "player" };
            var attackerTroops = new Dictionary<DefenderTier, int> { { DefenderTier.Basic, 12 } };
            var defenderTroops = new Dictionary<DefenderTier, int> { { DefenderTier.Basic, 10 } };
            var battle = new ZoneBattle("enemy", "player", "downtown", attackerTroops, defenderTroops, "player");
            battle.IsPlayerPresent = true;

            _battleManagerMock.Setup(b => b.GetBattleForZone("downtown")).Returns(battle);
            _pedSpawningMock.Setup(p => p.CanSpawn()).Returns(true);
            _zoneServiceMock.Setup(z => z.GetZone("downtown")).Returns(zone);

            // Set up mock to actually decrement battle troops when ReportTroopKilled is called
            _battleManagerMock.Setup(b => b.ReportTroopKilled("downtown", "enemy", It.IsAny<DefenderTier>()))
                .Callback<string, string, DefenderTier>((zoneId, factionId, tier) => battle.RemoveAttackerTroop(tier));

            // Spawn peds with unique handles (100-111)
            int pedHandle = 100;
            _pedSpawningMock.Setup(p => p.SpawnPed(It.IsAny<string>(), It.IsAny<Vector3>(), "enemy", "downtown"))
                .Returns(() => new PedHandle(pedHandle++));
            _gameBridgeMock.Setup(g => g.GetSafeCoordForPed(It.IsAny<Vector3>())).Returns(new Vector3(0, 0, 0));

            var manager = CreateManager("player");
            manager.OnPlayerZoneEntered(zone);

            // All 12 attackers should be spawned
            Assert.Equal(12, manager.GetSpawnedAttackerCount("downtown"));

            // Simulate one attacker (ped 100) dying
            _gameBridgeMock.Setup(g => g.IsPedAlive(100)).Returns(false);
            _gameBridgeMock.Setup(g => g.IsPedAlive(It.Is<int>(h => h > 100 && h <= 111))).Returns(true);

            // Reset invocation count to track replacement spawn
            _pedSpawningMock.Invocations.Clear();

            // Act - Update should detect death
            manager.Update();

            // Assert - NO replacement should spawn when all troops were spawned and one died
            // The attacker is truly dead (no reserve), so count goes from 12 to 11
            Assert.Equal(11, manager.GetSpawnedAttackerCount("downtown"));

            // Verify NO new spawn attempt (because there's no reserve)
            _pedSpawningMock.Verify(p => p.SpawnPed(It.IsAny<string>(), It.IsAny<Vector3>(), "enemy", "downtown"),
                Times.Never,
                "No replacement should spawn when battle has no reserve troops");
        }

        [Fact]
        public void OnPlayerZoneEntered_AfterExitAndReenter_WithBackgroundSimDepletion_ShouldRespectBattleState()
        {
            // Arrange
            // This test verifies that when background simulation kills attackers while player is away,
            // re-entering the zone respects the current battle state:
            // 1. Player enters zone, 5 attackers spawn
            // 2. Player exits zone, attackers despawn (but battle count stays at 5)
            // 3. Background simulation runs and depletes battle.AttackerTroops (kills them in simulation)
            // 4. Player re-enters zone
            // EXPECTED: No attackers spawn because they were killed by background sim

            var zone = new Zone("downtown", "Downtown", new Vector3(0, 0, 0), 100f) { OwnerFactionId = "player" };
            var attackerTroops = new Dictionary<DefenderTier, int> { { DefenderTier.Basic, 5 } };
            var defenderTroops = new Dictionary<DefenderTier, int> { { DefenderTier.Basic, 3 } };
            var battle = new ZoneBattle("enemy", "player", "downtown", attackerTroops, defenderTroops, "player");

            _battleManagerMock.Setup(b => b.GetBattleForZone("downtown")).Returns(battle);
            _pedSpawningMock.Setup(p => p.CanSpawn()).Returns(true);

            int pedHandle = 100;
            _pedSpawningMock.Setup(p => p.SpawnPed(It.IsAny<string>(), It.IsAny<Vector3>(), "enemy", "downtown"))
                .Returns(() => new PedHandle(pedHandle++));
            _gameBridgeMock.Setup(g => g.GetSafeCoordForPed(It.IsAny<Vector3>())).Returns(new Vector3(0, 0, 0));

            var manager = CreateManager("player");

            // Act 1: Player enters zone - attackers spawn
            manager.OnPlayerZoneEntered(zone);
            Assert.Equal(5, manager.GetSpawnedAttackerCount("downtown"));

            // Act 2: Player exits zone - attackers despawn
            manager.OnPlayerZoneExited(zone);
            Assert.Equal(0, manager.GetSpawnedAttackerCount("downtown"));

            // Simulate background battle killing all attacker troops (defenders won while player away)
            // The background simulation calls battle.RemoveAttackerTroop() for each simulated kill
            battle.RemoveAttackerTroop(DefenderTier.Basic);
            battle.RemoveAttackerTroop(DefenderTier.Basic);
            battle.RemoveAttackerTroop(DefenderTier.Basic);
            battle.RemoveAttackerTroop(DefenderTier.Basic);
            battle.RemoveAttackerTroop(DefenderTier.Basic);

            // Now battle.AttackerTroops[Basic] == 0 - defenders won in background
            Assert.Equal(0, battle.TotalAttackerTroops);

            // Reset spawn tracking for re-entry
            _pedSpawningMock.Invocations.Clear();
            pedHandle = 200; // New handles for re-spawned attackers

            // Act 3: Player re-enters zone
            manager.OnPlayerZoneEntered(zone);

            // Assert: No attackers spawn because they were killed by background sim
            // The ZoneBattle state is the source of truth - if attackers are 0, they're gone
            Assert.Equal(0, manager.GetSpawnedAttackerCount("downtown"));
        }

        [Fact]
        public void OnPlayerZoneEntered_AfterExitAndReenter_WithNoBackgroundSim_ShouldRespawnSameCount()
        {
            // Arrange
            // This test verifies that when player exits and re-enters quickly (no background sim runs),
            // the same number of attackers spawn because ZoneBattle state is unchanged.

            var zone = new Zone("downtown", "Downtown", new Vector3(0, 0, 0), 100f) { OwnerFactionId = "player" };
            var attackerTroops = new Dictionary<DefenderTier, int> { { DefenderTier.Basic, 5 } };
            var defenderTroops = new Dictionary<DefenderTier, int> { { DefenderTier.Basic, 3 } };
            var battle = new ZoneBattle("enemy", "player", "downtown", attackerTroops, defenderTroops, "player");

            _battleManagerMock.Setup(b => b.GetBattleForZone("downtown")).Returns(battle);
            _pedSpawningMock.Setup(p => p.CanSpawn()).Returns(true);

            int pedHandle = 100;
            _pedSpawningMock.Setup(p => p.SpawnPed(It.IsAny<string>(), It.IsAny<Vector3>(), "enemy", "downtown"))
                .Returns(() => new PedHandle(pedHandle++));
            _gameBridgeMock.Setup(g => g.GetSafeCoordForPed(It.IsAny<Vector3>())).Returns(new Vector3(0, 0, 0));

            var manager = CreateManager("player");

            // Act 1: Player enters zone - attackers spawn
            manager.OnPlayerZoneEntered(zone);
            Assert.Equal(5, manager.GetSpawnedAttackerCount("downtown"));

            // Act 2: Player exits zone - attackers despawn (but battle state unchanged)
            manager.OnPlayerZoneExited(zone);
            Assert.Equal(0, manager.GetSpawnedAttackerCount("downtown"));

            // No background sim runs - battle.AttackerTroops still has 5
            Assert.Equal(5, battle.TotalAttackerTroops);

            // Reset spawn tracking for re-entry
            _pedSpawningMock.Invocations.Clear();
            pedHandle = 200;

            // Act 3: Player re-enters zone immediately
            manager.OnPlayerZoneEntered(zone);

            // Assert: Same 5 attackers spawn because ZoneBattle state is unchanged
            Assert.Equal(5, manager.GetSpawnedAttackerCount("downtown"));
        }

        [Fact]
        public void Update_WhenAttackerDies_DecrementsAttackingFactionReserve()
        {
            // Real attacker ped death (player witnessing combat) must reduce the
            // attacking faction's reserve so attacks actually deplete forces.
            var zone = new Zone("downtown", "Downtown", new Vector3(0, 0, 0), 100f) { OwnerFactionId = "player" };
            var attackerTroops = new Dictionary<DefenderTier, int> { { DefenderTier.Basic, 1 } };
            var defenderTroops = new Dictionary<DefenderTier, int> { { DefenderTier.Basic, 1 } };
            var battle = new ZoneBattle("enemy", "player", "downtown", attackerTroops, defenderTroops, "player");
            battle.IsPlayerPresent = true;

            var enemyState = new FactionState("enemy");
            enemyState.AddReserveTroops(DefenderTier.Basic, 5);
            _factionServiceMock.Setup(f => f.GetFactionState("enemy")).Returns(enemyState);

            _battleManagerMock.Setup(b => b.GetBattleForZone("downtown")).Returns(battle);
            _pedSpawningMock.Setup(p => p.CanSpawn()).Returns(true);
            _pedSpawningMock.Setup(p => p.SpawnPed(It.IsAny<string>(), It.IsAny<Vector3>(), "enemy", "downtown"))
                .Returns(new PedHandle(100));
            _zoneServiceMock.Setup(z => z.GetZone("downtown")).Returns(zone);

            var manager = CreateManager("player");
            manager.OnPlayerZoneEntered(zone);

            _gameBridgeMock.Setup(g => g.IsPedAlive(100)).Returns(false);

            manager.Update();

            // Reserve was 5, one attacker died → reserve should now be 4.
            Assert.Equal(4, enemyState.GetReserveTroops(DefenderTier.Basic));
        }

        [Fact]
        public void HandleAttackerDeath_RaisesAttackerKilledEvent()
        {
            // Arrange: set up a battle with the player as defender, simulate spawning
            // one Basic attacker ped, then mark that ped dead and run Update.
            var zone = new Zone("downtown", "Downtown", new Vector3(0, 0, 0), 100f) { OwnerFactionId = "player" };
            var attackerTroops = new Dictionary<DefenderTier, int> { { DefenderTier.Basic, 1 } };
            var defenderTroops = new Dictionary<DefenderTier, int> { { DefenderTier.Basic, 1 } };
            var battle = new ZoneBattle("enemy", "player", "downtown", attackerTroops, defenderTroops, "player");
            _battleManagerMock.Setup(b => b.GetBattleForZone("downtown")).Returns(battle);
            _pedSpawningMock.Setup(p => p.CanSpawn()).Returns(true);
            _pedSpawningMock.Setup(p => p.SpawnPed(It.IsAny<string>(), It.IsAny<Vector3>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(new PedHandle(42));
            _gameBridgeMock.Setup(g => g.GetGroundZ(It.IsAny<float>(), It.IsAny<float>(), It.IsAny<float>())).Returns(0f);

            var manager = CreateManager("player");
            manager.OnPlayerZoneEntered(zone);

            // Capture the event
            AttackerKilledEventArgs? captured = null;
            manager.AttackerKilled += (_, args) => captured = args;

            // Mark the spawned ped as dead, with killer ped handle 99
            _gameBridgeMock.Setup(g => g.IsPedAlive(42)).Returns(false);
            _gameBridgeMock.Setup(g => g.DoesPedExist(42)).Returns(true);
            _gameBridgeMock.Setup(g => g.GetPedKiller(42)).Returns(99);

            // Act
            manager.Update();

            // Assert
            Assert.NotNull(captured);
            Assert.Equal("downtown", captured!.ZoneId);
            Assert.Equal("enemy", captured.FactionId);
            Assert.Equal(DefenderTier.Basic, captured.Tier);
            Assert.Equal(42, captured.PedHandle);
            Assert.Equal(99, captured.KillerPedHandle);
        }

        [Fact]
        public void DespawnAllAttackers_ShouldBeSafeWhenNoAttackersAreTracked()
        {
            var manager = CreateManager("player");

            manager.DespawnAllAttackers();

            _pedDespawnMock.Verify(p => p.DespawnPed(It.IsAny<int>()), Times.Never);
        }

        private BattleAttackerManager CreateManager(string playerFactionId)
        {
            return new BattleAttackerManager(
                _gameBridgeMock.Object,
                _battleManagerMock.Object,
                _pedSpawningMock.Object,
                _pedDespawnMock.Object,
                _tierServiceMock.Object,
                _blipServiceMock.Object,
                _zoneServiceMock.Object,
                _factionServiceMock.Object,
                playerFactionId);
        }
    }
}
