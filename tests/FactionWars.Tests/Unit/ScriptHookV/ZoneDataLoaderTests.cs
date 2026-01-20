using FactionWars.Core.Interfaces;
using FactionWars.ScriptHookV.Data;
using FactionWars.Territory.Interfaces;
using FactionWars.Territory.Models;
using FactionWars.Territory.Repositories;
using System;
using System.IO;
using Xunit;

namespace FactionWars.Tests.Unit.ScriptHookV
{
    /// <summary>
    /// Tests for ZoneDataLoader which loads zone definitions into the repository.
    /// </summary>
    public class ZoneDataLoaderTests
    {
        private readonly IZoneRepository _repository;

        public ZoneDataLoaderTests()
        {
            _repository = new InMemoryZoneRepository();
        }

        #region Constructor Tests

        [Fact]
        public void Constructor_ShouldThrowOnNullRepository()
        {
            Assert.Throws<ArgumentNullException>(() => new ZoneDataLoader(null!));
        }

        #endregion

        #region LoadDefaultZones Tests

        [Fact]
        public void LoadDefaultZones_ShouldLoad31Zones()
        {
            // Arrange
            var loader = new ZoneDataLoader(_repository);

            // Act
            loader.LoadDefaultZones();

            // Assert
            Assert.Equal(31, _repository.Count);
        }

        [Fact]
        public void LoadDefaultZones_ShouldLoadZonesWithValidIds()
        {
            // Arrange
            var loader = new ZoneDataLoader(_repository);

            // Act
            loader.LoadDefaultZones();

            // Assert
            foreach (var zone in _repository.GetAll())
            {
                Assert.False(string.IsNullOrWhiteSpace(zone.Id));
            }
        }

        [Fact]
        public void LoadDefaultZones_ShouldLoadZonesWithValidNames()
        {
            // Arrange
            var loader = new ZoneDataLoader(_repository);

            // Act
            loader.LoadDefaultZones();

            // Assert
            foreach (var zone in _repository.GetAll())
            {
                Assert.False(string.IsNullOrEmpty(zone.Name));
            }
        }

        [Fact]
        public void LoadDefaultZones_ShouldLoadZonesWithValidCoordinates()
        {
            // Arrange
            var loader = new ZoneDataLoader(_repository);

            // Act
            loader.LoadDefaultZones();

            // Assert
            foreach (var zone in _repository.GetAll())
            {
                // GTA V map coordinates are typically in range -3500 to 4500 for X/Y
                Assert.InRange(zone.Center.X, -4000f, 5000f);
                Assert.InRange(zone.Center.Y, -4000f, 8000f);
            }
        }

        [Fact]
        public void LoadDefaultZones_ShouldLoadZonesWithPositiveRadius()
        {
            // Arrange
            var loader = new ZoneDataLoader(_repository);

            // Act
            loader.LoadDefaultZones();

            // Assert
            foreach (var zone in _repository.GetAll())
            {
                Assert.True(zone.Radius > 0);
            }
        }

        [Fact]
        public void LoadDefaultZones_ShouldLoadZonesWithValidStrategicValue()
        {
            // Arrange
            var loader = new ZoneDataLoader(_repository);

            // Act
            loader.LoadDefaultZones();

            // Assert
            foreach (var zone in _repository.GetAll())
            {
                Assert.InRange(zone.StrategicValue, 1, 10);
            }
        }

        [Fact]
        public void LoadDefaultZones_ShouldNotLoadDuplicateIds()
        {
            // Arrange
            var loader = new ZoneDataLoader(_repository);

            // Act
            loader.LoadDefaultZones();

            // Assert - getting by ID should work for all zones
            var zones = _repository.GetAll();
            var ids = new System.Collections.Generic.HashSet<string>();
            foreach (var zone in zones)
            {
                Assert.True(ids.Add(zone.Id), $"Duplicate zone ID: {zone.Id}");
            }
        }

        [Fact]
        public void LoadDefaultZones_CalledTwice_ShouldThrow()
        {
            // Arrange
            var loader = new ZoneDataLoader(_repository);
            loader.LoadDefaultZones();

            // Act & Assert - loading again should fail because zones already exist
            Assert.Throws<InvalidOperationException>(() => loader.LoadDefaultZones());
        }

        #endregion

        #region LoadFromJson Tests

        [Fact]
        public void LoadFromJson_ShouldLoadZonesFromValidJson()
        {
            // Arrange
            var loader = new ZoneDataLoader(_repository);
            var json = @"[
                {
                    ""id"": ""test_zone_1"",
                    ""name"": ""Test Zone"",
                    ""centerX"": 100.0,
                    ""centerY"": 200.0,
                    ""centerZ"": 10.0,
                    ""radius"": 150.0,
                    ""strategicValue"": 5,
                    ""traits"": ""Commercial""
                }
            ]";

            // Act
            loader.LoadFromJson(json);

            // Assert
            Assert.Equal(1, _repository.Count);
            var zone = _repository.GetById("test_zone_1");
            Assert.NotNull(zone);
            Assert.Equal("Test Zone", zone!.Name);
            Assert.Equal(100f, zone.Center.X);
            Assert.Equal(200f, zone.Center.Y);
            Assert.Equal(10f, zone.Center.Z);
            Assert.Equal(150f, zone.Radius);
            Assert.Equal(5, zone.StrategicValue);
            Assert.Equal(ZoneTrait.Commercial, zone.Traits);
        }

        [Fact]
        public void LoadFromJson_ShouldLoadMultipleZones()
        {
            // Arrange
            var loader = new ZoneDataLoader(_repository);
            var json = @"[
                {
                    ""id"": ""zone_1"",
                    ""name"": ""Zone One"",
                    ""centerX"": 100.0,
                    ""centerY"": 100.0,
                    ""centerZ"": 0.0,
                    ""radius"": 100.0,
                    ""strategicValue"": 3
                },
                {
                    ""id"": ""zone_2"",
                    ""name"": ""Zone Two"",
                    ""centerX"": 200.0,
                    ""centerY"": 200.0,
                    ""centerZ"": 0.0,
                    ""radius"": 100.0,
                    ""strategicValue"": 5
                }
            ]";

            // Act
            loader.LoadFromJson(json);

            // Assert
            Assert.Equal(2, _repository.Count);
            Assert.NotNull(_repository.GetById("zone_1"));
            Assert.NotNull(_repository.GetById("zone_2"));
        }

        [Fact]
        public void LoadFromJson_WithCombinedTraits_ShouldParseCorrectly()
        {
            // Arrange
            var loader = new ZoneDataLoader(_repository);
            var json = @"[
                {
                    ""id"": ""multi_trait_zone"",
                    ""name"": ""Multi Trait Zone"",
                    ""centerX"": 100.0,
                    ""centerY"": 100.0,
                    ""centerZ"": 0.0,
                    ""radius"": 100.0,
                    ""strategicValue"": 5,
                    ""traits"": ""Industrial, Commercial""
                }
            ]";

            // Act
            loader.LoadFromJson(json);

            // Assert
            var zone = _repository.GetById("multi_trait_zone");
            Assert.NotNull(zone);
            Assert.True(zone!.Traits.HasFlag(ZoneTrait.Industrial));
            Assert.True(zone.Traits.HasFlag(ZoneTrait.Commercial));
        }

        [Fact]
        public void LoadFromJson_WithNoTraits_ShouldDefaultToNone()
        {
            // Arrange
            var loader = new ZoneDataLoader(_repository);
            var json = @"[
                {
                    ""id"": ""no_trait_zone"",
                    ""name"": ""No Trait Zone"",
                    ""centerX"": 100.0,
                    ""centerY"": 100.0,
                    ""centerZ"": 0.0,
                    ""radius"": 100.0,
                    ""strategicValue"": 1
                }
            ]";

            // Act
            loader.LoadFromJson(json);

            // Assert
            var zone = _repository.GetById("no_trait_zone");
            Assert.NotNull(zone);
            Assert.Equal(ZoneTrait.None, zone!.Traits);
        }

        [Fact]
        public void LoadFromJson_ShouldThrowOnNullJson()
        {
            // Arrange
            var loader = new ZoneDataLoader(_repository);

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => loader.LoadFromJson(null!));
        }

        [Fact]
        public void LoadFromJson_ShouldThrowOnEmptyJson()
        {
            // Arrange
            var loader = new ZoneDataLoader(_repository);

            // Act & Assert
            Assert.Throws<ArgumentException>(() => loader.LoadFromJson(""));
        }

        [Fact]
        public void LoadFromJson_ShouldThrowOnInvalidJson()
        {
            // Arrange
            var loader = new ZoneDataLoader(_repository);

            // Act & Assert
            Assert.ThrowsAny<Exception>(() => loader.LoadFromJson("not valid json"));
        }

        #endregion

        #region Known Zone Tests

        [Fact]
        public void LoadDefaultZones_ShouldIncludeDowntown()
        {
            // Arrange
            var loader = new ZoneDataLoader(_repository);

            // Act
            loader.LoadDefaultZones();

            // Assert - downtown is a key location
            var downtown = _repository.GetById("downtown");
            Assert.NotNull(downtown);
            Assert.Equal("Downtown", downtown!.Name);
        }

        [Fact]
        public void LoadDefaultZones_ShouldIncludeSandyShores()
        {
            // Arrange
            var loader = new ZoneDataLoader(_repository);

            // Act
            loader.LoadDefaultZones();

            // Assert - Sandy Shores is Trevor's territory
            var sandyShores = _repository.GetById("sandy_shores");
            Assert.NotNull(sandyShores);
        }

        [Fact]
        public void LoadDefaultZones_ShouldIncludeVinewood()
        {
            // Arrange
            var loader = new ZoneDataLoader(_repository);

            // Act
            loader.LoadDefaultZones();

            // Assert
            var vinewood = _repository.GetById("vinewood");
            Assert.NotNull(vinewood);
        }

        #endregion
    }
}
