using FactionWars.Core.Interfaces;
using Moq;
using Xunit;

namespace FactionWars.Tests.Unit.Core
{
    public class GameBridgeTests
    {
        [Fact]
        public void IGameBridge_ShouldBeImplementable()
        {
            // Arrange & Act
            var mock = new Mock<IGameBridge>();

            // Assert
            Assert.NotNull(mock.Object);
        }

        [Fact]
        public void GetPlayerPosition_ShouldReturnVector3()
        {
            // Arrange
            var mock = new Mock<IGameBridge>();
            mock.Setup(x => x.GetPlayerPosition())
                .Returns(new Vector3(100f, 200f, 300f));

            // Act
            var position = mock.Object.GetPlayerPosition();

            // Assert
            Assert.Equal(100f, position.X);
            Assert.Equal(200f, position.Y);
            Assert.Equal(300f, position.Z);
        }

        [Fact]
        public void CreatePed_ShouldReturnPedHandle()
        {
            // Arrange
            var mock = new Mock<IGameBridge>();
            mock.Setup(x => x.CreatePed(It.IsAny<string>(), It.IsAny<Vector3>()))
                .Returns(42);

            // Act
            var handle = mock.Object.CreatePed("model_name", new Vector3(0, 0, 0));

            // Assert
            Assert.Equal(42, handle);
        }

        [Fact]
        public void DeletePed_ShouldAcceptPedHandle()
        {
            // Arrange
            var mock = new Mock<IGameBridge>();
            mock.Setup(x => x.DeletePed(It.IsAny<int>()));

            // Act
            mock.Object.DeletePed(42);

            // Assert
            mock.Verify(x => x.DeletePed(42), Times.Once);
        }

        [Fact]
        public void IsPedAlive_ShouldReturnBool()
        {
            // Arrange
            var mock = new Mock<IGameBridge>();
            mock.Setup(x => x.IsPedAlive(It.IsAny<int>()))
                .Returns(true);

            // Act
            var isAlive = mock.Object.IsPedAlive(42);

            // Assert
            Assert.True(isAlive);
        }

        [Fact]
        public void SetPedRelationshipGroup_ShouldAcceptParameters()
        {
            // Arrange
            var mock = new Mock<IGameBridge>();

            // Act
            mock.Object.SetPedRelationshipGroup(42, "faction_group");

            // Assert
            mock.Verify(x => x.SetPedRelationshipGroup(42, "faction_group"), Times.Once);
        }

        [Fact]
        public void CreateBlip_ShouldReturnBlipHandle()
        {
            // Arrange
            var mock = new Mock<IGameBridge>();
            mock.Setup(x => x.CreateBlip(It.IsAny<Vector3>()))
                .Returns(123);

            // Act
            var blipHandle = mock.Object.CreateBlip(new Vector3(0, 0, 0));

            // Assert
            Assert.Equal(123, blipHandle);
        }

        [Fact]
        public void DeleteBlip_ShouldAcceptBlipHandle()
        {
            // Arrange
            var mock = new Mock<IGameBridge>();

            // Act
            mock.Object.DeleteBlip(123);

            // Assert
            mock.Verify(x => x.DeleteBlip(123), Times.Once);
        }

        [Fact]
        public void SetBlipColor_ShouldAcceptBlipHandleAndColor()
        {
            // Arrange
            var mock = new Mock<IGameBridge>();

            // Act
            mock.Object.SetBlipColor(123, BlipColor.Red);

            // Assert
            mock.Verify(x => x.SetBlipColor(123, BlipColor.Red), Times.Once);
        }

        [Fact]
        public void ShowNotification_ShouldAcceptMessage()
        {
            // Arrange
            var mock = new Mock<IGameBridge>();

            // Act
            mock.Object.ShowNotification("Test notification");

            // Assert
            mock.Verify(x => x.ShowNotification("Test notification"), Times.Once);
        }

        [Fact]
        public void GetGameTime_ShouldReturnInt()
        {
            // Arrange
            var mock = new Mock<IGameBridge>();
            mock.Setup(x => x.GetGameTime()).Returns(12345);

            // Act
            var time = mock.Object.GetGameTime();

            // Assert
            Assert.Equal(12345, time);
        }
    }
}
