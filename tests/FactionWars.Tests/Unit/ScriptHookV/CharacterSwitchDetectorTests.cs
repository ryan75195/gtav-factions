using FactionWars.Core.Interfaces;
using FactionWars.Core.Utils;
using FactionWars.ScriptHookV;
using Xunit;

namespace FactionWars.Tests.Unit.ScriptHookV
{
    public class CharacterSwitchDetectorTests
    {
        private readonly MockGameBridge _gameBridge;
        private readonly CharacterModelFactionDetector _factionDetector;
        private readonly CharacterSwitchDetector _detector;

        public CharacterSwitchDetectorTests()
        {
            _gameBridge = new MockGameBridge();
            _factionDetector = new CharacterModelFactionDetector();
            _detector = new CharacterSwitchDetector(_gameBridge, _factionDetector);
        }

        [Fact]
        public void Initialize_SetsCurrentFaction()
        {
            // Arrange
            _gameBridge.PlayerCharacterModel = "player_zero"; // Michael

            // Act
            _detector.Initialize();

            // Assert
            Assert.Equal("michael", _detector.CurrentFactionId);
        }

        [Fact]
        public void Initialize_WithTrevor_SetsTrevorFaction()
        {
            // Arrange
            _gameBridge.PlayerCharacterModel = "player_two"; // Trevor

            // Act
            _detector.Initialize();

            // Assert
            Assert.Equal("trevor", _detector.CurrentFactionId);
        }

        [Fact]
        public void Initialize_WithFranklin_SetsFranklinFaction()
        {
            // Arrange
            _gameBridge.PlayerCharacterModel = "player_one"; // Franklin

            // Act
            _detector.Initialize();

            // Assert
            Assert.Equal("franklin", _detector.CurrentFactionId);
        }

        [Fact]
        public void CheckForSwitch_NoChange_ReturnsFalse()
        {
            // Arrange
            _gameBridge.PlayerCharacterModel = "player_zero"; // Michael
            _detector.Initialize();

            // Act
            var result = _detector.CheckForSwitch();

            // Assert
            Assert.False(result);
            Assert.Equal("michael", _detector.CurrentFactionId);
        }

        [Fact]
        public void CheckForSwitch_CharacterChanged_ReturnsTrue()
        {
            // Arrange
            _gameBridge.PlayerCharacterModel = "player_zero"; // Michael
            _detector.Initialize();

            // Change character
            _gameBridge.PlayerCharacterModel = "player_two"; // Trevor

            // Act
            var result = _detector.CheckForSwitch();

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void CheckForSwitch_CharacterChanged_UpdatesCurrentFaction()
        {
            // Arrange
            _gameBridge.PlayerCharacterModel = "player_zero"; // Michael
            _detector.Initialize();

            // Change character
            _gameBridge.PlayerCharacterModel = "player_two"; // Trevor

            // Act
            _detector.CheckForSwitch();

            // Assert
            Assert.Equal("trevor", _detector.CurrentFactionId);
        }

        [Fact]
        public void CheckForSwitch_CharacterChanged_ExposesOldAndNewFaction()
        {
            // Arrange
            _gameBridge.PlayerCharacterModel = "player_zero"; // Michael
            _detector.Initialize();

            // Change character
            _gameBridge.PlayerCharacterModel = "player_one"; // Franklin

            // Act
            _detector.CheckForSwitch();

            // Assert
            Assert.Equal("michael", _detector.PreviousFactionId);
            Assert.Equal("franklin", _detector.CurrentFactionId);
        }

        [Fact]
        public void CheckForSwitch_MultipleChanges_TracksCorrectly()
        {
            // Arrange
            _gameBridge.PlayerCharacterModel = "player_zero"; // Michael
            _detector.Initialize();

            // First switch: Michael -> Trevor
            _gameBridge.PlayerCharacterModel = "player_two"; // Trevor
            var firstSwitch = _detector.CheckForSwitch();
            Assert.True(firstSwitch);
            Assert.Equal("trevor", _detector.CurrentFactionId);
            Assert.Equal("michael", _detector.PreviousFactionId);

            // Second check: No change
            var secondCheck = _detector.CheckForSwitch();
            Assert.False(secondCheck);
            Assert.Equal("trevor", _detector.CurrentFactionId);

            // Third switch: Trevor -> Franklin
            _gameBridge.PlayerCharacterModel = "player_one"; // Franklin
            var thirdSwitch = _detector.CheckForSwitch();
            Assert.True(thirdSwitch);
            Assert.Equal("franklin", _detector.CurrentFactionId);
            Assert.Equal("trevor", _detector.PreviousFactionId);
        }

        [Fact]
        public void OnCharacterSwitched_Event_RaisedOnSwitch()
        {
            // Arrange
            _gameBridge.PlayerCharacterModel = "player_zero"; // Michael
            _detector.Initialize();

            string? raisedOldFaction = null;
            string? raisedNewFaction = null;
            _detector.OnCharacterSwitched += (oldFaction, newFaction) =>
            {
                raisedOldFaction = oldFaction;
                raisedNewFaction = newFaction;
            };

            // Change character
            _gameBridge.PlayerCharacterModel = "player_two"; // Trevor

            // Act
            _detector.CheckForSwitch();

            // Assert
            Assert.Equal("michael", raisedOldFaction);
            Assert.Equal("trevor", raisedNewFaction);
        }

        [Fact]
        public void OnCharacterSwitched_Event_NotRaisedWhenNoSwitch()
        {
            // Arrange
            _gameBridge.PlayerCharacterModel = "player_zero"; // Michael
            _detector.Initialize();

            bool eventRaised = false;
            _detector.OnCharacterSwitched += (oldFaction, newFaction) =>
            {
                eventRaised = true;
            };

            // Act - No character change
            _detector.CheckForSwitch();

            // Assert
            Assert.False(eventRaised);
        }

        [Fact]
        public void CheckForSwitch_UnknownCharacterModel_ReturnsNull()
        {
            // Arrange
            _gameBridge.PlayerCharacterModel = "player_zero"; // Michael
            _detector.Initialize();

            // Change to unknown model (shouldn't happen but handle gracefully)
            _gameBridge.PlayerCharacterModel = "ig_bankman"; // Random NPC

            // Act
            var result = _detector.CheckForSwitch();

            // Assert - Should detect change but faction becomes null
            Assert.True(result);
            Assert.Null(_detector.CurrentFactionId);
            Assert.Equal("michael", _detector.PreviousFactionId);
        }

        [Fact]
        public void GetCurrentCharacterModel_ReturnsCurrentModel()
        {
            // Arrange
            _gameBridge.PlayerCharacterModel = "player_two"; // Trevor
            _detector.Initialize();

            // Act
            var model = _detector.CurrentCharacterModel;

            // Assert
            Assert.Equal("player_two", model);
        }

        [Fact]
        public void CheckForSwitch_BeforeInitialize_ReturnsFalse()
        {
            // Arrange - Don't call Initialize

            // Act
            var result = _detector.CheckForSwitch();

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsInitialized_BeforeInitialize_ReturnsFalse()
        {
            // Assert
            Assert.False(_detector.IsInitialized);
        }

        [Fact]
        public void IsInitialized_AfterInitialize_ReturnsTrue()
        {
            // Arrange
            _gameBridge.PlayerCharacterModel = "player_zero";

            // Act
            _detector.Initialize();

            // Assert
            Assert.True(_detector.IsInitialized);
        }
    }
}
