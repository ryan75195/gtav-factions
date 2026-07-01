using FactionWars.Core.Interfaces;
using FactionWars.Core.Utils;
using FactionWars.Economy.Interfaces;
using FactionWars.ScriptHookV.Managers;
using FactionWars.ScriptHookV.Managers.Interfaces;
using FactionWars.ScriptHookV.Models;
using FactionWars.ScriptHookV.UI;
using FactionWars.Tests.Mocks;
using FactionWars.Territory.Models;
using Moq;
using System;
using Xunit;

namespace FactionWars.Tests.Unit.ScriptHookV.UI
{
    public class SupportCallMenuControllerTests
    {
        private const string PlayerFactionId = "michael";

        private readonly MockMenuProvider _menuProvider;
        private readonly Mock<ISupportPackageService> _supportPackageServiceMock;
        private readonly Mock<ISupportSquadManager> _supportSquadManagerMock;
        private readonly Mock<ITerritoryEvents> _territoryMock;
        private readonly Mock<IPlayerContext> _playerContextMock;
        private readonly MockGameBridge _gameBridge;
        private readonly Zone _zone;
        private readonly SupportCallMenuController _controller;

        public SupportCallMenuControllerTests()
        {
            _menuProvider = new MockMenuProvider();
            _supportPackageServiceMock = new Mock<ISupportPackageService>();
            _supportSquadManagerMock = new Mock<ISupportSquadManager>();
            _territoryMock = new Mock<ITerritoryEvents>();
            _playerContextMock = new Mock<IPlayerContext>();
            _gameBridge = new MockGameBridge();
            _zone = new Zone("zone_1", "Zone One", new Vector3(0f, 0f, 0f));

            _playerContextMock.Setup(p => p.CurrentFactionId).Returns(PlayerFactionId);
            _territoryMock.Setup(t => t.CurrentZone).Returns(_zone);
            _supportPackageServiceMock.Setup(s => s.GetOwnedCount(PlayerFactionId)).Returns(1);
            _supportSquadManagerMock.Setup(m => m.HasActiveSquad).Returns(false);

            _controller = CreateController();
        }

        private SupportCallMenuController CreateController()
        {
            return new SupportCallMenuController(new SupportCallMenuControllerDependencies
            {
                MenuProvider = _menuProvider,
                SupportPackageService = _supportPackageServiceMock.Object,
                SupportSquadManager = _supportSquadManagerMock.Object,
                Territory = _territoryMock.Object,
                PlayerContext = _playerContextMock.Object,
                GameBridge = _gameBridge
            });
        }

        [Fact]
        public void Constructor_WithNullDependencies_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new SupportCallMenuController((SupportCallMenuControllerDependencies)null!));
        }

        [Fact]
        public void Constructor_WithNullMenuProvider_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new SupportCallMenuController(
                null,
                _supportPackageServiceMock.Object,
                _supportSquadManagerMock.Object,
                _territoryMock.Object,
                _playerContextMock.Object,
                _gameBridge));
        }

        [Fact]
        public void Constructor_WithNullSupportPackageService_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new SupportCallMenuController(
                _menuProvider,
                null,
                _supportSquadManagerMock.Object,
                _territoryMock.Object,
                _playerContextMock.Object,
                _gameBridge));
        }

        [Fact]
        public void Constructor_WithNullSupportSquadManager_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new SupportCallMenuController(
                _menuProvider,
                _supportPackageServiceMock.Object,
                null,
                _territoryMock.Object,
                _playerContextMock.Object,
                _gameBridge));
        }

        [Fact]
        public void Constructor_WithNullTerritory_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new SupportCallMenuController(
                _menuProvider,
                _supportPackageServiceMock.Object,
                _supportSquadManagerMock.Object,
                null,
                _playerContextMock.Object,
                _gameBridge));
        }

        [Fact]
        public void Constructor_WithNullPlayerContext_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new SupportCallMenuController(
                _menuProvider,
                _supportPackageServiceMock.Object,
                _supportSquadManagerMock.Object,
                _territoryMock.Object,
                null,
                _gameBridge));
        }

        [Fact]
        public void Constructor_WithNullGameBridge_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new SupportCallMenuController(
                _menuProvider,
                _supportPackageServiceMock.Object,
                _supportSquadManagerMock.Object,
                _territoryMock.Object,
                _playerContextMock.Object,
                null));
        }

        [Fact]
        public void Show_ShouldDisplaySupportCallMenu()
        {
            _controller.Show();

            Assert.True(_menuProvider.IsMenuVisible);
            Assert.Equal(SupportCallMenuController.MenuId, _menuProvider.CurrentMenuId);
        }

        [Fact]
        public void Show_OwnedLineReflectsGetOwnedCount()
        {
            _supportPackageServiceMock.Setup(s => s.GetOwnedCount(PlayerFactionId)).Returns(3);

            _controller.Show();

            var menu = _menuProvider.GetCurrentMenuDefinition();
            Assert.NotNull(menu);
            var ownedItem = menu!.GetItem(SupportCallMenuController.OwnedDisplayItemId);
            Assert.NotNull(ownedItem);
            Assert.Contains("3", ownedItem!.Text);
        }

        [Fact]
        public void Show_CallItem_EnabledWhenOwnedAndNoActiveSquadAndInZone()
        {
            _controller.Show();

            var menu = _menuProvider.GetCurrentMenuDefinition();
            var callItem = menu!.GetItem(SupportCallMenuController.CallItemId);
            Assert.NotNull(callItem);
            Assert.True(callItem!.IsEnabled);
        }

        [Fact]
        public void Show_CallItem_DisabledWhenNoneOwned()
        {
            _supportPackageServiceMock.Setup(s => s.GetOwnedCount(PlayerFactionId)).Returns(0);

            _controller.Show();

            var menu = _menuProvider.GetCurrentMenuDefinition();
            var callItem = menu!.GetItem(SupportCallMenuController.CallItemId);
            Assert.NotNull(callItem);
            Assert.False(callItem!.IsEnabled);
        }

        [Fact]
        public void Show_CallItem_DisabledWhenSquadAlreadyActive()
        {
            _supportSquadManagerMock.Setup(m => m.HasActiveSquad).Returns(true);

            _controller.Show();

            var menu = _menuProvider.GetCurrentMenuDefinition();
            var callItem = menu!.GetItem(SupportCallMenuController.CallItemId);
            Assert.NotNull(callItem);
            Assert.False(callItem!.IsEnabled);
        }

        [Fact]
        public void Show_CallItem_DisabledWhenNoCurrentZone()
        {
            _territoryMock.Setup(t => t.CurrentZone).Returns((Zone?)null);

            _controller.Show();

            var menu = _menuProvider.GetCurrentMenuDefinition();
            var callItem = menu!.GetItem(SupportCallMenuController.CallItemId);
            Assert.NotNull(callItem);
            Assert.False(callItem!.IsEnabled);
        }

        [Fact]
        public void SelectCall_WithPreconditionsMet_ConsumesAndCallsSquadAndNotifies()
        {
            _supportPackageServiceMock.Setup(s => s.TryConsume(PlayerFactionId)).Returns(true);
            _controller.Show();

            _menuProvider.SimulateItemSelection(SupportCallMenuController.CallItemId);

            _supportPackageServiceMock.Verify(s => s.TryConsume(PlayerFactionId), Times.Once);
            _supportSquadManagerMock.Verify(m => m.CallSupportSquad(_zone), Times.Once);
            Assert.NotEmpty(_gameBridge.Notifications);
        }

        [Fact]
        public void SelectCall_WhenSquadAlreadyActive_DoesNotCallSquad()
        {
            _supportSquadManagerMock.Setup(m => m.HasActiveSquad).Returns(true);
            _controller.Show();

            _menuProvider.SimulateItemSelection(SupportCallMenuController.CallItemId);

            _supportSquadManagerMock.Verify(m => m.CallSupportSquad(It.IsAny<Zone>()), Times.Never);
        }

        [Fact]
        public void SelectCall_WhenTryConsumeFails_DoesNotCallSquad()
        {
            _supportPackageServiceMock.Setup(s => s.TryConsume(PlayerFactionId)).Returns(false);
            _controller.Show();

            _menuProvider.SimulateItemSelection(SupportCallMenuController.CallItemId);

            _supportSquadManagerMock.Verify(m => m.CallSupportSquad(It.IsAny<Zone>()), Times.Never);
        }

        [Fact]
        public void SelectCall_WhenNoneOwned_DoesNotCallSquad()
        {
            _supportPackageServiceMock.Setup(s => s.GetOwnedCount(PlayerFactionId)).Returns(0);
            _controller.Show();

            _menuProvider.SimulateItemSelection(SupportCallMenuController.CallItemId);

            _supportSquadManagerMock.Verify(m => m.CallSupportSquad(It.IsAny<Zone>()), Times.Never);
            _supportPackageServiceMock.Verify(s => s.TryConsume(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public void SelectCall_WhenNoCurrentZone_DoesNotCallSquad()
        {
            _territoryMock.Setup(t => t.CurrentZone).Returns((Zone?)null);
            _controller.Show();

            _menuProvider.SimulateItemSelection(SupportCallMenuController.CallItemId);

            _supportSquadManagerMock.Verify(m => m.CallSupportSquad(It.IsAny<Zone>()), Times.Never);
            _supportPackageServiceMock.Verify(s => s.TryConsume(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public void Back_ShouldRaiseBackRequestedEvent()
        {
            var eventRaised = false;
            _controller.BackRequested += (s, e) => eventRaised = true;
            _controller.Show();

            _menuProvider.SimulateItemSelection(SupportCallMenuController.BackItemId);

            Assert.True(eventRaised);
        }

        [Fact]
        public void Back_ShouldCloseMenu()
        {
            _controller.Show();

            _menuProvider.SimulateItemSelection(SupportCallMenuController.BackItemId);

            Assert.False(_menuProvider.IsMenuVisible);
        }
    }
}
