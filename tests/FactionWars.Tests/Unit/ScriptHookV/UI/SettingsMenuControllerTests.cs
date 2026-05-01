using FactionWars.Core.Interfaces;
using FactionWars.Core.Models;
using FactionWars.ScriptHookV.UI;
using FactionWars.Tests.Mocks;
using Moq;
using System;
using Xunit;

namespace FactionWars.Tests.Unit.ScriptHookV.UI
{
    public class SettingsMenuControllerTests
    {
        private readonly MockMenuProvider _menuProvider;
        private readonly Mock<IDifficultyService> _difficultyServiceMock;
        private readonly Mock<IGameBridge> _gameBridgeMock;
        private readonly SettingsMenuController _controller;

        public SettingsMenuControllerTests()
        {
            _menuProvider = new MockMenuProvider();
            _difficultyServiceMock = new Mock<IDifficultyService>();
            _gameBridgeMock = new Mock<IGameBridge>();

            _difficultyServiceMock.Setup(d => d.Current).Returns(DifficultySettings.Normal);

            _controller = new SettingsMenuController(
                _menuProvider,
                _difficultyServiceMock.Object,
                _gameBridgeMock.Object);
        }

        #region Constructor Tests

        [Fact]
        public void Constructor_WithNullMenuProvider_ShouldThrowArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new SettingsMenuController(
                null!,
                _difficultyServiceMock.Object,
                _gameBridgeMock.Object));
        }

        [Fact]
        public void Constructor_WithNullDifficultyService_ShouldThrowArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new SettingsMenuController(
                _menuProvider,
                null!,
                _gameBridgeMock.Object));
        }

        [Fact]
        public void Constructor_WithNullGameBridge_ShouldThrowArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new SettingsMenuController(
                _menuProvider,
                _difficultyServiceMock.Object,
                null!));
        }

        #endregion

        #region Settings Menu Display

        [Fact]
        public void Show_ShouldOpenSettingsMenu()
        {
            _controller.Show();

            Assert.True(_menuProvider.IsMenuVisible);
            Assert.Equal(SettingsMenuController.SettingsMenuId, _menuProvider.CurrentMenuId);
        }

        [Fact]
        public void Show_ShouldHaveCorrectTitle()
        {
            _controller.Show();

            var menu = _menuProvider.GetCurrentMenuDefinition();
            Assert.NotNull(menu);
            Assert.Equal("Settings", menu!.Title);
        }

        [Fact]
        public void Show_ShouldHaveDebugModeItem()
        {
            _controller.Show();

            var menu = _menuProvider.GetCurrentMenuDefinition();
            Assert.NotNull(menu);
            var item = menu!.GetItem(SettingsMenuController.DebugModeItemId);
            Assert.NotNull(item);
            Assert.Contains("Debug", item!.Text);
        }

        [Fact]
        public void Show_ShouldHaveBackItem()
        {
            _controller.Show();

            var menu = _menuProvider.GetCurrentMenuDefinition();
            Assert.NotNull(menu);
            var item = menu!.GetItem(SettingsMenuController.BackItemId);
            Assert.NotNull(item);
            Assert.Equal("Back", item!.Text);
        }

        [Fact]
        public void Show_ShouldNotHaveSaveOrLoadItems()
        {
            _controller.Show();

            var menu = _menuProvider.GetCurrentMenuDefinition();
            Assert.NotNull(menu);
            // Save/Load slot UI was removed when mod state was tied to GTA's native saves.
            Assert.Null(menu!.GetItem("save_game"));
            Assert.Null(menu.GetItem("load_game"));
        }

        #endregion

        #region Debug Mode

        [Fact]
        public void Show_DebugModeItem_ShouldShowCurrentState()
        {
            Assert.False(_controller.IsDebugModeEnabled);

            _controller.Show();

            var menu = _menuProvider.GetCurrentMenuDefinition();
            var debugItem = menu?.GetItem(SettingsMenuController.DebugModeItemId);
            Assert.NotNull(debugItem);
            Assert.Contains("Off", debugItem!.Text);
        }

        [Fact]
        public void OnDebugModeSelected_ShouldToggleDebugMode()
        {
            _controller.Show();
            Assert.False(_controller.IsDebugModeEnabled);

            _menuProvider.SimulateItemSelection(SettingsMenuController.DebugModeItemId);

            Assert.True(_controller.IsDebugModeEnabled);
        }

        [Fact]
        public void OnDebugModeSelected_ShouldRaiseDebugModeChangedEvent()
        {
            bool eventRaised = false;
            bool newValue = false;
            _controller.DebugModeChanged += (sender, args) =>
            {
                eventRaised = true;
                newValue = args;
            };
            _controller.Show();

            _menuProvider.SimulateItemSelection(SettingsMenuController.DebugModeItemId);

            Assert.True(eventRaised);
            Assert.True(newValue);
        }

        [Fact]
        public void OnDebugModeSelected_WhenEnabled_ShouldDisable()
        {
            _controller.Show();
            _menuProvider.SimulateItemSelection(SettingsMenuController.DebugModeItemId);
            Assert.True(_controller.IsDebugModeEnabled);

            _controller.Show();
            _menuProvider.SimulateItemSelection(SettingsMenuController.DebugModeItemId);

            Assert.False(_controller.IsDebugModeEnabled);
        }

        #endregion

        #region Back Navigation

        [Fact]
        public void OnBackSelected_ShouldRaiseBackRequestedEvent()
        {
            bool eventRaised = false;
            _controller.BackRequested += (sender, args) => eventRaised = true;
            _controller.Show();

            _menuProvider.SimulateItemSelection(SettingsMenuController.BackItemId);

            Assert.True(eventRaised);
        }

        [Fact]
        public void OnBackSelected_ShouldCloseMenu()
        {
            _controller.Show();

            _menuProvider.SimulateItemSelection(SettingsMenuController.BackItemId);

            Assert.False(_menuProvider.IsMenuVisible);
        }

        #endregion

        #region Difficulty Menu

        [Fact]
        public void Show_IncludesDifficultyMenuItem()
        {
            _controller.Show();

            var menu = _menuProvider.GetCurrentMenuDefinition();
            Assert.NotNull(menu);
            var item = menu!.GetItem(SettingsMenuController.DifficultyItemId);
            Assert.NotNull(item);
            Assert.Contains("Difficulty", item!.Text);
            Assert.Contains("Normal", item.Text);
        }

        [Fact]
        public void Show_DifficultyMenuItem_ShowsCurrentDifficulty()
        {
            _difficultyServiceMock.Setup(d => d.Current).Returns(DifficultySettings.Hard);

            _controller.Show();

            var menu = _menuProvider.GetCurrentMenuDefinition();
            var item = menu?.GetItem(SettingsMenuController.DifficultyItemId);
            Assert.NotNull(item);
            Assert.Contains("Hard", item!.Text);
        }

        [Fact]
        public void ShowDifficultyMenu_ShowsThreeOptions()
        {
            _controller.ShowDifficultyMenu();

            Assert.True(_menuProvider.IsMenuVisible);
            Assert.Equal(SettingsMenuController.DifficultyMenuId, _menuProvider.CurrentMenuId);

            var menu = _menuProvider.GetCurrentMenuDefinition();
            Assert.NotNull(menu);

            var easyItem = menu!.GetItem("difficulty_easy");
            var normalItem = menu.GetItem("difficulty_normal");
            var hardItem = menu.GetItem("difficulty_hard");
            var backItem = menu.GetItem(SettingsMenuController.BackItemId);

            Assert.NotNull(easyItem);
            Assert.NotNull(normalItem);
            Assert.NotNull(hardItem);
            Assert.NotNull(backItem);

            Assert.Contains("Easy", easyItem!.Text);
            Assert.Contains("Normal", normalItem!.Text);
            Assert.Contains("Hard", hardItem!.Text);
        }

        [Fact]
        public void ShowDifficultyMenu_MarksCurrentDifficulty()
        {
            _difficultyServiceMock.Setup(d => d.Current).Returns(DifficultySettings.Normal);

            _controller.ShowDifficultyMenu();

            var menu = _menuProvider.GetCurrentMenuDefinition();
            var normalItem = menu?.GetItem("difficulty_normal");
            var easyItem = menu?.GetItem("difficulty_easy");
            var hardItem = menu?.GetItem("difficulty_hard");

            Assert.Contains("[Current]", normalItem!.Text);
            Assert.DoesNotContain("[Current]", easyItem!.Text);
            Assert.DoesNotContain("[Current]", hardItem!.Text);
        }

        [Fact]
        public void OnDifficultyItemSelected_ShouldShowDifficultyMenu()
        {
            _controller.Show();

            _menuProvider.SimulateItemSelection(SettingsMenuController.DifficultyItemId);

            Assert.Equal(SettingsMenuController.DifficultyMenuId, _menuProvider.CurrentMenuId);
        }

        [Fact]
        public void SelectDifficulty_ShowsConfirmation_WhenDifferent()
        {
            _difficultyServiceMock.Setup(d => d.Current).Returns(DifficultySettings.Normal);
            _controller.ShowDifficultyMenu();

            _menuProvider.SimulateItemSelection("difficulty_easy");

            Assert.Equal(SettingsMenuController.DifficultyConfirmMenuId, _menuProvider.CurrentMenuId);

            var menu = _menuProvider.GetCurrentMenuDefinition();
            Assert.NotNull(menu);
            var confirmItem = menu!.GetItem("confirm");
            var cancelItem = menu.GetItem("cancel");
            Assert.NotNull(confirmItem);
            Assert.NotNull(cancelItem);
        }

        [Fact]
        public void SelectDifficulty_SameAsCurrent_GoesBackToSettings()
        {
            _difficultyServiceMock.Setup(d => d.Current).Returns(DifficultySettings.Normal);
            _controller.ShowDifficultyMenu();

            _menuProvider.SimulateItemSelection("difficulty_normal");

            Assert.Equal(SettingsMenuController.SettingsMenuId, _menuProvider.CurrentMenuId);
        }

        [Fact]
        public void ConfirmDifficulty_SetsDifficultyAndReturnsToSettings()
        {
            _difficultyServiceMock.Setup(d => d.Current).Returns(DifficultySettings.Normal);
            _controller.ShowDifficultyMenu();
            _menuProvider.SimulateItemSelection("difficulty_easy");

            _menuProvider.SimulateItemSelection("confirm");

            _difficultyServiceMock.Verify(d => d.SetDifficulty(Difficulty.Easy), Times.Once);
            Assert.Equal(SettingsMenuController.SettingsMenuId, _menuProvider.CurrentMenuId);
        }

        [Fact]
        public void CancelDifficulty_GoesBackToDifficultyMenu()
        {
            _difficultyServiceMock.Setup(d => d.Current).Returns(DifficultySettings.Normal);
            _controller.ShowDifficultyMenu();
            _menuProvider.SimulateItemSelection("difficulty_easy");

            _menuProvider.SimulateItemSelection("cancel");

            Assert.Equal(SettingsMenuController.DifficultyMenuId, _menuProvider.CurrentMenuId);
        }

        [Fact]
        public void DifficultyMenu_BackItem_GoesToSettingsMenu()
        {
            _controller.ShowDifficultyMenu();

            _menuProvider.SimulateItemSelection(SettingsMenuController.BackItemId);

            Assert.Equal(SettingsMenuController.SettingsMenuId, _menuProvider.CurrentMenuId);
        }

        #endregion

        #region Init Starting Conditions

        [Fact]
        public void Show_ShouldHaveInitConditionsItem()
        {
            _controller.Show();

            var menu = _menuProvider.GetCurrentMenuDefinition();
            var item = menu?.GetItem(SettingsMenuController.InitializeConditionsItemId);
            Assert.NotNull(item);
        }

        [Fact]
        public void OnInitConditionsConfirmed_AppliesStartingConditions()
        {
            _controller.Show();
            _menuProvider.SimulateItemSelection(SettingsMenuController.InitializeConditionsItemId);

            _menuProvider.SimulateItemSelection("confirm");

            _gameBridgeMock.Verify(b => b.SetPlayerMoney(SettingsMenuController.StartingCashAmount), Times.Once);
            _gameBridgeMock.Verify(b => b.RemoveAllPlayerWeapons(), Times.Once);
            _gameBridgeMock.Verify(b => b.GivePlayerWeapon(SettingsMenuController.StartingWeapon, SettingsMenuController.StartingAmmo), Times.Once);
        }

        #endregion
    }
}
