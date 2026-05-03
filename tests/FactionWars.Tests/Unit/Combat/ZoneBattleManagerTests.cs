using System;
using System.Collections.Generic;
using System.Linq;
using FactionWars.Combat.Interfaces;
using FactionWars.Combat.Models;
using FactionWars.Combat.Services;
using FactionWars.Core.Interfaces;
using FactionWars.Core.Models;
using FactionWars.Factions.Interfaces;
using FactionWars.Territory.Models;
using Moq;
using Xunit;

namespace FactionWars.Tests.Unit.Combat
{
    /// <summary>
    /// Tests for ZoneBattleManager - the unified battle lifecycle manager.
    /// Phase 1: Foundation - basic lifecycle and state management.
    /// </summary>
    public class ZoneBattleManagerTests
    {
        #region Test Helpers

        private ZoneBattleManager CreateManager(string? playerFactionId = null)
        {
            // These tests don't exercise the simulated-kill reconciliation path, so
            // pass loose mocks. The dedicated reconciliation tests live in
            // ZoneBattleManagerAllocationSyncTests.
            var allocationService = new Mock<IZoneDefenderAllocationService>().Object;
            var factionService = new Mock<IFactionService>().Object;
            return new ZoneBattleManager(allocationService, factionService, playerFactionId);
        }

        private Dictionary<DefenderTier, int> CreateTroops(int basic, int medium, int heavy)
        {
            return new Dictionary<DefenderTier, int>
            {
                { DefenderTier.Basic, basic },
                { DefenderTier.Medium, medium },
                { DefenderTier.Heavy, heavy }
            };
        }

        private Zone CreateTestZone(string zoneId = "zone_vinewood")
        {
            return new Zone(zoneId, zoneId, new FactionWars.Core.Interfaces.Vector3(0, 0, 0), 100f);
        }

        #endregion

        #region Initial State

        [Fact]
        public void ZoneBattleManager_Constructor_ShouldHaveNoActiveBattles()
        {
            // Arrange & Act
            var manager = CreateManager();

            // Assert
            Assert.Equal(0, manager.BattleCount);
            Assert.Empty(manager.GetAllActiveBattles());
        }

        [Fact]
        public void ZoneBattleManager_Constructor_ShouldAcceptNullPlayerFaction()
        {
            // Arrange & Act
            var manager = CreateManager(playerFactionId: null);

            // Assert
            Assert.NotNull(manager);
        }

        #endregion

        #region StartBattle

        [Fact]
        public void ZoneBattleManager_StartBattle_ShouldCreateBattle()
        {
            // Arrange
            var manager = CreateManager();
            var attackerTroops = CreateTroops(5, 2, 1);
            var defenderTroops = CreateTroops(4, 2, 1);

            // Act
            var battle = manager.StartBattle(
                zoneId: "zone_vinewood",
                attackerFactionId: "faction_trevor",
                defenderFactionId: "faction_michael",
                attackerTroops: attackerTroops,
                defenderTroops: defenderTroops);

            // Assert
            Assert.NotNull(battle);
            Assert.Equal("zone_vinewood", battle.ZoneId);
            Assert.Equal("faction_trevor", battle.AttackerFactionId);
            Assert.Equal("faction_michael", battle.DefenderFactionId);
            Assert.Equal(1, manager.BattleCount);
        }

        [Fact]
        public void ZoneBattleManager_StartBattle_ShouldCopyTroops()
        {
            // Arrange
            var manager = CreateManager();
            var attackerTroops = CreateTroops(5, 2, 1);
            var defenderTroops = CreateTroops(4, 2, 1);

            // Act
            var battle = manager.StartBattle(
                zoneId: "zone_vinewood",
                attackerFactionId: "faction_trevor",
                defenderFactionId: "faction_michael",
                attackerTroops: attackerTroops,
                defenderTroops: defenderTroops);

            // Assert
            Assert.Equal(5, battle.AttackerTroops[DefenderTier.Basic]);
            Assert.Equal(4, battle.DefenderTroops[DefenderTier.Basic]);
        }

        [Fact]
        public void ZoneBattleManager_StartBattle_ShouldRaiseBattleStartedEvent()
        {
            // Arrange
            var manager = CreateManager();
            ZoneBattle? eventBattle = null;
            manager.BattleStarted += (battle) => eventBattle = battle;

            // Act
            var battle = manager.StartBattle(
                zoneId: "zone_vinewood",
                attackerFactionId: "faction_trevor",
                defenderFactionId: "faction_michael",
                attackerTroops: CreateTroops(5, 0, 0),
                defenderTroops: CreateTroops(4, 0, 0));

            // Assert
            Assert.NotNull(eventBattle);
            Assert.Same(battle, eventBattle);
        }

        [Fact]
        public void ZoneBattleManager_StartBattle_ShouldThrowIfBattleAlreadyExistsForZone()
        {
            // Arrange
            var manager = CreateManager();
            manager.StartBattle(
                zoneId: "zone_vinewood",
                attackerFactionId: "faction_trevor",
                defenderFactionId: "faction_michael",
                attackerTroops: CreateTroops(5, 0, 0),
                defenderTroops: CreateTroops(4, 0, 0));

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => manager.StartBattle(
                zoneId: "zone_vinewood",
                attackerFactionId: "faction_franklin",
                defenderFactionId: "faction_michael",
                attackerTroops: CreateTroops(3, 0, 0),
                defenderTroops: CreateTroops(4, 0, 0)));
        }

        [Fact]
        public void ZoneBattleManager_StartBattle_ShouldAllowMultipleBattlesInDifferentZones()
        {
            // Arrange
            var manager = CreateManager();

            // Act
            var battle1 = manager.StartBattle(
                zoneId: "zone_vinewood",
                attackerFactionId: "faction_trevor",
                defenderFactionId: "faction_michael",
                attackerTroops: CreateTroops(5, 0, 0),
                defenderTroops: CreateTroops(4, 0, 0));

            var battle2 = manager.StartBattle(
                zoneId: "zone_downtown",
                attackerFactionId: "faction_franklin",
                defenderFactionId: "faction_trevor",
                attackerTroops: CreateTroops(3, 0, 0),
                defenderTroops: CreateTroops(6, 0, 0));

            // Assert
            Assert.Equal(2, manager.BattleCount);
            Assert.NotEqual(battle1.Id, battle2.Id);
        }

        [Fact]
        public void ZoneBattleManager_StartBattle_ShouldSetPlayerFactionOnBattle()
        {
            // Arrange
            var manager = CreateManager(playerFactionId: "faction_michael");

            // Act
            var battle = manager.StartBattle(
                zoneId: "zone_vinewood",
                attackerFactionId: "faction_trevor",
                defenderFactionId: "faction_michael",
                attackerTroops: CreateTroops(5, 0, 0),
                defenderTroops: CreateTroops(4, 0, 0));

            // Assert
            Assert.True(battle.IsPlayerDefending);
            Assert.False(battle.IsPlayerAttacking);
        }

        #endregion

        #region GetBattle

        [Fact]
        public void ZoneBattleManager_GetBattleForZone_ShouldReturnBattleIfExists()
        {
            // Arrange
            var manager = CreateManager();
            var createdBattle = manager.StartBattle(
                zoneId: "zone_vinewood",
                attackerFactionId: "faction_trevor",
                defenderFactionId: "faction_michael",
                attackerTroops: CreateTroops(5, 0, 0),
                defenderTroops: CreateTroops(4, 0, 0));

            // Act
            var foundBattle = manager.GetBattleForZone("zone_vinewood");

            // Assert
            Assert.NotNull(foundBattle);
            Assert.Same(createdBattle, foundBattle);
        }

        [Fact]
        public void ZoneBattleManager_GetBattleForZone_ShouldReturnNullIfNotExists()
        {
            // Arrange
            var manager = CreateManager();

            // Act
            var battle = manager.GetBattleForZone("zone_nonexistent");

            // Assert
            Assert.Null(battle);
        }

        [Fact]
        public void ZoneBattleManager_GetAllActiveBattles_ShouldReturnAllBattles()
        {
            // Arrange
            var manager = CreateManager();
            manager.StartBattle(
                zoneId: "zone_vinewood",
                attackerFactionId: "faction_trevor",
                defenderFactionId: "faction_michael",
                attackerTroops: CreateTroops(5, 0, 0),
                defenderTroops: CreateTroops(4, 0, 0));
            manager.StartBattle(
                zoneId: "zone_downtown",
                attackerFactionId: "faction_franklin",
                defenderFactionId: "faction_trevor",
                attackerTroops: CreateTroops(3, 0, 0),
                defenderTroops: CreateTroops(6, 0, 0));

            // Act
            var battles = manager.GetAllActiveBattles();

            // Assert
            Assert.Equal(2, battles.Count);
        }

        [Fact]
        public void ZoneBattleManager_GetAllActiveBattles_ShouldReturnReadOnlyCopy()
        {
            // Arrange
            var manager = CreateManager();
            manager.StartBattle(
                zoneId: "zone_vinewood",
                attackerFactionId: "faction_trevor",
                defenderFactionId: "faction_michael",
                attackerTroops: CreateTroops(5, 0, 0),
                defenderTroops: CreateTroops(4, 0, 0));

            // Act
            var battles = manager.GetAllActiveBattles();

            // Assert - should not be able to modify the internal collection
            Assert.IsAssignableFrom<IReadOnlyList<ZoneBattle>>(battles);
        }

        #endregion

        #region EndBattle

        [Fact]
        public void ZoneBattleManager_EndBattle_ShouldRemoveBattleFromActive()
        {
            // Arrange
            var manager = CreateManager();
            manager.StartBattle(
                zoneId: "zone_vinewood",
                attackerFactionId: "faction_trevor",
                defenderFactionId: "faction_michael",
                attackerTroops: CreateTroops(5, 0, 0),
                defenderTroops: CreateTroops(4, 0, 0));

            // Act
            manager.EndBattle("zone_vinewood", BattleOutcome.AttackersWon);

            // Assert
            Assert.Equal(0, manager.BattleCount);
            Assert.Null(manager.GetBattleForZone("zone_vinewood"));
        }

        [Fact]
        public void ZoneBattleManager_EndBattle_ShouldRaiseBattleEndedEvent()
        {
            // Arrange
            var manager = CreateManager();
            var createdBattle = manager.StartBattle(
                zoneId: "zone_vinewood",
                attackerFactionId: "faction_trevor",
                defenderFactionId: "faction_michael",
                attackerTroops: CreateTroops(5, 0, 0),
                defenderTroops: CreateTroops(4, 0, 0));

            ZoneBattle? eventBattle = null;
            BattleOutcome? eventOutcome = null;
            manager.BattleEnded += (battle, outcome) =>
            {
                eventBattle = battle;
                eventOutcome = outcome;
            };

            // Act
            manager.EndBattle("zone_vinewood", BattleOutcome.DefendersWon);

            // Assert
            Assert.NotNull(eventBattle);
            Assert.Same(createdBattle, eventBattle);
            Assert.Equal(BattleOutcome.DefendersWon, eventOutcome);
        }

        [Fact]
        public void ZoneBattleManager_EndBattle_ShouldNotThrowIfBattleDoesNotExist()
        {
            // Arrange
            var manager = CreateManager();

            // Act & Assert - should not throw
            var exception = Record.Exception(() => manager.EndBattle("zone_nonexistent", BattleOutcome.AttackersWon));
            Assert.Null(exception);
        }

        #endregion

        #region Player Presence

        [Fact]
        public void ZoneBattleManager_OnPlayerEnteredZone_ShouldSetPlayerPresent()
        {
            // Arrange
            var manager = CreateManager();
            var zone = CreateTestZone("zone_vinewood");
            manager.StartBattle(
                zoneId: "zone_vinewood",
                attackerFactionId: "faction_trevor",
                defenderFactionId: "faction_michael",
                attackerTroops: CreateTroops(5, 0, 0),
                defenderTroops: CreateTroops(4, 0, 0));

            // Act
            manager.OnPlayerEnteredZone(zone);

            // Assert
            var battle = manager.GetBattleForZone("zone_vinewood");
            Assert.NotNull(battle);
            Assert.True(battle.IsPlayerPresent);
        }

        [Fact]
        public void ZoneBattleManager_OnPlayerEnteredZone_ShouldNotThrowIfNoBattleInZone()
        {
            // Arrange
            var manager = CreateManager();
            var zone = CreateTestZone("zone_peaceful");

            // Act & Assert
            var exception = Record.Exception(() => manager.OnPlayerEnteredZone(zone));
            Assert.Null(exception);
        }

        [Fact]
        public void ZoneBattleManager_OnPlayerExitedZone_ShouldClearPlayerPresent()
        {
            // Arrange
            var manager = CreateManager();
            var zone = CreateTestZone("zone_vinewood");
            manager.StartBattle(
                zoneId: "zone_vinewood",
                attackerFactionId: "faction_trevor",
                defenderFactionId: "faction_michael",
                attackerTroops: CreateTroops(5, 0, 0),
                defenderTroops: CreateTroops(4, 0, 0));
            manager.OnPlayerEnteredZone(zone);

            // Act
            manager.OnPlayerExitedZone(zone);

            // Assert
            var battle = manager.GetBattleForZone("zone_vinewood");
            Assert.NotNull(battle);
            Assert.False(battle.IsPlayerPresent);
        }

        #endregion

        #region SetPlayerFaction

        [Fact]
        public void ZoneBattleManager_SetPlayerFaction_ShouldUpdateForNewBattles()
        {
            // Arrange
            var manager = CreateManager(playerFactionId: null);

            // Act
            manager.SetPlayerFaction("faction_michael");

            var battle = manager.StartBattle(
                zoneId: "zone_vinewood",
                attackerFactionId: "faction_trevor",
                defenderFactionId: "faction_michael",
                attackerTroops: CreateTroops(5, 0, 0),
                defenderTroops: CreateTroops(4, 0, 0));

            // Assert
            Assert.True(battle.IsPlayerDefending);
        }

        #endregion

        #region Tick (Phase 1 - Foundation)

        [Fact]
        public void ZoneBattleManager_Tick_ShouldNotThrowWithNoBattles()
        {
            // Arrange
            var manager = CreateManager();

            // Act & Assert
            var exception = Record.Exception(() => manager.Tick(0.016f));
            Assert.Null(exception);
        }

        [Fact]
        public void ZoneBattleManager_Tick_ShouldAdvanceTimeOnBattlesWherePlayerNotPresent()
        {
            // Arrange
            var manager = CreateManager();
            var battle = manager.StartBattle(
                zoneId: "zone_vinewood",
                attackerFactionId: "faction_trevor",
                defenderFactionId: "faction_michael",
                attackerTroops: CreateTroops(5, 0, 0),
                defenderTroops: CreateTroops(4, 0, 0));
            Assert.False(battle.IsPlayerPresent);

            // Act
            manager.Tick(1.0f);

            // Assert
            Assert.Equal(1.0f, battle.ElapsedTime);
        }

        [Fact]
        public void ZoneBattleManager_Tick_ShouldNotAdvanceTimeOnBattlesWherePlayerIsPresent()
        {
            // Arrange
            var manager = CreateManager();
            var zone = CreateTestZone("zone_vinewood");
            var battle = manager.StartBattle(
                zoneId: "zone_vinewood",
                attackerFactionId: "faction_trevor",
                defenderFactionId: "faction_michael",
                attackerTroops: CreateTroops(5, 0, 0),
                defenderTroops: CreateTroops(4, 0, 0));
            manager.OnPlayerEnteredZone(zone);
            Assert.True(battle.IsPlayerPresent);

            // Act
            manager.Tick(1.0f);

            // Assert
            Assert.Equal(0f, battle.ElapsedTime);
        }

        #endregion

        #region BattleOutcome Enum

        [Fact]
        public void BattleOutcome_ShouldHaveExpectedValues()
        {
            // Assert
            Assert.Equal(0, (int)BattleOutcome.AttackersWon);
            Assert.Equal(1, (int)BattleOutcome.DefendersWon);
            Assert.Equal(2, (int)BattleOutcome.Draw);
        }

        #endregion

        #region Phase 2 - Tick-Based Kill Simulation

        [Fact]
        public void ZoneBattleManager_StartBattle_ShouldSetKillInterval()
        {
            // Arrange
            var manager = CreateManager();

            // Act - 9 total troops
            var battle = manager.StartBattle(
                zoneId: "zone_vinewood",
                attackerFactionId: "faction_trevor",
                defenderFactionId: "faction_michael",
                attackerTroops: CreateTroops(5, 0, 0),
                defenderTroops: CreateTroops(4, 0, 0));

            // Assert - kill interval should be set based on troop count
            Assert.True(battle.KillInterval > 0);
        }

        [Fact]
        public void ZoneBattleManager_Tick_ShouldProcessKillWhenTimerExpires()
        {
            // Arrange
            var manager = CreateManager();
            var battle = manager.StartBattle(
                zoneId: "zone_vinewood",
                attackerFactionId: "faction_trevor",
                defenderFactionId: "faction_michael",
                attackerTroops: CreateTroops(5, 0, 0),
                defenderTroops: CreateTroops(4, 0, 0));

            int initialTotal = battle.TotalAttackerTroops + battle.TotalDefenderTroops;

            // Act - advance time past kill interval
            float timeToAdvance = battle.KillInterval + 0.1f;
            manager.Tick(timeToAdvance);

            // Assert - one troop should have been killed
            int newTotal = battle.TotalAttackerTroops + battle.TotalDefenderTroops;
            Assert.Equal(initialTotal - 1, newTotal);
        }

        [Fact]
        public void ZoneBattleManager_Tick_ShouldRaiseTroopKilledEvent()
        {
            // Arrange
            var manager = CreateManager();
            var battle = manager.StartBattle(
                zoneId: "zone_vinewood",
                attackerFactionId: "faction_trevor",
                defenderFactionId: "faction_michael",
                attackerTroops: CreateTroops(5, 0, 0),
                defenderTroops: CreateTroops(4, 0, 0));

            ZoneBattle? eventBattle = null;
            DefenderTier? eventTier = null;
            string? eventSide = null;
            manager.TroopKilled += (b, tier, side) =>
            {
                eventBattle = b;
                eventTier = tier;
                eventSide = side;
            };

            // Act - advance time past kill interval
            float timeToAdvance = battle.KillInterval + 0.1f;
            manager.Tick(timeToAdvance);

            // Assert
            Assert.NotNull(eventBattle);
            Assert.Same(battle, eventBattle);
            Assert.NotNull(eventTier);
            Assert.NotNull(eventSide);
            Assert.True(eventSide == "attacker" || eventSide == "defender");
        }

        [Fact]
        public void ZoneBattleManager_Tick_ShouldResetKillTimerAfterKill()
        {
            // Arrange
            var manager = CreateManager();
            var battle = manager.StartBattle(
                zoneId: "zone_vinewood",
                attackerFactionId: "faction_trevor",
                defenderFactionId: "faction_michael",
                attackerTroops: CreateTroops(5, 0, 0),
                defenderTroops: CreateTroops(4, 0, 0));

            float killInterval = battle.KillInterval;

            // Act - advance time past kill interval
            manager.Tick(killInterval + 0.1f);

            // Assert - timer should be reset to near the kill interval
            Assert.True(battle.TimeUntilNextKill > 0);
            Assert.True(battle.TimeUntilNextKill <= killInterval);
        }

        [Fact]
        public void ZoneBattleManager_Tick_ShouldEndBattleWhenAttackersEliminated()
        {
            // Arrange
            var manager = CreateManager();
            var battle = manager.StartBattle(
                zoneId: "zone_vinewood",
                attackerFactionId: "faction_trevor",
                defenderFactionId: "faction_michael",
                attackerTroops: CreateTroops(1, 0, 0),  // Only 1 attacker
                defenderTroops: CreateTroops(10, 0, 0)); // Many defenders

            BattleOutcome? endedOutcome = null;
            manager.BattleEnded += (b, outcome) => endedOutcome = outcome;

            // Act - tick many times to ensure battle ends
            for (int i = 0; i < 100 && manager.BattleCount > 0; i++)
            {
                manager.Tick(battle.KillInterval + 0.1f);
            }

            // Assert - battle should have ended with defenders winning
            Assert.Equal(0, manager.BattleCount);
            Assert.NotNull(endedOutcome);
            Assert.Equal(BattleOutcome.DefendersWon, endedOutcome);
        }

        [Fact]
        public void ZoneBattleManager_Tick_ShouldEndBattleWhenDefendersEliminated()
        {
            // Arrange
            var manager = CreateManager();
            var battle = manager.StartBattle(
                zoneId: "zone_vinewood",
                attackerFactionId: "faction_trevor",
                defenderFactionId: "faction_michael",
                attackerTroops: CreateTroops(10, 0, 0), // Many attackers
                defenderTroops: CreateTroops(1, 0, 0)); // Only 1 defender

            BattleOutcome? endedOutcome = null;
            manager.BattleEnded += (b, outcome) => endedOutcome = outcome;

            // Act - tick many times to ensure battle ends
            for (int i = 0; i < 100 && manager.BattleCount > 0; i++)
            {
                manager.Tick(battle.KillInterval + 0.1f);
            }

            // Assert - battle should have ended with attackers winning
            Assert.Equal(0, manager.BattleCount);
            Assert.NotNull(endedOutcome);
            Assert.Equal(BattleOutcome.AttackersWon, endedOutcome);
        }

        [Fact]
        public void ZoneBattleManager_Tick_ShouldNotProcessKillsWhenPlayerPresent()
        {
            // Arrange
            var manager = CreateManager();
            var zone = CreateTestZone("zone_vinewood");
            var battle = manager.StartBattle(
                zoneId: "zone_vinewood",
                attackerFactionId: "faction_trevor",
                defenderFactionId: "faction_michael",
                attackerTroops: CreateTroops(5, 0, 0),
                defenderTroops: CreateTroops(4, 0, 0));

            manager.OnPlayerEnteredZone(zone);
            int initialTotal = battle.TotalAttackerTroops + battle.TotalDefenderTroops;

            // Act - advance time past kill interval
            manager.Tick(battle.KillInterval + 0.1f);

            // Assert - no kills should have occurred
            int newTotal = battle.TotalAttackerTroops + battle.TotalDefenderTroops;
            Assert.Equal(initialTotal, newTotal);
        }

        #endregion

        #region Phase 2 - Troop Reporting

        [Fact]
        public void ZoneBattleManager_ReportTroopKilled_ShouldDecrementAttackerTroops()
        {
            // Arrange
            var manager = CreateManager();
            var battle = manager.StartBattle(
                zoneId: "zone_vinewood",
                attackerFactionId: "faction_trevor",
                defenderFactionId: "faction_michael",
                attackerTroops: CreateTroops(5, 2, 1),
                defenderTroops: CreateTroops(4, 0, 0));

            int initialAttackers = battle.TotalAttackerTroops;

            // Act
            manager.ReportTroopKilled("zone_vinewood", "faction_trevor", DefenderTier.Basic);

            // Assert
            Assert.Equal(initialAttackers - 1, battle.TotalAttackerTroops);
            Assert.Equal(4, battle.AttackerTroops[DefenderTier.Basic]);
        }

        [Fact]
        public void ZoneBattleManager_ReportTroopKilled_ShouldDecrementDefenderTroops()
        {
            // Arrange
            var manager = CreateManager();
            var battle = manager.StartBattle(
                zoneId: "zone_vinewood",
                attackerFactionId: "faction_trevor",
                defenderFactionId: "faction_michael",
                attackerTroops: CreateTroops(5, 0, 0),
                defenderTroops: CreateTroops(4, 2, 1));

            int initialDefenders = battle.TotalDefenderTroops;

            // Act
            manager.ReportTroopKilled("zone_vinewood", "faction_michael", DefenderTier.Medium);

            // Assert
            Assert.Equal(initialDefenders - 1, battle.TotalDefenderTroops);
            Assert.Equal(1, battle.DefenderTroops[DefenderTier.Medium]);
        }

        [Fact]
        public void ZoneBattleManager_ReportTroopKilled_ShouldRaiseTroopKilledEvent()
        {
            // Arrange
            var manager = CreateManager();
            var battle = manager.StartBattle(
                zoneId: "zone_vinewood",
                attackerFactionId: "faction_trevor",
                defenderFactionId: "faction_michael",
                attackerTroops: CreateTroops(5, 0, 0),
                defenderTroops: CreateTroops(4, 0, 0));

            ZoneBattle? eventBattle = null;
            DefenderTier? eventTier = null;
            string? eventSide = null;
            manager.TroopKilled += (b, tier, side) =>
            {
                eventBattle = b;
                eventTier = tier;
                eventSide = side;
            };

            // Act
            manager.ReportTroopKilled("zone_vinewood", "faction_trevor", DefenderTier.Basic);

            // Assert
            Assert.NotNull(eventBattle);
            Assert.Equal(DefenderTier.Basic, eventTier);
            Assert.Equal("attacker", eventSide);
        }

        [Fact]
        public void ZoneBattleManager_ReportTroopKilled_ShouldEndBattleWhenOneSideEliminated()
        {
            // Arrange
            var manager = CreateManager();
            var battle = manager.StartBattle(
                zoneId: "zone_vinewood",
                attackerFactionId: "faction_trevor",
                defenderFactionId: "faction_michael",
                attackerTroops: CreateTroops(1, 0, 0),
                defenderTroops: CreateTroops(4, 0, 0));

            BattleOutcome? endedOutcome = null;
            manager.BattleEnded += (b, outcome) => endedOutcome = outcome;

            // Act - kill the last attacker
            manager.ReportTroopKilled("zone_vinewood", "faction_trevor", DefenderTier.Basic);

            // Assert
            Assert.Equal(0, manager.BattleCount);
            Assert.Equal(BattleOutcome.DefendersWon, endedOutcome);
        }

        [Fact]
        public void ZoneBattleManager_ReportTroopKilled_ShouldNotThrowForNonExistentBattle()
        {
            // Arrange
            var manager = CreateManager();

            // Act & Assert
            var exception = Record.Exception(() =>
                manager.ReportTroopKilled("zone_nonexistent", "faction_trevor", DefenderTier.Basic));
            Assert.Null(exception);
        }

        [Fact]
        public void ZoneBattleManager_ReportTroopKilled_ShouldNotThrowForUnrelatedFaction()
        {
            // Arrange
            var manager = CreateManager();
            manager.StartBattle(
                zoneId: "zone_vinewood",
                attackerFactionId: "faction_trevor",
                defenderFactionId: "faction_michael",
                attackerTroops: CreateTroops(5, 0, 0),
                defenderTroops: CreateTroops(4, 0, 0));

            // Act & Assert - faction_franklin is not in the battle
            var exception = Record.Exception(() =>
                manager.ReportTroopKilled("zone_vinewood", "faction_franklin", DefenderTier.Basic));
            Assert.Null(exception);
        }

        #endregion

        #region RemoveParticipant

        [Fact]
        public void RemoveParticipant_ReturnsFalse_WhenNoBattle()
        {
            var manager = CreateManager(playerFactionId: "player_faction");

            bool result = manager.RemoveParticipant("zone_1", "player_faction");

            Assert.False(result);
        }

        [Fact]
        public void RemoveParticipant_PlayerLeavesContestedZone_BattleContinues2Way()
        {
            var manager = CreateManager(playerFactionId: "player_faction");
            manager.StartBattle("zone_1", "trevor", "michael",
                new Dictionary<DefenderTier, int> { { DefenderTier.Basic, 3 } },
                new Dictionary<DefenderTier, int> { { DefenderTier.Basic, 5 } });
            manager.JoinAsAttacker("zone_1", "player_faction", true, () => 4, null);

            int endCount = 0;
            manager.BattleEnded += (b, _) => endCount++;

            bool result = manager.RemoveParticipant("zone_1", "player_faction");

            Assert.True(result);
            var battle = manager.GetBattleForZone("zone_1");
            Assert.NotNull(battle);
            Assert.Equal(2, battle!.Participants.Count);
            Assert.False(battle.Participants.Any(p => p.IsPlayer));
            Assert.Equal(0, endCount);
        }

        [Fact]
        public void RemoveParticipant_LastAttackerLeaves_DefenderWinsAndBattleEnds()
        {
            var manager = CreateManager(playerFactionId: "player_faction");
            manager.StartBattle("zone_1", "trevor", "michael",
                new Dictionary<DefenderTier, int> { { DefenderTier.Basic, 3 } },
                new Dictionary<DefenderTier, int> { { DefenderTier.Basic, 5 } });

            ZoneBattle? endedBattle = null;
            BattleOutcome? endedOutcome = null;
            manager.BattleEnded += (b, o) => { endedBattle = b; endedOutcome = o; };

            bool result = manager.RemoveParticipant("zone_1", "trevor");

            Assert.True(result);
            Assert.Null(manager.GetBattleForZone("zone_1"));
            Assert.NotNull(endedBattle);
            Assert.Equal(BattleOutcome.DefendersWon, endedOutcome);
        }

        #endregion

        #region StartPlayerCombat

        [Fact]
        public void StartPlayerCombat_CreatesNewBattle_WhenNoneExists()
        {
            var allocSvc = new Mock<IZoneDefenderAllocationService>();
            allocSvc.Setup(a => a.GetAllocation("michael", "zone_1"))
                .Returns((ZoneDefenderAllocation?)null);
            var factionSvc = new Mock<IFactionService>();
            var manager = new ZoneBattleManager(allocSvc.Object, factionSvc.Object, "player_faction");
            var zone = CreateTestZone("zone_1");
            zone.OwnerFactionId = "michael";

            var battle = manager.StartPlayerCombat(zone, "player_faction", () => 4);

            Assert.NotNull(battle);
            Assert.Equal("zone_1", battle!.ZoneId);
            Assert.Equal("michael", battle.Defender.FactionId);
            Assert.Single(battle.Attackers);
            Assert.Equal("player_faction", battle.Attackers[0].FactionId);
            Assert.True(battle.Attackers[0].IsPlayer);
            Assert.Equal(4, battle.Attackers[0].AliveCount);
        }

        [Fact]
        public void StartPlayerCombat_JoinsExistingBattle_AsThirdAttacker()
        {
            var manager = CreateManager(playerFactionId: "player_faction");
            manager.StartBattle("zone_1", "trevor", "michael",
                new Dictionary<DefenderTier, int> { { DefenderTier.Basic, 3 } },
                new Dictionary<DefenderTier, int> { { DefenderTier.Basic, 5 } });
            var zone = CreateTestZone("zone_1");
            zone.OwnerFactionId = "michael";

            var battle = manager.StartPlayerCombat(zone, "player_faction", () => 4);

            Assert.NotNull(battle);
            Assert.Equal(2, battle!.Attackers.Count);
            Assert.True(battle.Attackers.Any(p => p.IsPlayer));
            Assert.True(battle.Attackers.Any(p => p.FactionId == "trevor" && !p.IsPlayer));
        }

        [Fact]
        public void StartPlayerCombat_ReturnsNull_WhenAlreadyAParticipant()
        {
            var manager = CreateManager(playerFactionId: "player_faction");
            var zone = CreateTestZone("zone_1");
            zone.OwnerFactionId = "michael";
            var first = manager.StartPlayerCombat(zone, "player_faction", () => 4);
            Assert.NotNull(first);

            var second = manager.StartPlayerCombat(zone, "player_faction", () => 4);

            Assert.Null(second);
        }

        #endregion

        #region JoinAsAttacker

        [Fact]
        public void JoinAsAttacker_ReturnsFalse_WhenNoBattleInZone()
        {
            var manager = CreateManager(playerFactionId: "player_faction");

            bool result = manager.JoinAsAttacker(
                zoneId: "zone_1",
                factionId: "player_faction",
                isPlayer: true,
                aliveCountCallback: () => 4,
                troops: null);

            Assert.False(result);
        }

        [Fact]
        public void JoinAsAttacker_AddsPlayerParticipant_ToExistingBattle()
        {
            var manager = CreateManager(playerFactionId: "player_faction");
            manager.StartBattle("zone_1", "trevor", "michael",
                new Dictionary<DefenderTier, int> { { DefenderTier.Basic, 3 } },
                new Dictionary<DefenderTier, int> { { DefenderTier.Basic, 3 } });

            bool result = manager.JoinAsAttacker(
                zoneId: "zone_1",
                factionId: "player_faction",
                isPlayer: true,
                aliveCountCallback: () => 4,
                troops: null);

            Assert.True(result);
            var battle = manager.GetBattleForZone("zone_1");
            Assert.NotNull(battle);
            Assert.Equal(2, battle!.Attackers.Count);
            Assert.True(battle.Attackers.Any(p => p.IsPlayer && p.FactionId == "player_faction"));
        }

        [Fact]
        public void JoinAsAttacker_RejectsThirdAttacker()
        {
            var manager = CreateManager(playerFactionId: "player_faction");
            manager.StartBattle("zone_1", "trevor", "michael",
                new Dictionary<DefenderTier, int> { { DefenderTier.Basic, 3 } },
                new Dictionary<DefenderTier, int> { { DefenderTier.Basic, 3 } });
            manager.JoinAsAttacker("zone_1", "player_faction", true, () => 4, null);

            bool result = manager.JoinAsAttacker(
                zoneId: "zone_1",
                factionId: "franklin",
                isPlayer: false,
                aliveCountCallback: null,
                troops: new Dictionary<DefenderTier, int> { { DefenderTier.Basic, 2 } });

            Assert.False(result);
        }

        [Fact]
        public void JoinAsAttacker_RejectsNonPlayerThirdParty_InV1()
        {
            var manager = CreateManager(playerFactionId: "player_faction");
            manager.StartBattle("zone_1", "trevor", "michael",
                new Dictionary<DefenderTier, int> { { DefenderTier.Basic, 3 } },
                new Dictionary<DefenderTier, int> { { DefenderTier.Basic, 3 } });

            bool result = manager.JoinAsAttacker(
                zoneId: "zone_1",
                factionId: "franklin",
                isPlayer: false,
                aliveCountCallback: null,
                troops: new Dictionary<DefenderTier, int> { { DefenderTier.Basic, 2 } });

            Assert.False(result);
        }

        [Fact]
        public void JoinAsAttacker_RejectsDuplicateFaction()
        {
            var manager = CreateManager(playerFactionId: "player_faction");
            manager.StartBattle("zone_1", "trevor", "michael",
                new Dictionary<DefenderTier, int> { { DefenderTier.Basic, 3 } },
                new Dictionary<DefenderTier, int> { { DefenderTier.Basic, 3 } });

            bool result = manager.JoinAsAttacker(
                zoneId: "zone_1",
                factionId: "trevor",
                isPlayer: false,
                aliveCountCallback: null,
                troops: new Dictionary<DefenderTier, int> { { DefenderTier.Basic, 1 } });

            Assert.False(result);
        }

        #endregion

        #region IsPlayerInBattle / GetPlayerCurrentBattle

        [Fact]
        public void IsPlayerInBattle_ReturnsFalse_WhenNoBattle()
        {
            var manager = CreateManager(playerFactionId: "player_faction");

            Assert.False(manager.IsPlayerInBattle());
        }

        [Fact]
        public void IsPlayerInBattle_ReturnsFalse_WhenBattleHasNoPlayerParticipant()
        {
            var manager = CreateManager(playerFactionId: "player_faction");
            manager.StartBattle("zone_1", "trevor", "michael",
                new Dictionary<DefenderTier, int> { { DefenderTier.Basic, 3 } },
                new Dictionary<DefenderTier, int> { { DefenderTier.Basic, 3 } });

            Assert.False(manager.IsPlayerInBattle());
        }

        [Fact]
        public void GetPlayerCurrentBattle_ReturnsNull_WhenNoPlayerParticipant()
        {
            var manager = CreateManager(playerFactionId: "player_faction");
            manager.StartBattle("zone_1", "trevor", "michael",
                new Dictionary<DefenderTier, int> { { DefenderTier.Basic, 3 } },
                new Dictionary<DefenderTier, int> { { DefenderTier.Basic, 3 } });

            Assert.Null(manager.GetPlayerCurrentBattle());
        }

        #endregion
    }
}
