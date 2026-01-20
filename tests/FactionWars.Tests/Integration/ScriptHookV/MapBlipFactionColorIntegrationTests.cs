using System.Collections.Generic;
using System.Linq;
using FactionWars.Core.Interfaces;
using FactionWars.Core.Services;
using FactionWars.Factions.Interfaces;
using FactionWars.Factions.Repositories;
using FactionWars.Factions.Services;
using FactionWars.ScriptHookV;
using FactionWars.ScriptHookV.Managers;
using FactionWars.Territory.Interfaces;
using FactionWars.Territory.Models;
using FactionWars.Territory.Repositories;
using Moq;
using Xunit;

namespace FactionWars.Tests.Integration.ScriptHookV
{
    /// <summary>
    /// Integration tests verifying that zones appear on the map with correct faction colors.
    /// Tests the full flow: Zone initialization → FactionInitializer → MapBlipManager → Correct colors.
    /// </summary>
    public class MapBlipFactionColorIntegrationTests
    {
        private readonly Mock<IGameBridge> _gameBridgeMock;
        private readonly InMemoryZoneRepository _zoneRepository;
        private readonly InMemoryFactionRepository _factionRepository;
        private readonly IFactionService _factionService;
        private readonly Dictionary<int, BlipColor> _createdBlipColors;
        private int _nextBlipHandle;

        public MapBlipFactionColorIntegrationTests()
        {
            _gameBridgeMock = new Mock<IGameBridge>();
            _zoneRepository = new InMemoryZoneRepository();
            _factionRepository = new InMemoryFactionRepository();
            _factionService = new FactionService(_factionRepository);
            _createdBlipColors = new Dictionary<int, BlipColor>();
            _nextBlipHandle = 1;

            // Track blip creation and color assignments
            _gameBridgeMock.Setup(g => g.CreateBlip(It.IsAny<Vector3>()))
                .Returns(() => _nextBlipHandle++);
            _gameBridgeMock.Setup(g => g.SetBlipColor(It.IsAny<int>(), It.IsAny<BlipColor>()))
                .Callback<int, BlipColor>((handle, color) => _createdBlipColors[handle] = color);
        }

        [Fact]
        public void Initialize_WithMichaelZones_ShouldDisplayMichaelBlueBlips()
        {
            // Arrange: Create zones owned by Michael
            var michaelZone1 = new Zone("rockford_hills", "Rockford Hills", new Vector3(100f, 200f, 30f), 100f, 5);
            var michaelZone2 = new Zone("vinewood", "Vinewood", new Vector3(300f, 400f, 30f), 100f, 5);
            michaelZone1.OwnerFactionId = CharacterModelFactionDetector.MichaelFactionId;
            michaelZone2.OwnerFactionId = CharacterModelFactionDetector.MichaelFactionId;
            _zoneRepository.Add(michaelZone1);
            _zoneRepository.Add(michaelZone2);

            var mapBlipManager = new MapBlipManager(
                _gameBridgeMock.Object,
                _zoneRepository,
                _factionService);

            // Act
            mapBlipManager.Initialize();

            // Assert: All Michael's zones should have MichaelBlue blips
            Assert.Equal(2, _createdBlipColors.Count);
            Assert.All(_createdBlipColors.Values, color => Assert.Equal(BlipColor.MichaelBlue, color));
        }

        [Fact]
        public void Initialize_WithTrevorZones_ShouldDisplayTrevorOrangeBlips()
        {
            // Arrange: Create zones owned by Trevor
            var trevorZone1 = new Zone("sandy_shores", "Sandy Shores", new Vector3(100f, 200f, 30f), 100f, 3);
            var trevorZone2 = new Zone("grapeseed", "Grapeseed", new Vector3(300f, 400f, 30f), 100f, 3);
            trevorZone1.OwnerFactionId = CharacterModelFactionDetector.TrevorFactionId;
            trevorZone2.OwnerFactionId = CharacterModelFactionDetector.TrevorFactionId;
            _zoneRepository.Add(trevorZone1);
            _zoneRepository.Add(trevorZone2);

            var mapBlipManager = new MapBlipManager(
                _gameBridgeMock.Object,
                _zoneRepository,
                _factionService);

            // Act
            mapBlipManager.Initialize();

            // Assert: All Trevor's zones should have TrevorOrange blips
            Assert.Equal(2, _createdBlipColors.Count);
            Assert.All(_createdBlipColors.Values, color => Assert.Equal(BlipColor.TrevorOrange, color));
        }

        [Fact]
        public void Initialize_WithFranklinZones_ShouldDisplayFranklinGreenBlips()
        {
            // Arrange: Create zones owned by Franklin
            var franklinZone1 = new Zone("davis", "Davis", new Vector3(100f, 200f, 30f), 100f, 4);
            var franklinZone2 = new Zone("strawberry", "Strawberry", new Vector3(300f, 400f, 30f), 100f, 4);
            franklinZone1.OwnerFactionId = CharacterModelFactionDetector.FranklinFactionId;
            franklinZone2.OwnerFactionId = CharacterModelFactionDetector.FranklinFactionId;
            _zoneRepository.Add(franklinZone1);
            _zoneRepository.Add(franklinZone2);

            var mapBlipManager = new MapBlipManager(
                _gameBridgeMock.Object,
                _zoneRepository,
                _factionService);

            // Act
            mapBlipManager.Initialize();

            // Assert: All Franklin's zones should have FranklinGreen blips
            Assert.Equal(2, _createdBlipColors.Count);
            Assert.All(_createdBlipColors.Values, color => Assert.Equal(BlipColor.FranklinGreen, color));
        }

        [Fact]
        public void Initialize_WithNeutralZones_ShouldDisplayWhiteBlips()
        {
            // Arrange: Create unowned (neutral) zones
            var neutralZone1 = new Zone("mirror_park", "Mirror Park", new Vector3(100f, 200f, 30f), 100f, 3);
            var neutralZone2 = new Zone("hawick", "Hawick", new Vector3(300f, 400f, 30f), 100f, 3);
            // No owner set - neutral
            _zoneRepository.Add(neutralZone1);
            _zoneRepository.Add(neutralZone2);

            var mapBlipManager = new MapBlipManager(
                _gameBridgeMock.Object,
                _zoneRepository,
                _factionService);

            // Act
            mapBlipManager.Initialize();

            // Assert: Neutral zones should have White blips
            Assert.Equal(2, _createdBlipColors.Count);
            Assert.All(_createdBlipColors.Values, color => Assert.Equal(BlipColor.White, color));
        }

        [Fact]
        public void Initialize_WithMixedFactionZones_ShouldDisplayCorrectColorsForEachFaction()
        {
            // Arrange: Create zones for all three factions plus neutral
            var michaelZone = new Zone("rockford_hills", "Rockford Hills", new Vector3(100f, 100f, 30f), 100f, 5);
            michaelZone.OwnerFactionId = CharacterModelFactionDetector.MichaelFactionId;

            var trevorZone = new Zone("sandy_shores", "Sandy Shores", new Vector3(200f, 200f, 30f), 100f, 3);
            trevorZone.OwnerFactionId = CharacterModelFactionDetector.TrevorFactionId;

            var franklinZone = new Zone("davis", "Davis", new Vector3(300f, 300f, 30f), 100f, 4);
            franklinZone.OwnerFactionId = CharacterModelFactionDetector.FranklinFactionId;

            var neutralZone = new Zone("mirror_park", "Mirror Park", new Vector3(400f, 400f, 30f), 100f, 3);
            // No owner - neutral

            _zoneRepository.Add(michaelZone);
            _zoneRepository.Add(trevorZone);
            _zoneRepository.Add(franklinZone);
            _zoneRepository.Add(neutralZone);

            var mapBlipManager = new MapBlipManager(
                _gameBridgeMock.Object,
                _zoneRepository,
                _factionService);

            // Act
            mapBlipManager.Initialize();

            // Assert: Should have 4 blips with correct colors
            Assert.Equal(4, _createdBlipColors.Count);

            // Get blip handles for each zone
            var michaelBlipHandle = mapBlipManager.GetBlipHandle("rockford_hills");
            var trevorBlipHandle = mapBlipManager.GetBlipHandle("sandy_shores");
            var franklinBlipHandle = mapBlipManager.GetBlipHandle("davis");
            var neutralBlipHandle = mapBlipManager.GetBlipHandle("mirror_park");

            Assert.Equal(BlipColor.MichaelBlue, _createdBlipColors[michaelBlipHandle]);
            Assert.Equal(BlipColor.TrevorOrange, _createdBlipColors[trevorBlipHandle]);
            Assert.Equal(BlipColor.FranklinGreen, _createdBlipColors[franklinBlipHandle]);
            Assert.Equal(BlipColor.White, _createdBlipColors[neutralBlipHandle]);
        }

        [Fact]
        public void UpdateBlipColors_WhenZoneCaptured_ShouldChangeToNewFactionColor()
        {
            // Arrange: Create a zone owned by Trevor
            var zone = new Zone("sandy_shores", "Sandy Shores", new Vector3(100f, 200f, 30f), 100f, 3);
            zone.OwnerFactionId = CharacterModelFactionDetector.TrevorFactionId;
            _zoneRepository.Add(zone);

            var mapBlipManager = new MapBlipManager(
                _gameBridgeMock.Object,
                _zoneRepository,
                _factionService);

            mapBlipManager.Initialize();

            // Verify initial color is Trevor's orange
            var blipHandle = mapBlipManager.GetBlipHandle("sandy_shores");
            Assert.Equal(BlipColor.TrevorOrange, _createdBlipColors[blipHandle]);

            // Simulate zone capture - Michael captures the zone
            zone.OwnerFactionId = CharacterModelFactionDetector.MichaelFactionId;
            _zoneRepository.Update(zone);

            // Act: Update blip colors to reflect ownership change
            mapBlipManager.UpdateBlipColors();

            // Assert: Blip should now be Michael's blue
            Assert.Equal(BlipColor.MichaelBlue, _createdBlipColors[blipHandle]);
        }

        [Fact]
        public void FullIntegration_FactionInitializer_ThenMapBlipManager_ShouldDisplayCorrectColors()
        {
            // Arrange: Set up zones that will be assigned by FactionInitializer
            SetupDefaultZones();

            // Initialize factions with starting conditions
            var allocationRepository = new InMemoryZoneDefenderAllocationRepository();
            var allocationService = new ZoneDefenderAllocationService(allocationRepository);
            var factionInitializer = new FactionInitializer(_factionRepository, _zoneRepository, allocationService);
            factionInitializer.Initialize();

            var mapBlipManager = new MapBlipManager(
                _gameBridgeMock.Object,
                _zoneRepository,
                _factionService);

            // Act: Initialize map blips
            mapBlipManager.Initialize();

            // Assert: Verify faction-owned zones have correct colors
            var michaelZones = new[] { "rockford_hills", "vinewood", "richman", "del_perro",
                "morningwood", "pillbox_hill", "downtown", "vespucci" };
            foreach (var zoneId in michaelZones)
            {
                var handle = mapBlipManager.GetBlipHandle(zoneId);
                if (handle != -1) // Zone exists
                {
                    Assert.Equal(BlipColor.MichaelBlue, _createdBlipColors[handle]);
                }
            }

            var trevorZones = new[] { "sandy_shores", "grapeseed", "harmony", "alamo_sea",
                "grand_senora_desert", "trevor_airfield", "paleto_bay", "paleto_forest",
                "chiliad_wilderness", "cypress_flats" };
            foreach (var zoneId in trevorZones)
            {
                var handle = mapBlipManager.GetBlipHandle(zoneId);
                if (handle != -1) // Zone exists
                {
                    Assert.Equal(BlipColor.TrevorOrange, _createdBlipColors[handle]);
                }
            }

            var franklinZones = new[] { "davis", "strawberry", "rancho",
                "port_of_los_santos", "elysian_island" };
            foreach (var zoneId in franklinZones)
            {
                var handle = mapBlipManager.GetBlipHandle(zoneId);
                if (handle != -1) // Zone exists
                {
                    Assert.Equal(BlipColor.FranklinGreen, _createdBlipColors[handle]);
                }
            }
        }

        private void SetupDefaultZones()
        {
            // Create all 31 zones that the ZoneDataLoader would normally create
            // Michael's starting zones (8)
            AddZone("rockford_hills", "Rockford Hills", -857f, 204f, 5);
            AddZone("vinewood", "Vinewood", 266f, 196f, 4);
            AddZone("richman", "Richman", -1433f, 101f, 5);
            AddZone("del_perro", "Del Perro", -1525f, -276f, 4);
            AddZone("morningwood", "Morningwood", -1305f, -330f, 3);
            AddZone("pillbox_hill", "Pillbox Hill", -52f, -775f, 4);
            AddZone("downtown", "Downtown", 241f, -850f, 4);
            AddZone("vespucci", "Vespucci", -1111f, -1386f, 4);

            // Trevor's starting zones (10)
            AddZone("sandy_shores", "Sandy Shores", 1879f, 3761f, 3);
            AddZone("grapeseed", "Grapeseed", 1672f, 4782f, 2);
            AddZone("harmony", "Harmony", 621f, 2738f, 2);
            AddZone("alamo_sea", "Alamo Sea", 743f, 4085f, 2);
            AddZone("grand_senora_desert", "Grand Senora Desert", 2422f, 3044f, 2);
            AddZone("trevor_airfield", "Trevor's Airfield", 1732f, 3290f, 3);
            AddZone("paleto_bay", "Paleto Bay", -237f, 6300f, 3);
            AddZone("paleto_forest", "Paleto Forest", -554f, 5520f, 2);
            AddZone("chiliad_wilderness", "Chiliad Wilderness", 428f, 5565f, 2);
            AddZone("cypress_flats", "Cypress Flats", 689f, -1515f, 3);

            // Franklin's starting zones (5)
            AddZone("davis", "Davis", 111f, -1767f, 4);
            AddZone("strawberry", "Strawberry", 164f, -1374f, 3);
            AddZone("rancho", "Rancho", 444f, -1665f, 3);
            AddZone("port_of_los_santos", "Port of Los Santos", 170f, -2912f, 4);
            AddZone("elysian_island", "Elysian Island", -145f, -2424f, 3);

            // Neutral zones (8 remaining)
            AddZone("mirror_park", "Mirror Park", 1071f, -540f, 3);
            AddZone("hawick", "Hawick", 349f, 118f, 3);
            AddZone("vinewood_hills", "Vinewood Hills", 569f, 639f, 3);
            AddZone("tataviam_mountains", "Tataviam Mountains", 1570f, 1254f, 2);
            AddZone("el_burro_heights", "El Burro Heights", 1689f, -1130f, 3);
            AddZone("la_puerta", "La Puerta", -683f, -1164f, 3);
        }

        private void AddZone(string id, string name, float x, float y, int importance)
        {
            var zone = new Zone(id, name, new Vector3(x, y, 30f), 100f, importance);
            _zoneRepository.Add(zone);
        }
    }
}
