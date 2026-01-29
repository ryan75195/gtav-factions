using System;
using FactionWars.Core.Interfaces;
using FactionWars.Core.Models;
using FactionWars.Core.Services;
using Xunit;

namespace FactionWars.Tests.Unit.Core.Services
{
    /// <summary>
    /// Tests for DifficultyService implementation.
    /// Validates difficulty management, event handling, and state tracking.
    /// </summary>
    public class DifficultyServiceTests
    {
        #region Constructor Tests

        [Fact]
        public void Constructor_DefaultsToNormal()
        {
            // Arrange & Act
            var service = new DifficultyService();

            // Assert
            Assert.Equal(Difficulty.Normal, service.Current.Level);
            Assert.Same(DifficultySettings.Normal, service.Current);
        }

        [Fact]
        public void Constructor_WithDifficulty_SetsInitialValue()
        {
            // Arrange & Act
            var service = new DifficultyService(Difficulty.Hard);

            // Assert
            Assert.Equal(Difficulty.Hard, service.Current.Level);
            Assert.Same(DifficultySettings.Hard, service.Current);
        }

        [Theory]
        [InlineData(Difficulty.Easy)]
        [InlineData(Difficulty.Normal)]
        [InlineData(Difficulty.Hard)]
        public void Constructor_WithEachDifficulty_SetsCorrectSettings(Difficulty level)
        {
            // Arrange & Act
            var service = new DifficultyService(level);

            // Assert
            Assert.Equal(level, service.Current.Level);
            Assert.Same(DifficultySettings.FromLevel(level), service.Current);
        }

        #endregion

        #region SetDifficulty Tests

        [Fact]
        public void SetDifficulty_UpdatesCurrent()
        {
            // Arrange
            var service = new DifficultyService(Difficulty.Normal);

            // Act
            service.SetDifficulty(Difficulty.Hard);

            // Assert
            Assert.Equal(Difficulty.Hard, service.Current.Level);
            Assert.Same(DifficultySettings.Hard, service.Current);
        }

        [Fact]
        public void SetDifficulty_RaisesEvent()
        {
            // Arrange
            var service = new DifficultyService(Difficulty.Normal);
            DifficultySettings? receivedSettings = null;
            service.DifficultyChanged += (sender, settings) => receivedSettings = settings;

            // Act
            service.SetDifficulty(Difficulty.Hard);

            // Assert
            Assert.NotNull(receivedSettings);
            Assert.Equal(Difficulty.Hard, receivedSettings!.Level);
            Assert.Same(DifficultySettings.Hard, receivedSettings);
        }

        [Fact]
        public void SetDifficulty_SameLevel_DoesNotRaiseEvent()
        {
            // Arrange
            var service = new DifficultyService(Difficulty.Normal);
            int eventCount = 0;
            service.DifficultyChanged += (sender, settings) => eventCount++;

            // Act
            service.SetDifficulty(Difficulty.Normal);

            // Assert
            Assert.Equal(0, eventCount);
        }

        [Theory]
        [InlineData(Difficulty.Easy, Difficulty.Normal)]
        [InlineData(Difficulty.Easy, Difficulty.Hard)]
        [InlineData(Difficulty.Normal, Difficulty.Easy)]
        [InlineData(Difficulty.Normal, Difficulty.Hard)]
        [InlineData(Difficulty.Hard, Difficulty.Easy)]
        [InlineData(Difficulty.Hard, Difficulty.Normal)]
        public void SetDifficulty_ChangingLevel_RaisesEventWithNewSettings(Difficulty initial, Difficulty target)
        {
            // Arrange
            var service = new DifficultyService(initial);
            DifficultySettings? receivedSettings = null;
            service.DifficultyChanged += (sender, settings) => receivedSettings = settings;

            // Act
            service.SetDifficulty(target);

            // Assert
            Assert.NotNull(receivedSettings);
            Assert.Equal(target, receivedSettings!.Level);
        }

        [Fact]
        public void SetDifficulty_EventSenderIsService()
        {
            // Arrange
            var service = new DifficultyService(Difficulty.Normal);
            object? receivedSender = null;
            service.DifficultyChanged += (sender, settings) => receivedSender = sender;

            // Act
            service.SetDifficulty(Difficulty.Hard);

            // Assert
            Assert.Same(service, receivedSender);
        }

        #endregion

        #region Interface Compliance Tests

        [Fact]
        public void DifficultyService_ImplementsIDifficultyService()
        {
            // Arrange & Act
            var service = new DifficultyService();

            // Assert
            Assert.IsAssignableFrom<IDifficultyService>(service);
        }

        #endregion
    }
}
