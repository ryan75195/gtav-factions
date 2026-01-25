using FactionWars.Core.Interfaces;
using FactionWars.Core.Utils;
using Xunit;

namespace FactionWars.Tests.Unit.Core
{
    public class MockGameBridgeTests
    {
        [Fact]
        public void MockGameBridge_ImplementsIGameBridge()
        {
            // Arrange & Act
            var mockBridge = new MockGameBridge();

            // Assert
            Assert.IsAssignableFrom<IGameBridge>(mockBridge);
        }

        [Fact]
        public void GetPlayerPosition_ReturnsConfiguredPosition()
        {
            // Arrange
            var mockBridge = new MockGameBridge();
            var expectedPosition = new Vector3(100f, 200f, 300f);
            mockBridge.PlayerPosition = expectedPosition;

            // Act
            var position = mockBridge.GetPlayerPosition();

            // Assert
            Assert.Equal(expectedPosition, position);
        }

        [Fact]
        public void GetPlayerPosition_ReturnsZeroByDefault()
        {
            // Arrange
            var mockBridge = new MockGameBridge();

            // Act
            var position = mockBridge.GetPlayerPosition();

            // Assert
            Assert.Equal(Vector3.Zero, position);
        }

        [Fact]
        public void CreatePed_ReturnsIncrementingHandles()
        {
            // Arrange
            var mockBridge = new MockGameBridge();

            // Act
            var handle1 = mockBridge.CreatePed("model1", Vector3.Zero);
            var handle2 = mockBridge.CreatePed("model2", Vector3.Zero);

            // Assert
            Assert.Equal(1, handle1);
            Assert.Equal(2, handle2);
        }

        [Fact]
        public void CreatePed_StoresPedInfo()
        {
            // Arrange
            var mockBridge = new MockGameBridge();
            var position = new Vector3(100f, 200f, 0f);

            // Act
            var handle = mockBridge.CreatePed("test_model", position);

            // Assert
            Assert.True(mockBridge.PedExists(handle));
            Assert.True(mockBridge.IsPedAlive(handle));
        }

        [Fact]
        public void DeletePed_RemovesPed()
        {
            // Arrange
            var mockBridge = new MockGameBridge();
            var handle = mockBridge.CreatePed("test_model", Vector3.Zero);

            // Act
            mockBridge.DeletePed(handle);

            // Assert
            Assert.False(mockBridge.PedExists(handle));
            Assert.False(mockBridge.IsPedAlive(handle));
        }

        [Fact]
        public void IsPedAlive_ReturnsFalseForNonExistentPed()
        {
            // Arrange
            var mockBridge = new MockGameBridge();

            // Act
            var isAlive = mockBridge.IsPedAlive(999);

            // Assert
            Assert.False(isAlive);
        }

        [Fact]
        public void KillPed_MakesPedNotAlive()
        {
            // Arrange
            var mockBridge = new MockGameBridge();
            var handle = mockBridge.CreatePed("test_model", Vector3.Zero);

            // Act
            mockBridge.KillPed(handle);

            // Assert
            Assert.True(mockBridge.PedExists(handle));
            Assert.False(mockBridge.IsPedAlive(handle));
        }

        [Fact]
        public void SetPedRelationshipGroup_StoresGroupInfo()
        {
            // Arrange
            var mockBridge = new MockGameBridge();
            var handle = mockBridge.CreatePed("test_model", Vector3.Zero);

            // Act
            mockBridge.SetPedRelationshipGroup(handle, "faction_michael");

            // Assert
            Assert.Equal("faction_michael", mockBridge.GetPedRelationshipGroup(handle));
        }

        [Fact]
        public void CreateBlip_ReturnsIncrementingHandles()
        {
            // Arrange
            var mockBridge = new MockGameBridge();

            // Act
            var handle1 = mockBridge.CreateBlip(Vector3.Zero);
            var handle2 = mockBridge.CreateBlip(Vector3.Zero);

            // Assert
            Assert.Equal(1, handle1);
            Assert.Equal(2, handle2);
        }

        [Fact]
        public void DeleteBlip_RemovesBlip()
        {
            // Arrange
            var mockBridge = new MockGameBridge();
            var handle = mockBridge.CreateBlip(Vector3.Zero);

            // Act
            mockBridge.DeleteBlip(handle);

            // Assert
            Assert.False(mockBridge.BlipExists(handle));
        }

        [Fact]
        public void SetBlipColor_StoresColor()
        {
            // Arrange
            var mockBridge = new MockGameBridge();
            var handle = mockBridge.CreateBlip(Vector3.Zero);

            // Act
            mockBridge.SetBlipColor(handle, BlipColor.TrevorOrange);

            // Assert
            Assert.Equal(BlipColor.TrevorOrange, mockBridge.GetBlipColor(handle));
        }

        [Fact]
        public void ShowNotification_StoresNotifications()
        {
            // Arrange
            var mockBridge = new MockGameBridge();

            // Act
            mockBridge.ShowNotification("First message");
            mockBridge.ShowNotification("Second message");

            // Assert
            Assert.Equal(2, mockBridge.NotificationCount);
            Assert.Contains("First message", mockBridge.Notifications);
            Assert.Contains("Second message", mockBridge.Notifications);
        }

        [Fact]
        public void GetGameTime_ReturnsConfiguredTime()
        {
            // Arrange
            var mockBridge = new MockGameBridge();
            mockBridge.GameTime = 12345;

            // Act
            var time = mockBridge.GetGameTime();

            // Assert
            Assert.Equal(12345, time);
        }

        [Fact]
        public void AdvanceGameTime_IncrementsTime()
        {
            // Arrange
            var mockBridge = new MockGameBridge();
            mockBridge.GameTime = 1000;

            // Act
            mockBridge.AdvanceGameTime(500);

            // Assert
            Assert.Equal(1500, mockBridge.GetGameTime());
        }

        [Fact]
        public void Reset_ClearsAllState()
        {
            // Arrange
            var mockBridge = new MockGameBridge();
            mockBridge.CreatePed("model", Vector3.Zero);
            mockBridge.CreateBlip(Vector3.Zero);
            mockBridge.ShowNotification("test");
            mockBridge.GameTime = 5000;

            // Act
            mockBridge.Reset();

            // Assert
            Assert.Equal(0, mockBridge.GetGameTime());
            Assert.Equal(0, mockBridge.NotificationCount);
            Assert.False(mockBridge.PedExists(1));
            Assert.False(mockBridge.BlipExists(1));
        }

        [Fact]
        public void TaskPedWanderInAreaSprinting_DoesNotThrow()
        {
            // Arrange
            var mockBridge = new MockGameBridge();
            var handle = mockBridge.CreatePed("test_model", Vector3.Zero);
            var center = new Vector3(100f, 100f, 0f);

            // Act
            var exception = Record.Exception(() => mockBridge.TaskPedWanderInAreaSprinting(handle, center, 150f));

            // Assert
            Assert.Null(exception);
        }

        [Fact]
        public void TaskPedWanderInArea_DoesNotThrow()
        {
            // Arrange
            var mockBridge = new MockGameBridge();
            var handle = mockBridge.CreatePed("test_model", Vector3.Zero);
            var center = new Vector3(100f, 100f, 0f);

            // Act
            var exception = Record.Exception(() => mockBridge.TaskPedWanderInArea(handle, center, 150f));

            // Assert
            Assert.Null(exception);
        }

        [Fact]
        public void SetPedAsFriendly_SetsFriendlyDefendersGroup()
        {
            // Arrange
            var mockBridge = new MockGameBridge();
            var handle = mockBridge.CreatePed("test_model", Vector3.Zero);

            // Act
            mockBridge.SetPedAsFriendly(handle);

            // Assert - Should be in FRIENDLY_DEFENDERS group, not PLAYER or DEFENDER_ENEMIES
            var group = mockBridge.GetPedRelationshipGroup(handle);
            Assert.Equal("FRIENDLY_DEFENDERS", group);
            Assert.NotEqual("PLAYER", group);
            Assert.NotEqual("DEFENDER_ENEMIES", group);
        }

        [Fact]
        public void SetPedAsHostileWanderer_SetsDefenderEnemiesGroup()
        {
            // Arrange
            var mockBridge = new MockGameBridge();
            var handle = mockBridge.CreatePed("test_model", Vector3.Zero);

            // Act
            mockBridge.SetPedAsHostileWanderer(handle);

            // Assert - Should be in DEFENDER_ENEMIES group (hostile to player)
            var group = mockBridge.GetPedRelationshipGroup(handle);
            Assert.Equal("DEFENDER_ENEMIES", group);
        }

        [Fact]
        public void TaskPedWanderInArea_TracksWanderState()
        {
            // Arrange
            var mockBridge = new MockGameBridge();
            var handle = mockBridge.CreatePed("test_model", Vector3.Zero);
            var center = new Vector3(100f, 200f, 0f);
            var radius = 150f;

            // Act
            mockBridge.TaskPedWanderInArea(handle, center, radius);

            // Assert
            Assert.True(mockBridge.IsPedWandering(handle));
            Assert.False(mockBridge.IsPedWanderingSprinting(handle));
            Assert.Equal(center, mockBridge.GetPedWanderCenter(handle));
            Assert.Equal(radius, mockBridge.GetPedWanderRadius(handle));
        }

        [Fact]
        public void TaskPedWanderInAreaSprinting_TracksSprintingState()
        {
            // Arrange
            var mockBridge = new MockGameBridge();
            var handle = mockBridge.CreatePed("test_model", Vector3.Zero);
            var center = new Vector3(100f, 200f, 0f);
            var radius = 150f;

            // Act
            mockBridge.TaskPedWanderInAreaSprinting(handle, center, radius);

            // Assert
            Assert.True(mockBridge.IsPedWandering(handle));
            Assert.True(mockBridge.IsPedWanderingSprinting(handle));
            Assert.Equal(center, mockBridge.GetPedWanderCenter(handle));
            Assert.Equal(radius, mockBridge.GetPedWanderRadius(handle));
        }

        [Fact]
        public void Reset_ClearsWanderingPeds()
        {
            // Arrange
            var mockBridge = new MockGameBridge();
            var handle = mockBridge.CreatePed("test_model", Vector3.Zero);
            mockBridge.TaskPedWanderInArea(handle, new Vector3(100f, 100f, 0f), 50f);

            // Act
            mockBridge.Reset();

            // Assert
            Assert.False(mockBridge.IsPedWandering(handle));
        }

        [Fact]
        public void IsPlayerFreeAiming_DefaultsFalse()
        {
            var bridge = new MockGameBridge();
            Assert.False(bridge.IsPlayerFreeAiming());
        }

        [Fact]
        public void SetPlayerFreeAiming_ChangesState()
        {
            var bridge = new MockGameBridge();
            bridge.SetPlayerFreeAiming(true);
            Assert.True(bridge.IsPlayerFreeAiming());
        }

        [Fact]
        public void GetEntityPlayerIsAimingAt_DefaultsToZero()
        {
            var bridge = new MockGameBridge();
            Assert.Equal(0, bridge.GetEntityPlayerIsAimingAt());
        }

        [Fact]
        public void SetEntityPlayerIsAimingAt_SetsTarget()
        {
            var bridge = new MockGameBridge();
            bridge.SetEntityPlayerIsAimingAt(123);
            Assert.Equal(123, bridge.GetEntityPlayerIsAimingAt());
        }

        [Fact]
        public void DisplayHelpText_StoresLastHelpText()
        {
            var bridge = new MockGameBridge();
            bridge.DisplayHelpText("Press ~INPUT_CONTEXT~ to interact");
            Assert.Equal("Press ~INPUT_CONTEXT~ to interact", bridge.LastHelpText);
        }
    }
}
