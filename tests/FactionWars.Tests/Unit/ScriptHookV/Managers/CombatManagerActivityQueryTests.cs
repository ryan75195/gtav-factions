using System;
using System.Collections.Generic;
using FactionWars.Combat.Interfaces;
using FactionWars.Combat.Models;
using FactionWars.Core.Models;
using FactionWars.ScriptHookV;
using Xunit;

namespace FactionWars.Tests.Unit.ScriptHookV.Managers
{
    /// <summary>
    /// Tests that <see cref="ZoneBattleCombatActivityAdapter"/> correctly exposes
    /// <see cref="ICombatActivityQuery.HasActiveEncounter"/> via the ZoneBattleManager.
    /// </summary>
    public class CombatManagerActivityQueryTests
    {
        [Fact]
        public void HasActiveEncounter_WhenNoPlayerBattle_ReturnsFalse()
        {
            var battleManager = new FakeZoneBattleManager(playerInBattle: false);
            ICombatActivityQuery query = new ZoneBattleCombatActivityAdapter(battleManager);

            Assert.False(query.HasActiveEncounter);
        }

        [Fact]
        public void HasActiveEncounter_WhenPlayerIsInBattle_ReturnsTrue()
        {
            var battleManager = new FakeZoneBattleManager(playerInBattle: true);
            ICombatActivityQuery query = new ZoneBattleCombatActivityAdapter(battleManager);

            Assert.True(query.HasActiveEncounter);
        }

        // Minimal IZoneBattleManager stub — only IsPlayerInBattle() matters for this test.
        private sealed class FakeZoneBattleManager : IZoneBattleManager
        {
            private readonly bool _playerInBattle;
            public FakeZoneBattleManager(bool playerInBattle) { _playerInBattle = playerInBattle; }

            public bool IsPlayerInBattle() => _playerInBattle;
            public ZoneBattle? GetPlayerCurrentBattle() => null;

            // ---- stubs for the rest of the interface ----
            public int BattleCount => 0;
            public ZoneBattle StartBattle(string zoneId, string attackerFactionId, string defenderFactionId, Dictionary<DefenderTier, int> attackerTroops, Dictionary<DefenderTier, int> defenderTroops) => throw new NotImplementedException();
            public void EndBattle(string zoneId, BattleOutcome outcome) { }
            public void OnPlayerEnteredZone(FactionWars.Territory.Models.Zone zone) { }
            public void OnPlayerExitedZone(FactionWars.Territory.Models.Zone zone) { }
            public void SetPlayerFaction(string? factionId) { }
            public void Tick(float deltaTime) { }
            public void ReportTroopKilled(string zoneId, string factionId, DefenderTier tier) { }
            public ZoneBattle? GetBattleForZone(string zoneId) => null;
            public IReadOnlyList<ZoneBattle> GetAllActiveBattles() => Array.Empty<ZoneBattle>();
            public bool RemoveParticipant(string zoneId, string factionId) => false;
            public ZoneBattle? StartPlayerCombat(FactionWars.Territory.Models.Zone zone, string playerFactionId, Func<int> aliveCountCallback) => null;
            public bool JoinAsAttacker(string zoneId, string factionId, bool isPlayer, Func<int>? aliveCountCallback, Dictionary<DefenderTier, int>? troops) => false;

            public event Action<ZoneBattle>? BattleStarted { add { } remove { } }
            public event Action<ZoneBattle, BattleOutcome>? BattleEnded { add { } remove { } }
            public event Action<ZoneBattle, DefenderTier, string>? TroopKilled { add { } remove { } }
        }
    }
}
