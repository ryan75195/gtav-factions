using FactionWars.Core.Utils;
using Xunit;

namespace FactionWars.Tests.Unit.Core
{
    public class MockGameBridgeCombatStatsTests
    {
        [Fact]
        public void SetPedWeaponDamageModifier_IsTracked()
        {
            var b = new MockGameBridge();
            b.SetPedWeaponDamageModifier(42, 8f);
            Assert.Equal(8f, b.GetPedWeaponDamageModifierForTest(42), 2);
        }

        [Fact]
        public void PlayerStatSetters_AreTracked()
        {
            var b = new MockGameBridge();
            b.SetPlayerMaxHealth(600);
            b.SetPlayerWeaponDamageModifier(1.5f);
            b.SetPlayerWeaponDefenseModifier(0.5f);
            Assert.Equal(600, b.GetPlayerMaxHealthForTest());
            Assert.Equal(1.5f, b.GetPlayerWeaponDamageModifierForTest(), 2);
            Assert.Equal(0.5f, b.GetPlayerWeaponDefenseModifierForTest(), 2);
        }
    }
}
