using FactionWars.Core.Interfaces;
using FactionWars.Core.Models;
using FactionWars.Core.Utils;
using FactionWars.ScriptHookV;
using Xunit;

namespace FactionWars.Tests.Unit.ScriptHookV
{
    public class ServiceContainerFactoryCombatantStatsTests
    {
        [Fact]
        public void Create_RegistersCombatantStatsProvider_WithDefaultEnemyValues()
        {
            var container = ServiceContainerFactory.Create(new MockGameBridge());
            var provider = container.Resolve<ICombatantStatsProvider>();
            var s = provider.GetRoleStats(CombatantCategory.Enemies, DefenderRole.Rifleman);
            Assert.Equal(500, s.Health);
            Assert.Equal(0.60f, s.Accuracy, 2);
        }
    }
}