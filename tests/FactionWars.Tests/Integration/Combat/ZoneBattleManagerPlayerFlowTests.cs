using System;
using System.Collections.Generic;
using System.Linq;
using FactionWars.Combat.Interfaces;
using FactionWars.Combat.Models;
using FactionWars.Combat.Services;
using FactionWars.Core.Interfaces;
using FactionWars.Core.Models;
using FactionWars.Factions.Interfaces;
using FactionWars.Territory.Interfaces;
using FactionWars.Territory.Models;
using Moq;
using Xunit;

namespace FactionWars.Tests.Integration.Combat
{
    /// <summary>
    /// Integration tests for ZoneBattleManager covering player combat flows.
    /// Tests end-to-end sequences: StartPlayerCombat, JoinAsAttacker, RemoveParticipant,
    /// ReportTroopKilled, and BattleEnded outcome routing.
    ///
    /// Also includes a dedicated 3-way bug-repro test (Task 18) verifying that a single
    /// BattleEnded fires when the player wins a 3-way contested zone.
    /// </summary>
    public class ZoneBattleManagerPlayerFlowTests
    {
        #region Helpers

        /// <summary>
        /// Creates a ZoneBattleManager with a real allocation service backed by a
        /// minimal in-memory Moq setup. If <paramref name="allocation"/> is supplied it
        /// is returned for every GetAllocation call; otherwise null is returned (empty
        /// defender allocation).
        /// </summary>
        private static ZoneBattleManager MakeManager(
            string? playerFactionId = null,
            ZoneDefenderAllocation? allocation = null,
            IZoneService? zoneService = null)
        {
            var allocSvc = new Mock<IZoneDefenderAllocationService>();
            allocSvc
                .Setup(a => a.GetAllocation(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(allocation);
            var factionSvc = new Mock<IFactionService>().Object;
            var zoneSvc = zoneService ?? new Mock<IZoneService>().Object;
            return new ZoneBattleManager(allocSvc.Object, factionSvc, zoneSvc, playerFactionId);
        }

        /// <summary>
        /// Creates a Zone with the given id and owner. Center and radius are arbitrary.
        /// </summary>
        private static Zone MakeZone(string id, string ownerFactionId)
        {
            var zone = new Zone(id, id, new Vector3(0, 0, 0), 150f);
            zone.OwnerFactionId = ownerFactionId;
            return zone;
        }

        /// <summary>
        /// Builds a ZoneDefenderAllocation pre-populated with Basic troops.
        /// </summary>
        private static ZoneDefenderAllocation MakeAllocation(string factionId, string zoneId, int basicCount)
        {
            var alloc = new ZoneDefenderAllocation(factionId, zoneId);
            alloc.AddTroops(DefenderTier.Basic, basicCount);
            return alloc;
        }

        private static Dictionary<DefenderTier, int> Troops(int basic) =>
            new Dictionary<DefenderTier, int> { { DefenderTier.Basic, basic } };

        #endregion

        // -----------------------------------------------------------------------
        // Scenario 1: Player enters enemy zone, no existing battle → new battle
        // -----------------------------------------------------------------------

        [Fact]
        public void PlayerFlow_PlayerEntersEnemyZone_StartsNewBattle()
        {
            // Arrange
            var allocation = MakeAllocation("michael", "zone_1", 5);
            var manager = MakeManager(playerFactionId: "player_faction", allocation: allocation);
            var zone = MakeZone("zone_1", "michael");

            // Act
            var battle = manager.StartPlayerCombat(zone, "player_faction", () => 4);

            // Assert
            Assert.NotNull(battle);
            Assert.Equal("zone_1", battle!.ZoneId);
            Assert.Equal(1, manager.BattleCount);

            // Defender troop count comes from the allocation (5 Basic)
            Assert.Equal(5, battle.TotalDefenderTroops);
            Assert.Equal("michael", battle.DefenderFactionId);

            // Player is an attacker participant
            Assert.Equal(2, battle.Participants.Count);
            Assert.True(battle.Attackers.Any(p => p.IsPlayer && p.FactionId == "player_faction"));
        }

        // -----------------------------------------------------------------------
        // Scenario 2: Existing AI vs AI battle → player joins as 3rd participant
        // -----------------------------------------------------------------------

        [Fact]
        public void PlayerFlow_PlayerEntersZoneAlreadyContestedByAi_JoinsAsThirdAttacker()
        {
            // Arrange — AI battle already running
            var manager = MakeManager(playerFactionId: "player_faction");
            manager.StartBattle("zone_1", "trevor", "michael", Troops(3), Troops(3));
            var zone = MakeZone("zone_1", "michael");

            // Act
            var battle = manager.StartPlayerCombat(zone, "player_faction", () => 4);

            // Assert
            Assert.NotNull(battle);
            Assert.Equal(3, battle!.Participants.Count);
            int attackerCount = battle.Participants.Count(p => p.Role == BattleRole.Attacker);
            Assert.Equal(2, attackerCount);
            Assert.True(battle.Attackers.Any(p => p.IsPlayer && p.FactionId == "player_faction"));
        }

        // -----------------------------------------------------------------------
        // Scenario 3: Player wipes defender → BattleEnded with AttackersWon
        // -----------------------------------------------------------------------

        [Fact]
        public void PlayerFlow_PlayerWipesDefender_BattleEndsAttackersWon()
        {
            // Arrange
            var allocation = MakeAllocation("michael", "zone_1", 3);
            var manager = MakeManager(playerFactionId: "player_faction", allocation: allocation);
            var zone = MakeZone("zone_1", "michael");
            var battle = manager.StartPlayerCombat(zone, "player_faction", () => 4);
            Assert.NotNull(battle);

            var endedEvents = new List<(ZoneBattle, BattleOutcome)>();
            manager.BattleEnded += (b, o) => endedEvents.Add((b, o));

            // Act — kill all 3 Basic defenders
            manager.ReportTroopKilled("zone_1", "michael", DefenderTier.Basic);
            manager.ReportTroopKilled("zone_1", "michael", DefenderTier.Basic);
            manager.ReportTroopKilled("zone_1", "michael", DefenderTier.Basic);

            // Assert
            Assert.Single(endedEvents);
            Assert.Equal(BattleOutcome.AttackersWon, endedEvents[0].Item2);
            Assert.Null(manager.GetBattleForZone("zone_1"));
        }

        // -----------------------------------------------------------------------
        // Scenario 4: Player squad reaches 0 — skipped
        //
        // ReportTroopKilled on a player participant is a no-op (BattleParticipant.
        // RemoveTroop returns false for player). ResolveBattleIfDone is therefore
        // never triggered by this path; the manager has no periodic check that
        // polls the aliveCountCallback. There is no supported API to externally
        // signal "player squad wiped" in the current implementation.
        // This scenario would require a production change (e.g., a
        // NotifyPlayerSquadWiped entry-point) — out of scope for this task.
        // -----------------------------------------------------------------------

        // -----------------------------------------------------------------------
        // Scenario 5: Player exits zone as sole attacker → DefendersWon
        // -----------------------------------------------------------------------

        [Fact]
        public void PlayerFlow_PlayerExitsZone_RemovesParticipant_DefenderWinsIfPlayerWasOnlyAttacker()
        {
            // Arrange
            var allocation = MakeAllocation("michael", "zone_1", 5);
            var manager = MakeManager(playerFactionId: "player_faction", allocation: allocation);
            var zone = MakeZone("zone_1", "michael");
            manager.StartPlayerCombat(zone, "player_faction", () => 4);

            var endedEvents = new List<(ZoneBattle, BattleOutcome)>();
            manager.BattleEnded += (b, o) => endedEvents.Add((b, o));

            // Act
            manager.RemoveParticipant("zone_1", "player_faction");

            // Assert — no attackers left, defenders win
            Assert.Single(endedEvents);
            Assert.Equal(BattleOutcome.DefendersWon, endedEvents[0].Item2);
            Assert.Null(manager.GetBattleForZone("zone_1"));
        }

        // -----------------------------------------------------------------------
        // Scenario 6: Player exits contested zone → battle continues with AI pair
        // -----------------------------------------------------------------------

        [Fact]
        public void PlayerFlow_PlayerExitsContestedZone_BattleContinues2Way()
        {
            // Arrange — AI battle already running
            var manager = MakeManager(playerFactionId: "player_faction");
            manager.StartBattle("zone_1", "trevor", "michael", Troops(3), Troops(3));
            var zone = MakeZone("zone_1", "michael");
            manager.StartPlayerCombat(zone, "player_faction", () => 4);

            var endedFired = false;
            manager.BattleEnded += (_, _) => endedFired = true;

            // Act — player leaves
            manager.RemoveParticipant("zone_1", "player_faction");

            // Assert — battle still ongoing with trevor and michael
            Assert.False(endedFired);
            var remaining = manager.GetBattleForZone("zone_1");
            Assert.NotNull(remaining);
            Assert.Equal(2, remaining!.Participants.Count);
        }

        // -----------------------------------------------------------------------
        // Scenario 7: StartPlayerCombat on player's own zone → ArgumentException
        // -----------------------------------------------------------------------

        [Fact]
        public void PlayerFlow_StartPlayerCombat_OnPlayerOwnedZone_Throws()
        {
            // Arrange
            var manager = MakeManager(playerFactionId: "player_faction");
            var zone = MakeZone("zone_1", "player_faction"); // player owns this zone

            // Act & Assert
            Assert.Throws<ArgumentException>(() =>
                manager.StartPlayerCombat(zone, "player_faction", () => 4));
        }

        // -----------------------------------------------------------------------
        // Scenario 8: Calling StartPlayerCombat twice returns null the second time
        // -----------------------------------------------------------------------

        [Fact]
        public void PlayerFlow_DoubleStartPlayerCombat_ReturnsNullSecondTime()
        {
            // Arrange
            var allocation = MakeAllocation("michael", "zone_1", 3);
            var manager = MakeManager(playerFactionId: "player_faction", allocation: allocation);
            var zone = MakeZone("zone_1", "michael");

            // Act
            var first = manager.StartPlayerCombat(zone, "player_faction", () => 4);
            var second = manager.StartPlayerCombat(zone, "player_faction", () => 4);

            // Assert
            Assert.NotNull(first);
            Assert.Null(second); // JoinAsAttacker rejects duplicate faction id
            Assert.Equal(1, manager.BattleCount);
        }

        // -----------------------------------------------------------------------
        // Task 18: 3-way bug repro
        // -----------------------------------------------------------------------

        [Fact]
        public void ThreeWay_PlayerWinsAfterJoiningContestedAiZone_OneBattleEndedFires()
        {
            // Bug repro: AI₁ attacking AI₂'s zone, player walks in, wipes everyone.
            // Pre-Plan-2: two parallel battles — AI's resolution could overwrite player's.
            // After Plan 2: single unified battle, single BattleEnded event, player wins.

            var manager = MakeManager(playerFactionId: "player_faction");
            var zone = MakeZone("zone_1", ownerFactionId: "michael");

            // AI₁ (trevor) attacks AI₂ (michael).
            manager.StartBattle("zone_1", "trevor", "michael",
                new Dictionary<DefenderTier, int> { { DefenderTier.Basic, 1 } },
                new Dictionary<DefenderTier, int> { { DefenderTier.Basic, 1 } });

            // Player walks in.
            int squadCount = 4;
            var battle = manager.StartPlayerCombat(zone, "player_faction", () => squadCount);
            Assert.NotNull(battle);
            Assert.Equal(3, battle!.Participants.Count);

            // Track BattleEnded firings.
            var endedEvents = new List<(ZoneBattle, BattleOutcome)>();
            manager.BattleEnded += (b, o) => endedEvents.Add((b, o));

            // Player wipes both AI sides.
            manager.ReportTroopKilled("zone_1", "trevor", DefenderTier.Basic);
            manager.ReportTroopKilled("zone_1", "michael", DefenderTier.Basic);

            // Exactly one BattleEnded fires.
            Assert.Single(endedEvents);
            // Outcome is AttackersWon and the surviving attacker is the player.
            Assert.Equal(BattleOutcome.AttackersWon, endedEvents[0].Item2);
            Assert.True(endedEvents[0].Item1.Attackers.Any(p => p.IsPlayer));
            // Battle is removed from active battles.
            Assert.Null(manager.GetBattleForZone("zone_1"));
        }

        // -----------------------------------------------------------------------
        // Player-win → zone goes neutral (Q5.A)
        // -----------------------------------------------------------------------

        [Fact]
        public void PlayerFlow_PlayerWinsBattle_ZoneIsNeutralized()
        {
            // Q5.A: when the player wipes the defender (and any AI third party),
            // the zone goes neutral — TransferZoneOwnership(zoneId, null).
            var zoneSvc = new Mock<IZoneService>();
            var allocation = MakeAllocation("michael", "zone_1", 1);
            var manager = MakeManager(
                playerFactionId: "player_faction",
                allocation: allocation,
                zoneService: zoneSvc.Object);
            var zone = MakeZone("zone_1", ownerFactionId: "michael");
            manager.StartPlayerCombat(zone, "player_faction", () => 4);

            manager.ReportTroopKilled("zone_1", "michael", DefenderTier.Basic);

            zoneSvc.Verify(z => z.TransferZoneOwnership("zone_1", null), Times.Once);
        }

        [Fact]
        public void PlayerFlow_DefenderWinsBattle_ZoneIsNotNeutralized()
        {
            // Defender win: zone keeps its owner. No TransferZoneOwnership call.
            var zoneSvc = new Mock<IZoneService>();
            var manager = MakeManager(
                playerFactionId: "player_faction",
                zoneService: zoneSvc.Object);
            manager.StartBattle("zone_1", "trevor", "michael",
                Troops(1), Troops(1));

            // Trevor leaves before fighting → defender wins.
            manager.RemoveParticipant("zone_1", "trevor");

            zoneSvc.Verify(z => z.TransferZoneOwnership(It.IsAny<string>(), It.IsAny<string?>()), Times.Never);
        }

        [Fact]
        public void PlayerFlow_AliveCountCallbackDropsToZero_BattleEndsAsDefenderWin()
        {
            // Player-death case: GameLoopController's aliveCountCallback drops to 0
            // when the player is dead. Once the player is the only attacker and the
            // callback returns 0, IsOngoing flips false and the next Tick must end
            // the battle as a defender win (no Q5.A neutralization on a player loss).
            var zoneSvc = new Mock<IZoneService>();
            var allocation = MakeAllocation("michael", "zone_1", 5);
            var manager = MakeManager(
                playerFactionId: "player_faction",
                allocation: allocation,
                zoneService: zoneSvc.Object);
            var zone = MakeZone("zone_1", ownerFactionId: "michael");

            int aliveCount = 1;
            var battle = manager.StartPlayerCombat(zone, "player_faction", () => aliveCount);
            Assert.NotNull(battle);

            // Player dies → callback now returns 0.
            aliveCount = 0;
            Assert.False(battle!.IsOngoing);

            BattleOutcome? observed = null;
            manager.BattleEnded += (b, o) => observed = o;

            manager.Tick(0f);

            Assert.Equal(BattleOutcome.DefendersWon, observed);
            Assert.Null(manager.GetBattleForZone("zone_1"));
            // Defender wins → no zone transfer.
            zoneSvc.Verify(z => z.TransferZoneOwnership(It.IsAny<string>(), It.IsAny<string?>()), Times.Never);
        }

        [Fact]
        public void PlayerFlow_PlayerWinsViaTick_ZoneIsStillNeutralized()
        {
            // Defense in depth: even if a battle reaches the !IsOngoing state without
            // going through ReportTroopKilled (e.g., direct troop mutation in 3-way
            // edge cases), the Tick-driven BattleEnded path must still apply the
            // player-win zone neutralization. Otherwise the zone keeps its old
            // owner and Q5.A is silently bypassed.
            var zoneSvc = new Mock<IZoneService>();
            var allocation = MakeAllocation("michael", "zone_1", 1);
            var manager = MakeManager(
                playerFactionId: "player_faction",
                allocation: allocation,
                zoneService: zoneSvc.Object);
            var zone = MakeZone("zone_1", ownerFactionId: "michael");
            var battle = manager.StartPlayerCombat(zone, "player_faction", () => 4);
            Assert.NotNull(battle);

            // Wipe the defender directly on the battle model — bypassing
            // ReportTroopKilled / ResolveBattleIfDone — so the only path that can
            // see "battle is over" is the Tick loop.
            battle!.RemoveDefenderTroop(DefenderTier.Basic);
            Assert.False(battle.IsOngoing);

            manager.Tick(0f);

            zoneSvc.Verify(z => z.TransferZoneOwnership("zone_1", null), Times.Once);
            Assert.Null(manager.GetBattleForZone("zone_1"));
        }
    }
}
