using FactionWars.Configuration;
using FactionWars.Core.Interfaces;
using FactionWars.Core.Models;
using FactionWars.Core.Services;
using Xunit;

namespace FactionWars.Tests.Unit.Core
{
    public class CombatantStatsProviderTests
    {
        private static ICombatantStatsProvider Default()
            => CombatantStatsProviderFactory.Create(new CombatantsConfig());

        [Fact]
        public void GetRoleStats_DefaultEnemyRifleman_MatchesCurrentValues()
        {
            var s = Default().GetRoleStats(CombatantCategory.Enemies, DefenderRole.Rifleman);
            Assert.Equal(500, s.Health);
            Assert.Equal(200, s.Armor);
            Assert.Equal(0.60f, s.Accuracy, 2);
            Assert.Equal("WEAPON_CARBINERIFLE", s.Weapon);
            Assert.Equal(1.0f, s.DamageMultiplier, 2);
        }

        [Fact]
        public void GetRoleStats_ReadsPerCategoryOverride()
        {
            var cfg = new CombatantsConfig();
            cfg.Friendlies.Sniper.DamageMultiplier = 8.0f;
            var provider = CombatantStatsProviderFactory.Create(cfg);
            var s = provider.GetRoleStats(CombatantCategory.Friendlies, DefenderRole.Sniper);
            Assert.Equal(8.0f, s.DamageMultiplier, 2);
            Assert.Equal(1.0f, Default().GetRoleStats(CombatantCategory.Enemies, DefenderRole.Sniper).DamageMultiplier, 2);
        }

        [Fact]
        public void GetRoleStats_PlayerCategory_Throws()
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(
                () => Default().GetRoleStats(CombatantCategory.Player, DefenderRole.Rifleman));
        }

        [Fact]
        public void GetPlayerStats_DefaultIsVanilla()
        {
            var p = Default().GetPlayerStats();
            Assert.Equal(200, p.MaxHealth);
            Assert.Equal(0, p.SpawnArmor);
            Assert.Equal(1.0f, p.OutgoingDamageMultiplier, 2);
            Assert.Equal(1.0f, p.IncomingDamageMultiplier, 2);
        }
    }
}
