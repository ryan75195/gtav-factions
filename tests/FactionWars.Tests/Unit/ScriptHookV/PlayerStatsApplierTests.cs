using FactionWars.Configuration;
using FactionWars.Core.Utils;
using FactionWars.ScriptHookV.Managers;
using Xunit;

namespace FactionWars.Tests.Unit.ScriptHookV
{
    public class PlayerStatsApplierTests
    {
        [Fact]
        public void Apply_PushesConfiguredPlayerStatsToBridge()
        {
            var cfg = new CombatantsConfig();
            cfg.Player.MaxHealth = 600;
            cfg.Player.IncomingDamageMultiplier = 0.5f;
            var bridge = new MockGameBridge();
            new PlayerStatsApplier(bridge, CombatantStatsProviderFactory.Create(cfg)).Apply();
            Assert.Equal(600, bridge.GetPlayerMaxHealthForTest());
            Assert.Equal(0.5f, bridge.GetPlayerWeaponDefenseModifierForTest(), 2);
        }
    }
}
