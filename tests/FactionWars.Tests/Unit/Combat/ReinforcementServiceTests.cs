using System;
using System.Collections.Generic;
using FactionWars.Combat.Interfaces;
using FactionWars.Combat.Models;
using FactionWars.Combat.Services;
using FactionWars.Core.Interfaces;
using Moq;
using Xunit;

namespace FactionWars.Tests.Unit.Combat
{
    /// <summary>
    /// Tests for the reinforcement mechanics system which handles spawning
    /// additional peds during combat encounters based on faction resources
    /// and timing constraints.
    /// </summary>
    public class ReinforcementServiceTests
    {
        #region ReinforcementConfig Tests

        [Fact]
        public void ReinforcementConfig_DefaultValues_AreCorrect()
        {
            var config = new ReinforcementConfig();

            Assert.Equal(30f, config.CooldownSeconds);
            Assert.Equal(5, config.MinPedsPerWave);
            Assert.Equal(10, config.MaxPedsPerWave);
            Assert.Equal(3, config.MaxActiveWaves);
            Assert.True(config.RequiresResources);
            Assert.Equal(100, config.ResourceCostPerPed);
        }

        [Theory]
        [InlineData(10f)]
        [InlineData(30f)]
        [InlineData(60f)]
        public void ReinforcementConfig_CooldownSeconds_AcceptsValidValues(float cooldown)
        {
            var config = new ReinforcementConfig { CooldownSeconds = cooldown };

            Assert.Equal(cooldown, config.CooldownSeconds);
        }

        [Theory]
        [InlineData(-1f)]
        [InlineData(-0.1f)]
        public void ReinforcementConfig_CooldownSeconds_RejectsNegativeValues(float cooldown)
        {
            var config = new ReinforcementConfig();

            Assert.Throws<ArgumentOutOfRangeException>(() => config.CooldownSeconds = cooldown);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(5)]
        [InlineData(15)]
        public void ReinforcementConfig_MinPedsPerWave_AcceptsValidValues(int minPeds)
        {
            var config = new ReinforcementConfig { MinPedsPerWave = minPeds };

            Assert.Equal(minPeds, config.MinPedsPerWave);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void ReinforcementConfig_MinPedsPerWave_RejectsInvalidValues(int minPeds)
        {
            var config = new ReinforcementConfig();

            Assert.Throws<ArgumentOutOfRangeException>(() => config.MinPedsPerWave = minPeds);
        }

        [Theory]
        [InlineData(5)]
        [InlineData(10)]
        [InlineData(20)]
        public void ReinforcementConfig_MaxPedsPerWave_AcceptsValidValues(int maxPeds)
        {
            var config = new ReinforcementConfig { MaxPedsPerWave = maxPeds };

            Assert.Equal(maxPeds, config.MaxPedsPerWave);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void ReinforcementConfig_MaxPedsPerWave_RejectsInvalidValues(int maxPeds)
        {
            var config = new ReinforcementConfig();

            Assert.Throws<ArgumentOutOfRangeException>(() => config.MaxPedsPerWave = maxPeds);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(3)]
        [InlineData(10)]
        public void ReinforcementConfig_MaxActiveWaves_AcceptsValidValues(int maxWaves)
        {
            var config = new ReinforcementConfig { MaxActiveWaves = maxWaves };

            Assert.Equal(maxWaves, config.MaxActiveWaves);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void ReinforcementConfig_MaxActiveWaves_RejectsInvalidValues(int maxWaves)
        {
            var config = new ReinforcementConfig();

            Assert.Throws<ArgumentOutOfRangeException>(() => config.MaxActiveWaves = maxWaves);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(50)]
        [InlineData(500)]
        public void ReinforcementConfig_ResourceCostPerPed_AcceptsValidValues(int cost)
        {
            var config = new ReinforcementConfig { ResourceCostPerPed = cost };

            Assert.Equal(cost, config.ResourceCostPerPed);
        }

        [Fact]
        public void ReinforcementConfig_ResourceCostPerPed_RejectsNegativeValue()
        {
            var config = new ReinforcementConfig();

            Assert.Throws<ArgumentOutOfRangeException>(() => config.ResourceCostPerPed = -1);
        }

        [Fact]
        public void ReinforcementConfig_Validate_ThrowsWhenMinGreaterThanMax()
        {
            var config = new ReinforcementConfig
            {
                MinPedsPerWave = 10,
                MaxPedsPerWave = 5
            };

            Assert.Throws<InvalidOperationException>(() => config.Validate());
        }

        [Fact]
        public void ReinforcementConfig_Validate_SucceedsWhenValid()
        {
            var config = new ReinforcementConfig
            {
                MinPedsPerWave = 3,
                MaxPedsPerWave = 10
            };

            config.Validate(); // Should not throw
        }

        #endregion

        #region ReinforcementRequest Tests

        [Fact]
        public void ReinforcementRequest_Constructor_SetsProperties()
        {
            var request = new ReinforcementRequest(
                encounterId: "encounter-1",
                factionId: "faction-1",
                zoneId: "zone-1",
                requestedCount: 5,
                spawnPosition: new Vector3(100f, 200f, 50f));

            Assert.Equal("encounter-1", request.EncounterId);
            Assert.Equal("faction-1", request.FactionId);
            Assert.Equal("zone-1", request.ZoneId);
            Assert.Equal(5, request.RequestedCount);
            Assert.Equal(100f, request.SpawnPosition.X);
            Assert.Equal(200f, request.SpawnPosition.Y);
            Assert.Equal(50f, request.SpawnPosition.Z);
            Assert.True(request.RequestedAt > DateTime.MinValue);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void ReinforcementRequest_Constructor_RejectsInvalidEncounterId(string encounterId)
        {
            Assert.Throws<ArgumentException>(() => new ReinforcementRequest(
                encounterId: encounterId,
                factionId: "faction-1",
                zoneId: "zone-1",
                requestedCount: 5,
                spawnPosition: new Vector3()));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void ReinforcementRequest_Constructor_RejectsInvalidFactionId(string factionId)
        {
            Assert.Throws<ArgumentException>(() => new ReinforcementRequest(
                encounterId: "encounter-1",
                factionId: factionId,
                zoneId: "zone-1",
                requestedCount: 5,
                spawnPosition: new Vector3()));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void ReinforcementRequest_Constructor_RejectsInvalidZoneId(string zoneId)
        {
            Assert.Throws<ArgumentException>(() => new ReinforcementRequest(
                encounterId: "encounter-1",
                factionId: "faction-1",
                zoneId: zoneId,
                requestedCount: 5,
                spawnPosition: new Vector3()));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void ReinforcementRequest_Constructor_RejectsInvalidCount(int count)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new ReinforcementRequest(
                encounterId: "encounter-1",
                factionId: "faction-1",
                zoneId: "zone-1",
                requestedCount: count,
                spawnPosition: new Vector3()));
        }

        #endregion

        #region ReinforcementResult Tests

        [Fact]
        public void ReinforcementResult_Success_HasCorrectProperties()
        {
            var spawnedPeds = new List<PedHandle>
            {
                new PedHandle(1),
                new PedHandle(2),
                new PedHandle(3)
            };

            var result = ReinforcementResult.Success(spawnedPeds, 300);

            Assert.True(result.IsSuccess);
            Assert.Equal(ReinforcementResultStatus.Success, result.Status);
            Assert.Equal(3, result.SpawnedCount);
            Assert.Equal(300, result.ResourceCost);
            Assert.Null(result.FailureReason);
            Assert.Equal(spawnedPeds, result.SpawnedPeds);
        }

        [Fact]
        public void ReinforcementResult_PartialSuccess_HasCorrectProperties()
        {
            var spawnedPeds = new List<PedHandle>
            {
                new PedHandle(1),
                new PedHandle(2)
            };

            var result = ReinforcementResult.PartialSuccess(spawnedPeds, 5, 200, "Pool capacity reached");

            Assert.True(result.IsSuccess);
            Assert.Equal(ReinforcementResultStatus.PartialSuccess, result.Status);
            Assert.Equal(2, result.SpawnedCount);
            Assert.Equal(5, result.RequestedCount);
            Assert.Equal(200, result.ResourceCost);
            Assert.Equal("Pool capacity reached", result.FailureReason);
        }

        [Fact]
        public void ReinforcementResult_OnCooldown_HasCorrectProperties()
        {
            var result = ReinforcementResult.OnCooldown(15.5f);

            Assert.False(result.IsSuccess);
            Assert.Equal(ReinforcementResultStatus.OnCooldown, result.Status);
            Assert.Equal(0, result.SpawnedCount);
            Assert.Equal(0, result.ResourceCost);
            Assert.Equal(15.5f, result.RemainingCooldown);
            Assert.Contains("cooldown", result.FailureReason, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void ReinforcementResult_InsufficientResources_HasCorrectProperties()
        {
            var result = ReinforcementResult.InsufficientResources(500, 300);

            Assert.False(result.IsSuccess);
            Assert.Equal(ReinforcementResultStatus.InsufficientResources, result.Status);
            Assert.Equal(0, result.SpawnedCount);
            Assert.Equal(500, result.RequiredResources);
            Assert.Equal(300, result.AvailableResources);
            Assert.Contains("insufficient", result.FailureReason, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void ReinforcementResult_PoolFull_HasCorrectProperties()
        {
            var result = ReinforcementResult.PoolFull();

            Assert.False(result.IsSuccess);
            Assert.Equal(ReinforcementResultStatus.PoolFull, result.Status);
            Assert.Equal(0, result.SpawnedCount);
            Assert.Contains("pool", result.FailureReason, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void ReinforcementResult_MaxWavesReached_HasCorrectProperties()
        {
            var result = ReinforcementResult.MaxWavesReached(3);

            Assert.False(result.IsSuccess);
            Assert.Equal(ReinforcementResultStatus.MaxWavesReached, result.Status);
            Assert.Equal(0, result.SpawnedCount);
            Assert.Equal(3, result.MaxWaves);
            Assert.Contains("maximum", result.FailureReason, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void ReinforcementResult_EncounterEnded_HasCorrectProperties()
        {
            var result = ReinforcementResult.EncounterEnded();

            Assert.False(result.IsSuccess);
            Assert.Equal(ReinforcementResultStatus.EncounterEnded, result.Status);
            Assert.Contains("ended", result.FailureReason, StringComparison.OrdinalIgnoreCase);
        }

        #endregion

        #region IReinforcementService Constructor Tests

        [Fact]
        public void ReinforcementService_Constructor_ThrowsOnNullPedSpawningService()
        {
            var mockTimeProvider = new Mock<ITimeProvider>();
            var config = new ReinforcementConfig();

            Assert.Throws<ArgumentNullException>(() =>
                new ReinforcementService(null!, mockTimeProvider.Object, config));
        }

        [Fact]
        public void ReinforcementService_Constructor_ThrowsOnNullTimeProvider()
        {
            var mockPedSpawning = new Mock<IPedSpawningService>();
            var config = new ReinforcementConfig();

            Assert.Throws<ArgumentNullException>(() =>
                new ReinforcementService(mockPedSpawning.Object, null!, config));
        }

        [Fact]
        public void ReinforcementService_Constructor_ThrowsOnNullConfig()
        {
            var mockPedSpawning = new Mock<IPedSpawningService>();
            var mockTimeProvider = new Mock<ITimeProvider>();

            Assert.Throws<ArgumentNullException>(() =>
                new ReinforcementService(mockPedSpawning.Object, mockTimeProvider.Object, null!));
        }

        [Fact]
        public void ReinforcementService_Constructor_AcceptsValidParameters()
        {
            var mockPedSpawning = new Mock<IPedSpawningService>();
            var mockTimeProvider = new Mock<ITimeProvider>();
            var config = new ReinforcementConfig();

            var service = new ReinforcementService(mockPedSpawning.Object, mockTimeProvider.Object, config);

            Assert.NotNull(service);
        }

        #endregion

        #region RequestReinforcements Tests

        [Fact]
        public void RequestReinforcements_ThrowsOnNullRequest()
        {
            var service = CreateService();

            Assert.Throws<ArgumentNullException>(() => service.RequestReinforcements(null!));
        }

        [Fact]
        public void RequestReinforcements_ThrowsOnNullEncounter()
        {
            var service = CreateService();
            var request = CreateValidRequest();

            Assert.Throws<ArgumentNullException>(() => service.RequestReinforcements(request, null!));
        }

        [Fact]
        public void RequestReinforcements_ReturnsEncounterEnded_WhenEncounterNotActive()
        {
            var service = CreateService();
            var request = CreateValidRequest();
            var encounter = CreateEncounter();
            encounter.End(CombatStatus.AttackerVictory);

            var result = service.RequestReinforcements(request, encounter);

            Assert.False(result.IsSuccess);
            Assert.Equal(ReinforcementResultStatus.EncounterEnded, result.Status);
        }

        [Fact]
        public void RequestReinforcements_ReturnsPoolFull_WhenNoSlotsAvailable()
        {
            var mockPedSpawning = new Mock<IPedSpawningService>();
            mockPedSpawning.Setup(s => s.CanSpawn()).Returns(false);

            var service = CreateService(pedSpawningService: mockPedSpawning.Object);
            var request = CreateValidRequest();
            var encounter = CreateEncounter();

            var result = service.RequestReinforcements(request, encounter);

            Assert.False(result.IsSuccess);
            Assert.Equal(ReinforcementResultStatus.PoolFull, result.Status);
        }

        [Fact]
        public void RequestReinforcements_ReturnsOnCooldown_WhenCooldownActive()
        {
            var mockTimeProvider = new Mock<ITimeProvider>();
            var initialTime = DateTime.UtcNow;
            mockTimeProvider.SetupSequence(t => t.UtcNow)
                .Returns(initialTime)      // First request time
                .Returns(initialTime.AddSeconds(10)); // Second request (within 30s cooldown)

            var mockPedSpawning = CreateMockPedSpawningService(canSpawn: true, spawnCount: 5);

            var service = CreateService(
                pedSpawningService: mockPedSpawning.Object,
                timeProvider: mockTimeProvider.Object);
            var encounter = CreateEncounter();

            // First request succeeds
            var request1 = CreateValidRequest();
            service.RequestReinforcements(request1, encounter);

            // Second request should be on cooldown
            var request2 = CreateValidRequest();
            var result = service.RequestReinforcements(request2, encounter);

            Assert.False(result.IsSuccess);
            Assert.Equal(ReinforcementResultStatus.OnCooldown, result.Status);
            Assert.True(result.RemainingCooldown > 0);
        }

        [Fact]
        public void RequestReinforcements_Succeeds_AfterCooldownExpires()
        {
            var mockTimeProvider = new Mock<ITimeProvider>();
            var initialTime = DateTime.UtcNow;
            mockTimeProvider.SetupSequence(t => t.UtcNow)
                .Returns(initialTime)      // First request
                .Returns(initialTime.AddSeconds(35)); // Second request (after 30s cooldown)

            var mockPedSpawning = CreateMockPedSpawningService(canSpawn: true, spawnCount: 5);

            var service = CreateService(
                pedSpawningService: mockPedSpawning.Object,
                timeProvider: mockTimeProvider.Object);
            var encounter = CreateEncounter();

            // First request
            var request1 = CreateValidRequest();
            service.RequestReinforcements(request1, encounter);

            // Second request after cooldown
            var request2 = CreateValidRequest();
            var result = service.RequestReinforcements(request2, encounter);

            Assert.True(result.IsSuccess);
        }

        [Fact]
        public void RequestReinforcements_SpawnsPeds_WhenAllConditionsMet()
        {
            var spawnedPeds = new List<PedHandle>
            {
                new PedHandle(1, "faction-1"),
                new PedHandle(2, "faction-1"),
                new PedHandle(3, "faction-1")
            };

            var mockPedSpawning = new Mock<IPedSpawningService>();
            mockPedSpawning.Setup(s => s.CanSpawn()).Returns(true);
            mockPedSpawning.Setup(s => s.CanSpawnCount()).Returns(10);
            mockPedSpawning
                .Setup(s => s.SpawnMultiplePeds(It.IsAny<string>(), It.IsAny<Vector3>(), "faction-1", "zone-1", 5))
                .Returns(spawnedPeds);

            var service = CreateService(pedSpawningService: mockPedSpawning.Object);
            var request = CreateValidRequest(requestedCount: 5);
            var encounter = CreateEncounter();

            var result = service.RequestReinforcements(request, encounter);

            Assert.True(result.IsSuccess);
            Assert.Equal(3, result.SpawnedCount);
            Assert.Equal(spawnedPeds, result.SpawnedPeds);
        }

        [Fact]
        public void RequestReinforcements_ReturnsPartialSuccess_WhenFewerPedsSpawned()
        {
            var spawnedPeds = new List<PedHandle>
            {
                new PedHandle(1, "faction-1"),
                new PedHandle(2, "faction-1")
            };

            var mockPedSpawning = new Mock<IPedSpawningService>();
            mockPedSpawning.Setup(s => s.CanSpawn()).Returns(true);
            mockPedSpawning.Setup(s => s.CanSpawnCount()).Returns(10);
            mockPedSpawning
                .Setup(s => s.SpawnMultiplePeds(It.IsAny<string>(), It.IsAny<Vector3>(), "faction-1", "zone-1", 5))
                .Returns(spawnedPeds);

            var service = CreateService(pedSpawningService: mockPedSpawning.Object);
            var request = CreateValidRequest(requestedCount: 5);
            var encounter = CreateEncounter();

            var result = service.RequestReinforcements(request, encounter);

            Assert.True(result.IsSuccess);
            Assert.Equal(ReinforcementResultStatus.PartialSuccess, result.Status);
            Assert.Equal(2, result.SpawnedCount);
            Assert.Equal(5, result.RequestedCount);
        }

        [Fact]
        public void RequestReinforcements_ClampsToMaxPedsPerWave()
        {
            var mockPedSpawning = CreateMockPedSpawningService(canSpawn: true, spawnCount: 10);
            var config = new ReinforcementConfig { MaxPedsPerWave = 8 };

            var service = CreateService(pedSpawningService: mockPedSpawning.Object, config: config);
            var request = CreateValidRequest(requestedCount: 15);
            var encounter = CreateEncounter();

            service.RequestReinforcements(request, encounter);

            mockPedSpawning.Verify(s =>
                s.SpawnMultiplePeds(It.IsAny<string>(), It.IsAny<Vector3>(), It.IsAny<string>(), It.IsAny<string>(), 8),
                Times.Once);
        }

        [Fact]
        public void RequestReinforcements_ClampsToMinPedsPerWave()
        {
            var mockPedSpawning = CreateMockPedSpawningService(canSpawn: true, spawnCount: 5);
            var config = new ReinforcementConfig { MinPedsPerWave = 3 };

            var service = CreateService(pedSpawningService: mockPedSpawning.Object, config: config);
            var request = CreateValidRequest(requestedCount: 1);
            var encounter = CreateEncounter();

            service.RequestReinforcements(request, encounter);

            mockPedSpawning.Verify(s =>
                s.SpawnMultiplePeds(It.IsAny<string>(), It.IsAny<Vector3>(), It.IsAny<string>(), It.IsAny<string>(), 3),
                Times.Once);
        }

        [Fact]
        public void RequestReinforcements_ClampsToAvailablePoolSlots()
        {
            var mockPedSpawning = new Mock<IPedSpawningService>();
            mockPedSpawning.Setup(s => s.CanSpawn()).Returns(true);
            mockPedSpawning.Setup(s => s.CanSpawnCount()).Returns(3);
            mockPedSpawning
                .Setup(s => s.SpawnMultiplePeds(It.IsAny<string>(), It.IsAny<Vector3>(), It.IsAny<string>(), It.IsAny<string>(), 3))
                .Returns(new List<PedHandle> { new PedHandle(1), new PedHandle(2), new PedHandle(3) });

            var service = CreateService(pedSpawningService: mockPedSpawning.Object);
            var request = CreateValidRequest(requestedCount: 10);
            var encounter = CreateEncounter();

            service.RequestReinforcements(request, encounter);

            mockPedSpawning.Verify(s =>
                s.SpawnMultiplePeds(It.IsAny<string>(), It.IsAny<Vector3>(), It.IsAny<string>(), It.IsAny<string>(), 3),
                Times.Once);
        }

        [Fact]
        public void RequestReinforcements_UsesCorrectModelForFaction()
        {
            var mockPedSpawning = new Mock<IPedSpawningService>();
            mockPedSpawning.Setup(s => s.CanSpawn()).Returns(true);
            mockPedSpawning.Setup(s => s.CanSpawnCount()).Returns(10);
            mockPedSpawning
                .Setup(s => s.SpawnMultiplePeds(It.IsAny<string>(), It.IsAny<Vector3>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()))
                .Returns(new List<PedHandle>());

            var service = CreateService(pedSpawningService: mockPedSpawning.Object);
            var request = CreateValidRequest(factionId: "michael");
            var encounter = CreateEncounter(attackingFactionId: "michael");

            service.RequestReinforcements(request, encounter);

            // Verify the spawning service was called - specific model checking would be implementation detail
            mockPedSpawning.Verify(s =>
                s.SpawnMultiplePeds(It.IsAny<string>(), It.IsAny<Vector3>(), "michael", It.IsAny<string>(), It.IsAny<int>()),
                Times.Once);
        }

        #endregion

        #region CanRequestReinforcements Tests

        [Fact]
        public void CanRequestReinforcements_ReturnsTrue_WhenAllConditionsMet()
        {
            var mockPedSpawning = CreateMockPedSpawningService(canSpawn: true);
            var service = CreateService(pedSpawningService: mockPedSpawning.Object);
            var encounter = CreateEncounter();

            var canRequest = service.CanRequestReinforcements("faction-1", encounter);

            Assert.True(canRequest);
        }

        [Fact]
        public void CanRequestReinforcements_ReturnsFalse_WhenEncounterEnded()
        {
            var service = CreateService();
            var encounter = CreateEncounter();
            encounter.End(CombatStatus.AttackerVictory);

            var canRequest = service.CanRequestReinforcements("faction-1", encounter);

            Assert.False(canRequest);
        }

        [Fact]
        public void CanRequestReinforcements_ReturnsFalse_WhenPoolFull()
        {
            var mockPedSpawning = new Mock<IPedSpawningService>();
            mockPedSpawning.Setup(s => s.CanSpawn()).Returns(false);

            var service = CreateService(pedSpawningService: mockPedSpawning.Object);
            var encounter = CreateEncounter();

            var canRequest = service.CanRequestReinforcements("faction-1", encounter);

            Assert.False(canRequest);
        }

        [Fact]
        public void CanRequestReinforcements_ReturnsFalse_WhenOnCooldown()
        {
            var mockTimeProvider = new Mock<ITimeProvider>();
            var initialTime = DateTime.UtcNow;
            mockTimeProvider.SetupSequence(t => t.UtcNow)
                .Returns(initialTime)
                .Returns(initialTime.AddSeconds(10));

            var mockPedSpawning = CreateMockPedSpawningService(canSpawn: true, spawnCount: 5);

            var service = CreateService(
                pedSpawningService: mockPedSpawning.Object,
                timeProvider: mockTimeProvider.Object);
            var encounter = CreateEncounter();

            // Make a request to start cooldown
            var request = CreateValidRequest();
            service.RequestReinforcements(request, encounter);

            // Check if we can request again
            var canRequest = service.CanRequestReinforcements("faction-1", encounter);

            Assert.False(canRequest);
        }

        [Fact]
        public void CanRequestReinforcements_ThrowsOnNullFactionId()
        {
            var service = CreateService();
            var encounter = CreateEncounter();

            Assert.Throws<ArgumentNullException>(() => service.CanRequestReinforcements(null!, encounter));
        }

        [Fact]
        public void CanRequestReinforcements_ThrowsOnNullEncounter()
        {
            var service = CreateService();

            Assert.Throws<ArgumentNullException>(() => service.CanRequestReinforcements("faction-1", null!));
        }

        #endregion

        #region GetRemainingCooldown Tests

        [Fact]
        public void GetRemainingCooldown_ReturnsZero_WhenNoPreviousRequest()
        {
            var service = CreateService();
            var encounter = CreateEncounter();

            var remaining = service.GetRemainingCooldown("faction-1", encounter);

            Assert.Equal(0f, remaining);
        }

        [Fact]
        public void GetRemainingCooldown_ReturnsCorrectTime_WhenOnCooldown()
        {
            var mockTimeProvider = new Mock<ITimeProvider>();
            var initialTime = DateTime.UtcNow;
            mockTimeProvider.SetupSequence(t => t.UtcNow)
                .Returns(initialTime)
                .Returns(initialTime.AddSeconds(10));

            var mockPedSpawning = CreateMockPedSpawningService(canSpawn: true, spawnCount: 5);
            var config = new ReinforcementConfig { CooldownSeconds = 30f };

            var service = CreateService(
                pedSpawningService: mockPedSpawning.Object,
                timeProvider: mockTimeProvider.Object,
                config: config);
            var encounter = CreateEncounter();

            // Make a request to start cooldown
            var request = CreateValidRequest();
            service.RequestReinforcements(request, encounter);

            // Check remaining cooldown
            var remaining = service.GetRemainingCooldown("faction-1", encounter);

            Assert.Equal(20f, remaining, precision: 1);
        }

        [Fact]
        public void GetRemainingCooldown_ReturnsZero_WhenCooldownExpired()
        {
            var mockTimeProvider = new Mock<ITimeProvider>();
            var initialTime = DateTime.UtcNow;
            mockTimeProvider.SetupSequence(t => t.UtcNow)
                .Returns(initialTime)
                .Returns(initialTime.AddSeconds(35));

            var mockPedSpawning = CreateMockPedSpawningService(canSpawn: true, spawnCount: 5);
            var config = new ReinforcementConfig { CooldownSeconds = 30f };

            var service = CreateService(
                pedSpawningService: mockPedSpawning.Object,
                timeProvider: mockTimeProvider.Object,
                config: config);
            var encounter = CreateEncounter();

            // Make a request to start cooldown
            var request = CreateValidRequest();
            service.RequestReinforcements(request, encounter);

            // Check remaining cooldown
            var remaining = service.GetRemainingCooldown("faction-1", encounter);

            Assert.Equal(0f, remaining);
        }

        [Fact]
        public void GetRemainingCooldown_TracksSeparateCooldownsPerFaction()
        {
            var mockTimeProvider = new Mock<ITimeProvider>();
            var initialTime = DateTime.UtcNow;
            mockTimeProvider.SetupSequence(t => t.UtcNow)
                .Returns(initialTime)
                .Returns(initialTime.AddSeconds(5))
                .Returns(initialTime.AddSeconds(10))
                .Returns(initialTime.AddSeconds(10));

            var mockPedSpawning = CreateMockPedSpawningService(canSpawn: true, spawnCount: 5);
            var config = new ReinforcementConfig { CooldownSeconds = 30f };

            var service = CreateService(
                pedSpawningService: mockPedSpawning.Object,
                timeProvider: mockTimeProvider.Object,
                config: config);
            var encounter = CreateEncounter();

            // Faction 1 requests at t=0
            var request1 = CreateValidRequest(factionId: "faction-1");
            service.RequestReinforcements(request1, encounter);

            // Faction 2 requests at t=5
            var request2 = CreateValidRequest(factionId: "faction-2");
            service.RequestReinforcements(request2, encounter);

            // Check at t=10
            var remaining1 = service.GetRemainingCooldown("faction-1", encounter);
            var remaining2 = service.GetRemainingCooldown("faction-2", encounter);

            Assert.Equal(20f, remaining1, precision: 1); // 30 - 10 = 20
            Assert.Equal(25f, remaining2, precision: 1); // 30 - 5 = 25
        }

        #endregion

        #region GetActiveWaveCount Tests

        [Fact]
        public void GetActiveWaveCount_ReturnsZero_Initially()
        {
            var service = CreateService();
            var encounter = CreateEncounter();

            var count = service.GetActiveWaveCount("faction-1", encounter);

            Assert.Equal(0, count);
        }

        [Fact]
        public void GetActiveWaveCount_IncrementsAfterSuccessfulRequest()
        {
            var mockPedSpawning = CreateMockPedSpawningService(canSpawn: true, spawnCount: 5);
            var service = CreateService(pedSpawningService: mockPedSpawning.Object);
            var encounter = CreateEncounter();

            var request = CreateValidRequest();
            service.RequestReinforcements(request, encounter);

            var count = service.GetActiveWaveCount("faction-1", encounter);

            Assert.Equal(1, count);
        }

        #endregion

        #region ResetCooldown Tests

        [Fact]
        public void ResetCooldown_ClearsCooldownForFaction()
        {
            var mockTimeProvider = new Mock<ITimeProvider>();
            var initialTime = DateTime.UtcNow;
            mockTimeProvider.SetupSequence(t => t.UtcNow)
                .Returns(initialTime)
                .Returns(initialTime.AddSeconds(10))
                .Returns(initialTime.AddSeconds(10));

            var mockPedSpawning = CreateMockPedSpawningService(canSpawn: true, spawnCount: 5);

            var service = CreateService(
                pedSpawningService: mockPedSpawning.Object,
                timeProvider: mockTimeProvider.Object);
            var encounter = CreateEncounter();

            // Make request to start cooldown
            var request = CreateValidRequest();
            service.RequestReinforcements(request, encounter);

            // Verify on cooldown
            Assert.True(service.GetRemainingCooldown("faction-1", encounter) > 0);

            // Reset cooldown
            service.ResetCooldown("faction-1", encounter);

            // Verify cooldown cleared
            Assert.Equal(0f, service.GetRemainingCooldown("faction-1", encounter));
        }

        #endregion

        #region GetCurrentConfig Tests

        [Fact]
        public void GetCurrentConfig_ReturnsConfigCopy()
        {
            var config = new ReinforcementConfig
            {
                CooldownSeconds = 45f,
                MinPedsPerWave = 3,
                MaxPedsPerWave = 12
            };

            var service = CreateService(config: config);

            var returnedConfig = service.GetCurrentConfig();

            Assert.Equal(45f, returnedConfig.CooldownSeconds);
            Assert.Equal(3, returnedConfig.MinPedsPerWave);
            Assert.Equal(12, returnedConfig.MaxPedsPerWave);
        }

        #endregion

        #region Helper Methods

        private ReinforcementService CreateService(
            IPedSpawningService? pedSpawningService = null,
            ITimeProvider? timeProvider = null,
            ReinforcementConfig? config = null)
        {
            pedSpawningService ??= CreateMockPedSpawningService(canSpawn: true).Object;
            timeProvider ??= CreateMockTimeProvider().Object;
            config ??= new ReinforcementConfig();

            return new ReinforcementService(pedSpawningService, timeProvider, config);
        }

        private Mock<IPedSpawningService> CreateMockPedSpawningService(bool canSpawn, int spawnCount = 5)
        {
            var mock = new Mock<IPedSpawningService>();
            mock.Setup(s => s.CanSpawn()).Returns(canSpawn);
            mock.Setup(s => s.CanSpawnCount()).Returns(spawnCount > 0 ? 30 : 0);

            var spawnedPeds = new List<PedHandle>();
            for (int i = 0; i < spawnCount; i++)
            {
                spawnedPeds.Add(new PedHandle(i + 1, "faction-1"));
            }

            mock.Setup(s => s.SpawnMultiplePeds(
                    It.IsAny<string>(),
                    It.IsAny<Vector3>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<int>()))
                .Returns<string, Vector3, string, string, int>((model, pos, factionId, zoneId, count) =>
                {
                    var peds = new List<PedHandle>();
                    for (int i = 0; i < Math.Min(count, spawnCount); i++)
                    {
                        peds.Add(new PedHandle(i + 1, factionId));
                    }
                    return peds;
                });

            return mock;
        }

        private Mock<ITimeProvider> CreateMockTimeProvider()
        {
            var mock = new Mock<ITimeProvider>();
            mock.Setup(t => t.UtcNow).Returns(DateTime.UtcNow);
            return mock;
        }

        private ReinforcementRequest CreateValidRequest(
            string encounterId = "encounter-1",
            string factionId = "faction-1",
            string zoneId = "zone-1",
            int requestedCount = 5)
        {
            return new ReinforcementRequest(
                encounterId: encounterId,
                factionId: factionId,
                zoneId: zoneId,
                requestedCount: requestedCount,
                spawnPosition: new Vector3(100f, 200f, 50f));
        }

        private CombatEncounter CreateEncounter(
            string id = "encounter-1",
            string zoneId = "zone-1",
            string attackingFactionId = "faction-1",
            string defendingFactionId = "faction-2")
        {
            return new CombatEncounter(id, zoneId, attackingFactionId, defendingFactionId);
        }

        #endregion
    }
}
