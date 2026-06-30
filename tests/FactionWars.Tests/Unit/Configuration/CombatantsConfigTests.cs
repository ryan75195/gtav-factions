using FactionWars.Configuration;
using Newtonsoft.Json;
using Xunit;

namespace FactionWars.Tests.Unit.Configuration
{
    public class CombatantsConfigTests
    {
        [Fact]
        public void Default_Enemies_ReproduceCurrentRoleValues()
        {
            var c = new GameConfig().Combatants.Enemies;
            Assert.Equal(500, c.Rifleman.Health);
            Assert.Equal(200, c.Rifleman.Armor);
            Assert.Equal(0.60f, c.Rifleman.Accuracy, 2);
            Assert.Equal("WEAPON_CARBINERIFLE", c.Rifleman.Weapon);
            Assert.Equal(1.0f, c.Rifleman.DamageMultiplier, 2);
            Assert.Equal(0.25f, c.Grunt.Accuracy, 2);
            Assert.Equal(0.70f, c.Sniper.Accuracy, 2);
        }

        [Fact]
        public void Default_AllCategories_StartIdentical()
        {
            var cfg = new GameConfig().Combatants;
            Assert.Equal(cfg.Enemies.Gunner.Health, cfg.Squad.Gunner.Health);
            Assert.Equal(cfg.Enemies.Gunner.Health, cfg.Friendlies.Gunner.Health);
        }

        [Fact]
        public void Default_Player_IsVanilla()
        {
            var p = new GameConfig().Combatants.Player;
            Assert.Equal(200, p.MaxHealth);
            Assert.Equal(0, p.SpawnArmor);
            Assert.Equal(1.0f, p.OutgoingDamageMultiplier, 2);
            Assert.Equal(1.0f, p.IncomingDamageMultiplier, 2);
        }

        [Fact]
        public void RoundTrip_PreservesPerCategoryOverride()
        {
            var cfg = new GameConfig();
            cfg.Combatants.Enemies.Rifleman.Accuracy = 0.4f;
            var json = JsonConvert.SerializeObject(cfg);
            var back = JsonConvert.DeserializeObject<GameConfig>(json)!;
            Assert.Equal(0.4f, back.Combatants.Enemies.Rifleman.Accuracy, 2);
            Assert.Equal(0.6f, back.Combatants.Squad.Rifleman.Accuracy, 2);
        }
    }
}
