using FactionWars.Core.Interfaces;
using FactionWars.Core.Utils;
using FactionWars.Factions.Interfaces;
using FactionWars.Factions.Models;
using FactionWars.Territory.Interfaces;
using FactionWars.Territory.Models;
using FactionWars.UI.Interfaces;
using FactionWars.UI.Models;
using FactionWars.UI.Services;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace FactionWars.Tests.Unit.UI
{
    /// <summary>
    /// Tests for zone visual boundary rendering service.
    /// Following TDD - these tests define the expected behavior for the ZoneBoundaryRenderer.
    /// </summary>
    public class ZoneBoundaryRendererTests
    {
        #region Test Setup

        private MockGameBridge _gameBridge;
        private Mock<IZoneService> _zoneServiceMock;
        private Mock<IFactionRepository> _factionRepositoryMock;

        public ZoneBoundaryRendererTests()
        {
            _gameBridge = new MockGameBridge();
            _zoneServiceMock = new Mock<IZoneService>();
            _factionRepositoryMock = new Mock<IFactionRepository>();
        }

        private IZoneBoundaryRenderer CreateRenderer()
        {
            return new ZoneBoundaryRenderer(_gameBridge, _zoneServiceMock.Object, _factionRepositoryMock.Object);
        }

        private Zone CreateCircularZone(string id, string name, Vector3 center, float radius, string? ownerFactionId = null)
        {
            var zone = new Zone(id, name, center, radius, 5);
            zone.OwnerFactionId = ownerFactionId;
            return zone;
        }

        private Zone CreatePolygonZone(string id, string name, IEnumerable<Vector3> vertices, string? ownerFactionId = null)
        {
            var zone = new Zone(id, name, vertices, 5);
            zone.OwnerFactionId = ownerFactionId;
            return zone;
        }

        private Faction CreateTestFaction(string id, string name, FactionColor color)
        {
            return new Faction(id, name, null, "", color);
        }

        #endregion

        #region Constructor Validation

        [Fact]
        public void Constructor_ShouldThrowForNullGameBridge()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new ZoneBoundaryRenderer(null!, _zoneServiceMock.Object, _factionRepositoryMock.Object));
        }

        [Fact]
        public void Constructor_ShouldThrowForNullZoneService()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new ZoneBoundaryRenderer(_gameBridge, null!, _factionRepositoryMock.Object));
        }

        [Fact]
        public void Constructor_ShouldThrowForNullFactionRepository()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new ZoneBoundaryRenderer(_gameBridge, _zoneServiceMock.Object, null!));
        }

        #endregion

        #region RenderZoneBoundary - Circular Zones

        [Fact]
        public void RenderZoneBoundary_ShouldCreateMarkerForCircularZone()
        {
            // Arrange
            var renderer = CreateRenderer();
            var zone = CreateCircularZone("zone_1", "Test Zone", new Vector3(100, 200, 50), 150f);

            // Act
            renderer.RenderZoneBoundary(zone);

            // Assert
            Assert.True(renderer.IsZoneBoundaryRendered(zone.Id));
        }

        [Fact]
        public void RenderZoneBoundary_ShouldThrowForNullZone()
        {
            // Arrange
            var renderer = CreateRenderer();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => renderer.RenderZoneBoundary(null!));
        }

        [Fact]
        public void RenderZoneBoundary_CircularZone_ShouldUseZoneCenterAndRadius()
        {
            // Arrange
            var renderer = CreateRenderer();
            var center = new Vector3(100, 200, 50);
            var radius = 150f;
            var zone = CreateCircularZone("zone_1", "Test Zone", center, radius);

            // Act
            renderer.RenderZoneBoundary(zone);

            // Assert
            var renderData = renderer.GetZoneBoundaryRenderData(zone.Id);
            Assert.NotNull(renderData);
            Assert.Equal(center.X, renderData!.Center.X, 0.01);
            Assert.Equal(center.Y, renderData.Center.Y, 0.01);
            Assert.Equal(radius, renderData.Radius, 0.01);
        }

        [Fact]
        public void RenderZoneBoundary_CircularZone_ShouldUseNeutralColorForUnownedZone()
        {
            // Arrange
            var renderer = CreateRenderer();
            var zone = CreateCircularZone("zone_1", "Test Zone", new Vector3(100, 200, 50), 150f);

            // Act
            renderer.RenderZoneBoundary(zone);

            // Assert
            var renderData = renderer.GetZoneBoundaryRenderData(zone.Id);
            Assert.NotNull(renderData);
            Assert.Equal(BoundaryColor.Neutral, renderData!.Color);
        }

        [Fact]
        public void RenderZoneBoundary_CircularZone_ShouldUseMichaelColorForMichaelOwnedZone()
        {
            // Arrange
            var renderer = CreateRenderer();
            var zone = CreateCircularZone("zone_1", "Test Zone", new Vector3(100, 200, 50), 150f, "faction_michael");
            var michaelFaction = CreateTestFaction("faction_michael", "De Santa Family", new FactionColor(0, 100, 255));
            _factionRepositoryMock.Setup(r => r.GetById("faction_michael")).Returns(michaelFaction);

            // Act
            renderer.RenderZoneBoundary(zone);

            // Assert
            var renderData = renderer.GetZoneBoundaryRenderData(zone.Id);
            Assert.NotNull(renderData);
            Assert.Equal(BoundaryColor.Michael, renderData!.Color);
        }

        [Fact]
        public void RenderZoneBoundary_CircularZone_ShouldUseTrevorColorForTrevorOwnedZone()
        {
            // Arrange
            var renderer = CreateRenderer();
            var zone = CreateCircularZone("zone_1", "Test Zone", new Vector3(100, 200, 50), 150f, "faction_trevor");
            var trevorFaction = CreateTestFaction("faction_trevor", "Trevor Philips Industries", new FactionColor(255, 128, 0));
            _factionRepositoryMock.Setup(r => r.GetById("faction_trevor")).Returns(trevorFaction);

            // Act
            renderer.RenderZoneBoundary(zone);

            // Assert
            var renderData = renderer.GetZoneBoundaryRenderData(zone.Id);
            Assert.NotNull(renderData);
            Assert.Equal(BoundaryColor.Trevor, renderData!.Color);
        }

        [Fact]
        public void RenderZoneBoundary_CircularZone_ShouldUseFranklinColorForFranklinOwnedZone()
        {
            // Arrange
            var renderer = CreateRenderer();
            var zone = CreateCircularZone("zone_1", "Test Zone", new Vector3(100, 200, 50), 150f, "faction_franklin");
            var franklinFaction = CreateTestFaction("faction_franklin", "Clinton Organization", new FactionColor(0, 200, 100));
            _factionRepositoryMock.Setup(r => r.GetById("faction_franklin")).Returns(franklinFaction);

            // Act
            renderer.RenderZoneBoundary(zone);

            // Assert
            var renderData = renderer.GetZoneBoundaryRenderData(zone.Id);
            Assert.NotNull(renderData);
            Assert.Equal(BoundaryColor.Franklin, renderData!.Color);
        }

        #endregion

        #region RenderZoneBoundary - Polygon Zones

        [Fact]
        public void RenderZoneBoundary_ShouldCreateLinesForPolygonZone()
        {
            // Arrange
            var renderer = CreateRenderer();
            var vertices = new[]
            {
                new Vector3(0, 0, 50),
                new Vector3(100, 0, 50),
                new Vector3(100, 100, 50),
                new Vector3(0, 100, 50)
            };
            var zone = CreatePolygonZone("zone_1", "Test Zone", vertices);

            // Act
            renderer.RenderZoneBoundary(zone);

            // Assert
            Assert.True(renderer.IsZoneBoundaryRendered(zone.Id));
            var renderData = renderer.GetZoneBoundaryRenderData(zone.Id);
            Assert.NotNull(renderData);
            Assert.Equal(BoundaryRenderType.Polygon, renderData!.RenderType);
        }

        [Fact]
        public void RenderZoneBoundary_PolygonZone_ShouldStoreAllVertices()
        {
            // Arrange
            var renderer = CreateRenderer();
            var vertices = new[]
            {
                new Vector3(0, 0, 50),
                new Vector3(100, 0, 50),
                new Vector3(100, 100, 50),
                new Vector3(0, 100, 50)
            };
            var zone = CreatePolygonZone("zone_1", "Test Zone", vertices);

            // Act
            renderer.RenderZoneBoundary(zone);

            // Assert
            var renderData = renderer.GetZoneBoundaryRenderData(zone.Id);
            Assert.NotNull(renderData);
            Assert.Equal(4, renderData!.Vertices.Count);
        }

        [Fact]
        public void RenderZoneBoundary_PolygonZone_ShouldUseNeutralColorForUnownedZone()
        {
            // Arrange
            var renderer = CreateRenderer();
            var vertices = new[]
            {
                new Vector3(0, 0, 50),
                new Vector3(100, 0, 50),
                new Vector3(100, 100, 50)
            };
            var zone = CreatePolygonZone("zone_1", "Test Zone", vertices);

            // Act
            renderer.RenderZoneBoundary(zone);

            // Assert
            var renderData = renderer.GetZoneBoundaryRenderData(zone.Id);
            Assert.NotNull(renderData);
            Assert.Equal(BoundaryColor.Neutral, renderData!.Color);
        }

        #endregion

        #region RemoveZoneBoundary

        [Fact]
        public void RemoveZoneBoundary_ShouldRemoveRenderedBoundary()
        {
            // Arrange
            var renderer = CreateRenderer();
            var zone = CreateCircularZone("zone_1", "Test Zone", new Vector3(100, 200, 50), 150f);
            renderer.RenderZoneBoundary(zone);

            // Act
            var result = renderer.RemoveZoneBoundary(zone.Id);

            // Assert
            Assert.True(result);
            Assert.False(renderer.IsZoneBoundaryRendered(zone.Id));
        }

        [Fact]
        public void RemoveZoneBoundary_ShouldReturnFalseForNonExistentZone()
        {
            // Arrange
            var renderer = CreateRenderer();

            // Act
            var result = renderer.RemoveZoneBoundary("nonexistent_zone");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void RemoveZoneBoundary_ShouldThrowForNullZoneId()
        {
            // Arrange
            var renderer = CreateRenderer();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => renderer.RemoveZoneBoundary(null!));
        }

        #endregion

        #region UpdateZoneBoundaryColor

        [Fact]
        public void UpdateZoneBoundaryColor_ShouldUpdateColorForFactionChange()
        {
            // Arrange
            var renderer = CreateRenderer();
            var zone = CreateCircularZone("zone_1", "Test Zone", new Vector3(100, 200, 50), 150f);
            renderer.RenderZoneBoundary(zone);

            var michaelFaction = CreateTestFaction("faction_michael", "De Santa Family", new FactionColor(0, 100, 255));
            _factionRepositoryMock.Setup(r => r.GetById("faction_michael")).Returns(michaelFaction);

            // Act
            var result = renderer.UpdateZoneBoundaryColor(zone.Id, "faction_michael");

            // Assert
            Assert.True(result);
            var renderData = renderer.GetZoneBoundaryRenderData(zone.Id);
            Assert.NotNull(renderData);
            Assert.Equal(BoundaryColor.Michael, renderData!.Color);
        }

        [Fact]
        public void UpdateZoneBoundaryColor_ShouldSetNeutralColorForNullFaction()
        {
            // Arrange
            var renderer = CreateRenderer();
            var zone = CreateCircularZone("zone_1", "Test Zone", new Vector3(100, 200, 50), 150f, "faction_michael");
            var michaelFaction = CreateTestFaction("faction_michael", "De Santa Family", new FactionColor(0, 100, 255));
            _factionRepositoryMock.Setup(r => r.GetById("faction_michael")).Returns(michaelFaction);
            renderer.RenderZoneBoundary(zone);

            // Act
            var result = renderer.UpdateZoneBoundaryColor(zone.Id, null);

            // Assert
            Assert.True(result);
            var renderData = renderer.GetZoneBoundaryRenderData(zone.Id);
            Assert.NotNull(renderData);
            Assert.Equal(BoundaryColor.Neutral, renderData!.Color);
        }

        [Fact]
        public void UpdateZoneBoundaryColor_ShouldReturnFalseForNonRenderedZone()
        {
            // Arrange
            var renderer = CreateRenderer();

            // Act
            var result = renderer.UpdateZoneBoundaryColor("nonexistent_zone", "faction_michael");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void UpdateZoneBoundaryColor_ShouldThrowForNullZoneId()
        {
            // Arrange
            var renderer = CreateRenderer();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => renderer.UpdateZoneBoundaryColor(null!, "faction_michael"));
        }

        #endregion

        #region RenderAllZoneBoundaries

        [Fact]
        public void RenderAllZoneBoundaries_ShouldRenderBoundariesForAllZones()
        {
            // Arrange
            var renderer = CreateRenderer();
            var zones = new List<Zone>
            {
                CreateCircularZone("zone_1", "Zone 1", new Vector3(100, 100, 50), 100f),
                CreateCircularZone("zone_2", "Zone 2", new Vector3(200, 200, 50), 100f),
                CreateCircularZone("zone_3", "Zone 3", new Vector3(300, 300, 50), 100f)
            };
            _zoneServiceMock.Setup(s => s.GetAllZones()).Returns(zones);

            // Act
            var count = renderer.RenderAllZoneBoundaries();

            // Assert
            Assert.Equal(3, count);
            Assert.True(renderer.IsZoneBoundaryRendered("zone_1"));
            Assert.True(renderer.IsZoneBoundaryRendered("zone_2"));
            Assert.True(renderer.IsZoneBoundaryRendered("zone_3"));
        }

        [Fact]
        public void RenderAllZoneBoundaries_ShouldNotDuplicateExistingBoundaries()
        {
            // Arrange
            var renderer = CreateRenderer();
            var zones = new List<Zone>
            {
                CreateCircularZone("zone_1", "Zone 1", new Vector3(100, 100, 50), 100f),
                CreateCircularZone("zone_2", "Zone 2", new Vector3(200, 200, 50), 100f)
            };
            _zoneServiceMock.Setup(s => s.GetAllZones()).Returns(zones);

            // Render first zone manually
            renderer.RenderZoneBoundary(zones[0]);

            // Act
            var count = renderer.RenderAllZoneBoundaries();

            // Assert - should return 2 (total rendered, not new ones)
            Assert.Equal(2, count);
        }

        #endregion

        #region RemoveAllZoneBoundaries

        [Fact]
        public void RemoveAllZoneBoundaries_ShouldRemoveAllRenderedBoundaries()
        {
            // Arrange
            var renderer = CreateRenderer();
            var zones = new List<Zone>
            {
                CreateCircularZone("zone_1", "Zone 1", new Vector3(100, 100, 50), 100f),
                CreateCircularZone("zone_2", "Zone 2", new Vector3(200, 200, 50), 100f)
            };

            foreach (var zone in zones)
            {
                renderer.RenderZoneBoundary(zone);
            }

            // Act
            renderer.RemoveAllZoneBoundaries();

            // Assert
            Assert.False(renderer.IsZoneBoundaryRendered("zone_1"));
            Assert.False(renderer.IsZoneBoundaryRendered("zone_2"));
            Assert.Equal(0, renderer.GetRenderedZoneCount());
        }

        [Fact]
        public void RemoveAllZoneBoundaries_ShouldHandleEmptyList()
        {
            // Arrange
            var renderer = CreateRenderer();

            // Act & Assert - should not throw
            renderer.RemoveAllZoneBoundaries();
        }

        #endregion

        #region GetRenderedZoneCount

        [Fact]
        public void GetRenderedZoneCount_ShouldReturnZero_WhenNoBoundariesRendered()
        {
            // Arrange
            var renderer = CreateRenderer();

            // Act
            var count = renderer.GetRenderedZoneCount();

            // Assert
            Assert.Equal(0, count);
        }

        [Fact]
        public void GetRenderedZoneCount_ShouldReturnCorrectCount()
        {
            // Arrange
            var renderer = CreateRenderer();
            renderer.RenderZoneBoundary(CreateCircularZone("zone_1", "Zone 1", new Vector3(100, 100, 50), 100f));
            renderer.RenderZoneBoundary(CreateCircularZone("zone_2", "Zone 2", new Vector3(200, 200, 50), 100f));
            renderer.RenderZoneBoundary(CreateCircularZone("zone_3", "Zone 3", new Vector3(300, 300, 50), 100f));

            // Act
            var count = renderer.GetRenderedZoneCount();

            // Assert
            Assert.Equal(3, count);
        }

        [Fact]
        public void GetRenderedZoneCount_ShouldDecreaseAfterRemoval()
        {
            // Arrange
            var renderer = CreateRenderer();
            renderer.RenderZoneBoundary(CreateCircularZone("zone_1", "Zone 1", new Vector3(100, 100, 50), 100f));
            renderer.RenderZoneBoundary(CreateCircularZone("zone_2", "Zone 2", new Vector3(200, 200, 50), 100f));

            // Act
            renderer.RemoveZoneBoundary("zone_1");
            var count = renderer.GetRenderedZoneCount();

            // Assert
            Assert.Equal(1, count);
        }

        #endregion

        #region SyncWithZone

        [Fact]
        public void SyncWithZone_ShouldUpdateColorForFactionChange()
        {
            // Arrange
            var renderer = CreateRenderer();
            var zone = CreateCircularZone("zone_1", "Test Zone", new Vector3(100, 200, 50), 150f);
            renderer.RenderZoneBoundary(zone);

            // Update zone ownership
            zone.OwnerFactionId = "faction_trevor";
            var trevorFaction = CreateTestFaction("faction_trevor", "Trevor Philips Industries", new FactionColor(255, 128, 0));
            _factionRepositoryMock.Setup(r => r.GetById("faction_trevor")).Returns(trevorFaction);
            _zoneServiceMock.Setup(s => s.GetZone("zone_1")).Returns(zone);

            // Act
            renderer.SyncWithZone("zone_1");

            // Assert
            var renderData = renderer.GetZoneBoundaryRenderData(zone.Id);
            Assert.NotNull(renderData);
            Assert.Equal(BoundaryColor.Trevor, renderData!.Color);
        }

        [Fact]
        public void SyncWithZone_ShouldDoNothingForNonRenderedZone()
        {
            // Arrange
            var renderer = CreateRenderer();
            var zone = CreateCircularZone("zone_1", "Test Zone", new Vector3(100, 200, 50), 150f);
            _zoneServiceMock.Setup(s => s.GetZone("zone_1")).Returns(zone);

            // Act & Assert - should not throw
            renderer.SyncWithZone("zone_1");
        }

        [Fact]
        public void SyncWithZone_ShouldThrowForNullZoneId()
        {
            // Arrange
            var renderer = CreateRenderer();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => renderer.SyncWithZone(null!));
        }

        #endregion

        #region GetBoundaryColorForFaction

        [Fact]
        public void GetBoundaryColorForFaction_ShouldReturnNeutralForNullFaction()
        {
            // Arrange
            var renderer = CreateRenderer();

            // Act
            var color = renderer.GetBoundaryColorForFaction(null);

            // Assert
            Assert.Equal(BoundaryColor.Neutral, color);
        }

        [Fact]
        public void GetBoundaryColorForFaction_ShouldReturnMichaelForMichaelFaction()
        {
            // Arrange
            var renderer = CreateRenderer();
            var michaelFaction = CreateTestFaction("faction_michael", "De Santa Family", new FactionColor(0, 100, 255));
            _factionRepositoryMock.Setup(r => r.GetById("faction_michael")).Returns(michaelFaction);

            // Act
            var color = renderer.GetBoundaryColorForFaction("faction_michael");

            // Assert
            Assert.Equal(BoundaryColor.Michael, color);
        }

        [Fact]
        public void GetBoundaryColorForFaction_ShouldReturnTrevorForTrevorFaction()
        {
            // Arrange
            var renderer = CreateRenderer();
            var trevorFaction = CreateTestFaction("faction_trevor", "Trevor Philips Industries", new FactionColor(255, 128, 0));
            _factionRepositoryMock.Setup(r => r.GetById("faction_trevor")).Returns(trevorFaction);

            // Act
            var color = renderer.GetBoundaryColorForFaction("faction_trevor");

            // Assert
            Assert.Equal(BoundaryColor.Trevor, color);
        }

        [Fact]
        public void GetBoundaryColorForFaction_ShouldReturnFranklinForFranklinFaction()
        {
            // Arrange
            var renderer = CreateRenderer();
            var franklinFaction = CreateTestFaction("faction_franklin", "Clinton Organization", new FactionColor(0, 200, 100));
            _factionRepositoryMock.Setup(r => r.GetById("faction_franklin")).Returns(franklinFaction);

            // Act
            var color = renderer.GetBoundaryColorForFaction("faction_franklin");

            // Assert
            Assert.Equal(BoundaryColor.Franklin, color);
        }

        [Fact]
        public void GetBoundaryColorForFaction_ShouldReturnNeutralForUnknownFaction()
        {
            // Arrange
            var renderer = CreateRenderer();
            _factionRepositoryMock.Setup(r => r.GetById("unknown_faction")).Returns((Faction?)null);

            // Act
            var color = renderer.GetBoundaryColorForFaction("unknown_faction");

            // Assert
            Assert.Equal(BoundaryColor.Neutral, color);
        }

        #endregion

        #region SetBoundaryAlpha

        [Fact]
        public void SetBoundaryAlpha_ShouldUpdateAlphaForZone()
        {
            // Arrange
            var renderer = CreateRenderer();
            var zone = CreateCircularZone("zone_1", "Test Zone", new Vector3(100, 200, 50), 150f);
            renderer.RenderZoneBoundary(zone);

            // Act
            var result = renderer.SetBoundaryAlpha(zone.Id, 128);

            // Assert
            Assert.True(result);
            var renderData = renderer.GetZoneBoundaryRenderData(zone.Id);
            Assert.NotNull(renderData);
            Assert.Equal(128, renderData!.Alpha);
        }

        [Fact]
        public void SetBoundaryAlpha_ShouldClampAlphaToValidRange()
        {
            // Arrange
            var renderer = CreateRenderer();
            var zone = CreateCircularZone("zone_1", "Test Zone", new Vector3(100, 200, 50), 150f);
            renderer.RenderZoneBoundary(zone);

            // Act - attempt to set alpha above max
            renderer.SetBoundaryAlpha(zone.Id, 300);

            // Assert
            var renderData = renderer.GetZoneBoundaryRenderData(zone.Id);
            Assert.NotNull(renderData);
            Assert.Equal(255, renderData!.Alpha);
        }

        [Fact]
        public void SetBoundaryAlpha_ShouldClampNegativeAlphaToZero()
        {
            // Arrange
            var renderer = CreateRenderer();
            var zone = CreateCircularZone("zone_1", "Test Zone", new Vector3(100, 200, 50), 150f);
            renderer.RenderZoneBoundary(zone);

            // Act - attempt to set negative alpha
            renderer.SetBoundaryAlpha(zone.Id, -50);

            // Assert
            var renderData = renderer.GetZoneBoundaryRenderData(zone.Id);
            Assert.NotNull(renderData);
            Assert.Equal(0, renderData!.Alpha);
        }

        [Fact]
        public void SetBoundaryAlpha_ShouldReturnFalseForNonRenderedZone()
        {
            // Arrange
            var renderer = CreateRenderer();

            // Act
            var result = renderer.SetBoundaryAlpha("nonexistent_zone", 128);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void SetBoundaryAlpha_ShouldThrowForNullZoneId()
        {
            // Arrange
            var renderer = CreateRenderer();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => renderer.SetBoundaryAlpha(null!, 128));
        }

        #endregion

        #region SetGlobalBoundaryAlpha

        [Fact]
        public void SetGlobalBoundaryAlpha_ShouldUpdateAlphaForAllZones()
        {
            // Arrange
            var renderer = CreateRenderer();
            renderer.RenderZoneBoundary(CreateCircularZone("zone_1", "Zone 1", new Vector3(100, 100, 50), 100f));
            renderer.RenderZoneBoundary(CreateCircularZone("zone_2", "Zone 2", new Vector3(200, 200, 50), 100f));

            // Act
            renderer.SetGlobalBoundaryAlpha(100);

            // Assert
            var renderData1 = renderer.GetZoneBoundaryRenderData("zone_1");
            var renderData2 = renderer.GetZoneBoundaryRenderData("zone_2");
            Assert.Equal(100, renderData1!.Alpha);
            Assert.Equal(100, renderData2!.Alpha);
        }

        #endregion

        #region ShowZoneBoundary / HideZoneBoundary

        [Fact]
        public void ShowZoneBoundary_ShouldMakeBoundaryVisible()
        {
            // Arrange
            var renderer = CreateRenderer();
            var zone = CreateCircularZone("zone_1", "Test Zone", new Vector3(100, 200, 50), 150f);
            renderer.RenderZoneBoundary(zone);
            renderer.HideZoneBoundary(zone.Id);

            // Act
            var result = renderer.ShowZoneBoundary(zone.Id);

            // Assert
            Assert.True(result);
            var renderData = renderer.GetZoneBoundaryRenderData(zone.Id);
            Assert.NotNull(renderData);
            Assert.True(renderData!.IsVisible);
        }

        [Fact]
        public void HideZoneBoundary_ShouldMakeBoundaryInvisible()
        {
            // Arrange
            var renderer = CreateRenderer();
            var zone = CreateCircularZone("zone_1", "Test Zone", new Vector3(100, 200, 50), 150f);
            renderer.RenderZoneBoundary(zone);

            // Act
            var result = renderer.HideZoneBoundary(zone.Id);

            // Assert
            Assert.True(result);
            var renderData = renderer.GetZoneBoundaryRenderData(zone.Id);
            Assert.NotNull(renderData);
            Assert.False(renderData!.IsVisible);
        }

        [Fact]
        public void ShowZoneBoundary_ShouldReturnFalseForNonRenderedZone()
        {
            // Arrange
            var renderer = CreateRenderer();

            // Act
            var result = renderer.ShowZoneBoundary("nonexistent_zone");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void HideZoneBoundary_ShouldReturnFalseForNonRenderedZone()
        {
            // Arrange
            var renderer = CreateRenderer();

            // Act
            var result = renderer.HideZoneBoundary("nonexistent_zone");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void ShowZoneBoundary_ShouldThrowForNullZoneId()
        {
            // Arrange
            var renderer = CreateRenderer();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => renderer.ShowZoneBoundary(null!));
        }

        [Fact]
        public void HideZoneBoundary_ShouldThrowForNullZoneId()
        {
            // Arrange
            var renderer = CreateRenderer();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => renderer.HideZoneBoundary(null!));
        }

        #endregion

        #region RenderZoneBoundary - Already Rendered

        [Fact]
        public void RenderZoneBoundary_ShouldNotDuplicateIfAlreadyRendered()
        {
            // Arrange
            var renderer = CreateRenderer();
            var zone = CreateCircularZone("zone_1", "Test Zone", new Vector3(100, 200, 50), 150f);

            // Act
            renderer.RenderZoneBoundary(zone);
            renderer.RenderZoneBoundary(zone); // Render again

            // Assert - should still have only one rendered boundary
            Assert.Equal(1, renderer.GetRenderedZoneCount());
        }

        #endregion
    }
}
