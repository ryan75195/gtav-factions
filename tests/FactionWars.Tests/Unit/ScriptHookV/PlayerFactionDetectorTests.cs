using FactionWars.ScriptHookV;
using Xunit;

namespace FactionWars.Tests.Unit.ScriptHookV
{
    public class PlayerFactionDetectorTests
    {
        [Theory]
        [InlineData("player_zero", "michael")]    // Michael's ped model
        [InlineData("PLAYER_ZERO", "michael")]    // Case insensitive
        [InlineData("player_one", "franklin")]    // Franklin's ped model
        [InlineData("PLAYER_ONE", "franklin")]    // Case insensitive
        [InlineData("player_two", "trevor")]      // Trevor's ped model
        [InlineData("PLAYER_TWO", "trevor")]      // Case insensitive
        public void GetFactionIdFromCharacterModel_WithKnownCharacter_ReturnsFactionId(string modelName, string expectedFactionId)
        {
            // Arrange
            var detector = new CharacterModelFactionDetector();

            // Act
            var result = detector.GetFactionIdFromCharacterModel(modelName);

            // Assert
            Assert.Equal(expectedFactionId, result);
        }

        [Theory]
        [InlineData("ig_bankman")]           // Random NPC
        [InlineData("a_m_m_bevhills_01")]    // Random pedestrian
        [InlineData("")]                      // Empty string
        public void GetFactionIdFromCharacterModel_WithUnknownCharacter_ReturnsNull(string modelName)
        {
            // Arrange
            var detector = new CharacterModelFactionDetector();

            // Act
            var result = detector.GetFactionIdFromCharacterModel(modelName);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void GetFactionIdFromCharacterModel_WithNull_ReturnsNull()
        {
            // Arrange
            var detector = new CharacterModelFactionDetector();

            // Act
            var result = detector.GetFactionIdFromCharacterModel(null!);

            // Assert
            Assert.Null(result);
        }

        [Theory]
        [InlineData("michael", "player_zero")]
        [InlineData("franklin", "player_one")]
        [InlineData("trevor", "player_two")]
        public void GetCharacterModelForFaction_WithKnownFaction_ReturnsModelName(string factionId, string expectedModel)
        {
            // Arrange
            var detector = new CharacterModelFactionDetector();

            // Act
            var result = detector.GetCharacterModelForFaction(factionId);

            // Assert
            Assert.Equal(expectedModel, result);
        }

        [Theory]
        [InlineData("unknown_faction")]
        [InlineData("")]
        public void GetCharacterModelForFaction_WithUnknownFaction_ReturnsNull(string factionId)
        {
            // Arrange
            var detector = new CharacterModelFactionDetector();

            // Act
            var result = detector.GetCharacterModelForFaction(factionId);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void GetCharacterModelForFaction_WithNull_ReturnsNull()
        {
            // Arrange
            var detector = new CharacterModelFactionDetector();

            // Act
            var result = detector.GetCharacterModelForFaction(null!);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void GetFactionIdFromCharacterModel_CaseInsensitive_WorksCorrectly()
        {
            // Arrange
            var detector = new CharacterModelFactionDetector();

            // Act & Assert
            Assert.Equal("michael", detector.GetFactionIdFromCharacterModel("Player_Zero"));
            Assert.Equal("michael", detector.GetFactionIdFromCharacterModel("PLAYER_ZERO"));
            Assert.Equal("michael", detector.GetFactionIdFromCharacterModel("player_zero"));
        }

        [Fact]
        public void GetCharacterModelForFaction_CaseInsensitive_WorksCorrectly()
        {
            // Arrange
            var detector = new CharacterModelFactionDetector();

            // Act & Assert
            Assert.Equal("player_zero", detector.GetCharacterModelForFaction("Michael"));
            Assert.Equal("player_zero", detector.GetCharacterModelForFaction("MICHAEL"));
            Assert.Equal("player_zero", detector.GetCharacterModelForFaction("michael"));
        }
    }
}
