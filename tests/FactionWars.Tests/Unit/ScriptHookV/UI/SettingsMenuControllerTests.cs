using FactionWars.Core.Interfaces;
using FactionWars.Core.Models;
using FactionWars.Persistence.Models;
using FactionWars.ScriptHookV.UI;
using FactionWars.Tests.Mocks;
using FactionWars.UI.Interfaces;
using FactionWars.UI.Models;
using Moq;
using System;
using System.Collections.Generic;
using Xunit;

namespace FactionWars.Tests.Unit.ScriptHookV.UI
{
    /// <summary>
    /// Tests for SettingsMenuController providing save, load, and debug options.
    /// </summary>
    public class SettingsMenuControllerTests
    {
        private readonly MockMenuProvider _menuProvider;
        private readonly Mock<ISaveSlotManager> _saveSlotManagerMock;
        private readonly Mock<IGameStateCoordinator> _gameStateCoordinatorMock;
        private readonly Mock<IDifficultyService> _difficultyServiceMock;
        private readonly SettingsMenuController _controller;

        public SettingsMenuControllerTests()
        {
            _menuProvider = new MockMenuProvider();
            _saveSlotManagerMock = new Mock<ISaveSlotManager>();
            _gameStateCoordinatorMock = new Mock<IGameStateCoordinator>();
            _difficultyServiceMock = new Mock<IDifficultyService>();

            // Setup default save slot configuration
            _saveSlotManagerMock.Setup(s => s.MaxSlots).Returns(5);
            _saveSlotManagerMock.Setup(s => s.GetAllSlotInfo()).Returns(new List<SaveSlotInfo>
            {
                CreateEmptySlotInfo(0),
                CreateOccupiedSlotInfo(1, "My Save", DateTime.Now.AddHours(-2), 3600),
                CreateEmptySlotInfo(2),
                CreateEmptySlotInfo(3),
                CreateEmptySlotInfo(4)
            });

            // Setup default difficulty
            _difficultyServiceMock.Setup(d => d.Current).Returns(DifficultySettings.Normal);

            _controller = new SettingsMenuController(
                _menuProvider,
                _saveSlotManagerMock.Object,
                _gameStateCoordinatorMock.Object,
                _difficultyServiceMock.Object);
        }

        private SaveSlotInfo CreateEmptySlotInfo(int slotNumber)
        {
            return new SaveSlotInfo(slotNumber);
        }

        private SaveSlotInfo CreateOccupiedSlotInfo(int slotNumber, string name, DateTime modifiedAt, long playTime)
        {
            var info = new SaveSlotInfo(slotNumber);
            info.SetOccupied(name, modifiedAt, playTime);
            return info;
        }

        #region Constructor Tests

        [Fact]
        public void Constructor_WithNullMenuProvider_ShouldThrowArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new SettingsMenuController(
                null!,
                _saveSlotManagerMock.Object,
                _gameStateCoordinatorMock.Object,
                _difficultyServiceMock.Object));
        }

        [Fact]
        public void Constructor_WithNullSaveSlotManager_ShouldThrowArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new SettingsMenuController(
                _menuProvider,
                null!,
                _gameStateCoordinatorMock.Object,
                _difficultyServiceMock.Object));
        }

        [Fact]
        public void Constructor_WithNullGameStateCoordinator_ShouldThrowArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new SettingsMenuController(
                _menuProvider,
                _saveSlotManagerMock.Object,
                null!,
                _difficultyServiceMock.Object));
        }

        [Fact]
        public void Constructor_WithNullDifficultyService_ShouldThrowArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new SettingsMenuController(
                _menuProvider,
                _saveSlotManagerMock.Object,
                _gameStateCoordinatorMock.Object,
                null!));
        }

        #endregion

        #region Menu Display Tests

        [Fact]
        public void Show_ShouldOpenSettingsMenu()
        {
            // Act
            _controller.Show();

            // Assert
            Assert.True(_menuProvider.IsMenuVisible);
            Assert.Equal(SettingsMenuController.SettingsMenuId, _menuProvider.CurrentMenuId);
        }

        [Fact]
        public void Show_ShouldHaveCorrectTitle()
        {
            // Act
            _controller.Show();

            // Assert
            var menu = _menuProvider.GetCurrentMenuDefinition();
            Assert.NotNull(menu);
            Assert.Equal("Settings", menu!.Title);
        }

        [Fact]
        public void Show_ShouldHaveSaveGameItem()
        {
            // Act
            _controller.Show();

            // Assert
            var menu = _menuProvider.GetCurrentMenuDefinition();
            Assert.NotNull(menu);
            var item = menu!.GetItem(SettingsMenuController.SaveGameItemId);
            Assert.NotNull(item);
            Assert.Contains("Save", item!.Text);
        }

        [Fact]
        public void Show_ShouldHaveLoadGameItem()
        {
            // Act
            _controller.Show();

            // Assert
            var menu = _menuProvider.GetCurrentMenuDefinition();
            Assert.NotNull(menu);
            var item = menu!.GetItem(SettingsMenuController.LoadGameItemId);
            Assert.NotNull(item);
            Assert.Contains("Load", item!.Text);
        }

        [Fact]
        public void Show_ShouldHaveDebugModeItem()
        {
            // Act
            _controller.Show();

            // Assert
            var menu = _menuProvider.GetCurrentMenuDefinition();
            Assert.NotNull(menu);
            var item = menu!.GetItem(SettingsMenuController.DebugModeItemId);
            Assert.NotNull(item);
            Assert.Contains("Debug", item!.Text);
        }

        [Fact]
        public void Show_ShouldHaveBackItem()
        {
            // Act
            _controller.Show();

            // Assert
            var menu = _menuProvider.GetCurrentMenuDefinition();
            Assert.NotNull(menu);
            var item = menu!.GetItem(SettingsMenuController.BackItemId);
            Assert.NotNull(item);
            Assert.Equal("Back", item!.Text);
        }

        #endregion

        #region Save Menu Tests

        [Fact]
        public void ShowSaveMenu_ShouldOpenSaveMenu()
        {
            // Act
            _controller.ShowSaveMenu();

            // Assert
            Assert.True(_menuProvider.IsMenuVisible);
            Assert.Equal(SettingsMenuController.SaveMenuId, _menuProvider.CurrentMenuId);
        }

        [Fact]
        public void ShowSaveMenu_ShouldDisplayAllSlots()
        {
            // Act
            _controller.ShowSaveMenu();

            // Assert
            var menu = _menuProvider.GetCurrentMenuDefinition();
            Assert.NotNull(menu);

            // Should have items for all 5 slots plus back
            for (int i = 0; i < 5; i++)
            {
                var item = menu!.GetItem($"save_slot_{i}");
                Assert.NotNull(item);
            }
        }

        [Fact]
        public void ShowSaveMenu_EmptySlot_ShouldShowEmptyLabel()
        {
            // Act
            _controller.ShowSaveMenu();

            // Assert
            var menu = _menuProvider.GetCurrentMenuDefinition();
            var emptySlotItem = menu?.GetItem("save_slot_0");
            Assert.NotNull(emptySlotItem);
            Assert.Contains("Empty", emptySlotItem!.Text);
        }

        [Fact]
        public void ShowSaveMenu_OccupiedSlot_ShouldShowSaveName()
        {
            // Act
            _controller.ShowSaveMenu();

            // Assert
            var menu = _menuProvider.GetCurrentMenuDefinition();
            var occupiedSlotItem = menu?.GetItem("save_slot_1");
            Assert.NotNull(occupiedSlotItem);
            Assert.Contains("My Save", occupiedSlotItem!.Text);
        }

        [Fact]
        public void OnSaveSlotSelected_ShouldTriggerSave()
        {
            // Arrange
            _controller.ShowSaveMenu();

            // Act
            _menuProvider.SimulateItemSelection("save_slot_0");

            // Assert
            _gameStateCoordinatorMock.Verify(g => g.SaveToSlot(0), Times.Once);
        }

        [Fact]
        public void OnSaveSlotSelected_ShouldRaiseSaveCompletedEvent()
        {
            // Arrange
            bool eventRaised = false;
            _controller.SaveCompleted += (sender, args) => eventRaised = true;
            _controller.ShowSaveMenu();

            // Act
            _menuProvider.SimulateItemSelection("save_slot_0");

            // Assert
            Assert.True(eventRaised);
        }

        #endregion

        #region Load Menu Tests

        [Fact]
        public void ShowLoadMenu_ShouldOpenLoadMenu()
        {
            // Act
            _controller.ShowLoadMenu();

            // Assert
            Assert.True(_menuProvider.IsMenuVisible);
            Assert.Equal(SettingsMenuController.LoadMenuId, _menuProvider.CurrentMenuId);
        }

        [Fact]
        public void ShowLoadMenu_ShouldDisplayAllSlots()
        {
            // Act
            _controller.ShowLoadMenu();

            // Assert
            var menu = _menuProvider.GetCurrentMenuDefinition();
            Assert.NotNull(menu);

            for (int i = 0; i < 5; i++)
            {
                var item = menu!.GetItem($"load_slot_{i}");
                Assert.NotNull(item);
            }
        }

        [Fact]
        public void ShowLoadMenu_EmptySlot_ShouldBeDisabled()
        {
            // Act
            _controller.ShowLoadMenu();

            // Assert
            var menu = _menuProvider.GetCurrentMenuDefinition();
            var emptySlotItem = menu?.GetItem("load_slot_0");
            Assert.NotNull(emptySlotItem);
            Assert.False(emptySlotItem!.IsEnabled);
        }

        [Fact]
        public void ShowLoadMenu_OccupiedSlot_ShouldBeEnabled()
        {
            // Act
            _controller.ShowLoadMenu();

            // Assert
            var menu = _menuProvider.GetCurrentMenuDefinition();
            var occupiedSlotItem = menu?.GetItem("load_slot_1");
            Assert.NotNull(occupiedSlotItem);
            Assert.True(occupiedSlotItem!.IsEnabled);
        }

        [Fact]
        public void OnLoadSlotSelected_ShouldTriggerLoad()
        {
            // Arrange
            _controller.ShowLoadMenu();

            // Act
            _menuProvider.SimulateItemSelection("load_slot_1");

            // Assert
            _gameStateCoordinatorMock.Verify(g => g.LoadFromSlot(1), Times.Once);
        }

        [Fact]
        public void OnLoadSlotSelected_ShouldRaiseLoadCompletedEvent()
        {
            // Arrange
            bool eventRaised = false;
            _controller.LoadCompleted += (sender, args) => eventRaised = true;
            _controller.ShowLoadMenu();

            // Act
            _menuProvider.SimulateItemSelection("load_slot_1");

            // Assert
            Assert.True(eventRaised);
        }

        #endregion

        #region Debug Mode Tests

        [Fact]
        public void Show_DebugModeItem_ShouldShowCurrentState()
        {
            // Arrange - debug mode is off by default
            Assert.False(_controller.IsDebugModeEnabled);

            // Act
            _controller.Show();

            // Assert
            var menu = _menuProvider.GetCurrentMenuDefinition();
            var debugItem = menu?.GetItem(SettingsMenuController.DebugModeItemId);
            Assert.NotNull(debugItem);
            Assert.Contains("Off", debugItem!.Text);
        }

        [Fact]
        public void OnDebugModeSelected_ShouldToggleDebugMode()
        {
            // Arrange
            _controller.Show();
            Assert.False(_controller.IsDebugModeEnabled);

            // Act
            _menuProvider.SimulateItemSelection(SettingsMenuController.DebugModeItemId);

            // Assert
            Assert.True(_controller.IsDebugModeEnabled);
        }

        [Fact]
        public void OnDebugModeSelected_ShouldRaiseDebugModeChangedEvent()
        {
            // Arrange
            bool eventRaised = false;
            bool newValue = false;
            _controller.DebugModeChanged += (sender, args) =>
            {
                eventRaised = true;
                newValue = args;
            };
            _controller.Show();

            // Act
            _menuProvider.SimulateItemSelection(SettingsMenuController.DebugModeItemId);

            // Assert
            Assert.True(eventRaised);
            Assert.True(newValue);
        }

        [Fact]
        public void OnDebugModeSelected_WhenEnabled_ShouldDisable()
        {
            // Arrange
            _controller.Show();
            _menuProvider.SimulateItemSelection(SettingsMenuController.DebugModeItemId); // Turn on
            Assert.True(_controller.IsDebugModeEnabled);

            // Act
            _controller.Show(); // Refresh menu
            _menuProvider.SimulateItemSelection(SettingsMenuController.DebugModeItemId); // Turn off

            // Assert
            Assert.False(_controller.IsDebugModeEnabled);
        }

        #endregion

        #region Back Navigation Tests

        [Fact]
        public void OnBackSelected_ShouldRaiseBackRequestedEvent()
        {
            // Arrange
            bool eventRaised = false;
            _controller.BackRequested += (sender, args) => eventRaised = true;
            _controller.Show();

            // Act
            _menuProvider.SimulateItemSelection(SettingsMenuController.BackItemId);

            // Assert
            Assert.True(eventRaised);
        }

        [Fact]
        public void OnBackSelected_ShouldCloseMenu()
        {
            // Arrange
            _controller.Show();

            // Act
            _menuProvider.SimulateItemSelection(SettingsMenuController.BackItemId);

            // Assert
            Assert.False(_menuProvider.IsMenuVisible);
        }

        [Fact]
        public void SaveMenu_OnBackSelected_ShouldReturnToSettingsMenu()
        {
            // Arrange
            bool backRaised = false;
            _controller.SaveMenuBackRequested += (sender, args) => backRaised = true;
            _controller.ShowSaveMenu();

            // Act
            _menuProvider.SimulateItemSelection(SettingsMenuController.BackItemId);

            // Assert
            Assert.True(backRaised);
        }

        [Fact]
        public void LoadMenu_OnBackSelected_ShouldReturnToSettingsMenu()
        {
            // Arrange
            bool backRaised = false;
            _controller.LoadMenuBackRequested += (sender, args) => backRaised = true;
            _controller.ShowLoadMenu();

            // Act
            _menuProvider.SimulateItemSelection(SettingsMenuController.BackItemId);

            // Assert
            Assert.True(backRaised);
        }

        #endregion

        #region Menu Item Selection Navigation Tests

        [Fact]
        public void OnSaveGameItemSelected_ShouldShowSaveMenu()
        {
            // Arrange
            _controller.Show();

            // Act
            _menuProvider.SimulateItemSelection(SettingsMenuController.SaveGameItemId);

            // Assert
            Assert.Equal(SettingsMenuController.SaveMenuId, _menuProvider.CurrentMenuId);
        }

        [Fact]
        public void OnLoadGameItemSelected_ShouldShowLoadMenu()
        {
            // Arrange
            _controller.Show();

            // Act
            _menuProvider.SimulateItemSelection(SettingsMenuController.LoadGameItemId);

            // Assert
            Assert.Equal(SettingsMenuController.LoadMenuId, _menuProvider.CurrentMenuId);
        }

        #endregion

        #region Menu Item Order Tests

        [Fact]
        public void Show_MenuItemsShouldBeInCorrectOrder()
        {
            // Act
            _controller.Show();

            // Assert
            var menu = _menuProvider.GetCurrentMenuDefinition();
            Assert.NotNull(menu);

            // Order: Save Game, Load Game, Difficulty, Debug Mode, Back
            Assert.Equal(SettingsMenuController.SaveGameItemId, menu!.Items[0].Id);
            Assert.Equal(SettingsMenuController.LoadGameItemId, menu.Items[1].Id);
            Assert.Equal(SettingsMenuController.DifficultyItemId, menu.Items[2].Id);
            Assert.Equal(SettingsMenuController.DebugModeItemId, menu.Items[3].Id);
            Assert.Equal(SettingsMenuController.BackItemId, menu.Items[menu.Items.Count - 1].Id);
        }

        #endregion

        #region Items Enabled State Tests

        [Fact]
        public void Show_AllActionItemsShouldBeEnabled()
        {
            // Act
            _controller.Show();

            // Assert
            var menu = _menuProvider.GetCurrentMenuDefinition();
            Assert.NotNull(menu);

            var saveItem = menu!.GetItem(SettingsMenuController.SaveGameItemId);
            var loadItem = menu.GetItem(SettingsMenuController.LoadGameItemId);
            var difficultyItem = menu.GetItem(SettingsMenuController.DifficultyItemId);
            var debugItem = menu.GetItem(SettingsMenuController.DebugModeItemId);
            var backItem = menu.GetItem(SettingsMenuController.BackItemId);

            Assert.True(saveItem!.IsEnabled);
            Assert.True(loadItem!.IsEnabled);
            Assert.True(difficultyItem!.IsEnabled);
            Assert.True(debugItem!.IsEnabled);
            Assert.True(backItem!.IsEnabled);
        }

        #endregion

        #region Difficulty Menu Tests

        [Fact]
        public void Show_IncludesDifficultyMenuItem()
        {
            // Act
            _controller.Show();

            // Assert
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
            // Arrange
            _difficultyServiceMock.Setup(d => d.Current).Returns(DifficultySettings.Hard);

            // Act
            _controller.Show();

            // Assert
            var menu = _menuProvider.GetCurrentMenuDefinition();
            var item = menu?.GetItem(SettingsMenuController.DifficultyItemId);
            Assert.NotNull(item);
            Assert.Contains("Hard", item!.Text);
        }

        [Fact]
        public void ShowDifficultyMenu_ShowsThreeOptions()
        {
            // Act
            _controller.ShowDifficultyMenu();

            // Assert
            Assert.True(_menuProvider.IsMenuVisible);
            Assert.Equal(SettingsMenuController.DifficultyMenuId, _menuProvider.CurrentMenuId);

            var menu = _menuProvider.GetCurrentMenuDefinition();
            Assert.NotNull(menu);

            // Should have Easy, Normal, Hard plus Back
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
            // Arrange - Normal is current
            _difficultyServiceMock.Setup(d => d.Current).Returns(DifficultySettings.Normal);

            // Act
            _controller.ShowDifficultyMenu();

            // Assert
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
            // Arrange
            _controller.Show();

            // Act
            _menuProvider.SimulateItemSelection(SettingsMenuController.DifficultyItemId);

            // Assert
            Assert.Equal(SettingsMenuController.DifficultyMenuId, _menuProvider.CurrentMenuId);
        }

        [Fact]
        public void SelectDifficulty_ShowsConfirmation_WhenDifferent()
        {
            // Arrange - Current is Normal, selecting Easy
            _difficultyServiceMock.Setup(d => d.Current).Returns(DifficultySettings.Normal);
            _controller.ShowDifficultyMenu();

            // Act
            _menuProvider.SimulateItemSelection("difficulty_easy");

            // Assert - Should show confirmation menu
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
            // Arrange - Current is Normal, selecting Normal
            _difficultyServiceMock.Setup(d => d.Current).Returns(DifficultySettings.Normal);
            _controller.ShowDifficultyMenu();

            // Act
            _menuProvider.SimulateItemSelection("difficulty_normal");

            // Assert - Should go back to settings menu (not confirmation)
            Assert.Equal(SettingsMenuController.SettingsMenuId, _menuProvider.CurrentMenuId);
        }

        [Fact]
        public void ConfirmDifficulty_SetsDifficultyAndReturnsToSettings()
        {
            // Arrange - Current is Normal, selecting Easy
            _difficultyServiceMock.Setup(d => d.Current).Returns(DifficultySettings.Normal);
            _controller.ShowDifficultyMenu();
            _menuProvider.SimulateItemSelection("difficulty_easy");

            // Act - Confirm the selection
            _menuProvider.SimulateItemSelection("confirm");

            // Assert
            _difficultyServiceMock.Verify(d => d.SetDifficulty(Difficulty.Easy), Times.Once);
            Assert.Equal(SettingsMenuController.SettingsMenuId, _menuProvider.CurrentMenuId);
        }

        [Fact]
        public void CancelDifficulty_GoesBackToDifficultyMenu()
        {
            // Arrange - Current is Normal, selecting Easy
            _difficultyServiceMock.Setup(d => d.Current).Returns(DifficultySettings.Normal);
            _controller.ShowDifficultyMenu();
            _menuProvider.SimulateItemSelection("difficulty_easy");

            // Act - Cancel the selection
            _menuProvider.SimulateItemSelection("cancel");

            // Assert - Should go back to difficulty menu
            Assert.Equal(SettingsMenuController.DifficultyMenuId, _menuProvider.CurrentMenuId);
        }

        [Fact]
        public void DifficultyMenu_BackItem_GoesToSettingsMenu()
        {
            // Arrange
            _controller.ShowDifficultyMenu();

            // Act
            _menuProvider.SimulateItemSelection(SettingsMenuController.BackItemId);

            // Assert
            Assert.Equal(SettingsMenuController.SettingsMenuId, _menuProvider.CurrentMenuId);
        }

        #endregion
    }
}
