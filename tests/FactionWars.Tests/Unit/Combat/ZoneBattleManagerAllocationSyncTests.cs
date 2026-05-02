using System.Collections.Generic;
using FactionWars.Combat.Services;
using FactionWars.Core.Interfaces;
using FactionWars.Core.Models;
using FactionWars.Factions.Interfaces;
using FactionWars.Factions.Models;
using Moq;
using Xunit;

namespace FactionWars.Tests.Unit.Combat
{
    /// <summary>
    /// When a battle simulates kills (player not streaming the zone), losses must
    /// flow back to source-of-truth state so we don't get phantom troops on the next
    /// player visit:
    /// - Defender losses decrement the defending faction's ZoneDefenderAllocation
    /// - Attacker losses decrement the attacking faction's reserve troops
    /// </summary>
    public class ZoneBattleManagerAllocationSyncTests
    {
        private const string AttackerFaction = "trevor";
        private const string DefenderFaction = "michael";
        private const string ZoneId = "zone_vinewood";

        private readonly Mock<IZoneDefenderAllocationService> _allocationService = new Mock<IZoneDefenderAllocationService>();
        private readonly Mock<IFactionService> _factionService = new Mock<IFactionService>();

        private readonly ZoneDefenderAllocation _defenderAllocation;
        private readonly FactionState _attackerState;

        public ZoneBattleManagerAllocationSyncTests()
        {
            // Large pools so the simulator produces kills on both sides before either is wiped.
            _defenderAllocation = BuildAllocation(DefenderFaction, ZoneId, basic: 100);
            _attackerState = BuildFactionStateWithReserve(AttackerFaction, basic: 100);

            _allocationService
                .Setup(s => s.GetAllocation(DefenderFaction, ZoneId))
                .Returns(_defenderAllocation);
            _factionService
                .Setup(s => s.GetFactionState(AttackerFaction))
                .Returns(_attackerState);
        }

        private static ZoneDefenderAllocation BuildAllocation(string factionId, string zoneId, int basic)
        {
            var allocation = new ZoneDefenderAllocation(factionId, zoneId);
            allocation.AddTroops(DefenderTier.Basic, basic);
            return allocation;
        }

        private static FactionState BuildFactionStateWithReserve(string factionId, int basic)
        {
            var state = new FactionState(factionId);
            state.AddReserveTroops(DefenderTier.Basic, basic);
            return state;
        }

        private ZoneBattleManager BuildManager()
        {
            return new ZoneBattleManager(_allocationService.Object, _factionService.Object, playerFactionId: null);
        }

        private static Dictionary<DefenderTier, int> Troops(int basic)
            => new Dictionary<DefenderTier, int> { { DefenderTier.Basic, basic } };

        [Fact]
        public void SimulatedKills_DecrementSourceOfTruthState()
        {
            // 100 vs 100, no player presence: simulator runs and kills accumulate on
            // both sides. After N ticks, the per-side kill count should match the
            // decrement applied to allocation (defenders) and reserve (attackers).
            var manager = BuildManager();
            var battle = manager.StartBattle(ZoneId, AttackerFaction, DefenderFaction,
                attackerTroops: Troops(100), defenderTroops: Troops(100));

            int defenderKills = 0;
            int attackerKills = 0;
            manager.TroopKilled += (b, tier, side) =>
            {
                if (side == "defender") defenderKills++;
                else if (side == "attacker") attackerKills++;
            };

            // Force 30 simulator ticks. Each AdvanceTime + Tick call is one ProcessKill
            // when the kill timer is past zero.
            for (int i = 0; i < 30; i++)
            {
                battle.AdvanceTime(battle.TimeUntilNextKill + 0.01f);
                manager.Tick(0.0f);
            }

            // Sanity: simulator did produce kills on both sides given the even matchup.
            Assert.True(defenderKills > 0, "Expected at least one defender kill");
            Assert.True(attackerKills > 0, "Expected at least one attacker kill");

            // Each defender kill decrements ZoneDefenderAllocation by 1.
            Assert.Equal(100 - defenderKills, _defenderAllocation.GetTroopCount(DefenderTier.Basic));

            // Each attacker kill decrements the attacking faction's reserve by 1.
            Assert.Equal(100 - attackerKills, _attackerState.GetReserveTroops(DefenderTier.Basic));
        }
    }
}
