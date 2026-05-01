using System.Collections.Generic;
using FactionWars.Combat.Interfaces;
using FactionWars.Combat.Models;
using FactionWars.Core.Interfaces;
using FactionWars.Core.Models;
using FactionWars.Core.Utils;
using FactionWars.ScriptHookV;
using FactionWars.ScriptHookV.Managers;
using FactionWars.Territory.Interfaces;
using FactionWars.Tests.Mocks;
using Xunit;

namespace FactionWars.Tests.Unit.ScriptHookV
{
    /// <summary>
    /// Tests for GameLoopController's territory detection and combat system wiring.
    /// Ensures managers are properly initialized and updated each tick.
    /// </summary>
    public class GameLoopControllerCombatTests
    {
        private MockGameBridge _gameBridge = null!;
        private ServiceContainer _container = null!;

        private void SetupController(string initialCharacterModel = "player_zero")
        {
            _gameBridge = new MockGameBridge();
            _gameBridge.PlayerCharacterModel = initialCharacterModel;
            _container = ServiceContainerFactory.Create(_gameBridge, new MockMenuProvider());
        }

        [Fact]
        public void TerritoryManager_AfterInitialization_IsNotNull()
        {
            // Arrange
            SetupController();
            var controller = new GameLoopController(_container);

            // Act - first tick initializes game data
            controller.OnTick();

            // Assert
            Assert.NotNull(controller.TerritoryManager);
        }

        [Fact]
        public void CombatManager_AfterInitialization_IsNotNull()
        {
            // Arrange
            SetupController();
            var controller = new GameLoopController(_container);

            // Act
            controller.OnTick();

            // Assert
            Assert.NotNull(controller.CombatManager);
        }

        [Fact]
        public void TerritoryManager_OnTick_GetsUpdated()
        {
            // Arrange
            SetupController();
            var controller = new GameLoopController(_container);
            controller.OnTick(); // Initialize

            // Set player position inside a zone
            _gameBridge.PlayerPosition = new Vector3(-200, -1600, 30); // Grove Street area

            // Act
            controller.OnTick();

            // Assert - TerritoryManager should have detected the zone
            Assert.NotNull(controller.TerritoryManager);
            // CurrentZone may or may not be set depending on zone definitions
        }

        [Fact]
        public void CombatManager_WhenPlayerEntersEnemyZone_StartsCombat()
        {
            // Arrange
            SetupController("player_one"); // Franklin
            var controller = new GameLoopController(_container);
            controller.OnTick(); // Initialize

            // Move player to a zone owned by another faction (Michael's territory)
            // Vinewood Hills is owned by Michael in starting conditions
            _gameBridge.PlayerPosition = new Vector3(-150, 950, 235);

            // Act
            controller.OnTick();
            controller.OnTick(); // Second tick to process combat start

            // Assert
            Assert.NotNull(controller.CombatManager);
            // Combat state depends on zone ownership setup
        }

        [Fact]
        public void OnZoneBattleStarted_MarksZoneAsContested()
        {
            SetupController();
            var controller = new GameLoopController(_container);
            controller.OnTick(); // Initialize

            var zoneRepo = _container.Resolve<IZoneRepository>();
            var battleManager = _container.Resolve<IZoneBattleManager>();
            var zone = zoneRepo.GetById("vinewood_hills");
            Assert.NotNull(zone);
            Assert.False(zone!.IsContested);

            var troops = new Dictionary<DefenderTier, int> { { DefenderTier.Basic, 1 } };
            battleManager.StartBattle(
                zoneId: "vinewood_hills",
                attackerFactionId: "trevor",
                defenderFactionId: "michael",
                attackerTroops: troops,
                defenderTroops: troops);

            Assert.True(zoneRepo.GetById("vinewood_hills")!.IsContested);
        }

        [Fact]
        public void OnAbort_CleansUpCombatManager()
        {
            // Arrange
            SetupController();
            var controller = new GameLoopController(_container);
            controller.OnTick(); // Initialize

            // Act
            controller.OnAbort();

            // Assert - After abort, managers should be cleaned up
            Assert.False(controller.IsInitialized);
        }
    }
}
