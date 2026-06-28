using System;
using System.Collections.Generic;
using System.Linq;
using FactionWars.Combat.Models;
using FactionWars.Core.Models;
using Xunit;

namespace FactionWars.Tests.Unit.Combat
{
    /// <summary>
    /// Tests for ZoneBattle model - the unified battle state representation.
    /// </summary>
    public class ZoneBattleTests
    {
        #region Test Helpers

        private ZoneBattle CreateBattle(
            string? attackerFactionId = null,
            string? defenderFactionId = null,
            string? zoneId = null,
            Dictionary<DefenderRole, int>? attackerTroops = null,
            Dictionary<DefenderRole, int>? defenderTroops = null,
            string? playerFactionId = null)
        {
            return new ZoneBattle(
                attackerFactionId: attackerFactionId ?? "faction_trevor",
                defenderFactionId: defenderFactionId ?? "faction_michael",
                zoneId: zoneId ?? "zone_vinewood",
                attackerTroops: attackerTroops ?? CreateDefaultTroops(5, 2, 1),
                defenderTroops: defenderTroops ?? CreateDefaultTroops(4, 2, 1),
                playerFactionId: playerFactionId);
        }

        private Dictionary<DefenderRole, int> CreateDefaultTroops(int basic, int medium, int heavy)
        {
            return new Dictionary<DefenderRole, int>
            {
                { DefenderRole.Grunt, basic },
                { DefenderRole.Gunner, medium },
                { DefenderRole.Rifleman, heavy }
            };
        }

        #endregion

        #region Construction

        [Fact]
        public void ZoneBattle_Constructor_ShouldInitializeWithUniqueId()
        {
            // Arrange & Act
            var battle1 = CreateBattle();
            var battle2 = CreateBattle();

            // Assert
            Assert.NotNull(battle1.Id);
            Assert.NotNull(battle2.Id);
            Assert.NotEqual(battle1.Id, battle2.Id);
        }

        [Fact]
        public void ZoneBattle_Constructor_ShouldSetFactionIds()
        {
            // Arrange & Act
            var battle = CreateBattle(
                attackerFactionId: "faction_trevor",
                defenderFactionId: "faction_michael");

            // Assert
            Assert.Equal("faction_trevor", battle.AttackerFactionId);
            Assert.Equal("faction_michael", battle.DefenderFactionId);
        }

        [Fact]
        public void ZoneBattle_Constructor_ShouldSetZoneId()
        {
            // Arrange & Act
            var battle = CreateBattle(zoneId: "zone_downtown");

            // Assert
            Assert.Equal("zone_downtown", battle.ZoneId);
        }

        [Fact]
        public void ZoneBattle_Constructor_ShouldCopyTroopDictionaries()
        {
            // Arrange
            var attackerTroops = CreateDefaultTroops(5, 2, 1);
            var defenderTroops = CreateDefaultTroops(4, 2, 1);

            // Act
            var battle = CreateBattle(attackerTroops: attackerTroops, defenderTroops: defenderTroops);

            // Modify original dictionaries
            attackerTroops[DefenderRole.Grunt] = 100;
            defenderTroops[DefenderRole.Grunt] = 100;

            // Assert - Battle troops should be unchanged
            Assert.Equal(5, battle.AttackerTroops[DefenderRole.Grunt]);
            Assert.Equal(4, battle.DefenderTroops[DefenderRole.Grunt]);
        }

        [Fact]
        public void ZoneBattle_Constructor_ShouldThrowForNullAttackerFactionId()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new ZoneBattle(
                attackerFactionId: null!,
                defenderFactionId: "faction_michael",
                zoneId: "zone_vinewood",
                attackerTroops: CreateDefaultTroops(5, 2, 1),
                defenderTroops: CreateDefaultTroops(4, 2, 1)));
        }

        [Fact]
        public void ZoneBattle_Constructor_ShouldThrowForNullDefenderFactionId()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new ZoneBattle(
                attackerFactionId: "faction_trevor",
                defenderFactionId: null!,
                zoneId: "zone_vinewood",
                attackerTroops: CreateDefaultTroops(5, 2, 1),
                defenderTroops: CreateDefaultTroops(4, 2, 1)));
        }

        [Fact]
        public void ZoneBattle_Constructor_ShouldThrowForNullZoneId()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new ZoneBattle(
                attackerFactionId: "faction_trevor",
                defenderFactionId: "faction_michael",
                zoneId: null!,
                attackerTroops: CreateDefaultTroops(5, 2, 1),
                defenderTroops: CreateDefaultTroops(4, 2, 1)));
        }

        [Fact]
        public void ZoneBattle_Constructor_ShouldInitializeSpawnedPedDictionaries()
        {
            // Arrange & Act
            var battle = CreateBattle();

            // Assert
            Assert.NotNull(battle.SpawnedAttackers);
            Assert.NotNull(battle.SpawnedDefenders);
            Assert.Empty(battle.SpawnedAttackers);
            Assert.Empty(battle.SpawnedDefenders);
        }

        [Fact]
        public void ZoneBattle_Constructor_ShouldDefaultToPlayerNotPresent()
        {
            // Arrange & Act
            var battle = CreateBattle();

            // Assert
            Assert.False(battle.IsPlayerPresent);
        }

        [Fact]
        public void ZoneBattle_Constructor_ShouldInitializeTimingToZero()
        {
            // Arrange & Act
            var battle = CreateBattle();

            // Assert
            Assert.Equal(0f, battle.ElapsedTime);
            Assert.Equal(0f, battle.TimeUntilNextKill);
        }

        #endregion

        #region Initial Troop Counts

        [Fact]
        public void ZoneBattle_InitialAttackerTroops_ShouldTrackInitialCount()
        {
            // Arrange
            var attackerTroops = CreateDefaultTroops(5, 2, 1); // 8 total
            var defenderTroops = CreateDefaultTroops(4, 2, 1); // 7 total

            // Act
            var battle = CreateBattle(attackerTroops: attackerTroops, defenderTroops: defenderTroops);

            // Assert
            Assert.Equal(8, battle.InitialAttackerTroops);
        }

        [Fact]
        public void ZoneBattle_InitialDefenderTroops_ShouldTrackInitialCount()
        {
            // Arrange
            var attackerTroops = CreateDefaultTroops(5, 2, 1); // 8 total
            var defenderTroops = CreateDefaultTroops(4, 2, 1); // 7 total

            // Act
            var battle = CreateBattle(attackerTroops: attackerTroops, defenderTroops: defenderTroops);

            // Assert
            Assert.Equal(7, battle.InitialDefenderTroops);
        }

        [Fact]
        public void ZoneBattle_InitialTroops_ShouldNotChangeWhenTroopsRemoved()
        {
            // Arrange
            var attackerTroops = CreateDefaultTroops(5, 2, 1); // 8 total
            var defenderTroops = CreateDefaultTroops(4, 2, 1); // 7 total
            var battle = CreateBattle(attackerTroops: attackerTroops, defenderTroops: defenderTroops);

            // Act
            battle.RemoveAttackerTroop(DefenderRole.Grunt);
            battle.RemoveDefenderTroop(DefenderRole.Grunt);

            // Assert - initial counts unchanged
            Assert.Equal(8, battle.InitialAttackerTroops);
            Assert.Equal(7, battle.InitialDefenderTroops);
            // Current counts changed
            Assert.Equal(7, battle.TotalAttackerTroops);
            Assert.Equal(6, battle.TotalDefenderTroops);
        }

        #endregion

        #region Troop Counts

        [Fact]
        public void ZoneBattle_TotalAttackerTroops_ShouldSumAllTiers()
        {
            // Arrange
            var battle = CreateBattle(attackerTroops: CreateDefaultTroops(5, 2, 1));

            // Assert
            Assert.Equal(8, battle.TotalAttackerTroops);
        }

        [Fact]
        public void ZoneBattle_TotalDefenderTroops_ShouldSumAllTiers()
        {
            // Arrange
            var battle = CreateBattle(defenderTroops: CreateDefaultTroops(4, 2, 1));

            // Assert
            Assert.Equal(7, battle.TotalDefenderTroops);
        }

        [Fact]
        public void ZoneBattle_TotalSpawnedAttackers_ShouldCountSpawnedPeds()
        {
            // Arrange
            var battle = CreateBattle();
            battle.SpawnedAttackers[101] = DefenderRole.Grunt;
            battle.SpawnedAttackers[102] = DefenderRole.Gunner;
            battle.SpawnedAttackers[103] = DefenderRole.Rifleman;

            // Assert
            Assert.Equal(3, battle.TotalSpawnedAttackers);
        }

        [Fact]
        public void ZoneBattle_TotalSpawnedDefenders_ShouldCountSpawnedPeds()
        {
            // Arrange
            var battle = CreateBattle();
            battle.SpawnedDefenders[201] = DefenderRole.Grunt;
            battle.SpawnedDefenders[202] = DefenderRole.Grunt;

            // Assert
            Assert.Equal(2, battle.TotalSpawnedDefenders);
        }

        #endregion

        #region Battle State

        [Fact]
        public void ZoneBattle_IsOngoing_ShouldBeTrueWhenBothSidesHaveTroops()
        {
            // Arrange
            var battle = CreateBattle(
                attackerTroops: CreateDefaultTroops(5, 0, 0),
                defenderTroops: CreateDefaultTroops(3, 0, 0));

            // Assert
            Assert.True(battle.IsOngoing);
        }

        [Fact]
        public void ZoneBattle_IsOngoing_ShouldBeFalseWhenAttackersEliminated()
        {
            // Arrange
            var battle = CreateBattle(
                attackerTroops: CreateDefaultTroops(0, 0, 0),
                defenderTroops: CreateDefaultTroops(3, 0, 0));

            // Assert
            Assert.False(battle.IsOngoing);
        }

        [Fact]
        public void ZoneBattle_IsOngoing_ShouldBeFalseWhenDefendersEliminated()
        {
            // Arrange
            var battle = CreateBattle(
                attackerTroops: CreateDefaultTroops(5, 0, 0),
                defenderTroops: CreateDefaultTroops(0, 0, 0));

            // Assert
            Assert.False(battle.IsOngoing);
        }

        [Fact]
        public void ZoneBattle_AttackersWon_ShouldBeTrueWhenDefendersEliminated()
        {
            // Arrange
            var battle = CreateBattle(
                attackerTroops: CreateDefaultTroops(5, 0, 0),
                defenderTroops: CreateDefaultTroops(0, 0, 0));

            // Assert
            Assert.True(battle.AttackersWon);
            Assert.False(battle.DefendersWon);
        }

        [Fact]
        public void ZoneBattle_DefendersWon_ShouldBeTrueWhenAttackersEliminated()
        {
            // Arrange
            var battle = CreateBattle(
                attackerTroops: CreateDefaultTroops(0, 0, 0),
                defenderTroops: CreateDefaultTroops(3, 0, 0));

            // Assert
            Assert.True(battle.DefendersWon);
            Assert.False(battle.AttackersWon);
        }

        #endregion

        #region Player Presence

        [Fact]
        public void ZoneBattle_IsPlayerDefending_ShouldBeTrueWhenPlayerFactionIsDefender()
        {
            // Arrange
            var battle = CreateBattle(
                defenderFactionId: "faction_michael",
                playerFactionId: "faction_michael");

            // Assert
            Assert.True(battle.IsPlayerDefending);
            Assert.False(battle.IsPlayerAttacking);
        }

        [Fact]
        public void ZoneBattle_IsPlayerAttacking_ShouldBeTrueWhenPlayerFactionIsAttacker()
        {
            // Arrange
            var battle = CreateBattle(
                attackerFactionId: "faction_trevor",
                playerFactionId: "faction_trevor");

            // Assert
            Assert.True(battle.IsPlayerAttacking);
            Assert.False(battle.IsPlayerDefending);
        }

        [Fact]
        public void ZoneBattle_IsPlayerDefending_ShouldBeFalseWhenPlayerFactionIsNull()
        {
            // Arrange
            var battle = CreateBattle(playerFactionId: null);

            // Assert
            Assert.False(battle.IsPlayerDefending);
            Assert.False(battle.IsPlayerAttacking);
        }

        [Fact]
        public void ZoneBattle_IsPlayerDefending_ShouldBeFalseWhenPlayerFactionIsThirdParty()
        {
            // Arrange
            var battle = CreateBattle(
                attackerFactionId: "faction_trevor",
                defenderFactionId: "faction_michael",
                playerFactionId: "faction_franklin");

            // Assert
            Assert.False(battle.IsPlayerDefending);
            Assert.False(battle.IsPlayerAttacking);
        }

        [Fact]
        public void ZoneBattle_SetPlayerPresent_ShouldUpdateFlag()
        {
            // Arrange
            var battle = CreateBattle();
            Assert.False(battle.IsPlayerPresent);

            // Act
            battle.IsPlayerPresent = true;

            // Assert
            Assert.True(battle.IsPlayerPresent);
        }

        #endregion

        #region Troop Modifications

        [Fact]
        public void ZoneBattle_RemoveAttackerTroop_ShouldDecrementCount()
        {
            // Arrange
            var battle = CreateBattle(attackerTroops: CreateDefaultTroops(5, 2, 1));

            // Act
            var result = battle.RemoveAttackerTroop(DefenderRole.Grunt);

            // Assert
            Assert.True(result);
            Assert.Equal(4, battle.AttackerTroops[DefenderRole.Grunt]);
        }

        [Fact]
        public void ZoneBattle_RemoveAttackerTroop_ShouldReturnFalseWhenNoTroopsOfTier()
        {
            // Arrange
            var battle = CreateBattle(attackerTroops: CreateDefaultTroops(0, 2, 1));

            // Act
            var result = battle.RemoveAttackerTroop(DefenderRole.Grunt);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void ZoneBattle_RemoveDefenderTroop_ShouldDecrementCount()
        {
            // Arrange
            var battle = CreateBattle(defenderTroops: CreateDefaultTroops(4, 2, 1));

            // Act
            var result = battle.RemoveDefenderTroop(DefenderRole.Gunner);

            // Assert
            Assert.True(result);
            Assert.Equal(1, battle.DefenderTroops[DefenderRole.Gunner]);
        }

        [Fact]
        public void ZoneBattle_RemoveDefenderTroop_ShouldReturnFalseWhenNoTroopsOfTier()
        {
            // Arrange
            var battle = CreateBattle(defenderTroops: CreateDefaultTroops(4, 0, 1));

            // Act
            var result = battle.RemoveDefenderTroop(DefenderRole.Gunner);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void ZoneBattle_AddAttackerTroops_ShouldIncrementCount()
        {
            // Arrange
            var battle = CreateBattle(attackerTroops: CreateDefaultTroops(5, 2, 1));

            // Act
            battle.AddAttackerTroops(DefenderRole.Rifleman, 3);

            // Assert
            Assert.Equal(4, battle.AttackerTroops[DefenderRole.Rifleman]);
        }

        [Fact]
        public void ZoneBattle_AddAttackerTroops_ShouldIgnoreZeroOrNegativeCount()
        {
            // Arrange
            var battle = CreateBattle(attackerTroops: CreateDefaultTroops(5, 2, 1));

            // Act
            battle.AddAttackerTroops(DefenderRole.Grunt, 0);
            battle.AddAttackerTroops(DefenderRole.Gunner, -1);

            // Assert
            Assert.Equal(5, battle.AttackerTroops[DefenderRole.Grunt]);
            Assert.Equal(2, battle.AttackerTroops[DefenderRole.Gunner]);
        }

        [Fact]
        public void ZoneBattle_AddDefenderTroops_ShouldIncrementCount()
        {
            // Arrange
            var battle = CreateBattle(defenderTroops: CreateDefaultTroops(4, 2, 1));

            // Act
            battle.AddDefenderTroops(DefenderRole.Grunt, 5);

            // Assert
            Assert.Equal(9, battle.DefenderTroops[DefenderRole.Grunt]);
        }

        [Fact]
        public void ZoneBattle_AddDefenderTroops_ShouldIgnoreZeroOrNegativeCount()
        {
            // Arrange
            var battle = CreateBattle(defenderTroops: CreateDefaultTroops(4, 2, 1));

            // Act
            battle.AddDefenderTroops(DefenderRole.Grunt, 0);
            battle.AddDefenderTroops(DefenderRole.Gunner, -2);

            // Assert
            Assert.Equal(4, battle.DefenderTroops[DefenderRole.Grunt]);
            Assert.Equal(2, battle.DefenderTroops[DefenderRole.Gunner]);
        }

        #endregion

        #region Timing

        [Fact]
        public void ZoneBattle_AdvanceTime_ShouldIncrementElapsedTime()
        {
            // Arrange
            var battle = CreateBattle();

            // Act
            battle.AdvanceTime(2.5f);

            // Assert
            Assert.Equal(2.5f, battle.ElapsedTime);
        }

        [Fact]
        public void ZoneBattle_AdvanceTime_ShouldDecrementTimeUntilNextKill()
        {
            // Arrange
            var battle = CreateBattle();
            battle.SetKillInterval(5.0f);

            // Act
            battle.AdvanceTime(2.0f);

            // Assert
            Assert.Equal(3.0f, battle.TimeUntilNextKill);
        }

        [Fact]
        public void ZoneBattle_ResetKillTimer_ShouldRestoreKillInterval()
        {
            // Arrange
            var battle = CreateBattle();
            battle.SetKillInterval(5.0f);
            battle.AdvanceTime(4.0f);
            Assert.Equal(1.0f, battle.TimeUntilNextKill);

            // Act
            battle.ResetKillTimer();

            // Assert
            Assert.Equal(5.0f, battle.TimeUntilNextKill);
        }

        [Fact]
        public void ZoneBattle_SetKillInterval_ShouldUpdateIntervalAndTimer()
        {
            // Arrange
            var battle = CreateBattle();

            // Act
            battle.SetKillInterval(3.0f);

            // Assert
            Assert.Equal(3.0f, battle.KillInterval);
            Assert.Equal(3.0f, battle.TimeUntilNextKill);
        }

        #endregion

        #region Spawned Ped Tracking

        [Fact]
        public void ZoneBattle_RegisterSpawnedAttacker_ShouldAddToSpawnedAttackers()
        {
            // Arrange
            var battle = CreateBattle();

            // Act
            battle.RegisterSpawnedAttacker(101, DefenderRole.Grunt);
            battle.RegisterSpawnedAttacker(102, DefenderRole.Gunner);

            // Assert
            Assert.Equal(2, battle.SpawnedAttackers.Count);
            Assert.Equal(DefenderRole.Grunt, battle.SpawnedAttackers[101]);
            Assert.Equal(DefenderRole.Gunner, battle.SpawnedAttackers[102]);
        }

        [Fact]
        public void ZoneBattle_RegisterSpawnedDefender_ShouldAddToSpawnedDefenders()
        {
            // Arrange
            var battle = CreateBattle();

            // Act
            battle.RegisterSpawnedDefender(201, DefenderRole.Rifleman);

            // Assert
            Assert.Single(battle.SpawnedDefenders);
            Assert.Equal(DefenderRole.Rifleman, battle.SpawnedDefenders[201]);
        }

        [Fact]
        public void ZoneBattle_UnregisterSpawnedAttacker_ShouldRemoveFromSpawnedAttackers()
        {
            // Arrange
            var battle = CreateBattle();
            battle.RegisterSpawnedAttacker(101, DefenderRole.Grunt);
            battle.RegisterSpawnedAttacker(102, DefenderRole.Gunner);

            // Act
            var result = battle.UnregisterSpawnedAttacker(101);

            // Assert
            Assert.True(result);
            Assert.Single(battle.SpawnedAttackers);
            Assert.False(battle.SpawnedAttackers.ContainsKey(101));
        }

        [Fact]
        public void ZoneBattle_UnregisterSpawnedAttacker_ShouldReturnFalseIfNotFound()
        {
            // Arrange
            var battle = CreateBattle();

            // Act
            var result = battle.UnregisterSpawnedAttacker(999);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void ZoneBattle_UnregisterSpawnedDefender_ShouldRemoveFromSpawnedDefenders()
        {
            // Arrange
            var battle = CreateBattle();
            battle.RegisterSpawnedDefender(201, DefenderRole.Grunt);

            // Act
            var result = battle.UnregisterSpawnedDefender(201);

            // Assert
            Assert.True(result);
            Assert.Empty(battle.SpawnedDefenders);
        }

        [Fact]
        public void ZoneBattle_GetSpawnedAttackerTier_ShouldReturnTierIfFound()
        {
            // Arrange
            var battle = CreateBattle();
            battle.RegisterSpawnedAttacker(101, DefenderRole.Rifleman);

            // Act
            var tier = battle.GetSpawnedAttackerTier(101);

            // Assert
            Assert.Equal(DefenderRole.Rifleman, tier);
        }

        [Fact]
        public void ZoneBattle_GetSpawnedAttackerTier_ShouldReturnNullIfNotFound()
        {
            // Arrange
            var battle = CreateBattle();

            // Act
            var tier = battle.GetSpawnedAttackerTier(999);

            // Assert
            Assert.Null(tier);
        }

        [Fact]
        public void ZoneBattle_GetSpawnedDefenderRole_ShouldReturnTierIfFound()
        {
            // Arrange
            var battle = CreateBattle();
            battle.RegisterSpawnedDefender(201, DefenderRole.Gunner);

            // Act
            var tier = battle.GetSpawnedDefenderRole(201);

            // Assert
            Assert.Equal(DefenderRole.Gunner, tier);
        }

        [Fact]
        public void ZoneBattle_ClearSpawnedPeds_ShouldClearBothDictionaries()
        {
            // Arrange
            var battle = CreateBattle();
            battle.RegisterSpawnedAttacker(101, DefenderRole.Grunt);
            battle.RegisterSpawnedDefender(201, DefenderRole.Grunt);

            // Act
            battle.ClearSpawnedPeds();

            // Assert
            Assert.Empty(battle.SpawnedAttackers);
            Assert.Empty(battle.SpawnedDefenders);
        }

        #endregion

        #region Spawned Count By Tier

        [Fact]
        public void ZoneBattle_GetSpawnedAttackerCountByTier_ShouldCountCorrectly()
        {
            // Arrange
            var battle = CreateBattle();
            battle.RegisterSpawnedAttacker(101, DefenderRole.Grunt);
            battle.RegisterSpawnedAttacker(102, DefenderRole.Grunt);
            battle.RegisterSpawnedAttacker(103, DefenderRole.Gunner);

            // Act & Assert
            Assert.Equal(2, battle.GetSpawnedAttackerCountByTier(DefenderRole.Grunt));
            Assert.Equal(1, battle.GetSpawnedAttackerCountByTier(DefenderRole.Gunner));
            Assert.Equal(0, battle.GetSpawnedAttackerCountByTier(DefenderRole.Rifleman));
        }

        [Fact]
        public void ZoneBattle_GetSpawnedDefenderCountByTier_ShouldCountCorrectly()
        {
            // Arrange
            var battle = CreateBattle();
            battle.RegisterSpawnedDefender(201, DefenderRole.Rifleman);
            battle.RegisterSpawnedDefender(202, DefenderRole.Rifleman);
            battle.RegisterSpawnedDefender(203, DefenderRole.Grunt);

            // Act & Assert
            Assert.Equal(1, battle.GetSpawnedDefenderCountByTier(DefenderRole.Grunt));
            Assert.Equal(0, battle.GetSpawnedDefenderCountByTier(DefenderRole.Gunner));
            Assert.Equal(2, battle.GetSpawnedDefenderCountByTier(DefenderRole.Rifleman));
        }

        #endregion
    }
}
