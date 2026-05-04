using System;
using System.Collections.Generic;
using System.Linq;
using FactionWars.Core.Interfaces;
using FactionWars.Core.Models;
using FactionWars.Core.Services;
using Xunit;

namespace FactionWars.Tests.Unit.Core
{
    /// <summary>
    /// Tests for IVictoryConditionService.
    /// The victory condition service detects when a faction has achieved total control (100% of zones)
    /// and provides information about victory state and progress.
    /// </summary>
    public class VictoryConditionServiceTests
    {
        #region Interface Tests

        [Fact]
        public void IVictoryConditionService_CheckVictoryCondition_IsDefinedWithCorrectSignature()
        {
            // This test verifies the interface method exists with correct signature
            // The interface should have a CheckVictoryCondition method that takes:
            // - factionId: string
            // And returns: VictoryCheckResult
            var interfaceType = typeof(IVictoryConditionService);
            var method = interfaceType.GetMethod("CheckVictoryCondition");

            Assert.NotNull(method);
            Assert.Equal(typeof(VictoryCheckResult), method!.ReturnType);

            var parameters = method.GetParameters();
            Assert.Equal(1, parameters.Length);
            Assert.Equal("factionId", parameters[0].Name);
            Assert.Equal(typeof(string), parameters[0].ParameterType);
        }

        [Fact]
        public void IVictoryConditionService_GetVictoryProgress_IsDefinedWithCorrectSignature()
        {
            // This test verifies the interface method exists with correct signature
            // The interface should have a GetVictoryProgress method that takes:
            // - factionId: string
            // And returns: float (percentage 0-100)
            var interfaceType = typeof(IVictoryConditionService);
            var method = interfaceType.GetMethod("GetVictoryProgress");

            Assert.NotNull(method);
            Assert.Equal(typeof(float), method!.ReturnType);

            var parameters = method.GetParameters();
            Assert.Equal(1, parameters.Length);
            Assert.Equal("factionId", parameters[0].Name);
            Assert.Equal(typeof(string), parameters[0].ParameterType);
        }

        [Fact]
        public void IVictoryConditionService_GetFactionZoneCount_IsDefinedWithCorrectSignature()
        {
            // This test verifies the interface method exists with correct signature
            // The interface should have a GetFactionZoneCount method that takes:
            // - factionId: string
            // And returns: int (number of zones owned)
            var interfaceType = typeof(IVictoryConditionService);
            var method = interfaceType.GetMethod("GetFactionZoneCount");

            Assert.NotNull(method);
            Assert.Equal(typeof(int), method!.ReturnType);

            var parameters = method.GetParameters();
            Assert.Equal(1, parameters.Length);
            Assert.Equal("factionId", parameters[0].Name);
            Assert.Equal(typeof(string), parameters[0].ParameterType);
        }

        [Fact]
        public void IVictoryConditionService_GetTotalZoneCount_IsDefinedWithCorrectSignature()
        {
            // This test verifies the interface method exists with correct signature
            // The interface should have a GetTotalZoneCount method that returns int
            var interfaceType = typeof(IVictoryConditionService);
            var method = interfaceType.GetMethod("GetTotalZoneCount");

            Assert.NotNull(method);
            Assert.Equal(typeof(int), method!.ReturnType);

            var parameters = method.GetParameters();
            Assert.Equal(0, parameters.Length);
        }

        [Fact]
        public void IVictoryConditionService_IsGameOver_IsDefinedWithCorrectSignature()
        {
            // This test verifies the interface method exists with correct signature
            // The interface should have an IsGameOver method that returns bool
            var interfaceType = typeof(IVictoryConditionService);
            var method = interfaceType.GetMethod("IsGameOver");

            Assert.NotNull(method);
            Assert.Equal(typeof(bool), method!.ReturnType);

            var parameters = method.GetParameters();
            Assert.Equal(0, parameters.Length);
        }

        [Fact]
        public void IVictoryConditionService_GetWinningFactionId_IsDefinedWithCorrectSignature()
        {
            // This test verifies the interface method exists with correct signature
            // The interface should have a GetWinningFactionId method that returns string? (null if no winner)
            var interfaceType = typeof(IVictoryConditionService);
            var method = interfaceType.GetMethod("GetWinningFactionId");

            Assert.NotNull(method);
            Assert.True(method!.ReturnType == typeof(string),
                $"Expected return type string (nullable), got {method.ReturnType.Name}");

            var parameters = method.GetParameters();
            Assert.Equal(0, parameters.Length);
        }

        #endregion

        #region VictoryCheckResult Model Tests

        [Fact]
        public void VictoryCheckResult_HasRequiredProperties()
        {
            // Verify VictoryCheckResult has all required properties
            var resultType = typeof(VictoryCheckResult);

            Assert.NotNull(resultType.GetProperty("FactionId"));
            Assert.NotNull(resultType.GetProperty("IsVictory"));
            Assert.NotNull(resultType.GetProperty("ZonesOwned"));
            Assert.NotNull(resultType.GetProperty("TotalZones"));
            Assert.NotNull(resultType.GetProperty("ControlPercentage"));
        }

        [Fact]
        public void VictoryCheckResult_Victory_CreatesVictoryResult()
        {
            // Act
            var result = VictoryCheckResult.Victory("faction_blue", zonesOwned: 31, totalZones: 31);

            // Assert
            Assert.Equal("faction_blue", result.FactionId);
            Assert.True(result.IsVictory);
            Assert.Equal(31, result.ZonesOwned);
            Assert.Equal(31, result.TotalZones);
            Assert.Equal(100f, result.ControlPercentage, 2);
        }

        [Fact]
        public void VictoryCheckResult_InProgress_CreatesNonVictoryResult()
        {
            // Act
            var result = VictoryCheckResult.InProgress("faction_orange", zonesOwned: 15, totalZones: 31);

            // Assert
            Assert.Equal("faction_orange", result.FactionId);
            Assert.False(result.IsVictory);
            Assert.Equal(15, result.ZonesOwned);
            Assert.Equal(31, result.TotalZones);
            Assert.True(result.ControlPercentage > 48f && result.ControlPercentage < 49f,
                $"Expected ~48.4% but got {result.ControlPercentage}");
        }

        [Fact]
        public void VictoryCheckResult_NullFactionId_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => VictoryCheckResult.Victory(null!, 31, 31));
            Assert.Throws<ArgumentNullException>(() => VictoryCheckResult.InProgress(null!, 15, 31));
        }

        [Fact]
        public void VictoryCheckResult_ZeroTotalZones_ThrowsArgumentOutOfRangeException()
        {
            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => VictoryCheckResult.Victory("faction_blue", 0, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => VictoryCheckResult.InProgress("faction_blue", 0, 0));
        }

        [Fact]
        public void VictoryCheckResult_NegativeZonesOwned_ThrowsArgumentOutOfRangeException()
        {
            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => VictoryCheckResult.Victory("faction_blue", -1, 31));
            Assert.Throws<ArgumentOutOfRangeException>(() => VictoryCheckResult.InProgress("faction_blue", -1, 31));
        }

        [Fact]
        public void VictoryCheckResult_ZonesOwnedExceedsTotal_ThrowsArgumentOutOfRangeException()
        {
            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => VictoryCheckResult.Victory("faction_blue", 32, 31));
            Assert.Throws<ArgumentOutOfRangeException>(() => VictoryCheckResult.InProgress("faction_blue", 50, 31));
        }

        #endregion

        #region Implementation Tests

        // Test helper for creating a mock zone service
        private static MockZoneService CreateMockZoneService(int totalZones, Dictionary<string, int>? zonesByFaction = null)
        {
            return new MockZoneService(totalZones, zonesByFaction ?? new Dictionary<string, int>());
        }

        [Fact]
        public void CheckVictoryCondition_NullFactionId_ThrowsArgumentNullException()
        {
            // Arrange
            var zoneService = CreateMockZoneService(31);
            var service = new VictoryConditionService(zoneService);

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => service.CheckVictoryCondition(null!));
        }

        [Fact]
        public void CheckVictoryCondition_FactionOwnsAllZones_ReturnsVictory()
        {
            // Arrange
            var zonesByFaction = new Dictionary<string, int>
            {
                { "faction_blue", 31 }
            };
            var zoneService = CreateMockZoneService(31, zonesByFaction);
            var service = new VictoryConditionService(zoneService);

            // Act
            var result = service.CheckVictoryCondition("faction_blue");

            // Assert
            Assert.True(result.IsVictory);
            Assert.Equal("faction_blue", result.FactionId);
            Assert.Equal(31, result.ZonesOwned);
            Assert.Equal(31, result.TotalZones);
            Assert.Equal(100f, result.ControlPercentage, 2);
        }

        [Fact]
        public void CheckVictoryCondition_FactionOwnsPartialZones_ReturnsInProgress()
        {
            // Arrange
            var zonesByFaction = new Dictionary<string, int>
            {
                { "faction_blue", 10 },
                { "faction_orange", 15 },
                { "faction_green", 6 }
            };
            var zoneService = CreateMockZoneService(31, zonesByFaction);
            var service = new VictoryConditionService(zoneService);

            // Act
            var result = service.CheckVictoryCondition("faction_blue");

            // Assert
            Assert.False(result.IsVictory);
            Assert.Equal("faction_blue", result.FactionId);
            Assert.Equal(10, result.ZonesOwned);
            Assert.Equal(31, result.TotalZones);
        }

        [Fact]
        public void CheckVictoryCondition_FactionOwnsNoZones_ReturnsZeroProgress()
        {
            // Arrange
            var zonesByFaction = new Dictionary<string, int>
            {
                { "faction_orange", 31 }
            };
            var zoneService = CreateMockZoneService(31, zonesByFaction);
            var service = new VictoryConditionService(zoneService);

            // Act
            var result = service.CheckVictoryCondition("faction_blue");

            // Assert
            Assert.False(result.IsVictory);
            Assert.Equal(0, result.ZonesOwned);
            Assert.Equal(0f, result.ControlPercentage, 2);
        }

        [Fact]
        public void GetVictoryProgress_NullFactionId_ThrowsArgumentNullException()
        {
            // Arrange
            var zoneService = CreateMockZoneService(31);
            var service = new VictoryConditionService(zoneService);

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => service.GetVictoryProgress(null!));
        }

        [Fact]
        public void GetVictoryProgress_FactionOwnsHalfZones_ReturnsApproximately50Percent()
        {
            // Arrange
            var zonesByFaction = new Dictionary<string, int>
            {
                { "faction_blue", 15 },
                { "faction_orange", 16 }
            };
            var zoneService = CreateMockZoneService(31, zonesByFaction);
            var service = new VictoryConditionService(zoneService);

            // Act
            var progress = service.GetVictoryProgress("faction_blue");

            // Assert
            Assert.True(progress > 48f && progress < 49f, $"Expected ~48.4% but got {progress}");
        }

        [Fact]
        public void GetVictoryProgress_FactionOwnsAllZones_Returns100()
        {
            // Arrange
            var zonesByFaction = new Dictionary<string, int>
            {
                { "faction_blue", 31 }
            };
            var zoneService = CreateMockZoneService(31, zonesByFaction);
            var service = new VictoryConditionService(zoneService);

            // Act
            var progress = service.GetVictoryProgress("faction_blue");

            // Assert
            Assert.Equal(100f, progress, 2);
        }

        [Fact]
        public void GetFactionZoneCount_NullFactionId_ThrowsArgumentNullException()
        {
            // Arrange
            var zoneService = CreateMockZoneService(31);
            var service = new VictoryConditionService(zoneService);

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => service.GetFactionZoneCount(null!));
        }

        [Fact]
        public void GetFactionZoneCount_ReturnsCorrectCount()
        {
            // Arrange
            var zonesByFaction = new Dictionary<string, int>
            {
                { "faction_blue", 8 },
                { "faction_orange", 10 },
                { "faction_green", 5 }
            };
            var zoneService = CreateMockZoneService(23, zonesByFaction);
            var service = new VictoryConditionService(zoneService);

            // Act & Assert
            Assert.Equal(8, service.GetFactionZoneCount("faction_blue"));
            Assert.Equal(10, service.GetFactionZoneCount("faction_orange"));
            Assert.Equal(5, service.GetFactionZoneCount("faction_green"));
        }

        [Fact]
        public void GetTotalZoneCount_ReturnsCorrectCount()
        {
            // Arrange
            var zoneService = CreateMockZoneService(31);
            var service = new VictoryConditionService(zoneService);

            // Act
            var total = service.GetTotalZoneCount();

            // Assert
            Assert.Equal(31, total);
        }

        [Fact]
        public void IsGameOver_NoFactionHasAllZones_ReturnsFalse()
        {
            // Arrange
            var zonesByFaction = new Dictionary<string, int>
            {
                { "faction_blue", 10 },
                { "faction_orange", 15 },
                { "faction_green", 6 }
            };
            var zoneService = CreateMockZoneService(31, zonesByFaction);
            var service = new VictoryConditionService(zoneService);

            // Act
            var isGameOver = service.IsGameOver();

            // Assert
            Assert.False(isGameOver);
        }

        [Fact]
        public void IsGameOver_OneFactionHasAllZones_ReturnsTrue()
        {
            // Arrange
            var zonesByFaction = new Dictionary<string, int>
            {
                { "faction_orange", 31 }
            };
            var zoneService = CreateMockZoneService(31, zonesByFaction);
            var service = new VictoryConditionService(zoneService);

            // Act
            var isGameOver = service.IsGameOver();

            // Assert
            Assert.True(isGameOver);
        }

        [Fact]
        public void GetWinningFactionId_NoWinner_ReturnsNull()
        {
            // Arrange
            var zonesByFaction = new Dictionary<string, int>
            {
                { "faction_blue", 10 },
                { "faction_orange", 15 },
                { "faction_green", 6 }
            };
            var zoneService = CreateMockZoneService(31, zonesByFaction);
            var service = new VictoryConditionService(zoneService);

            // Act
            var winner = service.GetWinningFactionId();

            // Assert
            Assert.Null(winner);
        }

        [Fact]
        public void GetWinningFactionId_FactionWins_ReturnsWinnerFactionId()
        {
            // Arrange
            var zonesByFaction = new Dictionary<string, int>
            {
                { "faction_green", 31 }
            };
            var zoneService = CreateMockZoneService(31, zonesByFaction);
            var service = new VictoryConditionService(zoneService);

            // Act
            var winner = service.GetWinningFactionId();

            // Assert
            Assert.Equal("faction_green", winner);
        }

        [Fact]
        public void Constructor_NullZoneService_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new VictoryConditionService(null!));
        }

        [Fact]
        public void IsGameOver_ZeroZones_ReturnsFalse()
        {
            // Arrange - Edge case: no zones exist
            var zoneService = CreateMockZoneService(0);
            var service = new VictoryConditionService(zoneService);

            // Act
            var isGameOver = service.IsGameOver();

            // Assert
            Assert.False(isGameOver);
        }

        [Fact]
        public void GetWinningFactionId_ZeroZones_ReturnsNull()
        {
            // Arrange - Edge case: no zones exist
            var zoneService = CreateMockZoneService(0);
            var service = new VictoryConditionService(zoneService);

            // Act
            var winner = service.GetWinningFactionId();

            // Assert
            Assert.Null(winner);
        }

        [Fact]
        public void GetVictoryProgress_ZeroZones_ReturnsZero()
        {
            // Arrange - Edge case: no zones exist
            var zoneService = CreateMockZoneService(0);
            var service = new VictoryConditionService(zoneService);

            // Act
            var progress = service.GetVictoryProgress("faction_blue");

            // Assert
            Assert.Equal(0f, progress, 2);
        }

        [Fact]
        public void CheckVictoryCondition_SingleZone_FactionOwns_ReturnsVictory()
        {
            // Arrange - Edge case: single zone, faction owns it
            var zonesByFaction = new Dictionary<string, int>
            {
                { "faction_blue", 1 }
            };
            var zoneService = CreateMockZoneService(1, zonesByFaction);
            var service = new VictoryConditionService(zoneService);

            // Act
            var result = service.CheckVictoryCondition("faction_blue");

            // Assert
            Assert.True(result.IsVictory);
            Assert.Equal(100f, result.ControlPercentage, 2);
        }

        [Fact]
        public void CheckVictoryCondition_SingleZone_FactionDoesNotOwn_ReturnsInProgress()
        {
            // Arrange - Edge case: single zone, faction doesn't own it
            var zonesByFaction = new Dictionary<string, int>
            {
                { "faction_orange", 1 }
            };
            var zoneService = CreateMockZoneService(1, zonesByFaction);
            var service = new VictoryConditionService(zoneService);

            // Act
            var result = service.CheckVictoryCondition("faction_blue");

            // Assert
            Assert.False(result.IsVictory);
            Assert.Equal(0f, result.ControlPercentage, 2);
        }

        [Fact]
        public void IsGameOver_SomeZonesNeutral_ReturnsFalse()
        {
            // Arrange - Some zones owned by one faction, some neutral
            var zonesByFaction = new Dictionary<string, int>
            {
                { "faction_blue", 25 }
            };
            var zoneService = CreateMockZoneService(31, zonesByFaction);
            var service = new VictoryConditionService(zoneService);

            // Act
            var isGameOver = service.IsGameOver();

            // Assert
            Assert.False(isGameOver);
        }

        [Fact]
        public void VictoryCheckResult_ToString_Victory_IncludesVictoryText()
        {
            // Arrange
            var result = VictoryCheckResult.Victory("faction_blue", 31, 31);

            // Act
            var text = result.ToString();

            // Assert
            Assert.Contains("VICTORY", text);
            Assert.Contains("faction_blue", text);
            Assert.Contains("31/31", text);
        }

        [Fact]
        public void VictoryCheckResult_ToString_InProgress_ShowsProgressInfo()
        {
            // Arrange
            var result = VictoryCheckResult.InProgress("faction_orange", 15, 31);

            // Act
            var text = result.ToString();

            // Assert
            Assert.DoesNotContain("VICTORY", text);
            Assert.Contains("faction_orange", text);
            Assert.Contains("15/31", text);
        }

        #endregion
    }

    /// <summary>
    /// Mock implementation of IZoneService for testing VictoryConditionService.
    /// </summary>
    internal class MockZoneService : FactionWars.Territory.Interfaces.IZoneService
    {
        private readonly int _totalZones;
        private readonly Dictionary<string, int> _zonesByFaction;

        public MockZoneService(int totalZones, Dictionary<string, int> zonesByFaction)
        {
            _totalZones = totalZones;
            _zonesByFaction = zonesByFaction;
        }

        public FactionWars.Territory.Models.Zone? GetZone(string id) => null;

        public IEnumerable<FactionWars.Territory.Models.Zone> GetAllZones()
        {
            // Return zones with correct owners set
            var zones = new List<FactionWars.Territory.Models.Zone>();
            int zoneIndex = 0;

            // First add zones with owners
            foreach (var kvp in _zonesByFaction)
            {
                string factionId = kvp.Key;
                int count = kvp.Value;
                for (int i = 0; i < count; i++)
                {
                    var zone = new FactionWars.Territory.Models.Zone($"zone_{zoneIndex}", $"Zone {zoneIndex}",
                        new FactionWars.Core.Interfaces.Vector3(0, 0, 0), 100f, 1);
                    zone.OwnerFactionId = factionId;
                    zones.Add(zone);
                    zoneIndex++;
                }
            }

            // Add remaining zones as neutral (no owner)
            while (zoneIndex < _totalZones)
            {
                zones.Add(new FactionWars.Territory.Models.Zone($"zone_{zoneIndex}", $"Zone {zoneIndex}",
                    new FactionWars.Core.Interfaces.Vector3(0, 0, 0), 100f, 1));
                zoneIndex++;
            }

            return zones;
        }

        public FactionWars.Territory.Models.Zone? GetZoneAtPosition(FactionWars.Core.Interfaces.Vector3 position) => null;

        public IEnumerable<FactionWars.Territory.Models.Zone> GetZonesByOwner(string? factionId)
        {
            if (factionId == null) return Enumerable.Empty<FactionWars.Territory.Models.Zone>();

            if (_zonesByFaction.TryGetValue(factionId, out int count))
            {
                var zones = new List<FactionWars.Territory.Models.Zone>();
                for (int i = 0; i < count; i++)
                {
                    var zone = new FactionWars.Territory.Models.Zone($"zone_{factionId}_{i}", $"Zone {i}",
                        new FactionWars.Core.Interfaces.Vector3(0, 0, 0), 100f, 1);
                    zone.OwnerFactionId = factionId;
                    zones.Add(zone);
                }
                return zones;
            }
            return Enumerable.Empty<FactionWars.Territory.Models.Zone>();
        }

        public event EventHandler<FactionWars.Territory.Events.ZoneOwnershipChangedEventArgs>? ZoneOwnershipChanged { add { } remove { } }

        public IEnumerable<FactionWars.Territory.Models.Zone> GetContestedZones() => Enumerable.Empty<FactionWars.Territory.Models.Zone>();
        public IEnumerable<FactionWars.Territory.Models.Zone> GetZonesByTrait(FactionWars.Territory.Models.ZoneTrait trait) => Enumerable.Empty<FactionWars.Territory.Models.Zone>();
        public IEnumerable<FactionWars.Territory.Models.Zone> GetHighValueZones(int count) => Enumerable.Empty<FactionWars.Territory.Models.Zone>();
        public bool TransferZoneOwnership(string zoneId, string? newOwnerFactionId) => true;
        public bool UpdateZoneControl(string zoneId, float controlPercentage) => true;
        public bool SetZoneContested(string zoneId, bool isContested) => true;
        public int GetFactionTerritoryValue(string factionId) => 0;

        public int GetZoneCount(string? factionId)
        {
            if (factionId == null) return 0;
            return _zonesByFaction.TryGetValue(factionId, out int count) ? count : 0;
        }

        public bool IsPositionInAnyZone(FactionWars.Core.Interfaces.Vector3 position) => false;
        public IEnumerable<FactionWars.Territory.Models.Zone> GetAdjacentZones(string zoneId) => Enumerable.Empty<FactionWars.Territory.Models.Zone>();
        public bool AreZonesAdjacent(string zoneId1, string zoneId2) => false;
        public IEnumerable<FactionWars.Territory.Models.Zone> GetConnectedZones(string zoneId) => Enumerable.Empty<FactionWars.Territory.Models.Zone>();
        public IEnumerable<FactionWars.Territory.Models.Zone> GetConnectedZonesByOwner(string zoneId, string factionId) => Enumerable.Empty<FactionWars.Territory.Models.Zone>();

        /// <summary>
        /// Gets all unique faction IDs that own zones.
        /// </summary>
        public IEnumerable<string> GetAllFactionIds() => _zonesByFaction.Keys;
    }
}
