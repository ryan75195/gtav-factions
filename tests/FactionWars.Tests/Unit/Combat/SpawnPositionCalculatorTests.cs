using FactionWars.Combat.Interfaces;
using FactionWars.Combat.Services;
using FactionWars.Core.Interfaces;
using Moq;
using Xunit;

namespace FactionWars.Tests.Unit.Combat
{
    public class SpawnPositionCalculatorTests
    {
        private readonly Mock<IGameBridge> _gameBridgeMock;
        private readonly SpawnPositionCalculator _calculator;

        public SpawnPositionCalculatorTests()
        {
            _gameBridgeMock = new Mock<IGameBridge>();
            _calculator = new SpawnPositionCalculator(_gameBridgeMock.Object);
        }

        [Fact]
        public void Constructor_WithNullGameBridge_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<System.ArgumentNullException>(() => new SpawnPositionCalculator(null!));
        }

        [Fact]
        public void CalculateBehindPlayer_ShouldReturnPositionBehindPlayer()
        {
            // Arrange
            var playerPosition = new Vector3(100f, 200f, 30f);
            var playerHeading = 0f; // Facing north (positive Y direction)
            _gameBridgeMock.Setup(g => g.GetPlayerPosition()).Returns(playerPosition);
            _gameBridgeMock.Setup(g => g.GetPlayerHeading()).Returns(playerHeading);

            // Act
            var spawnPosition = _calculator.CalculateBehindPlayer(20f);

            // Assert - behind player means negative Y direction when facing north
            Assert.Equal(100f, spawnPosition.X, 0.1f);
            Assert.True(spawnPosition.Y < playerPosition.Y, "Spawn should be behind player (lower Y when facing north)");
            Assert.Equal(30f, spawnPosition.Z, 0.1f);
        }

        [Fact]
        public void CalculateBehindPlayer_FacingEast_ShouldReturnPositionBehindPlayer()
        {
            // Arrange
            var playerPosition = new Vector3(100f, 200f, 30f);
            var playerHeading = 90f; // Facing east (positive X direction)
            _gameBridgeMock.Setup(g => g.GetPlayerPosition()).Returns(playerPosition);
            _gameBridgeMock.Setup(g => g.GetPlayerHeading()).Returns(playerHeading);

            // Act
            var spawnPosition = _calculator.CalculateBehindPlayer(20f);

            // Assert - behind player means negative X direction when facing east
            Assert.True(spawnPosition.X < playerPosition.X, "Spawn should be behind player (lower X when facing east)");
            Assert.Equal(200f, spawnPosition.Y, 0.1f);
            Assert.Equal(30f, spawnPosition.Z, 0.1f);
        }

        [Fact]
        public void CalculateBehindPlayer_WithZeroDistance_ShouldReturnPlayerPosition()
        {
            // Arrange
            var playerPosition = new Vector3(100f, 200f, 30f);
            var playerHeading = 45f;
            _gameBridgeMock.Setup(g => g.GetPlayerPosition()).Returns(playerPosition);
            _gameBridgeMock.Setup(g => g.GetPlayerHeading()).Returns(playerHeading);

            // Act
            var spawnPosition = _calculator.CalculateBehindPlayer(0f);

            // Assert
            Assert.Equal(playerPosition.X, spawnPosition.X, 0.01f);
            Assert.Equal(playerPosition.Y, spawnPosition.Y, 0.01f);
            Assert.Equal(playerPosition.Z, spawnPosition.Z, 0.01f);
        }

        [Fact]
        public void CalculateBehindPlayer_ShouldRespectDistance()
        {
            // Arrange
            var playerPosition = new Vector3(0f, 0f, 0f);
            var playerHeading = 0f; // Facing north
            _gameBridgeMock.Setup(g => g.GetPlayerPosition()).Returns(playerPosition);
            _gameBridgeMock.Setup(g => g.GetPlayerHeading()).Returns(playerHeading);
            var distance = 15f;

            // Act
            var spawnPosition = _calculator.CalculateBehindPlayer(distance);

            // Assert - distance should be approximately 15
            var actualDistance = playerPosition.DistanceTo(spawnPosition);
            Assert.Equal(distance, actualDistance, 0.1f);
        }

        [Fact]
        public void CalculateNaturalSpawnPosition_ShouldReturnPositionBehindPlayerWithOffset()
        {
            // Arrange
            var playerPosition = new Vector3(100f, 200f, 30f);
            var playerHeading = 0f;
            _gameBridgeMock.Setup(g => g.GetPlayerPosition()).Returns(playerPosition);
            _gameBridgeMock.Setup(g => g.GetPlayerHeading()).Returns(playerHeading);

            // Act
            var spawnPosition = _calculator.CalculateNaturalSpawnPosition();

            // Assert - natural spawn should be behind player at a comfortable distance
            var distance = playerPosition.DistanceTo(spawnPosition);
            Assert.True(distance >= 15f && distance <= 35f,
                $"Natural spawn distance should be between 15-35m, but was {distance}");
            Assert.True(spawnPosition.Y < playerPosition.Y,
                "Natural spawn should be behind player");
        }

        [Fact]
        public void CalculateNaturalSpawnPositions_ShouldReturnMultiplePositions()
        {
            // Arrange
            var playerPosition = new Vector3(100f, 200f, 30f);
            var playerHeading = 0f;
            _gameBridgeMock.Setup(g => g.GetPlayerPosition()).Returns(playerPosition);
            _gameBridgeMock.Setup(g => g.GetPlayerHeading()).Returns(playerHeading);

            // Act
            var spawnPositions = _calculator.CalculateNaturalSpawnPositions(5);

            // Assert
            Assert.Equal(5, spawnPositions.Count);
            foreach (var pos in spawnPositions)
            {
                Assert.True(pos.Y < playerPosition.Y, "All spawns should be behind player");
            }
        }

        [Fact]
        public void CalculateNaturalSpawnPositions_WithZeroCount_ShouldReturnEmptyList()
        {
            // Arrange
            _gameBridgeMock.Setup(g => g.GetPlayerPosition()).Returns(new Vector3(0, 0, 0));
            _gameBridgeMock.Setup(g => g.GetPlayerHeading()).Returns(0f);

            // Act
            var spawnPositions = _calculator.CalculateNaturalSpawnPositions(0);

            // Assert
            Assert.Empty(spawnPositions);
        }

        [Fact]
        public void CalculateNaturalSpawnPositions_ShouldSpreadPositions()
        {
            // Arrange
            var playerPosition = new Vector3(100f, 200f, 30f);
            var playerHeading = 0f;
            _gameBridgeMock.Setup(g => g.GetPlayerPosition()).Returns(playerPosition);
            _gameBridgeMock.Setup(g => g.GetPlayerHeading()).Returns(playerHeading);

            // Act
            var spawnPositions = _calculator.CalculateNaturalSpawnPositions(3);

            // Assert - positions should be spread apart (not all at same spot)
            for (int i = 0; i < spawnPositions.Count - 1; i++)
            {
                for (int j = i + 1; j < spawnPositions.Count; j++)
                {
                    var distance = spawnPositions[i].DistanceTo(spawnPositions[j]);
                    Assert.True(distance > 1f, "Spawn positions should be spread apart");
                }
            }
        }

        [Fact]
        public void CalculateSpreadPositions_ShouldPlaceRequestedCountAroundCenter()
        {
            var center = new Vector3(10f, 20f, 5f);

            var positions = _calculator.CalculateSpreadPositions(center, 4, 2f, 6f);

            Assert.Equal(4, positions.Count);
            Assert.All(positions, position => Assert.Equal(center.Z, position.Z));
            Assert.All(positions, position => Assert.True(position.DistanceTo(center) >= 1.9f));
        }
    }
}
