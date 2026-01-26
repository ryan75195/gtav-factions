using FactionWars.AI.Interfaces;
using FactionWars.AI.Models;
using FactionWars.AI.Services;
using Xunit;

namespace FactionWars.Tests.Unit.AI.Services
{
    /// <summary>
    /// Tests for the VehicleThreatService.
    /// The vehicle threat service classifies vehicles by threat level and determines
    /// the required RPG response for AI troop purchasing decisions.
    /// </summary>
    public class VehicleThreatServiceTests
    {
        #region Constructor Tests

        [Fact]
        public void Constructor_CreatesInstance()
        {
            var service = new VehicleThreatService();

            Assert.NotNull(service);
        }

        [Fact]
        public void Constructor_ImplementsIVehicleThreatService()
        {
            var service = new VehicleThreatService();

            Assert.IsAssignableFrom<IVehicleThreatService>(service);
        }

        #endregion

        #region GetThreatLevel - None Threat Tests

        [Fact]
        public void GetThreatLevel_Bati_ReturnsNone()
        {
            var service = new VehicleThreatService();

            var result = service.GetThreatLevel("bati");

            Assert.Equal(VehicleThreatLevel.None, result);
        }

        [Fact]
        public void GetThreatLevel_BatiUpperCase_ReturnsNone()
        {
            var service = new VehicleThreatService();

            var result = service.GetThreatLevel("BATI");

            Assert.Equal(VehicleThreatLevel.None, result);
        }

        [Fact]
        public void GetThreatLevel_BatiMixedCase_ReturnsNone()
        {
            var service = new VehicleThreatService();

            var result = service.GetThreatLevel("BaTi");

            Assert.Equal(VehicleThreatLevel.None, result);
        }

        [Fact]
        public void GetThreatLevel_UnknownVehicle_ReturnsNone()
        {
            var service = new VehicleThreatService();

            var result = service.GetThreatLevel("unknownmodel123");

            Assert.Equal(VehicleThreatLevel.None, result);
        }

        [Fact]
        public void GetThreatLevel_EmptyString_ReturnsNone()
        {
            var service = new VehicleThreatService();

            var result = service.GetThreatLevel("");

            Assert.Equal(VehicleThreatLevel.None, result);
        }

        [Fact]
        public void GetThreatLevel_NullString_ReturnsNone()
        {
            var service = new VehicleThreatService();

            var result = service.GetThreatLevel(null!);

            Assert.Equal(VehicleThreatLevel.None, result);
        }

        #endregion

        #region GetThreatLevel - Light Threat Tests

        [Fact]
        public void GetThreatLevel_Technical_ReturnsLight()
        {
            var service = new VehicleThreatService();

            var result = service.GetThreatLevel("technical");

            Assert.Equal(VehicleThreatLevel.Light, result);
        }

        [Fact]
        public void GetThreatLevel_TechnicalUpperCase_ReturnsLight()
        {
            var service = new VehicleThreatService();

            var result = service.GetThreatLevel("TECHNICAL");

            Assert.Equal(VehicleThreatLevel.Light, result);
        }

        [Fact]
        public void GetThreatLevel_Zentorno_ReturnsLight()
        {
            var service = new VehicleThreatService();

            var result = service.GetThreatLevel("zentorno");

            Assert.Equal(VehicleThreatLevel.Light, result);
        }

        [Fact]
        public void GetThreatLevel_ZentornoUpperCase_ReturnsLight()
        {
            var service = new VehicleThreatService();

            var result = service.GetThreatLevel("ZENTORNO");

            Assert.Equal(VehicleThreatLevel.Light, result);
        }

        #endregion

        #region GetThreatLevel - Heavy Threat Tests

        [Fact]
        public void GetThreatLevel_Insurgent_ReturnsHeavy()
        {
            var service = new VehicleThreatService();

            var result = service.GetThreatLevel("insurgent");

            Assert.Equal(VehicleThreatLevel.Heavy, result);
        }

        [Fact]
        public void GetThreatLevel_InsurgentUpperCase_ReturnsHeavy()
        {
            var service = new VehicleThreatService();

            var result = service.GetThreatLevel("INSURGENT");

            Assert.Equal(VehicleThreatLevel.Heavy, result);
        }

        [Fact]
        public void GetThreatLevel_Apc_ReturnsHeavy()
        {
            var service = new VehicleThreatService();

            var result = service.GetThreatLevel("apc");

            Assert.Equal(VehicleThreatLevel.Heavy, result);
        }

        [Fact]
        public void GetThreatLevel_ApcUpperCase_ReturnsHeavy()
        {
            var service = new VehicleThreatService();

            var result = service.GetThreatLevel("APC");

            Assert.Equal(VehicleThreatLevel.Heavy, result);
        }

        [Fact]
        public void GetThreatLevel_Buzzard_ReturnsHeavy()
        {
            var service = new VehicleThreatService();

            var result = service.GetThreatLevel("buzzard");

            Assert.Equal(VehicleThreatLevel.Heavy, result);
        }

        [Fact]
        public void GetThreatLevel_BuzzardUpperCase_ReturnsHeavy()
        {
            var service = new VehicleThreatService();

            var result = service.GetThreatLevel("BUZZARD");

            Assert.Equal(VehicleThreatLevel.Heavy, result);
        }

        [Fact]
        public void GetThreatLevel_Khanjali_ReturnsHeavy()
        {
            var service = new VehicleThreatService();

            var result = service.GetThreatLevel("khanjali");

            Assert.Equal(VehicleThreatLevel.Heavy, result);
        }

        [Fact]
        public void GetThreatLevel_KhanjaliUpperCase_ReturnsHeavy()
        {
            var service = new VehicleThreatService();

            var result = service.GetThreatLevel("KHANJALI");

            Assert.Equal(VehicleThreatLevel.Heavy, result);
        }

        #endregion

        #region GetRequiredRpgCount Tests

        [Fact]
        public void GetRequiredRpgCount_None_ReturnsZero()
        {
            var service = new VehicleThreatService();

            var result = service.GetRequiredRpgCount(VehicleThreatLevel.None);

            Assert.Equal(0, result);
        }

        [Fact]
        public void GetRequiredRpgCount_Light_ReturnsOne()
        {
            var service = new VehicleThreatService();

            var result = service.GetRequiredRpgCount(VehicleThreatLevel.Light);

            Assert.Equal(1, result);
        }

        [Fact]
        public void GetRequiredRpgCount_Heavy_ReturnsTwo()
        {
            var service = new VehicleThreatService();

            var result = service.GetRequiredRpgCount(VehicleThreatLevel.Heavy);

            Assert.Equal(2, result);
        }

        #endregion

        #region Integration Tests

        [Theory]
        [InlineData("bati", VehicleThreatLevel.None, 0)]
        [InlineData("technical", VehicleThreatLevel.Light, 1)]
        [InlineData("zentorno", VehicleThreatLevel.Light, 1)]
        [InlineData("insurgent", VehicleThreatLevel.Heavy, 2)]
        [InlineData("apc", VehicleThreatLevel.Heavy, 2)]
        [InlineData("buzzard", VehicleThreatLevel.Heavy, 2)]
        [InlineData("khanjali", VehicleThreatLevel.Heavy, 2)]
        [InlineData("unknowncar", VehicleThreatLevel.None, 0)]
        public void GetThreatLevelAndRpgCount_AllVehicles_ReturnsExpectedValues(
            string vehicleModel,
            VehicleThreatLevel expectedThreatLevel,
            int expectedRpgCount)
        {
            var service = new VehicleThreatService();

            var threatLevel = service.GetThreatLevel(vehicleModel);
            var rpgCount = service.GetRequiredRpgCount(threatLevel);

            Assert.Equal(expectedThreatLevel, threatLevel);
            Assert.Equal(expectedRpgCount, rpgCount);
        }

        [Theory]
        [InlineData("BATI")]
        [InlineData("Bati")]
        [InlineData("bATI")]
        [InlineData("bati")]
        public void GetThreatLevel_CaseInsensitive_BatiVariations(string vehicleModel)
        {
            var service = new VehicleThreatService();

            var result = service.GetThreatLevel(vehicleModel);

            Assert.Equal(VehicleThreatLevel.None, result);
        }

        [Theory]
        [InlineData("INSURGENT")]
        [InlineData("Insurgent")]
        [InlineData("iNSURGENT")]
        [InlineData("insurgent")]
        public void GetThreatLevel_CaseInsensitive_InsurgentVariations(string vehicleModel)
        {
            var service = new VehicleThreatService();

            var result = service.GetThreatLevel(vehicleModel);

            Assert.Equal(VehicleThreatLevel.Heavy, result);
        }

        #endregion
    }
}
