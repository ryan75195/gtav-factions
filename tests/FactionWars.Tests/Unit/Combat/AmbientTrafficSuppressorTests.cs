using System.Collections.Generic;
using FactionWars.Combat.Interfaces;
using FactionWars.Combat.Models;
using FactionWars.Combat.Services;
using Xunit;

namespace FactionWars.Tests.Unit.Combat
{
    public class AmbientTrafficSuppressorTests
    {
        private readonly IAmbientTrafficSuppressor _suppressor = new AmbientTrafficSuppressor();

        [Fact]
        public void EvictsAmbientCarWithDriver()
        {
            var vehicles = new List<VehicleSnapshot> { new VehicleSnapshot(10, false, 99) };
            Assert.Equal(new[] { 99 }, _suppressor.SelectDriversToEvict(vehicles, playerVehicleHandle: -1));
        }

        [Fact]
        public void SkipsPersistentCar()
        {
            var vehicles = new List<VehicleSnapshot> { new VehicleSnapshot(10, true, 99) };
            Assert.Empty(_suppressor.SelectDriversToEvict(vehicles, -1));
        }

        [Fact]
        public void SkipsDriverlessCar()
        {
            var vehicles = new List<VehicleSnapshot> { new VehicleSnapshot(10, false, -1) };
            Assert.Empty(_suppressor.SelectDriversToEvict(vehicles, -1));
        }

        [Fact]
        public void SkipsPlayerVehicle()
        {
            var vehicles = new List<VehicleSnapshot> { new VehicleSnapshot(10, false, 99) };
            Assert.Empty(_suppressor.SelectDriversToEvict(vehicles, playerVehicleHandle: 10));
        }

        [Fact]
        public void ReturnsAllQualifyingDrivers()
        {
            var vehicles = new List<VehicleSnapshot>
            {
                new VehicleSnapshot(10, false, 1),
                new VehicleSnapshot(11, true, 2),   // persistent -> skip
                new VehicleSnapshot(12, false, 3),
            };
            Assert.Equal(new[] { 1, 3 }, _suppressor.SelectDriversToEvict(vehicles, -1));
        }
    }
}
