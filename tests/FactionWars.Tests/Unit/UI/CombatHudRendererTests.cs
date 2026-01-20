using Xunit;
using FactionWars.UI.Models;
using FactionWars.ScriptHookV.UI;
using System;

namespace FactionWars.Tests.Unit.UI
{
    public class CombatHudRendererTests
    {
        [Fact]
        public void IsVisible_InitiallyFalse()
        {
            var renderer = new CombatHudRenderer();

            Assert.False(renderer.IsVisible);
        }

        [Fact]
        public void RenderCombatHud_SetsIsVisibleTrue()
        {
            var renderer = new CombatHudRenderer();
            var data = CreateValidCombatHudData();

            renderer.RenderCombatHud(data);

            Assert.True(renderer.IsVisible);
        }

        [Fact]
        public void RenderCombatHud_WithNullData_ThrowsArgumentNullException()
        {
            var renderer = new CombatHudRenderer();

            Assert.Throws<ArgumentNullException>(() => renderer.RenderCombatHud(null!));
        }

        [Fact]
        public void HideCombatHud_SetsIsVisibleFalse()
        {
            var renderer = new CombatHudRenderer();
            var data = CreateValidCombatHudData();

            renderer.RenderCombatHud(data);
            Assert.True(renderer.IsVisible);

            renderer.HideCombatHud();

            Assert.False(renderer.IsVisible);
        }

        [Fact]
        public void HideCombatHud_WhenAlreadyHidden_RemainsHidden()
        {
            var renderer = new CombatHudRenderer();

            renderer.HideCombatHud();

            Assert.False(renderer.IsVisible);
        }

        [Fact]
        public void CurrentData_IsNull_WhenNotRendering()
        {
            var renderer = new CombatHudRenderer();

            Assert.Null(renderer.CurrentData);
        }

        [Fact]
        public void CurrentData_StoresProvidedData()
        {
            var renderer = new CombatHudRenderer();
            var data = CreateValidCombatHudData();

            renderer.RenderCombatHud(data);

            Assert.Same(data, renderer.CurrentData);
        }

        [Fact]
        public void CurrentData_ClearedOnHide()
        {
            var renderer = new CombatHudRenderer();
            var data = CreateValidCombatHudData();

            renderer.RenderCombatHud(data);
            renderer.HideCombatHud();

            Assert.Null(renderer.CurrentData);
        }

        [Fact]
        public void RenderCombatHud_UpdatesCurrentData()
        {
            var renderer = new CombatHudRenderer();
            var data1 = CreateValidCombatHudData(attackerPedCount: 5);
            var data2 = CreateValidCombatHudData(attackerPedCount: 10);

            renderer.RenderCombatHud(data1);
            Assert.Equal(5, renderer.CurrentData?.AttackerPedCount);

            renderer.RenderCombatHud(data2);
            Assert.Equal(10, renderer.CurrentData?.AttackerPedCount);
        }

        private static CombatHudData CreateValidCombatHudData(
            string zoneId = "zone1",
            string zoneName = "Downtown",
            string attackerFactionId = "michael",
            string defenderFactionId = "trevor",
            float attackerControlPercent = 35f,
            float defenderControlPercent = 65f,
            int attackerPedCount = 5,
            int defenderPedCount = 8,
            float reinforcementCooldownSeconds = 0f,
            bool isPlayerAttacker = true)
        {
            return new CombatHudData(
                zoneId: zoneId,
                zoneName: zoneName,
                attackerFactionId: attackerFactionId,
                defenderFactionId: defenderFactionId,
                attackerControlPercent: attackerControlPercent,
                defenderControlPercent: defenderControlPercent,
                attackerPedCount: attackerPedCount,
                defenderPedCount: defenderPedCount,
                reinforcementCooldownSeconds: reinforcementCooldownSeconds,
                isPlayerAttacker: isPlayerAttacker,
                combatDuration: TimeSpan.FromMinutes(1));
        }
    }
}
