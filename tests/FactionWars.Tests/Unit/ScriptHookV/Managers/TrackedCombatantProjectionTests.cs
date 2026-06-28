using System.Collections.Generic;
using System.Linq;
using FactionWars.Core.Models;
using FactionWars.ScriptHookV.Managers;
using FactionWars.Telemetry.Models;
using Xunit;

namespace FactionWars.Tests.Unit.ScriptHookV.Managers
{
    public class TrackedCombatantProjectionTests
    {
        [Fact]
        public void FromTierMap_FlattensAllZonesWithKindAndRole()
        {
            var map = new Dictionary<string, Dictionary<int, DefenderRole>>
            {
                ["zoneA"] = new Dictionary<int, DefenderRole> { [10] = DefenderRole.Sniper, [11] = DefenderRole.Grunt },
                ["zoneB"] = new Dictionary<int, DefenderRole> { [20] = DefenderRole.Rocketeer }
            };

            var result = TrackedCombatantProjection.FromTierMap(map, CombatantKind.EnemyDefender);

            Assert.Equal(3, result.Count);
            Assert.All(result, c => Assert.Equal(CombatantKind.EnemyDefender, c.Kind));
            Assert.Contains(result, c => c.Handle == 10 && c.Role == DefenderRole.Sniper);
            Assert.Contains(result, c => c.Handle == 11 && c.Role == DefenderRole.Grunt);
            Assert.Contains(result, c => c.Handle == 20 && c.Role == DefenderRole.Rocketeer);
        }

        [Fact]
        public void FromTierMap_EmptyMap_ReturnsEmpty()
        {
            var map = new Dictionary<string, Dictionary<int, DefenderRole>>();

            var result = TrackedCombatantProjection.FromTierMap(map, CombatantKind.FriendlyDefender);

            Assert.Empty(result);
        }

        [Fact]
        public void FromTierMap_PropagatesGivenKind()
        {
            var map = new Dictionary<string, Dictionary<int, DefenderRole>>
            {
                ["z"] = new Dictionary<int, DefenderRole> { [1] = DefenderRole.Gunner }
            };

            var result = TrackedCombatantProjection.FromTierMap(map, CombatantKind.BattleAttacker);

            Assert.Equal(CombatantKind.BattleAttacker, result.Single().Kind);
        }
    }
}
