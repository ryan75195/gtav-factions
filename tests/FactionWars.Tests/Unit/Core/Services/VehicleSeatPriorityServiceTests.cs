using FactionWars.Core.Interfaces;
using FactionWars.Core.Services;
using FactionWars.Core.Utils;
using Xunit;

namespace FactionWars.Tests.Unit.Core.Services
{
    public class VehicleSeatPriorityServiceTests
    {
        private readonly MockGameBridge _gameBridge;
        private readonly VehicleSeatPriorityService _service;

        public VehicleSeatPriorityServiceTests()
        {
            _gameBridge = new MockGameBridge();
            _service = new VehicleSeatPriorityService(_gameBridge);
        }

        [Fact]
        public void GetPrioritizedFreeSeats_TurretVehicle_TurretSeatFirst()
        {
            // Arrange - Technical with turret at seat 2
            var vehicle = _gameBridge.CreateVehicle("technical", Vector3.Zero);
            _gameBridge.SetVehicleClass(vehicle, 19); // Military
            _gameBridge.SetSeatAsTurret(vehicle, 2);
            // Seats 1, 2, 3 are free (0 is driver)

            // Act
            var seats = _service.GetPrioritizedFreeSeats(vehicle);

            // Assert - turret seat (2) should be first
            Assert.Equal(2, seats[0]);
        }

        [Fact]
        public void GetPrioritizedFreeSeats_Helicopter_BackSeatsFirst()
        {
            // Arrange - Helicopter (class 15) with 4 seats
            var vehicle = _gameBridge.CreateVehicle("buzzard", Vector3.Zero);
            _gameBridge.SetVehicleClass(vehicle, 15);
            // Free seats: 1 (front passenger), 2 (back left), 3 (back right)

            // Act
            var seats = _service.GetPrioritizedFreeSeats(vehicle);

            // Assert - back seats (2, 3) should come before front (1)
            // The first seat should be a back seat (index > 1)
            Assert.True(seats.Length > 0);
            Assert.True(seats[0] > 1 || seats.Length == 1);
        }

        [Fact]
        public void GetPrioritizedFreeSeats_RegularCar_BackSeatsFirst()
        {
            // Arrange - Regular car (class 1 = Sedan) with 4 seats
            var vehicle = _gameBridge.CreateVehicle("sultan", Vector3.Zero);
            _gameBridge.SetVehicleClass(vehicle, 1);
            // Free seats: 1, 2, 3

            // Act
            var seats = _service.GetPrioritizedFreeSeats(vehicle);

            // Assert - back seats (2, 3) should come before front (1)
            Assert.True(seats.Length > 0);
            Assert.True(seats[0] > 1 || seats.Length == 1);
        }

        [Fact]
        public void FilterFollowersByProximity_ReturnsOnlyNearbyFollowers()
        {
            // Arrange
            var vehicle = _gameBridge.CreateVehicle("car", new Vector3(100, 100, 0));
            _gameBridge.SetVehiclePosition(vehicle, new Vector3(100, 100, 0));

            var nearPed = _gameBridge.CreatePed("ped1", new Vector3(105, 100, 0)); // 5m away
            var farPed = _gameBridge.CreatePed("ped2", new Vector3(200, 100, 0));  // 100m away

            // Act
            var nearby = _service.FilterFollowersByProximity(
                new[] { nearPed, farPed }, vehicle, 15f);

            // Assert
            Assert.Single(nearby);
            Assert.Equal(nearPed, nearby[0]);
        }

        [Fact]
        public void FilterFollowersByProximity_PreservesOrder()
        {
            // Arrange
            var vehicle = _gameBridge.CreateVehicle("car", new Vector3(100, 100, 0));
            _gameBridge.SetVehiclePosition(vehicle, new Vector3(100, 100, 0));

            var ped1 = _gameBridge.CreatePed("ped1", new Vector3(105, 100, 0)); // 5m
            var ped2 = _gameBridge.CreatePed("ped2", new Vector3(110, 100, 0)); // 10m
            var ped3 = _gameBridge.CreatePed("ped3", new Vector3(112, 100, 0)); // 12m

            // Act
            var nearby = _service.FilterFollowersByProximity(
                new[] { ped1, ped2, ped3 }, vehicle, 15f);

            // Assert - order preserved
            Assert.Equal(3, nearby.Length);
            Assert.Equal(ped1, nearby[0]);
            Assert.Equal(ped2, nearby[1]);
            Assert.Equal(ped3, nearby[2]);
        }

        [Fact]
        public void FilterFollowersByProximity_EmptyArray_ReturnsEmpty()
        {
            // Arrange
            var vehicle = _gameBridge.CreateVehicle("car", new Vector3(100, 100, 0));
            _gameBridge.SetVehiclePosition(vehicle, new Vector3(100, 100, 0));

            // Act
            var nearby = _service.FilterFollowersByProximity(
                new int[0], vehicle, 15f);

            // Assert
            Assert.Empty(nearby);
        }

        [Fact]
        public void GetPrioritizedFreeSeats_InvalidVehicle_ReturnsEmpty()
        {
            // Act
            var seats = _service.GetPrioritizedFreeSeats(9999);

            // Assert
            Assert.Empty(seats);
        }
    }
}
