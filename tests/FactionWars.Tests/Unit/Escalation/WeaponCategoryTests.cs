using Xunit;
using FactionWars.Escalation.Models;

namespace FactionWars.Tests.Unit.Escalation
{
    /// <summary>
    /// Tests for the WeaponCategory enum which categorizes weapons by type.
    /// Each category contains different weapons that fit a similar role in combat.
    /// </summary>
    public class WeaponCategoryTests
    {
        [Fact]
        public void WeaponCategory_HasPistolValue()
        {
            var category = WeaponCategory.Pistol;

            Assert.Equal(0, (int)category);
        }

        [Fact]
        public void WeaponCategory_HasSMGValue()
        {
            var category = WeaponCategory.SMG;

            Assert.Equal(1, (int)category);
        }

        [Fact]
        public void WeaponCategory_HasShotgunValue()
        {
            var category = WeaponCategory.Shotgun;

            Assert.Equal(2, (int)category);
        }

        [Fact]
        public void WeaponCategory_HasAssaultRifleValue()
        {
            var category = WeaponCategory.AssaultRifle;

            Assert.Equal(3, (int)category);
        }

        [Fact]
        public void WeaponCategory_HasLMGValue()
        {
            var category = WeaponCategory.LMG;

            Assert.Equal(4, (int)category);
        }

        [Fact]
        public void WeaponCategory_HasSniperValue()
        {
            var category = WeaponCategory.Sniper;

            Assert.Equal(5, (int)category);
        }

        [Fact]
        public void WeaponCategory_HasHeavyValue()
        {
            var category = WeaponCategory.Heavy;

            Assert.Equal(6, (int)category);
        }

        [Fact]
        public void WeaponCategory_HasMeleeValue()
        {
            var category = WeaponCategory.Melee;

            Assert.Equal(7, (int)category);
        }

        [Fact]
        public void WeaponCategory_HasThrownValue()
        {
            var category = WeaponCategory.Thrown;

            Assert.Equal(8, (int)category);
        }

        [Fact]
        public void WeaponCategory_HasNineValues()
        {
            var values = System.Enum.GetValues(typeof(WeaponCategory));

            Assert.Equal(9, values.Length);
        }
    }
}
