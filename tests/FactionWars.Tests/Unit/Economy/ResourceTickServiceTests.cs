using System;
using System.Collections.Generic;
using FactionWars.Core.Interfaces;
using FactionWars.Economy.Interfaces;
using FactionWars.Economy.Models;
using FactionWars.Economy.Services;
using FactionWars.Factions.Interfaces;
using FactionWars.Factions.Models;
using FactionWars.Territory.Interfaces;
using FactionWars.Territory.Models;
using Moq;
using Xunit;

namespace FactionWars.Tests.Unit.Economy
{
    /// <summary>
    /// Tests for the ResourceTickService which handles periodic resource generation.
    /// </summary>
    public class ResourceTickServiceTests
    {
        private readonly Mock<IFactionService> _mockFactionService;
        private readonly Mock<IZoneService> _mockZoneService;
        private readonly Mock<IZoneTraitResourceModifier> _mockModifier;
        private readonly Mock<ISupplyLineService> _mockSupplyLineService;
        private readonly ResourceTickService _service;

        private const int DefaultTickInterval = 300; // 5 minutes in seconds

        public ResourceTickServiceTests()
        {
            _mockFactionService = new Mock<IFactionService>();
            _mockZoneService = new Mock<IZoneService>();
            _mockModifier = new Mock<IZoneTraitResourceModifier>();
            _mockSupplyLineService = new Mock<ISupplyLineService>();

            // Default supply line efficiency is 1.0 (full efficiency)
            _mockSupplyLineService.Setup(s => s.GetSupplyLineEfficiency(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(1.0f);

            _service = new ResourceTickService(
                _mockFactionService.Object,
                _mockZoneService.Object,
                _mockModifier.Object,
                _mockSupplyLineService.Object,
                DefaultTickInterval);
        }

        #region Constructor Tests

        [Fact]
        public void Constructor_WithValidDependencies_CreatesInstance()
        {
            // Arrange & Act
            var service = new ResourceTickService(
                _mockFactionService.Object,
                _mockZoneService.Object,
                _mockModifier.Object,
                _mockSupplyLineService.Object,
                DefaultTickInterval);

            // Assert
            Assert.NotNull(service);
        }

        [Fact]
        public void Constructor_WithNullFactionService_ThrowsArgumentNullException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() =>
                new ResourceTickService(null!, _mockZoneService.Object, _mockModifier.Object, _mockSupplyLineService.Object, DefaultTickInterval));
            Assert.Equal("factionService", exception.ParamName);
        }

        [Fact]
        public void Constructor_WithNullZoneService_ThrowsArgumentNullException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() =>
                new ResourceTickService(_mockFactionService.Object, null!, _mockModifier.Object, _mockSupplyLineService.Object, DefaultTickInterval));
            Assert.Equal("zoneService", exception.ParamName);
        }

        [Fact]
        public void Constructor_WithNullModifier_ThrowsArgumentNullException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() =>
                new ResourceTickService(_mockFactionService.Object, _mockZoneService.Object, null!, _mockSupplyLineService.Object, DefaultTickInterval));
            Assert.Equal("resourceModifier", exception.ParamName);
        }

        [Fact]
        public void Constructor_WithNullSupplyLineService_ThrowsArgumentNullException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() =>
                new ResourceTickService(_mockFactionService.Object, _mockZoneService.Object, _mockModifier.Object, null!, DefaultTickInterval));
            Assert.Equal("supplyLineService", exception.ParamName);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(-100)]
        public void Constructor_WithNonPositiveInterval_ThrowsArgumentOutOfRangeException(int invalidInterval)
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
                new ResourceTickService(
                    _mockFactionService.Object,
                    _mockZoneService.Object,
                    _mockModifier.Object,
                    _mockSupplyLineService.Object,
                    invalidInterval));
            Assert.Equal("tickIntervalSeconds", exception.ParamName);
        }

        [Fact]
        public void Constructor_SetsTickInterval()
        {
            // Arrange & Act
            var service = new ResourceTickService(
                _mockFactionService.Object,
                _mockZoneService.Object,
                _mockModifier.Object,
                _mockSupplyLineService.Object,
                600);

            // Assert
            Assert.Equal(600, service.TickIntervalSeconds);
        }

        #endregion

        #region Initial State Tests

        [Fact]
        public void InitialState_IsNotRunning()
        {
            Assert.False(_service.IsRunning);
        }

        [Fact]
        public void InitialState_TimeUntilNextTick_EqualsInterval()
        {
            Assert.Equal(DefaultTickInterval, _service.TimeUntilNextTick);
        }

        [Fact]
        public void InitialState_TickProgress_IsZero()
        {
            Assert.Equal(0f, _service.TickProgress);
        }

        #endregion

        #region Start/Stop Tests

        [Fact]
        public void Start_SetsIsRunningToTrue()
        {
            // Act
            _service.Start();

            // Assert
            Assert.True(_service.IsRunning);
        }

        [Fact]
        public void Stop_SetsIsRunningToFalse()
        {
            // Arrange
            _service.Start();

            // Act
            _service.Stop();

            // Assert
            Assert.False(_service.IsRunning);
        }

        [Fact]
        public void Stop_WhenAlreadyStopped_RemainsNotRunning()
        {
            // Act
            _service.Stop();

            // Assert
            Assert.False(_service.IsRunning);
        }

        [Fact]
        public void Start_AfterStop_IsRunningAgain()
        {
            // Arrange
            _service.Start();
            _service.Stop();

            // Act
            _service.Start();

            // Assert
            Assert.True(_service.IsRunning);
        }

        #endregion

        #region Reset Tests

        [Fact]
        public void Reset_ResetsTimeUntilNextTick()
        {
            // Arrange
            _service.Start();
            _service.Update(100f); // Advance 100 seconds

            // Act
            _service.Reset();

            // Assert
            Assert.Equal(DefaultTickInterval, _service.TimeUntilNextTick);
        }

        [Fact]
        public void Reset_ResetsTickProgress()
        {
            // Arrange
            _service.Start();
            _service.Update(150f); // 50% progress

            // Act
            _service.Reset();

            // Assert
            Assert.Equal(0f, _service.TickProgress);
        }

        [Fact]
        public void Reset_DoesNotChangeRunningState()
        {
            // Arrange
            _service.Start();

            // Act
            _service.Reset();

            // Assert
            Assert.True(_service.IsRunning);
        }

        #endregion

        #region SetTickInterval Tests

        [Fact]
        public void SetTickInterval_WithValidValue_UpdatesInterval()
        {
            // Act
            _service.SetTickInterval(600);

            // Assert
            Assert.Equal(600, _service.TickIntervalSeconds);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(-100)]
        public void SetTickInterval_WithNonPositiveValue_ThrowsArgumentOutOfRangeException(int invalidInterval)
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
                _service.SetTickInterval(invalidInterval));
            Assert.Equal("seconds", exception.ParamName);
        }

        [Fact]
        public void SetTickInterval_RecalculatesTimeUntilNextTick()
        {
            // Arrange
            _service.Start();
            _service.Update(100f); // 100 seconds elapsed

            // Act
            _service.SetTickInterval(600); // Change to 600 seconds

            // Assert
            Assert.Equal(500f, _service.TimeUntilNextTick); // 600 - 100 = 500
        }

        [Fact]
        public void SetTickInterval_WhenNewIntervalLessThanElapsed_TriggersTicksForMissedIntervals()
        {
            // Arrange
            SetupSingleFactionWithZone();
            _service.Start();
            _service.Update(200f); // 200 seconds elapsed (doesn't tick with 300s interval)
            var tickCount = 0;
            _service.OnResourceTick += (_, _) => tickCount++;

            // Act
            _service.SetTickInterval(100); // New interval is 100s, so 200/100 = 2 complete ticks

            // Assert - 2 ticks because 200 elapsed is 2 complete 100-second intervals
            Assert.Equal(2, tickCount);
        }

        #endregion

        #region Update Tests - Timer Progression

        [Fact]
        public void Update_WhenNotRunning_DoesNotAdvanceTimer()
        {
            // Act
            _service.Update(100f);

            // Assert
            Assert.Equal(DefaultTickInterval, _service.TimeUntilNextTick);
        }

        [Fact]
        public void Update_WhenRunning_AdvancesTimer()
        {
            // Arrange
            _service.Start();

            // Act
            _service.Update(100f);

            // Assert
            Assert.Equal(DefaultTickInterval - 100f, _service.TimeUntilNextTick);
        }

        [Theory]
        [InlineData(150f, 50f)] // 150/300 = 50%
        [InlineData(75f, 25f)]  // 75/300 = 25%
        [InlineData(300f, 0f)] // Full tick, resets to 0
        public void Update_CalculatesCorrectTickProgress(float elapsed, float expectedProgress)
        {
            // Arrange
            _service.Start();

            // Act
            _service.Update(elapsed);

            // Assert
            Assert.Equal(expectedProgress, _service.TickProgress, precision: 1);
        }

        [Fact]
        public void Update_WithNegativeDeltaTime_DoesNotAdvanceTimer()
        {
            // Arrange
            _service.Start();
            _service.Update(100f);

            // Act
            _service.Update(-50f);

            // Assert
            Assert.Equal(DefaultTickInterval - 100f, _service.TimeUntilNextTick);
        }

        #endregion

        #region Update Tests - Tick Triggering

        [Fact]
        public void Update_WhenTickCompletes_RaisesOnResourceTickEvent()
        {
            // Arrange
            SetupSingleFactionWithZone();
            _service.Start();
            var eventRaised = false;
            _service.OnResourceTick += (_, _) => eventRaised = true;

            // Act
            _service.Update(DefaultTickInterval);

            // Assert
            Assert.True(eventRaised);
        }

        [Fact]
        public void Update_WhenTickCompletes_ResetsTimer()
        {
            // Arrange
            SetupSingleFactionWithZone();
            _service.Start();

            // Act
            _service.Update(DefaultTickInterval);

            // Assert
            Assert.Equal(DefaultTickInterval, _service.TimeUntilNextTick);
        }

        [Fact]
        public void Update_WhenMultipleTicksComplete_TriggersMultipleTicks()
        {
            // Arrange
            SetupSingleFactionWithZone();
            _service.Start();
            var tickCount = 0;
            _service.OnResourceTick += (_, _) => tickCount++;

            // Act
            _service.Update(DefaultTickInterval * 3); // 3 full ticks

            // Assert
            Assert.Equal(3, tickCount);
        }

        [Fact]
        public void Update_WhenTickOvershoots_CarriesOverExcessTime()
        {
            // Arrange
            SetupSingleFactionWithZone();
            _service.Start();

            // Act
            _service.Update(DefaultTickInterval + 50f); // Overshoot by 50 seconds

            // Assert
            Assert.Equal(DefaultTickInterval - 50f, _service.TimeUntilNextTick);
        }

        #endregion

        #region ForceTick Tests

        [Fact]
        public void ForceTick_TriggersTickImmediately()
        {
            // Arrange
            SetupSingleFactionWithZone();
            var eventRaised = false;
            _service.OnResourceTick += (_, _) => eventRaised = true;

            // Act
            _service.ForceTick();

            // Assert
            Assert.True(eventRaised);
        }

        [Fact]
        public void ForceTick_DoesNotResetTimer()
        {
            // Arrange
            _service.Start();
            _service.Update(100f);

            // Act
            _service.ForceTick();

            // Assert
            Assert.Equal(DefaultTickInterval - 100f, _service.TimeUntilNextTick);
        }

        [Fact]
        public void ForceTick_WorksWhenServiceIsStopped()
        {
            // Arrange
            SetupSingleFactionWithZone();
            var eventRaised = false;
            _service.OnResourceTick += (_, _) => eventRaised = true;

            // Act
            _service.ForceTick();

            // Assert
            Assert.True(eventRaised);
        }

        #endregion

        #region Resource Generation Tests

        [Fact]
        public void Tick_WithNoActiveFactions_DoesNotRaiseEvents()
        {
            // Arrange
            _mockFactionService.Setup(f => f.GetActiveFactions())
                .Returns(Array.Empty<Faction>());
            var eventRaised = false;
            _service.OnResourceTick += (_, _) => eventRaised = true;

            // Act
            _service.ForceTick();

            // Assert
            Assert.False(eventRaised);
        }

        [Fact]
        public void Tick_WithFactionHavingNoZones_RaisesEventWithZeroResources()
        {
            // Arrange
            var faction = CreateFaction("faction1");
            var state = new FactionState("faction1", 0, 0);
            _mockFactionService.Setup(f => f.GetActiveFactions())
                .Returns(new[] { faction });
            _mockFactionService.Setup(f => f.GetFactionState("faction1"))
                .Returns(state);
            _mockZoneService.Setup(z => z.GetZonesByOwner("faction1"))
                .Returns(Array.Empty<Zone>());

            ResourceTickEventArgs? receivedArgs = null;
            _service.OnResourceTick += (_, args) => receivedArgs = args;

            // Act
            _service.ForceTick();

            // Assert
            Assert.NotNull(receivedArgs);
            Assert.Equal("faction1", receivedArgs.FactionId);
            Assert.Equal(0, receivedArgs.CashGenerated);
            Assert.Equal(0, receivedArgs.RecruitmentGenerated);
            Assert.Equal(0, receivedArgs.WeaponsGenerated);
        }

        [Fact]
        public void Tick_WithSingleZone_CalculatesResourcesCorrectly()
        {
            // Arrange
            var faction = CreateFaction("faction1");
            var state = new FactionState("faction1", 0, 0);
            var zone = CreateZone("zone1", "faction1", ZoneTrait.None, 1);

            _mockFactionService.Setup(f => f.GetActiveFactions())
                .Returns(new[] { faction });
            _mockFactionService.Setup(f => f.GetFactionState("faction1"))
                .Returns(state);
            _mockZoneService.Setup(z => z.GetZonesByOwner("faction1"))
                .Returns(new[] { zone });
            SetupModifierForNoBonus();

            ResourceTickEventArgs? receivedArgs = null;
            _service.OnResourceTick += (_, args) => receivedArgs = args;

            // Act
            _service.ForceTick();

            // Assert
            Assert.NotNull(receivedArgs);
            // Base rates: Cash=100, Recruitment=10, Weapons=5 (from ResourceTypeInfo)
            Assert.Equal(100, receivedArgs.CashGenerated);
            Assert.Equal(10, receivedArgs.RecruitmentGenerated);
            Assert.Equal(5, receivedArgs.WeaponsGenerated);
        }

        [Fact]
        public void Tick_WithStrategicValueMultiplier_AppliesMultiplier()
        {
            // Arrange
            var faction = CreateFaction("faction1");
            var state = new FactionState("faction1", 0, 0);
            var zone = CreateZone("zone1", "faction1", ZoneTrait.None, 2); // 2x strategic value

            _mockFactionService.Setup(f => f.GetActiveFactions())
                .Returns(new[] { faction });
            _mockFactionService.Setup(f => f.GetFactionState("faction1"))
                .Returns(state);
            _mockZoneService.Setup(z => z.GetZonesByOwner("faction1"))
                .Returns(new[] { zone });
            SetupModifierForNoBonus();

            ResourceTickEventArgs? receivedArgs = null;
            _service.OnResourceTick += (_, args) => receivedArgs = args;

            // Act
            _service.ForceTick();

            // Assert
            Assert.NotNull(receivedArgs);
            Assert.Equal(200, receivedArgs.CashGenerated);
            Assert.Equal(20, receivedArgs.RecruitmentGenerated);
            Assert.Equal(10, receivedArgs.WeaponsGenerated);
        }

        [Fact]
        public void Tick_WithTraitBonus_AppliesModifier()
        {
            // Arrange
            var faction = CreateFaction("faction1");
            var state = new FactionState("faction1", 0, 0);
            var zone = CreateZone("zone1", "faction1", ZoneTrait.Commercial, 1);

            _mockFactionService.Setup(f => f.GetActiveFactions())
                .Returns(new[] { faction });
            _mockFactionService.Setup(f => f.GetFactionState("faction1"))
                .Returns(state);
            _mockZoneService.Setup(z => z.GetZonesByOwner("faction1"))
                .Returns(new[] { zone });

            // Commercial gives +50% cash
            _mockModifier.Setup(m => m.GetModifier(ZoneTrait.Commercial, ResourceType.Cash))
                .Returns(1.5f);
            _mockModifier.Setup(m => m.GetModifier(ZoneTrait.Commercial, ResourceType.Recruitment))
                .Returns(1.0f);
            _mockModifier.Setup(m => m.GetModifier(ZoneTrait.Commercial, ResourceType.Weapons))
                .Returns(1.0f);

            ResourceTickEventArgs? receivedArgs = null;
            _service.OnResourceTick += (_, args) => receivedArgs = args;

            // Act
            _service.ForceTick();

            // Assert
            Assert.NotNull(receivedArgs);
            Assert.Equal(150, receivedArgs.CashGenerated); // 100 * 1.5
            Assert.Equal(10, receivedArgs.RecruitmentGenerated);
            Assert.Equal(5, receivedArgs.WeaponsGenerated);
        }

        [Fact]
        public void Tick_WithMultipleZones_SumsResources()
        {
            // Arrange
            var faction = CreateFaction("faction1");
            var state = new FactionState("faction1", 0, 0);
            var zone1 = CreateZone("zone1", "faction1", ZoneTrait.None, 1);
            var zone2 = CreateZone("zone2", "faction1", ZoneTrait.None, 1);

            _mockFactionService.Setup(f => f.GetActiveFactions())
                .Returns(new[] { faction });
            _mockFactionService.Setup(f => f.GetFactionState("faction1"))
                .Returns(state);
            _mockZoneService.Setup(z => z.GetZonesByOwner("faction1"))
                .Returns(new[] { zone1, zone2 });
            SetupModifierForNoBonus();

            ResourceTickEventArgs? receivedArgs = null;
            _service.OnResourceTick += (_, args) => receivedArgs = args;

            // Act
            _service.ForceTick();

            // Assert
            Assert.NotNull(receivedArgs);
            Assert.Equal(200, receivedArgs.CashGenerated); // 100 + 100
            Assert.Equal(20, receivedArgs.RecruitmentGenerated); // 10 + 10
            Assert.Equal(10, receivedArgs.WeaponsGenerated); // 5 + 5
        }

        [Fact]
        public void Tick_WithMultipleFactions_RaisesEventForEach()
        {
            // Arrange
            var faction1 = CreateFaction("faction1");
            var faction2 = CreateFaction("faction2");
            var state1 = new FactionState("faction1", 0, 0);
            var state2 = new FactionState("faction2", 0, 0);
            var zone1 = CreateZone("zone1", "faction1", ZoneTrait.None, 1);
            var zone2 = CreateZone("zone2", "faction2", ZoneTrait.None, 1);

            _mockFactionService.Setup(f => f.GetActiveFactions())
                .Returns(new[] { faction1, faction2 });
            _mockFactionService.Setup(f => f.GetFactionState("faction1"))
                .Returns(state1);
            _mockFactionService.Setup(f => f.GetFactionState("faction2"))
                .Returns(state2);
            _mockZoneService.Setup(z => z.GetZonesByOwner("faction1"))
                .Returns(new[] { zone1 });
            _mockZoneService.Setup(z => z.GetZonesByOwner("faction2"))
                .Returns(new[] { zone2 });
            SetupModifierForNoBonus();

            var receivedFactionIds = new List<string>();
            _service.OnResourceTick += (_, args) => receivedFactionIds.Add(args.FactionId);

            // Act
            _service.ForceTick();

            // Assert
            Assert.Equal(2, receivedFactionIds.Count);
            Assert.Contains("faction1", receivedFactionIds);
            Assert.Contains("faction2", receivedFactionIds);
        }

        [Fact]
        public void Tick_AddsCashToFaction()
        {
            // Arrange
            SetupSingleFactionWithZone();

            // Act
            _service.ForceTick();

            // Assert
            _mockFactionService.Verify(f => f.AddCash("faction1", It.Is<int>(c => c > 0)), Times.Once);
        }

        [Fact]
        public void Tick_AddsWeaponsToFaction()
        {
            // Arrange
            SetupSingleFactionWithZone();

            // Act
            _service.ForceTick();

            // Assert
            _mockFactionService.Verify(f => f.AddWeapons("faction1", It.Is<int>(w => w > 0)), Times.Once);
        }

        #endregion

        #region Event Args Tests

        [Fact]
        public void ResourceTickEventArgs_WithNullFactionId_ThrowsArgumentNullException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() =>
                new ResourceTickEventArgs(null!, 100, 10, 5));
            Assert.Equal("factionId", exception.ParamName);
        }

        [Fact]
        public void ResourceTickEventArgs_StoresAllValues()
        {
            // Arrange & Act
            var args = new ResourceTickEventArgs("faction1", 100, 50, 25);

            // Assert
            Assert.Equal("faction1", args.FactionId);
            Assert.Equal(100, args.CashGenerated);
            Assert.Equal(50, args.RecruitmentGenerated);
            Assert.Equal(25, args.WeaponsGenerated);
        }

        #endregion

        #region Supply Line Efficiency Tests

        [Fact]
        public void Tick_WithDisconnectedZone_AppliesReducedEfficiency()
        {
            // Arrange
            var faction = CreateFaction("faction1");
            var state = new FactionState("faction1", 0, 0);
            var zone = CreateZone("zone1", "faction1", ZoneTrait.None, 1);

            _mockFactionService.Setup(f => f.GetActiveFactions())
                .Returns(new[] { faction });
            _mockFactionService.Setup(f => f.GetFactionState("faction1"))
                .Returns(state);
            _mockZoneService.Setup(z => z.GetZonesByOwner("faction1"))
                .Returns(new[] { zone });
            SetupModifierForNoBonus();

            // Disconnected zone has 50% efficiency
            _mockSupplyLineService.Setup(s => s.GetSupplyLineEfficiency("faction1", "zone1"))
                .Returns(0.5f);

            ResourceTickEventArgs? receivedArgs = null;
            _service.OnResourceTick += (_, args) => receivedArgs = args;

            // Act
            _service.ForceTick();

            // Assert
            Assert.NotNull(receivedArgs);
            // Base rates: Cash=100, Recruitment=10, Weapons=5 multiplied by 0.5 efficiency
            Assert.Equal(50, receivedArgs.CashGenerated);
            Assert.Equal(5, receivedArgs.RecruitmentGenerated);
            Assert.Equal(2, receivedArgs.WeaponsGenerated); // 5 * 0.5 = 2.5, truncated to 2
        }

        [Fact]
        public void Tick_WithConnectedZone_AppliesFullEfficiency()
        {
            // Arrange
            var faction = CreateFaction("faction1");
            var state = new FactionState("faction1", 0, 0);
            var zone = CreateZone("zone1", "faction1", ZoneTrait.None, 1);

            _mockFactionService.Setup(f => f.GetActiveFactions())
                .Returns(new[] { faction });
            _mockFactionService.Setup(f => f.GetFactionState("faction1"))
                .Returns(state);
            _mockZoneService.Setup(z => z.GetZonesByOwner("faction1"))
                .Returns(new[] { zone });
            SetupModifierForNoBonus();

            // Connected zone has 100% efficiency
            _mockSupplyLineService.Setup(s => s.GetSupplyLineEfficiency("faction1", "zone1"))
                .Returns(1.0f);

            ResourceTickEventArgs? receivedArgs = null;
            _service.OnResourceTick += (_, args) => receivedArgs = args;

            // Act
            _service.ForceTick();

            // Assert
            Assert.NotNull(receivedArgs);
            Assert.Equal(100, receivedArgs.CashGenerated);
            Assert.Equal(10, receivedArgs.RecruitmentGenerated);
            Assert.Equal(5, receivedArgs.WeaponsGenerated);
        }

        [Fact]
        public void Tick_WithMixedConnectivity_AppliesCorrectEfficiencyToEachZone()
        {
            // Arrange
            var faction = CreateFaction("faction1");
            var state = new FactionState("faction1", 0, 0);
            var connectedZone = CreateZone("zone1", "faction1", ZoneTrait.None, 1);
            var disconnectedZone = CreateZone("zone2", "faction1", ZoneTrait.None, 1);

            _mockFactionService.Setup(f => f.GetActiveFactions())
                .Returns(new[] { faction });
            _mockFactionService.Setup(f => f.GetFactionState("faction1"))
                .Returns(state);
            _mockZoneService.Setup(z => z.GetZonesByOwner("faction1"))
                .Returns(new[] { connectedZone, disconnectedZone });
            SetupModifierForNoBonus();

            // Connected zone = full efficiency, disconnected zone = half efficiency
            _mockSupplyLineService.Setup(s => s.GetSupplyLineEfficiency("faction1", "zone1"))
                .Returns(1.0f);
            _mockSupplyLineService.Setup(s => s.GetSupplyLineEfficiency("faction1", "zone2"))
                .Returns(0.5f);

            ResourceTickEventArgs? receivedArgs = null;
            _service.OnResourceTick += (_, args) => receivedArgs = args;

            // Act
            _service.ForceTick();

            // Assert
            Assert.NotNull(receivedArgs);
            // Connected: 100 + Disconnected: 50 = 150 cash
            Assert.Equal(150, receivedArgs.CashGenerated);
            // Connected: 10 + Disconnected: 5 = 15 recruitment
            Assert.Equal(15, receivedArgs.RecruitmentGenerated);
            // Connected: 5 + Disconnected: 2 = 7 weapons
            Assert.Equal(7, receivedArgs.WeaponsGenerated);
        }

        [Fact]
        public void Tick_SupplyLineEfficiencyStacksWithTraitBonus()
        {
            // Arrange
            var faction = CreateFaction("faction1");
            var state = new FactionState("faction1", 0, 0);
            var zone = CreateZone("zone1", "faction1", ZoneTrait.Commercial, 1);

            _mockFactionService.Setup(f => f.GetActiveFactions())
                .Returns(new[] { faction });
            _mockFactionService.Setup(f => f.GetFactionState("faction1"))
                .Returns(state);
            _mockZoneService.Setup(z => z.GetZonesByOwner("faction1"))
                .Returns(new[] { zone });

            // Commercial gives +50% cash
            _mockModifier.Setup(m => m.GetModifier(ZoneTrait.Commercial, ResourceType.Cash))
                .Returns(1.5f);
            _mockModifier.Setup(m => m.GetModifier(ZoneTrait.Commercial, ResourceType.Recruitment))
                .Returns(1.0f);
            _mockModifier.Setup(m => m.GetModifier(ZoneTrait.Commercial, ResourceType.Weapons))
                .Returns(1.0f);

            // 50% supply line efficiency (disconnected)
            _mockSupplyLineService.Setup(s => s.GetSupplyLineEfficiency("faction1", "zone1"))
                .Returns(0.5f);

            ResourceTickEventArgs? receivedArgs = null;
            _service.OnResourceTick += (_, args) => receivedArgs = args;

            // Act
            _service.ForceTick();

            // Assert
            Assert.NotNull(receivedArgs);
            // Base: 100 * trait: 1.5 * efficiency: 0.5 = 75 cash
            Assert.Equal(75, receivedArgs.CashGenerated);
            // Base: 10 * trait: 1.0 * efficiency: 0.5 = 5 recruitment
            Assert.Equal(5, receivedArgs.RecruitmentGenerated);
            // Base: 5 * trait: 1.0 * efficiency: 0.5 = 2 weapons (truncated from 2.5)
            Assert.Equal(2, receivedArgs.WeaponsGenerated);
        }

        [Fact]
        public void Tick_SupplyLineEfficiencyStacksWithStrategicValue()
        {
            // Arrange
            var faction = CreateFaction("faction1");
            var state = new FactionState("faction1", 0, 0);
            var zone = CreateZone("zone1", "faction1", ZoneTrait.None, 2); // 2x strategic value

            _mockFactionService.Setup(f => f.GetActiveFactions())
                .Returns(new[] { faction });
            _mockFactionService.Setup(f => f.GetFactionState("faction1"))
                .Returns(state);
            _mockZoneService.Setup(z => z.GetZonesByOwner("faction1"))
                .Returns(new[] { zone });
            SetupModifierForNoBonus();

            // 50% supply line efficiency (disconnected)
            _mockSupplyLineService.Setup(s => s.GetSupplyLineEfficiency("faction1", "zone1"))
                .Returns(0.5f);

            ResourceTickEventArgs? receivedArgs = null;
            _service.OnResourceTick += (_, args) => receivedArgs = args;

            // Act
            _service.ForceTick();

            // Assert
            Assert.NotNull(receivedArgs);
            // Base: 100 * strategic: 2 * efficiency: 0.5 = 100 cash
            Assert.Equal(100, receivedArgs.CashGenerated);
            // Base: 10 * strategic: 2 * efficiency: 0.5 = 10 recruitment
            Assert.Equal(10, receivedArgs.RecruitmentGenerated);
            // Base: 5 * strategic: 2 * efficiency: 0.5 = 5 weapons
            Assert.Equal(5, receivedArgs.WeaponsGenerated);
        }

        #endregion

        #region Helper Methods

        private void SetupSingleFactionWithZone()
        {
            var faction = CreateFaction("faction1");
            var state = new FactionState("faction1", 0, 0);
            var zone = CreateZone("zone1", "faction1", ZoneTrait.None, 1);

            _mockFactionService.Setup(f => f.GetActiveFactions())
                .Returns(new[] { faction });
            _mockFactionService.Setup(f => f.GetFactionState("faction1"))
                .Returns(state);
            _mockZoneService.Setup(z => z.GetZonesByOwner("faction1"))
                .Returns(new[] { zone });
            SetupModifierForNoBonus();
        }

        private Faction CreateFaction(string id)
        {
            return new Faction(id, $"Test Faction {id}");
        }

        private Zone CreateZone(string id, string? ownerId, ZoneTrait traits, int strategicValue)
        {
            var zone = new Zone(id, $"Test Zone {id}", new Vector3(0, 0, 0), 100f, strategicValue);
            zone.Traits = traits;
            zone.OwnerFactionId = ownerId;
            return zone;
        }

        private void SetupModifierForNoBonus()
        {
            _mockModifier.Setup(m => m.GetModifier(It.IsAny<ZoneTrait>(), It.IsAny<ResourceType>()))
                .Returns(1.0f);
        }

        #endregion
    }
}
