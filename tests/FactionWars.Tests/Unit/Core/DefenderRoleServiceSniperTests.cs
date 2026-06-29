using FactionWars.Core.Models;
using FactionWars.Core.Services;
using Xunit;

namespace FactionWars.Tests.Unit.Core
{
    public class DefenderRoleServiceSniperTests
    {
        private readonly DefenderRoleService _service = new DefenderRoleService();

        [Fact]
        public void GetRoleConfig_Sniper_HasSpecialistStats()
        {
            var config = _service.GetRoleConfig(DefenderRole.Sniper);

            Assert.Equal(DefenderRole.Sniper, config.Role);
            Assert.Equal(1500, config.Cost);
            Assert.Equal(275, config.Health);
            Assert.Equal(50, config.Armor);
            Assert.Equal("WEAPON_SNIPERRIFLE", config.Weapon);
            Assert.Equal(0.7f, config.Accuracy);
            Assert.False(config.RagdollEnabled);
        }

        [Fact]
        public void GetAllRoleConfigs_IncludesFiveRoles()
        {
            Assert.Equal(5, _service.GetAllRoleConfigs().Count);
        }
    }
}
