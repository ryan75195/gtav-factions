using FactionWars.Core.Interfaces;
using FactionWars.Core.Utils;
using FactionWars.ScriptHookV;
using FactionWars.Territory.Interfaces;
using FactionWars.Tests.Mocks;
using FactionWars.UI.Interfaces;
using Xunit;

namespace FactionWars.Tests.Integration.ScriptHookV
{
    /// <summary>
    /// Tests verifying that IMapBlipService is properly wired for zone blip updates.
    /// Ensures blip colors update when zone ownership changes during gameplay.
    /// </summary>
    public class MapBlipServiceWiringTests
    {
        private MockGameBridge _gameBridge = null!;
        private ServiceContainer _container = null!;
        private GameLoopController _controller = null!;

        private void SetupController(string initialCharacterModel = "player_zero")
        {
            _gameBridge = new MockGameBridge();
            _gameBridge.PlayerCharacterModel = initialCharacterModel;
            _container = ServiceContainerFactory.Create(_gameBridge, new MockMenuProvider());
            _controller = new GameLoopController(_container);
        }

        [Fact]
        public void GameLoopController_ShouldExposeMapBlipManager()
        {
            // Arrange
            SetupController("player_zero");
            _controller.OnTick(); // Initialize

            // Act
            var mapBlipManager = _controller.MapBlipManager;

            // Assert - Should have access to MapBlipManager for blip updates
            Assert.NotNull(mapBlipManager);
        }

        [Fact]
        public void OnTick_WhenZoneOwnershipChanges_ShouldUpdateBlipColors()
        {
            // Arrange
            SetupController("player_zero");
            _controller.OnTick(); // Initialize game data and blips

            // Get the zone and map blip services
            var zoneRepository = _container.Resolve<IZoneRepository>();
            var zoneService = _container.Resolve<IZoneService>();

            // Get a zone owned by Michael (blue)
            var michaelZone = zoneRepository.GetById("rockford_hills");
            Assert.NotNull(michaelZone);
            Assert.Equal("michael", michaelZone!.OwnerFactionId);

            // Verify initial blip color is Michael's blue
            var initialBlipHandle = _controller.MapBlipManager!.GetBlipHandle("rockford_hills");
            Assert.NotEqual(-1, initialBlipHandle);
            Assert.Equal(BlipColor.MichaelBlue, _gameBridge.GetBlipColor(initialBlipHandle));

            // Act: Transfer zone to Franklin
            zoneService.TransferZoneOwnership("rockford_hills", "franklin");

            // Trigger the game loop update which should sync blip colors
            _controller.OnTick();

            // Assert: Blip color should now be Franklin's green
            var newBlipColor = _gameBridge.GetBlipColor(initialBlipHandle);
            Assert.Equal(BlipColor.FranklinGreen, newBlipColor);
        }

        [Fact]
        public void OnTick_WhenZoneBecomesNeutral_ShouldUpdateBlipToWhite()
        {
            // Arrange
            SetupController("player_zero");
            _controller.OnTick(); // Initialize

            var zoneService = _container.Resolve<IZoneService>();

            // Get a zone owned by Trevor
            var trevorZone = _container.Resolve<IZoneRepository>().GetById("sandy_shores");
            Assert.NotNull(trevorZone);
            Assert.Equal("trevor", trevorZone!.OwnerFactionId);

            // Verify initial blip color is Trevor's orange
            var blipHandle = _controller.MapBlipManager!.GetBlipHandle("sandy_shores");
            Assert.NotEqual(-1, blipHandle);
            Assert.Equal(BlipColor.TrevorOrange, _gameBridge.GetBlipColor(blipHandle));

            // Act: Transfer zone to neutral (no owner)
            zoneService.TransferZoneOwnership("sandy_shores", null);
            _controller.OnTick();

            // Assert: Blip should now be white (neutral)
            var newBlipColor = _gameBridge.GetBlipColor(blipHandle);
            Assert.Equal(BlipColor.White, newBlipColor);
        }

        [Fact]
        public void UpdateAllBlipColors_ShouldSyncAllZonesWithCurrentOwnership()
        {
            // Arrange
            SetupController("player_zero");
            _controller.OnTick(); // Initialize

            var zoneService = _container.Resolve<IZoneService>();
            var mapBlipManager = _controller.MapBlipManager!;

            // Transfer multiple zones
            zoneService.TransferZoneOwnership("rockford_hills", "trevor");
            zoneService.TransferZoneOwnership("davis", "michael");
            zoneService.TransferZoneOwnership("sandy_shores", "franklin");

            // Act: Update all blip colors through game loop
            _controller.OnTick();

            // Assert: All blips should reflect new ownership
            var rockfordHandle = mapBlipManager.GetBlipHandle("rockford_hills");
            var davisHandle = mapBlipManager.GetBlipHandle("davis");
            var sandyShoresHandle = mapBlipManager.GetBlipHandle("sandy_shores");

            Assert.Equal(BlipColor.TrevorOrange, _gameBridge.GetBlipColor(rockfordHandle));
            Assert.Equal(BlipColor.MichaelBlue, _gameBridge.GetBlipColor(davisHandle));
            Assert.Equal(BlipColor.FranklinGreen, _gameBridge.GetBlipColor(sandyShoresHandle));
        }

        [Fact]
        public void IMapBlipService_ShouldBeRegisteredInContainer()
        {
            // Arrange
            SetupController("player_zero");

            // Act & Assert: IMapBlipService should be registered and resolvable
            var mapBlipService = _container.Resolve<IMapBlipService>();
            Assert.NotNull(mapBlipService);
        }

        [Fact]
        public void OnTick_ShouldUpdateBlipsAtRegularIntervals()
        {
            // Arrange
            SetupController("player_zero");
            _controller.OnTick(); // Initialize

            var zoneService = _container.Resolve<IZoneService>();
            var blipHandle = _controller.MapBlipManager!.GetBlipHandle("rockford_hills");

            // Transfer zone ownership without calling OnTick
            zoneService.TransferZoneOwnership("rockford_hills", "franklin");

            // Blip should still be Michael's blue (not updated yet)
            Assert.Equal(BlipColor.MichaelBlue, _gameBridge.GetBlipColor(blipHandle));

            // Act: Call OnTick to trigger blip sync
            _controller.OnTick();

            // Assert: Now blip should be updated
            Assert.Equal(BlipColor.FranklinGreen, _gameBridge.GetBlipColor(blipHandle));
        }
    }
}
