using System;
using FactionWars.Core.Interfaces;
using FactionWars.Core.Models;
using FactionWars.Core.Services;
using FactionWars.Economy.Interfaces;
using FactionWars.Economy.Services;
using FactionWars.Factions.Interfaces;
using FactionWars.Factions.Models;
using FactionWars.Factions.Repositories;
using FactionWars.Factions.Services;
using FactionWars.Territory.Interfaces;
using FactionWars.Territory.Models;
using FactionWars.Territory.Repositories;
using FactionWars.Territory.Services;
using Moq;
using Xunit;

namespace FactionWars.Tests.Integration
{
    /// <summary>
    /// Integration tests for the difficulty system.
    /// Tests the wiring between DifficultyService and ResourceTickService,
    /// verifying that difficulty changes correctly update tick intervals and AI income multipliers.
    /// </summary>
    public class DifficultyIntegrationTests
    {
        private readonly InMemoryZoneRepository _zoneRepository;
        private readonly IZoneService _zoneService;
        private readonly InMemoryFactionRepository _factionRepository;
        private readonly IFactionService _factionService;
        private readonly IZoneTraitResourceModifier _resourceModifier;
        private readonly ISupplyLineService _supplyLineService;

        private const string PlayerFactionId = "player-faction";
        private const string AiFactionId = "ai-faction";

        public DifficultyIntegrationTests()
        {
            // Real implementations for services
            _zoneRepository = new InMemoryZoneRepository();
            _zoneService = new ZoneService(_zoneRepository);
            _factionRepository = new InMemoryFactionRepository();
            _factionService = new FactionService(_factionRepository);
            _resourceModifier = new ZoneTraitResourceModifier();
            _supplyLineService = new SupplyLineService(_zoneService);
        }

        #region DifficultyService_InitializesWithCorrectDefaults

        [Fact]
        public void DifficultyService_InitializesWithCorrectDefaults()
        {
            // Arrange & Act
            var difficultyService = new DifficultyService();

            // Assert: Default is Normal difficulty
            Assert.Equal(Difficulty.Normal, difficultyService.Current.Level);
            Assert.Equal(1.0f, difficultyService.Current.AiIncomeMultiplier);
            Assert.Equal(5, difficultyService.Current.TickIntervalMinutes);
            Assert.Equal(300, difficultyService.Current.TickIntervalSeconds);
        }

        [Theory]
        [InlineData(Difficulty.Easy, 0.75f, 7, 420)]
        [InlineData(Difficulty.Normal, 1.0f, 5, 300)]
        [InlineData(Difficulty.Hard, 1.25f, 3, 180)]
        public void DifficultyService_InitializesWithCorrectSettings_ForEachLevel(
            Difficulty level,
            float expectedAiMultiplier,
            int expectedTickMinutes,
            int expectedTickSeconds)
        {
            // Arrange & Act
            var difficultyService = new DifficultyService(level);

            // Assert
            Assert.Equal(level, difficultyService.Current.Level);
            Assert.Equal(expectedAiMultiplier, difficultyService.Current.AiIncomeMultiplier);
            Assert.Equal(expectedTickMinutes, difficultyService.Current.TickIntervalMinutes);
            Assert.Equal(expectedTickSeconds, difficultyService.Current.TickIntervalSeconds);
        }

        #endregion

        #region DifficultyChanged_EventContainsCorrectSettings

        [Fact]
        public void DifficultyChanged_EventContainsCorrectSettings()
        {
            // Arrange
            var difficultyService = new DifficultyService(Difficulty.Normal);
            DifficultySettings? receivedSettings = null;
            object? receivedSender = null;

            difficultyService.DifficultyChanged += (sender, settings) =>
            {
                receivedSender = sender;
                receivedSettings = settings;
            };

            // Act
            difficultyService.SetDifficulty(Difficulty.Hard);

            // Assert
            Assert.NotNull(receivedSettings);
            Assert.Same(difficultyService, receivedSender);
            Assert.Equal(Difficulty.Hard, receivedSettings!.Level);
            Assert.Equal(1.25f, receivedSettings.AiIncomeMultiplier);
            Assert.Equal(3, receivedSettings.TickIntervalMinutes);
            Assert.Equal(180, receivedSettings.TickIntervalSeconds);
        }

        [Fact]
        public void DifficultyChanged_EventContainsCorrectSettings_ForAllTransitions()
        {
            // Arrange
            var difficultyService = new DifficultyService(Difficulty.Easy);
            DifficultySettings? receivedSettings = null;

            difficultyService.DifficultyChanged += (sender, settings) =>
            {
                receivedSettings = settings;
            };

            // Act & Assert: Easy -> Normal
            difficultyService.SetDifficulty(Difficulty.Normal);
            Assert.NotNull(receivedSettings);
            Assert.Equal(Difficulty.Normal, receivedSettings!.Level);
            Assert.Equal(1.0f, receivedSettings.AiIncomeMultiplier);
            Assert.Equal(300, receivedSettings.TickIntervalSeconds);

            // Act & Assert: Normal -> Hard
            difficultyService.SetDifficulty(Difficulty.Hard);
            Assert.Equal(Difficulty.Hard, receivedSettings.Level);
            Assert.Equal(1.25f, receivedSettings.AiIncomeMultiplier);
            Assert.Equal(180, receivedSettings.TickIntervalSeconds);

            // Act & Assert: Hard -> Easy
            difficultyService.SetDifficulty(Difficulty.Easy);
            Assert.Equal(Difficulty.Easy, receivedSettings.Level);
            Assert.Equal(0.75f, receivedSettings.AiIncomeMultiplier);
            Assert.Equal(420, receivedSettings.TickIntervalSeconds);
        }

        #endregion

        #region DifficultyChange_UpdatesTickServiceAndPersists

        [Fact]
        public void DifficultyChange_UpdatesTickServiceAndPersists()
        {
            // Arrange
            var difficultyService = new DifficultyService(Difficulty.Normal);
            var resourceTickService = CreateResourceTickService(DifficultySettings.Normal.TickIntervalSeconds);

            // Wire up the difficulty change handler
            difficultyService.DifficultyChanged += (sender, settings) =>
            {
                resourceTickService.SetTickInterval(settings.TickIntervalSeconds);
                resourceTickService.SetAiIncomeMultiplier(settings.AiIncomeMultiplier);
            };

            // Verify initial state (Normal)
            Assert.Equal(300, resourceTickService.TickIntervalSeconds);

            // Act: Change to Hard difficulty
            difficultyService.SetDifficulty(Difficulty.Hard);

            // Assert: ResourceTickService was updated
            Assert.Equal(180, resourceTickService.TickIntervalSeconds);

            // Act: Change to Easy difficulty
            difficultyService.SetDifficulty(Difficulty.Easy);

            // Assert: ResourceTickService was updated again
            Assert.Equal(420, resourceTickService.TickIntervalSeconds);
        }

        [Fact]
        public void DifficultyChange_UpdatesAiIncomeMultiplier_AffectsResourceGeneration()
        {
            // Arrange
            SetupFaction(PlayerFactionId, "Player", initialCash: 0);
            SetupFaction(AiFactionId, "AI Enemy", initialCash: 0);
            CreateAndAddZone("zone-player", "Player Zone", PlayerFactionId, strategicValue: 1);
            CreateAndAddZone("zone-ai", "AI Zone", AiFactionId, strategicValue: 1);

            var difficultyService = new DifficultyService(Difficulty.Easy);
            var resourceTickService = CreateResourceTickService(DifficultySettings.Easy.TickIntervalSeconds);

            // Set initial AI multiplier and player faction
            resourceTickService.SetAiIncomeMultiplier(difficultyService.Current.AiIncomeMultiplier);
            resourceTickService.SetPlayerFactionId(PlayerFactionId);

            // Wire up difficulty changes
            difficultyService.DifficultyChanged += (sender, settings) =>
            {
                resourceTickService.SetTickInterval(settings.TickIntervalSeconds);
                resourceTickService.SetAiIncomeMultiplier(settings.AiIncomeMultiplier);
            };

            // Act: Force tick on Easy difficulty (AI gets 0.75x)
            resourceTickService.ForceTick();

            var playerStateEasy = _factionService.GetFactionState(PlayerFactionId);
            var aiStateEasy = _factionService.GetFactionState(AiFactionId);

            Assert.NotNull(playerStateEasy);
            Assert.NotNull(aiStateEasy);

            int playerCashEasy = playerStateEasy.Cash;
            int aiCashEasy = aiStateEasy.Cash;

            // Assert: On Easy, AI gets reduced income (0.75x)
            // Player gets 100 cash (base), AI gets 75 cash (100 * 0.75)
            Assert.Equal(100, playerCashEasy);
            Assert.Equal(75, aiCashEasy);

            // Change to Hard difficulty
            difficultyService.SetDifficulty(Difficulty.Hard);

            // Force another tick
            resourceTickService.ForceTick();

            var playerStateHard = _factionService.GetFactionState(PlayerFactionId);
            var aiStateHard = _factionService.GetFactionState(AiFactionId);

            Assert.NotNull(playerStateHard);
            Assert.NotNull(aiStateHard);

            // Assert: On Hard, AI gets increased income (1.25x)
            // Player: 100 + 100 = 200, AI: 75 + 125 = 200
            Assert.Equal(200, playerStateHard.Cash);
            Assert.Equal(200, aiStateHard.Cash); // 75 from easy + 125 from hard
        }

        [Fact]
        public void DifficultyChange_UpdatesTickInterval_AffectsTickTiming()
        {
            // Arrange
            SetupFaction(PlayerFactionId, "Player", initialCash: 0);
            CreateAndAddZone("zone-1", "Test Zone", PlayerFactionId, strategicValue: 1);

            var difficultyService = new DifficultyService(Difficulty.Normal);
            var resourceTickService = CreateResourceTickService(DifficultySettings.Normal.TickIntervalSeconds);
            resourceTickService.SetPlayerFactionId(PlayerFactionId);

            // Wire up difficulty changes
            difficultyService.DifficultyChanged += (sender, settings) =>
            {
                resourceTickService.SetTickInterval(settings.TickIntervalSeconds);
                resourceTickService.SetAiIncomeMultiplier(settings.AiIncomeMultiplier);
            };

            int tickCount = 0;
            resourceTickService.OnResourceTick += (sender, args) => tickCount++;

            resourceTickService.Start();

            // Normal: 300 second intervals (5 minutes)
            // Update for 600 seconds (10 minutes) - should get 2 ticks
            resourceTickService.Update(600f);
            Assert.Equal(2, tickCount);

            // Change to Hard: 180 second intervals (3 minutes)
            difficultyService.SetDifficulty(Difficulty.Hard);
            tickCount = 0;
            resourceTickService.Reset();

            // Update for 540 seconds (9 minutes) - should get 3 ticks
            resourceTickService.Update(540f);
            Assert.Equal(3, tickCount);
        }

        #endregion

        #region Full Integration Scenarios

        [Fact]
        public void FullIntegration_DifficultyChangeFlow_WorksEndToEnd()
        {
            // Arrange: Setup factions and zones
            SetupFaction(PlayerFactionId, "Player Faction", initialCash: 0);
            SetupFaction(AiFactionId, "AI Faction", initialCash: 0);
            CreateAndAddZone("zone-1", "Player Base", PlayerFactionId, strategicValue: 2);
            CreateAndAddZone("zone-2", "AI Base", AiFactionId, strategicValue: 2);

            // Create services with Normal difficulty
            var difficultyService = new DifficultyService(Difficulty.Normal);
            var resourceTickService = CreateResourceTickService(difficultyService.Current.TickIntervalSeconds);
            resourceTickService.SetPlayerFactionId(PlayerFactionId);
            resourceTickService.SetAiIncomeMultiplier(difficultyService.Current.AiIncomeMultiplier);

            // Wire up difficulty changes (simulating GameLoopController wiring)
            difficultyService.DifficultyChanged += (sender, settings) =>
            {
                resourceTickService.SetTickInterval(settings.TickIntervalSeconds);
                resourceTickService.SetAiIncomeMultiplier(settings.AiIncomeMultiplier);
            };

            // Assert: Initial state is Normal
            Assert.Equal(Difficulty.Normal, difficultyService.Current.Level);
            Assert.Equal(300, resourceTickService.TickIntervalSeconds);

            // Act: Generate resources on Normal
            resourceTickService.ForceTick();

            var playerState = _factionService.GetFactionState(PlayerFactionId);
            var aiState = _factionService.GetFactionState(AiFactionId);

            Assert.NotNull(playerState);
            Assert.NotNull(aiState);

            // On Normal, both factions get 200 cash (100 base * 2 strategic value * 1.0 multiplier)
            Assert.Equal(200, playerState.Cash);
            Assert.Equal(200, aiState.Cash);

            // Act: Change to Easy
            difficultyService.SetDifficulty(Difficulty.Easy);

            // Assert: Settings updated
            Assert.Equal(Difficulty.Easy, difficultyService.Current.Level);
            Assert.Equal(420, resourceTickService.TickIntervalSeconds);

            // Generate more resources on Easy
            resourceTickService.ForceTick();

            playerState = _factionService.GetFactionState(PlayerFactionId);
            aiState = _factionService.GetFactionState(AiFactionId);

            Assert.NotNull(playerState);
            Assert.NotNull(aiState);

            // Player: 200 + 200 = 400 (1.0x multiplier still applies to player)
            // AI: 200 + 150 = 350 (0.75x multiplier: 200 * 0.75 = 150)
            Assert.Equal(400, playerState.Cash);
            Assert.Equal(350, aiState.Cash);

            // Act: Change to Hard
            difficultyService.SetDifficulty(Difficulty.Hard);

            // Assert: Settings updated
            Assert.Equal(Difficulty.Hard, difficultyService.Current.Level);
            Assert.Equal(180, resourceTickService.TickIntervalSeconds);

            // Generate more resources on Hard
            resourceTickService.ForceTick();

            playerState = _factionService.GetFactionState(PlayerFactionId);
            aiState = _factionService.GetFactionState(AiFactionId);

            Assert.NotNull(playerState);
            Assert.NotNull(aiState);

            // Player: 400 + 200 = 600
            // AI: 350 + 250 = 600 (1.25x multiplier: 200 * 1.25 = 250)
            Assert.Equal(600, playerState.Cash);
            Assert.Equal(600, aiState.Cash);
        }

        [Fact]
        public void DifficultySettings_StaticPresets_HaveCorrectValues()
        {
            // Assert Easy settings
            Assert.Equal(Difficulty.Easy, DifficultySettings.Easy.Level);
            Assert.Equal(0.75f, DifficultySettings.Easy.AiIncomeMultiplier);
            Assert.Equal(7, DifficultySettings.Easy.TickIntervalMinutes);
            Assert.Equal(420, DifficultySettings.Easy.TickIntervalSeconds);

            // Assert Normal settings
            Assert.Equal(Difficulty.Normal, DifficultySettings.Normal.Level);
            Assert.Equal(1.0f, DifficultySettings.Normal.AiIncomeMultiplier);
            Assert.Equal(5, DifficultySettings.Normal.TickIntervalMinutes);
            Assert.Equal(300, DifficultySettings.Normal.TickIntervalSeconds);

            // Assert Hard settings
            Assert.Equal(Difficulty.Hard, DifficultySettings.Hard.Level);
            Assert.Equal(1.25f, DifficultySettings.Hard.AiIncomeMultiplier);
            Assert.Equal(3, DifficultySettings.Hard.TickIntervalMinutes);
            Assert.Equal(180, DifficultySettings.Hard.TickIntervalSeconds);
        }

        #endregion

        #region Helper Methods

        private ResourceTickService CreateResourceTickService(int tickIntervalSeconds)
        {
            return new ResourceTickService(
                _factionService,
                _zoneService,
                _resourceModifier,
                _supplyLineService,
                tickIntervalSeconds);
        }

        private void SetupFaction(string factionId, string name, int initialCash = 0, int initialTroops = 0)
        {
            var faction = new Faction(factionId, name);
            _factionRepository.Add(faction);
            _factionService.InitializeFactionState(factionId, initialCash, initialTroops);
        }

        private Zone CreateAndAddZone(string id, string name, string? ownerFactionId, int strategicValue = 1)
        {
            var zone = new Zone(id, name, new Vector3(0, 0, 0), 150f, strategicValue);
            zone.OwnerFactionId = ownerFactionId;
            zone.ControlPercentage = 100f;
            _zoneRepository.Add(zone);

            if (ownerFactionId != null)
            {
                _factionService.AddZoneToFaction(ownerFactionId, id);
            }

            return zone;
        }

        #endregion
    }
}
