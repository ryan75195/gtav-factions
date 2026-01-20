using System.Linq;
using FactionWars.Core.Interfaces;
using FactionWars.Core.Utils;
using FactionWars.ScriptHookV;
using FactionWars.ScriptHookV.Managers;
using FactionWars.Territory.Interfaces;
using FactionWars.Tests.Mocks;
using Xunit;

namespace FactionWars.Tests.Unit.ScriptHookV
{
    /// <summary>
    /// Tests for GameLoopController's map blip initialization functionality.
    /// </summary>
    public class GameLoopControllerMapBlipTests
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
        public void OnTick_AfterInitialization_ShouldCreateMapBlips()
        {
            // Arrange
            SetupController("player_zero");
            var controller = new GameLoopController(_container);

            // Act - First tick initializes game data and creates map blips
            controller.OnTick();

            // Assert - Check that blips were created (via MockGameBridge tracking)
            Assert.True(_gameBridge.BlipsCreated.Count > 0, "Map blips should be created during initialization");
        }

        [Fact]
        public void OnTick_AfterInitialization_ShouldCreateBlipsForAllZones()
        {
            // Arrange
            SetupController("player_zero");
            var controller = new GameLoopController(_container);
            var zoneRepository = _container.Resolve<IZoneRepository>();

            // Act - First tick initializes game data including zones
            controller.OnTick();

            // Get the zone count after initialization
            var zoneCount = zoneRepository.GetAll().Count();

            // Assert - Should have one blip per zone
            Assert.True(zoneCount > 0, "Zones should be loaded");
            Assert.Equal(zoneCount, _gameBridge.BlipsCreated.Count);
        }

        [Fact]
        public void OnTick_AfterInitialization_ShouldSetBlipColors()
        {
            // Arrange
            SetupController("player_zero");
            var controller = new GameLoopController(_container);

            // Act - First tick initializes
            controller.OnTick();

            // Assert - Verify colors were set for blips
            Assert.True(_gameBridge.BlipColors.Count > 0, "Blip colors should be set");
        }

        [Fact]
        public void OnTick_WithMichaelZones_ShouldSetMichaelBlueColor()
        {
            // Arrange
            SetupController("player_zero");
            var controller = new GameLoopController(_container);

            // Act
            controller.OnTick();

            // Assert - At least one blip should have Michael's blue color (42)
            Assert.Contains(_gameBridge.BlipColors.Values, color => color == BlipColor.MichaelBlue);
        }

        [Fact]
        public void OnTick_WithTrevorZones_ShouldSetTrevorOrangeColor()
        {
            // Arrange
            SetupController("player_zero");
            var controller = new GameLoopController(_container);

            // Act
            controller.OnTick();

            // Assert - At least one blip should have Trevor's orange color (44)
            Assert.Contains(_gameBridge.BlipColors.Values, color => color == BlipColor.TrevorOrange);
        }

        [Fact]
        public void OnTick_WithFranklinZones_ShouldSetFranklinGreenColor()
        {
            // Arrange
            SetupController("player_zero");
            var controller = new GameLoopController(_container);

            // Act
            controller.OnTick();

            // Assert - At least one blip should have Franklin's green color (43)
            Assert.Contains(_gameBridge.BlipColors.Values, color => color == BlipColor.FranklinGreen);
        }

        [Fact]
        public void OnAbort_ShouldCleanupMapBlips()
        {
            // Arrange
            SetupController("player_zero");
            var controller = new GameLoopController(_container);
            controller.OnTick(); // Initialize

            var blipCount = _gameBridge.BlipsCreated.Count;

            // Act
            controller.OnAbort();

            // Assert - All blips should be deleted
            Assert.Equal(blipCount, _gameBridge.BlipsDeleted.Count);
        }

        [Fact]
        public void MapBlipManager_ShouldBeAccessible()
        {
            // Arrange
            SetupController("player_zero");
            var controller = new GameLoopController(_container);
            controller.OnTick(); // Initialize

            // Act
            var mapBlipManager = controller.MapBlipManager;

            // Assert - Should have a valid MapBlipManager
            Assert.NotNull(mapBlipManager);
        }
    }
}
