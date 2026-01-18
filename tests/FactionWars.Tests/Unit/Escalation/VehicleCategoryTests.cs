using Xunit;
using FactionWars.Escalation.Models;

namespace FactionWars.Tests.Unit.Escalation
{
    /// <summary>
    /// Tests for the VehicleCategory enum which categorizes vehicles by type.
    /// Each category contains different vehicles that fit a similar role.
    /// </summary>
    public class VehicleCategoryTests
    {
        [Fact]
        public void VehicleCategory_HasCompactValue()
        {
            var category = VehicleCategory.Compact;

            Assert.Equal(0, (int)category);
        }

        [Fact]
        public void VehicleCategory_HasSedanValue()
        {
            var category = VehicleCategory.Sedan;

            Assert.Equal(1, (int)category);
        }

        [Fact]
        public void VehicleCategory_HasSUVValue()
        {
            var category = VehicleCategory.SUV;

            Assert.Equal(2, (int)category);
        }

        [Fact]
        public void VehicleCategory_HasCoupeValue()
        {
            var category = VehicleCategory.Coupe;

            Assert.Equal(3, (int)category);
        }

        [Fact]
        public void VehicleCategory_HasMuscleValue()
        {
            var category = VehicleCategory.Muscle;

            Assert.Equal(4, (int)category);
        }

        [Fact]
        public void VehicleCategory_HasSportsValue()
        {
            var category = VehicleCategory.Sports;

            Assert.Equal(5, (int)category);
        }

        [Fact]
        public void VehicleCategory_HasMotorcycleValue()
        {
            var category = VehicleCategory.Motorcycle;

            Assert.Equal(6, (int)category);
        }

        [Fact]
        public void VehicleCategory_HasVanValue()
        {
            var category = VehicleCategory.Van;

            Assert.Equal(7, (int)category);
        }

        [Fact]
        public void VehicleCategory_HasArmoredValue()
        {
            var category = VehicleCategory.Armored;

            Assert.Equal(8, (int)category);
        }

        [Fact]
        public void VehicleCategory_HasMilitaryValue()
        {
            var category = VehicleCategory.Military;

            Assert.Equal(9, (int)category);
        }

        [Fact]
        public void VehicleCategory_HasTenValues()
        {
            var values = System.Enum.GetValues(typeof(VehicleCategory));

            Assert.Equal(10, values.Length);
        }
    }
}
