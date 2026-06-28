using FactionWars.Core.Interfaces;
using FactionWars.Core.Utils;
using Xunit;

namespace FactionWars.Tests.Unit.Core.Utils
{
    public class MockGameBridgeObservabilityTests
    {
        private readonly MockGameBridge _bridge = new MockGameBridge();

        private int NewPed() => _bridge.CreatePed("test", new Vector3(0f, 0f, 0f));

        [Fact]
        public void IsPedShooting_DefaultsFalse()
        {
            Assert.False(_bridge.IsPedShooting(NewPed()));
        }

        [Fact]
        public void SetPedShooting_TogglesIsPedShooting()
        {
            var ped = NewPed();
            _bridge.SetPedShooting(ped, true);
            Assert.True(_bridge.IsPedShooting(ped));
            _bridge.SetPedShooting(ped, false);
            Assert.False(_bridge.IsPedShooting(ped));
        }

        [Fact]
        public void GetSelectedWeapon_ReturnsGivenWeaponUpperCased()
        {
            var ped = NewPed();
            _bridge.GivePedWeapon(ped, "weapon_pistol");
            Assert.Equal("WEAPON_PISTOL", _bridge.GetSelectedWeapon(ped));
        }

        [Fact]
        public void GetSelectedWeapon_PrefersActiveWeapon()
        {
            var ped = NewPed();
            _bridge.GivePedWeapon(ped, "weapon_sniperrifle");
            _bridge.SetPedActiveWeapon(ped, "weapon_pistol");
            Assert.Equal("WEAPON_PISTOL", _bridge.GetSelectedWeapon(ped));
        }

        [Fact]
        public void GetSelectedWeapon_UnknownPed_ReturnsEmpty()
        {
            Assert.Equal("", _bridge.GetSelectedWeapon(999));
        }

        [Fact]
        public void GetPedAmmo_DefaultsToMinusOne()
        {
            Assert.Equal(-1, _bridge.GetPedAmmo(NewPed()));
        }

        [Fact]
        public void SetPedAmmo_IsReadBack()
        {
            var ped = NewPed();
            _bridge.SetPedAmmo(ped, 30);
            Assert.Equal(30, _bridge.GetPedAmmo(ped));
        }

        [Fact]
        public void GetPedCombatAbilityValue_ReflectsProfile()
        {
            var ped = NewPed();
            _bridge.SetPedCombatProfile(ped, 2, -1, -1);
            Assert.Equal(2, _bridge.GetPedCombatAbilityValue(ped));
        }

        [Fact]
        public void GetPedCombatAbilityValue_UnknownPed_ReturnsMinusOne()
        {
            Assert.Equal(-1, _bridge.GetPedCombatAbilityValue(999));
        }

        // --- Mock calibration: combat tasks drive in-combat state (matches real game) ---

        [Fact]
        public void TaskCombatPed_PutsPedInCombat()
        {
            var ped = NewPed();
            _bridge.TaskCombatPed(ped, NewPed());
            Assert.True(_bridge.IsPedInCombat(ped));
        }

        [Fact]
        public void TaskCombatHatedTargetsAroundPed_PutsPedInCombat()
        {
            var ped = NewPed();
            _bridge.TaskCombatHatedTargetsAroundPed(ped, 50f);
            Assert.True(_bridge.IsPedInCombat(ped));
        }

        [Fact]
        public void ClearPedTasks_ClearsInCombat()
        {
            var ped = NewPed();
            _bridge.TaskCombatPed(ped, NewPed());
            _bridge.ClearPedTasks(ped);
            Assert.False(_bridge.IsPedInCombat(ped));
        }
    }
}
