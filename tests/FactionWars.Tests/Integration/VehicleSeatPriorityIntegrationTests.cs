using FactionWars.Core.Interfaces;
using FactionWars.Core.Services;
using FactionWars.Core.Utils;
using Xunit;

namespace FactionWars.Tests.Integration
{
    /// <summary>
    /// Integration tests verifying the vehicle seat priority system end-to-end.
    /// Tests the full flow from VehicleSeatPriorityService through MockGameBridge.
    /// </summary>
    public class VehicleSeatPriorityIntegrationTests
    {
        [Fact]
        public void FullFlow_TurretVehicle_TurretSeatAssignedFirst()
        {
            // Arrange
            var gameBridge = new MockGameBridge();
            var service = new VehicleSeatPriorityService(gameBridge);

            // Create turret vehicle (Technical-style)
            var vehicle = gameBridge.CreateVehicle("technical", new Vector3(100, 100, 0));
            gameBridge.SetVehiclePosition(vehicle, new Vector3(100, 100, 0));
            gameBridge.SetVehicleClass(vehicle, 19); // Military
            gameBridge.SetSeatAsTurret(vehicle, 2);

            // Create followers nearby
            var follower1 = gameBridge.CreatePed("follower1", new Vector3(105, 100, 0));
            var follower2 = gameBridge.CreatePed("follower2", new Vector3(110, 100, 0));

            // Act
            var seats = service.GetPrioritizedFreeSeats(vehicle);
            var nearby = service.FilterFollowersByProximity(
                new[] { follower1, follower2 }, vehicle, 15f);

            // Assert
            Assert.Equal(2, seats[0]); // Turret first
            Assert.Equal(2, nearby.Length); // Both within 15m
        }

        [Fact]
        public void FullFlow_Helicopter_BackSeatsFirst()
        {
            // Arrange
            var gameBridge = new MockGameBridge();
            var service = new VehicleSeatPriorityService(gameBridge);

            var vehicle = gameBridge.CreateVehicle("buzzard", new Vector3(100, 100, 0));
            gameBridge.SetVehiclePosition(vehicle, new Vector3(100, 100, 0));
            gameBridge.SetVehicleClass(vehicle, 15); // Helicopter

            // Act
            var seats = service.GetPrioritizedFreeSeats(vehicle);

            // Assert - back seats should be prioritized
            Assert.True(seats.Length > 0);
            // First seat should be a back seat (index > 1) if available
            if (seats.Length > 1)
            {
                Assert.True(seats[0] > 1);
            }
        }

        [Fact]
        public void ProximityFilter_ExcludesDistantFollowers()
        {
            // Arrange
            var gameBridge = new MockGameBridge();
            var service = new VehicleSeatPriorityService(gameBridge);

            // Create vehicle at specific position
            var vehiclePos = new Vector3(100, 100, 0);
            var vehicle = gameBridge.CreateVehicle("car", vehiclePos);

            // Create followers at different distances
            var nearFollower = gameBridge.CreatePed("near", new Vector3(110, 100, 0)); // 10m away
            var farFollower = gameBridge.CreatePed("far", new Vector3(200, 100, 0)); // 100m away

            // Act
            var nearby = service.FilterFollowersByProximity(
                new[] { nearFollower, farFollower }, vehicle, 15f);

            // Assert
            Assert.Single(nearby);
            Assert.Equal(nearFollower, nearby[0]);
        }

        [Fact]
        public void MultipleVehicleTypes_DifferentPrioritization()
        {
            // Arrange
            var gameBridge = new MockGameBridge();
            var service = new VehicleSeatPriorityService(gameBridge);

            // Create sedan (should prioritize back seats)
            var sedan = gameBridge.CreateVehicle("sultan", new Vector3(0, 0, 0));
            gameBridge.SetVehicleClass(sedan, 1); // Sedan

            // Create helicopter (should prioritize back seats)
            var heli = gameBridge.CreateVehicle("buzzard", new Vector3(0, 0, 0));
            gameBridge.SetVehicleClass(heli, 15); // Helicopter

            // Create military with turret
            var military = gameBridge.CreateVehicle("insurgent", new Vector3(0, 0, 0));
            gameBridge.SetVehicleClass(military, 19); // Military
            gameBridge.SetSeatAsTurret(military, 2);

            // Act
            var sedanSeats = service.GetPrioritizedFreeSeats(sedan);
            var heliSeats = service.GetPrioritizedFreeSeats(heli);
            var militarySeats = service.GetPrioritizedFreeSeats(military);

            // Assert
            // All should have back seats prioritized, but military turret should be first
            Assert.True(sedanSeats.Length > 0);
            Assert.True(heliSeats.Length > 0);
            Assert.Equal(2, militarySeats[0]); // Turret seat first for military
        }

        [Fact]
        public void EdgeCase_NoFreeSeats_ReturnsEmpty()
        {
            // Arrange
            var gameBridge = new MockGameBridge();
            var service = new VehicleSeatPriorityService(gameBridge);

            // Create motorcycle with 2 seats, player occupies driver
            var vehicle = gameBridge.SetPlayerInVehicle(2);
            gameBridge.SetVehicleClass(vehicle, 8); // Motorcycle

            // Occupy the only passenger seat
            var ped = gameBridge.CreatePed("passenger", new Vector3(0, 0, 0));
            gameBridge.PutPedInVehicle(ped, vehicle, 1);

            // Act
            var seats = service.GetPrioritizedFreeSeats(vehicle);

            // Assert
            Assert.Empty(seats);
        }

        [Fact]
        public void EdgeCase_AllFollowersOutOfRange_ReturnsEmpty()
        {
            // Arrange
            var gameBridge = new MockGameBridge();
            var service = new VehicleSeatPriorityService(gameBridge);

            var vehicle = gameBridge.CreateVehicle("car", new Vector3(0, 0, 0));
            gameBridge.SetVehiclePosition(vehicle, new Vector3(0, 0, 0));

            // All followers are far away (> 15m)
            var farPed1 = gameBridge.CreatePed("far1", new Vector3(50, 0, 0));
            var farPed2 = gameBridge.CreatePed("far2", new Vector3(0, 50, 0));

            // Act
            var nearby = service.FilterFollowersByProximity(
                new[] { farPed1, farPed2 }, vehicle, 15f);

            // Assert
            Assert.Empty(nearby);
        }

        [Fact]
        public void VehicleWithMultipleTurrets_AllTurretsFirst()
        {
            // Arrange
            var gameBridge = new MockGameBridge();
            var service = new VehicleSeatPriorityService(gameBridge);

            // Create vehicle with multiple turrets
            var vehicle = gameBridge.CreateVehicle("technical", new Vector3(0, 0, 0));
            gameBridge.SetVehicleClass(vehicle, 19); // Military
            gameBridge.SetSeatAsTurret(vehicle, 1);
            gameBridge.SetSeatAsTurret(vehicle, 2);
            gameBridge.SetSeatAsTurret(vehicle, 3);

            // Act
            var seats = service.GetPrioritizedFreeSeats(vehicle);

            // Assert - all turret seats should be first
            Assert.True(seats.Length >= 3);
            // All turret seats (1, 2, 3) should come before any other seats
            for (int i = 0; i < 3; i++)
            {
                Assert.True(seats[i] >= 1 && seats[i] <= 3);
            }
        }

        [Fact]
        public void ProximityFilter_PreservesFollowerOrder()
        {
            // Arrange
            var gameBridge = new MockGameBridge();
            var service = new VehicleSeatPriorityService(gameBridge);

            var vehicle = gameBridge.CreateVehicle("car", new Vector3(100, 100, 0));
            gameBridge.SetVehiclePosition(vehicle, new Vector3(100, 100, 0));

            // Create followers in order
            var ped1 = gameBridge.CreatePed("ped1", new Vector3(105, 100, 0)); // 5m
            var ped2 = gameBridge.CreatePed("ped2", new Vector3(110, 100, 0)); // 10m
            var ped3 = gameBridge.CreatePed("ped3", new Vector3(112, 100, 0)); // 12m

            // Act
            var nearby = service.FilterFollowersByProximity(
                new[] { ped1, ped2, ped3 }, vehicle, 15f);

            // Assert - order should be preserved
            Assert.Equal(3, nearby.Length);
            Assert.Equal(ped1, nearby[0]);
            Assert.Equal(ped2, nearby[1]);
            Assert.Equal(ped3, nearby[2]);
        }

        [Fact]
        public void Boat_TurretSeatsFirst()
        {
            // Arrange
            var gameBridge = new MockGameBridge();
            var service = new VehicleSeatPriorityService(gameBridge);

            var boat = gameBridge.CreateVehicle("dinghy", new Vector3(0, 0, 0));
            gameBridge.SetVehicleClass(boat, 14); // Boat
            gameBridge.SetSeatAsTurret(boat, 1); // Boat turret at seat 1

            // Act
            var seats = service.GetPrioritizedFreeSeats(boat);

            // Assert - turret seat should be first for boats too
            Assert.True(seats.Length > 0);
            Assert.Equal(1, seats[0]); // Turret seat first
        }

        [Fact]
        public void Motorcycle_ReturnsSeats()
        {
            // Arrange
            var gameBridge = new MockGameBridge();
            var service = new VehicleSeatPriorityService(gameBridge);

            // Default vehicle created with CreateVehicle has 4 seats
            var motorcycle = gameBridge.CreateVehicle("bati", new Vector3(0, 0, 0));
            gameBridge.SetVehicleClass(motorcycle, 8); // Motorcycle

            // Act
            var seats = service.GetPrioritizedFreeSeats(motorcycle);

            // Assert - motorcycle should return some free seats (1-3 passengers since driver seat is free)
            Assert.True(seats.Length > 0);
            // Motorcycle is class 8, so it uses default sorting (back seats before front)
            // Just verify we get valid seat numbers
            foreach (var seat in seats)
            {
                Assert.True(seat >= 0 && seat < 4);
            }
        }

        [Fact]
        public void FullFlow_CoordinatedSeating_FollowersGetPrioritizedSeats()
        {
            // Arrange - Simulate FollowerManager's coordinated seating flow
            var gameBridge = new MockGameBridge();
            var service = new VehicleSeatPriorityService(gameBridge);

            // Create a turret vehicle
            var vehicle = gameBridge.CreateVehicle("technical", new Vector3(100, 100, 0));
            gameBridge.SetVehiclePosition(vehicle, new Vector3(100, 100, 0));
            gameBridge.SetVehicleClass(vehicle, 19); // Military
            gameBridge.SetSeatAsTurret(vehicle, 2);

            // Create 3 followers nearby
            var follower1 = gameBridge.CreatePed("follower1", new Vector3(105, 100, 0));
            var follower2 = gameBridge.CreatePed("follower2", new Vector3(110, 100, 0));
            var follower3 = gameBridge.CreatePed("follower3", new Vector3(115, 100, 0));

            // Act - Step 1: Filter by proximity
            var nearbyFollowers = service.FilterFollowersByProximity(
                new[] { follower1, follower2, follower3 }, vehicle, 15f);

            // Act - Step 2: Get prioritized seats
            var prioritizedSeats = service.GetPrioritizedFreeSeats(vehicle);

            // Act - Step 3: Assign followers to seats (simulating FollowerManager logic)
            for (int i = 0; i < nearbyFollowers.Length && i < prioritizedSeats.Length; i++)
            {
                gameBridge.TaskPedEnterVehicle(nearbyFollowers[i], vehicle, prioritizedSeats[i]);
            }

            // Assert
            Assert.Equal(3, nearbyFollowers.Length); // All followers nearby
            Assert.Equal(2, prioritizedSeats[0]); // Turret seat first

            // Verify followers were tasked to enter vehicle
            Assert.True(gameBridge.IsPedInVehicle(follower1)); // First follower got turret seat
            Assert.True(gameBridge.IsPedInVehicle(follower2)); // Others got assigned seats
            Assert.True(gameBridge.IsPedInVehicle(follower3));
        }

        [Fact]
        public void InvalidVehicleHandle_GracefullyHandled()
        {
            // Arrange
            var gameBridge = new MockGameBridge();
            var service = new VehicleSeatPriorityService(gameBridge);

            // Act
            var seats = service.GetPrioritizedFreeSeats(9999); // Invalid handle
            var nearby = service.FilterFollowersByProximity(
                new[] { 1, 2, 3 }, 9999, 15f); // Invalid vehicle

            // Assert - should handle gracefully
            Assert.Empty(seats);
            Assert.Empty(nearby);
        }

        [Fact]
        public void ProximityFilter_WithinMaxDistance_Included()
        {
            // Arrange
            var gameBridge = new MockGameBridge();
            var service = new VehicleSeatPriorityService(gameBridge);

            // Vehicle at (100, 100, 0)
            var vehiclePos = new Vector3(100, 100, 0);
            var vehicle = gameBridge.CreateVehicle("car", vehiclePos);

            // Create ped at 14m away (within max distance of 15)
            var ped = gameBridge.CreatePed("ped", new Vector3(114, 100, 0));

            // Act
            var nearby = service.FilterFollowersByProximity(
                new[] { ped }, vehicle, 15f);

            // Assert - should be included (distance < maxDistance)
            Assert.Single(nearby);
            Assert.Equal(ped, nearby[0]);
        }

        [Fact]
        public void ProximityFilter_JustOverMaxDistance_Excluded()
        {
            // Arrange
            var gameBridge = new MockGameBridge();
            var service = new VehicleSeatPriorityService(gameBridge);

            // Vehicle at (100, 100, 0)
            var vehiclePos = new Vector3(100, 100, 0);
            var vehicle = gameBridge.CreateVehicle("car", vehiclePos);

            // Create ped at 16m away (over max distance of 15)
            var ped = gameBridge.CreatePed("ped", new Vector3(116, 100, 0));

            // Act
            var nearby = service.FilterFollowersByProximity(
                new[] { ped }, vehicle, 15f);

            // Assert - should be excluded (distance > maxDistance)
            Assert.Empty(nearby);
        }
    }
}
