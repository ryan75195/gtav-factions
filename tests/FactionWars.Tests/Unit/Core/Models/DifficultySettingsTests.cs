using FactionWars.Core.Models;
using Xunit;

namespace FactionWars.Tests.Unit.Core.Models
{
    /// <summary>
    /// Tests for Difficulty enum and DifficultySettings model.
    /// Validates that difficulty presets have correct values and FromLevel returns appropriate settings.
    /// </summary>
    public class DifficultySettingsTests
    {
        #region Difficulty Enum Tests

        [Fact]
        public void Difficulty_HasEasyLevel()
        {
            var difficulty = Difficulty.Easy;
            Assert.Equal(Difficulty.Easy, difficulty);
        }

        [Fact]
        public void Difficulty_HasNormalLevel()
        {
            var difficulty = Difficulty.Normal;
            Assert.Equal(Difficulty.Normal, difficulty);
        }

        [Fact]
        public void Difficulty_HasHardLevel()
        {
            var difficulty = Difficulty.Hard;
            Assert.Equal(Difficulty.Hard, difficulty);
        }

        #endregion

        #region Easy Preset Tests

        [Fact]
        public void Easy_HasCorrectLevel()
        {
            var settings = DifficultySettings.Easy;

            Assert.Equal(Difficulty.Easy, settings.Level);
        }

        [Fact]
        public void Easy_HasCorrectAiIncomeMultiplier()
        {
            var settings = DifficultySettings.Easy;

            Assert.Equal(0.75f, settings.AiIncomeMultiplier);
        }

        [Fact]
        public void Easy_HasCorrectTickIntervalMinutes()
        {
            var settings = DifficultySettings.Easy;

            Assert.Equal(7, settings.TickIntervalMinutes);
        }

        [Fact]
        public void Easy_HasCorrectTickIntervalSeconds()
        {
            var settings = DifficultySettings.Easy;

            Assert.Equal(420, settings.TickIntervalSeconds);
        }

        #endregion

        #region Normal Preset Tests

        [Fact]
        public void Normal_HasCorrectLevel()
        {
            var settings = DifficultySettings.Normal;

            Assert.Equal(Difficulty.Normal, settings.Level);
        }

        [Fact]
        public void Normal_HasCorrectAiIncomeMultiplier()
        {
            var settings = DifficultySettings.Normal;

            Assert.Equal(1.0f, settings.AiIncomeMultiplier);
        }

        [Fact]
        public void Normal_HasCorrectTickIntervalMinutes()
        {
            var settings = DifficultySettings.Normal;

            Assert.Equal(5, settings.TickIntervalMinutes);
        }

        [Fact]
        public void Normal_HasCorrectTickIntervalSeconds()
        {
            var settings = DifficultySettings.Normal;

            Assert.Equal(300, settings.TickIntervalSeconds);
        }

        #endregion

        #region Hard Preset Tests

        [Fact]
        public void Hard_HasCorrectLevel()
        {
            var settings = DifficultySettings.Hard;

            Assert.Equal(Difficulty.Hard, settings.Level);
        }

        [Fact]
        public void Hard_HasCorrectAiIncomeMultiplier()
        {
            var settings = DifficultySettings.Hard;

            Assert.Equal(1.25f, settings.AiIncomeMultiplier);
        }

        [Fact]
        public void Hard_HasCorrectTickIntervalMinutes()
        {
            var settings = DifficultySettings.Hard;

            Assert.Equal(3, settings.TickIntervalMinutes);
        }

        [Fact]
        public void Hard_HasCorrectTickIntervalSeconds()
        {
            var settings = DifficultySettings.Hard;

            Assert.Equal(180, settings.TickIntervalSeconds);
        }

        #endregion

        #region FromLevel Tests

        [Fact]
        public void FromLevel_Easy_ReturnsEasyPreset()
        {
            var settings = DifficultySettings.FromLevel(Difficulty.Easy);

            Assert.Equal(Difficulty.Easy, settings.Level);
            Assert.Equal(0.75f, settings.AiIncomeMultiplier);
            Assert.Equal(7, settings.TickIntervalMinutes);
        }

        [Fact]
        public void FromLevel_Normal_ReturnsNormalPreset()
        {
            var settings = DifficultySettings.FromLevel(Difficulty.Normal);

            Assert.Equal(Difficulty.Normal, settings.Level);
            Assert.Equal(1.0f, settings.AiIncomeMultiplier);
            Assert.Equal(5, settings.TickIntervalMinutes);
        }

        [Fact]
        public void FromLevel_Hard_ReturnsHardPreset()
        {
            var settings = DifficultySettings.FromLevel(Difficulty.Hard);

            Assert.Equal(Difficulty.Hard, settings.Level);
            Assert.Equal(1.25f, settings.AiIncomeMultiplier);
            Assert.Equal(3, settings.TickIntervalMinutes);
        }

        #endregion

        #region Immutability Tests

        [Fact]
        public void Easy_ReturnsSameInstanceEachTime()
        {
            var settings1 = DifficultySettings.Easy;
            var settings2 = DifficultySettings.Easy;

            Assert.Same(settings1, settings2);
        }

        [Fact]
        public void Normal_ReturnsSameInstanceEachTime()
        {
            var settings1 = DifficultySettings.Normal;
            var settings2 = DifficultySettings.Normal;

            Assert.Same(settings1, settings2);
        }

        [Fact]
        public void Hard_ReturnsSameInstanceEachTime()
        {
            var settings1 = DifficultySettings.Hard;
            var settings2 = DifficultySettings.Hard;

            Assert.Same(settings1, settings2);
        }

        [Fact]
        public void FromLevel_Easy_ReturnsSameInstanceAsEasyPreset()
        {
            var fromLevel = DifficultySettings.FromLevel(Difficulty.Easy);
            var preset = DifficultySettings.Easy;

            Assert.Same(preset, fromLevel);
        }

        [Fact]
        public void FromLevel_Normal_ReturnsSameInstanceAsNormalPreset()
        {
            var fromLevel = DifficultySettings.FromLevel(Difficulty.Normal);
            var preset = DifficultySettings.Normal;

            Assert.Same(preset, fromLevel);
        }

        [Fact]
        public void FromLevel_Hard_ReturnsSameInstanceAsHardPreset()
        {
            var fromLevel = DifficultySettings.FromLevel(Difficulty.Hard);
            var preset = DifficultySettings.Hard;

            Assert.Same(preset, fromLevel);
        }

        #endregion
    }
}
