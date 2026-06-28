using System.Linq;
using FactionWars.Core.Interfaces;
using FactionWars.Core.Services;
using FactionWars.Factions.Interfaces;
using FactionWars.Factions.Models;
using FactionWars.Factions.Repositories;
using FactionWars.ScriptHookV;
using FactionWars.ScriptHookV.Data;
using FactionWars.Territory.Interfaces;
using FactionWars.Territory.Repositories;
using Xunit;

namespace FactionWars.Tests.Unit.ScriptHookV
{
    /// <summary>
    /// Tests for FactionInitializer which sets up the three factions with starting conditions.
    /// </summary>
    public class FactionInitializerTests
    {
        private readonly IFactionRepository _factionRepository;
        private readonly IZoneRepository _zoneRepository;
        private readonly IZoneDefenderAllocationRepository _allocationRepository;
        private readonly IZoneDefenderAllocationService _allocationService;
        private readonly FactionInitializer _initializer;

        public FactionInitializerTests()
        {
            _factionRepository = new InMemoryFactionRepository();
            _zoneRepository = new InMemoryZoneRepository();
            _allocationRepository = new InMemoryZoneDefenderAllocationRepository();
            _allocationService = new ZoneDefenderAllocationService(_allocationRepository);

            // Load zones so we can assign them
            var zoneLoader = new ZoneDataLoader(_zoneRepository);
            zoneLoader.LoadDefaultZones();

            _initializer = new FactionInitializer(_factionRepository, _zoneRepository, _allocationService);
        }

        #region Constructor Tests

        [Fact]
        public void Constructor_ShouldThrowOnNullFactionRepository()
        {
            Assert.Throws<System.ArgumentNullException>(() =>
                new FactionInitializer(null!, _zoneRepository, _allocationService));
        }

        [Fact]
        public void Constructor_ShouldThrowOnNullZoneRepository()
        {
            Assert.Throws<System.ArgumentNullException>(() =>
                new FactionInitializer(_factionRepository, null!, _allocationService));
        }

        [Fact]
        public void Constructor_ShouldThrowOnNullAllocationService()
        {
            Assert.Throws<System.ArgumentNullException>(() =>
                new FactionInitializer(_factionRepository, _zoneRepository, null!));
        }

        #endregion

        #region Initialize Tests - Faction Creation

        [Fact]
        public void Initialize_ShouldCreateThreeFactions()
        {
            // Act
            _initializer.Initialize();

            // Assert
            Assert.Equal(3, _factionRepository.Count);
        }

        [Fact]
        public void Initialize_ShouldCreateMichaelFaction()
        {
            // Act
            _initializer.Initialize();

            // Assert
            var michael = _factionRepository.GetById(CharacterModelFactionDetector.MichaelFactionId);
            Assert.NotNull(michael);
            Assert.Equal("Michael", michael!.Name);
            Assert.Equal("Michael De Santa", michael.Leader);
        }

        [Fact]
        public void Initialize_ShouldCreateTrevorFaction()
        {
            // Act
            _initializer.Initialize();

            // Assert
            var trevor = _factionRepository.GetById(CharacterModelFactionDetector.TrevorFactionId);
            Assert.NotNull(trevor);
            Assert.Equal("Trevor", trevor!.Name);
            Assert.Equal("Trevor Philips", trevor.Leader);
        }

        [Fact]
        public void Initialize_ShouldCreateFranklinFaction()
        {
            // Act
            _initializer.Initialize();

            // Assert
            var franklin = _factionRepository.GetById(CharacterModelFactionDetector.FranklinFactionId);
            Assert.NotNull(franklin);
            Assert.Equal("Franklin", franklin!.Name);
            Assert.Equal("Franklin Clinton", franklin.Leader);
        }

        [Fact]
        public void Initialize_MichaelFaction_ShouldHaveBlueColor()
        {
            // Act
            _initializer.Initialize();

            // Assert
            var michael = _factionRepository.GetById(CharacterModelFactionDetector.MichaelFactionId);
            Assert.NotNull(michael);
            // Blue color for Michael
            Assert.Equal(0, michael!.Color.R);
            Assert.Equal(128, michael.Color.G);
            Assert.Equal(255, michael.Color.B);
        }

        [Fact]
        public void Initialize_TrevorFaction_ShouldHaveOrangeColor()
        {
            // Act
            _initializer.Initialize();

            // Assert
            var trevor = _factionRepository.GetById(CharacterModelFactionDetector.TrevorFactionId);
            Assert.NotNull(trevor);
            // Orange color for Trevor
            Assert.Equal(255, trevor!.Color.R);
            Assert.Equal(128, trevor.Color.G);
            Assert.Equal(0, trevor.Color.B);
        }

        [Fact]
        public void Initialize_FranklinFaction_ShouldHaveGreenColor()
        {
            // Act
            _initializer.Initialize();

            // Assert
            var franklin = _factionRepository.GetById(CharacterModelFactionDetector.FranklinFactionId);
            Assert.NotNull(franklin);
            // Green color for Franklin
            Assert.Equal(0, franklin!.Color.R);
            Assert.Equal(200, franklin.Color.G);
            Assert.Equal(0, franklin.Color.B);
        }

        #endregion

        #region Initialize Tests - Starting Resources (NORMALIZED)

        [Fact]
        public void Initialize_AllFactions_ShouldHave5kCash()
        {
            // Act
            _initializer.Initialize();

            // Assert - All factions start with equal $5k cash
            var michaelState = _factionRepository.GetState(CharacterModelFactionDetector.MichaelFactionId);
            var trevorState = _factionRepository.GetState(CharacterModelFactionDetector.TrevorFactionId);
            var franklinState = _factionRepository.GetState(CharacterModelFactionDetector.FranklinFactionId);

            Assert.NotNull(michaelState);
            Assert.NotNull(trevorState);
            Assert.NotNull(franklinState);
            Assert.Equal(5000, michaelState!.Cash);
            Assert.Equal(5000, trevorState!.Cash);
            Assert.Equal(5000, franklinState!.Cash);
        }

        [Fact]
        public void Initialize_AllFactions_ShouldHaveStartingReserveTroops()
        {
            // Act
            _initializer.Initialize();

            // Assert - All factions start with 10 reserve troops for AI attacks
            var michaelState = _factionRepository.GetState(CharacterModelFactionDetector.MichaelFactionId);
            var trevorState = _factionRepository.GetState(CharacterModelFactionDetector.TrevorFactionId);
            var franklinState = _factionRepository.GetState(CharacterModelFactionDetector.FranklinFactionId);

            Assert.NotNull(michaelState);
            Assert.NotNull(trevorState);
            Assert.NotNull(franklinState);
            Assert.Equal(10, michaelState!.TroopCount);
            Assert.Equal(10, trevorState!.TroopCount);
            Assert.Equal(10, franklinState!.TroopCount);
        }

        #endregion

        #region Initialize Tests - Starting Zones (NORMALIZED)

        [Fact]
        public void Initialize_AllFactions_ShouldHave3Zones()
        {
            // Act
            _initializer.Initialize();

            // Assert - All factions start with equal 3 zones
            var michaelState = _factionRepository.GetState(CharacterModelFactionDetector.MichaelFactionId);
            var trevorState = _factionRepository.GetState(CharacterModelFactionDetector.TrevorFactionId);
            var franklinState = _factionRepository.GetState(CharacterModelFactionDetector.FranklinFactionId);

            Assert.NotNull(michaelState);
            Assert.NotNull(trevorState);
            Assert.NotNull(franklinState);
            Assert.Equal(3, michaelState!.ZoneCount);
            Assert.Equal(3, trevorState!.ZoneCount);
            Assert.Equal(3, franklinState!.ZoneCount);
        }

        [Fact]
        public void Initialize_ShouldAssignZonesToAllThreeFactions()
        {
            // Act
            _initializer.Initialize();

            // Assert - total zones assigned should be 3 + 3 + 3 = 9
            var michaelState = _factionRepository.GetState(CharacterModelFactionDetector.MichaelFactionId)!;
            var trevorState = _factionRepository.GetState(CharacterModelFactionDetector.TrevorFactionId)!;
            var franklinState = _factionRepository.GetState(CharacterModelFactionDetector.FranklinFactionId)!;

            var totalZones = michaelState.ZoneCount + trevorState.ZoneCount + franklinState.ZoneCount;
            Assert.Equal(9, totalZones);
        }

        [Fact]
        public void Initialize_ShouldSetOwnerIdOnZones()
        {
            // Act
            _initializer.Initialize();

            // Assert - verify some zones have owners assigned
            var michaelState = _factionRepository.GetState(CharacterModelFactionDetector.MichaelFactionId)!;
            foreach (var zoneId in michaelState.OwnedZoneIds)
            {
                var zone = _zoneRepository.GetById(zoneId);
                Assert.NotNull(zone);
                Assert.Equal(CharacterModelFactionDetector.MichaelFactionId, zone!.OwnerFactionId);
            }
        }

        [Fact]
        public void Initialize_MichaelZones_ShouldIncludeRockfordHills()
        {
            // Act
            _initializer.Initialize();

            // Assert - Michael's territory should include upscale areas
            var state = _factionRepository.GetState(CharacterModelFactionDetector.MichaelFactionId);
            Assert.NotNull(state);
            Assert.Contains("rockford_hills", state!.OwnedZoneIds);
        }

        [Fact]
        public void Initialize_TrevorZones_ShouldIncludeSandyShores()
        {
            // Act
            _initializer.Initialize();

            // Assert - Trevor's territory should include Sandy Shores
            var state = _factionRepository.GetState(CharacterModelFactionDetector.TrevorFactionId);
            Assert.NotNull(state);
            Assert.Contains("sandy_shores", state!.OwnedZoneIds);
        }

        [Fact]
        public void Initialize_FranklinZones_ShouldIncludeDavis()
        {
            // Act
            _initializer.Initialize();

            // Assert - Franklin's territory should include Davis (his home area)
            var state = _factionRepository.GetState(CharacterModelFactionDetector.FranklinFactionId);
            Assert.NotNull(state);
            Assert.Contains("davis", state!.OwnedZoneIds);
        }

        #endregion

        #region Initialize Tests - Troop Allocation

        [Fact]
        public void Initialize_ShouldAllocate5BasicTroopsToEachStartingZone()
        {
            // Act
            _initializer.Initialize();

            // Assert - Each starting zone should have 5 Basic troops allocated
            var michaelState = _factionRepository.GetState(CharacterModelFactionDetector.MichaelFactionId)!;
            foreach (var zoneId in michaelState.OwnedZoneIds)
            {
                var allocation = _allocationService.GetAllocation(CharacterModelFactionDetector.MichaelFactionId, zoneId);
                Assert.NotNull(allocation);
                Assert.Equal(5, allocation!.GetTroopCount(FactionWars.Core.Models.DefenderRole.Grunt));
            }

            var trevorState = _factionRepository.GetState(CharacterModelFactionDetector.TrevorFactionId)!;
            foreach (var zoneId in trevorState.OwnedZoneIds)
            {
                var allocation = _allocationService.GetAllocation(CharacterModelFactionDetector.TrevorFactionId, zoneId);
                Assert.NotNull(allocation);
                Assert.Equal(5, allocation!.GetTroopCount(FactionWars.Core.Models.DefenderRole.Grunt));
            }

            var franklinState = _factionRepository.GetState(CharacterModelFactionDetector.FranklinFactionId)!;
            foreach (var zoneId in franklinState.OwnedZoneIds)
            {
                var allocation = _allocationService.GetAllocation(CharacterModelFactionDetector.FranklinFactionId, zoneId);
                Assert.NotNull(allocation);
                Assert.Equal(5, allocation!.GetTroopCount(FactionWars.Core.Models.DefenderRole.Grunt));
            }
        }

        [Fact]
        public void Initialize_TotalAllocatedTroops_ShouldBe45()
        {
            // Act
            _initializer.Initialize();

            // Assert - 9 zones * 5 troops = 45 total allocated troops
            var michaelTroops = _allocationService.GetTotalAllocatedTroops(CharacterModelFactionDetector.MichaelFactionId);
            var trevorTroops = _allocationService.GetTotalAllocatedTroops(CharacterModelFactionDetector.TrevorFactionId);
            var franklinTroops = _allocationService.GetTotalAllocatedTroops(CharacterModelFactionDetector.FranklinFactionId);

            Assert.Equal(15, michaelTroops);  // 3 zones * 5 troops
            Assert.Equal(15, trevorTroops);   // 3 zones * 5 troops
            Assert.Equal(15, franklinTroops); // 3 zones * 5 troops
            Assert.Equal(45, michaelTroops + trevorTroops + franklinTroops);
        }

        #endregion

        #region Initialize Tests - Zone Uniqueness

        [Fact]
        public void Initialize_ShouldNotAssignSameZoneToMultipleFactions()
        {
            // Act
            _initializer.Initialize();

            // Assert
            var michaelState = _factionRepository.GetState(CharacterModelFactionDetector.MichaelFactionId)!;
            var trevorState = _factionRepository.GetState(CharacterModelFactionDetector.TrevorFactionId)!;
            var franklinState = _factionRepository.GetState(CharacterModelFactionDetector.FranklinFactionId)!;

            var michaelZones = michaelState.OwnedZoneIds.ToHashSet();
            var trevorZones = trevorState.OwnedZoneIds.ToHashSet();
            var franklinZones = franklinState.OwnedZoneIds.ToHashSet();

            // No overlap between Michael and Trevor
            Assert.Empty(michaelZones.Intersect(trevorZones));
            // No overlap between Michael and Franklin
            Assert.Empty(michaelZones.Intersect(franklinZones));
            // No overlap between Trevor and Franklin
            Assert.Empty(trevorZones.Intersect(franklinZones));
        }

        #endregion

        #region Initialize Tests - All Factions Active

        [Fact]
        public void Initialize_AllFactions_ShouldBeActive()
        {
            // Act
            _initializer.Initialize();

            // Assert
            var michael = _factionRepository.GetById(CharacterModelFactionDetector.MichaelFactionId);
            var trevor = _factionRepository.GetById(CharacterModelFactionDetector.TrevorFactionId);
            var franklin = _factionRepository.GetById(CharacterModelFactionDetector.FranklinFactionId);

            Assert.True(michael!.IsActive);
            Assert.True(trevor!.IsActive);
            Assert.True(franklin!.IsActive);
        }

        #endregion

        #region Double Initialize Tests

        [Fact]
        public void Initialize_CalledTwice_ShouldThrow()
        {
            // Arrange
            _initializer.Initialize();

            // Act & Assert
            Assert.Throws<System.InvalidOperationException>(() => _initializer.Initialize());
        }

        #endregion

        #region IsInitialized Tests

        [Fact]
        public void IsInitialized_BeforeInitialize_ShouldBeFalse()
        {
            // Assert
            Assert.False(_initializer.IsInitialized);
        }

        [Fact]
        public void IsInitialized_AfterInitialize_ShouldBeTrue()
        {
            // Act
            _initializer.Initialize();

            // Assert
            Assert.True(_initializer.IsInitialized);
        }

        #endregion
    }
}
