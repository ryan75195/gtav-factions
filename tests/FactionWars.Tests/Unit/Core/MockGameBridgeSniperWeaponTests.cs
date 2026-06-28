using FactionWars.Core.Interfaces;
using FactionWars.Core.Utils;
using Xunit;

namespace FactionWars.Tests.Unit.Core
{
    public class MockGameBridgeSniperWeaponTests
    {
        [Fact]
        public void SetPedActiveWeapon_RecordsLastWeapon()
        {
            var bridge = new MockGameBridge();
            int ped = bridge.CreatePed("s_m_y_blackops_03", new Vector3(0f, 0f, 0f));

            bridge.SetPedActiveWeapon(ped, "weapon_pistol");
            Assert.Equal("weapon_pistol", bridge.GetPedActiveWeapon(ped));

            bridge.SetPedActiveWeapon(ped, "WEAPON_SNIPERRIFLE");
            Assert.Equal("WEAPON_SNIPERRIFLE", bridge.GetPedActiveWeapon(ped));
        }
    }
}
