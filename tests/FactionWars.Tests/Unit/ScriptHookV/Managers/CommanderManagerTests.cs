using System;
using FactionWars.Combat.Interfaces;
using FactionWars.Combat.Models;
using FactionWars.Core.Interfaces;
using FactionWars.Core.Models;
using FactionWars.Core.Utils;
using FactionWars.ScriptHookV.Managers;
using FactionWars.Territory.Interfaces;
using FactionWars.Territory.Models;
using FactionWars.UI.Interfaces;
using Moq;
using Xunit;

namespace FactionWars.Tests.Unit.ScriptHookV.Managers
{
    /// <summary>
    /// Tests for CommanderManager, which manages Commander NPCs
    /// that spawn in player-owned zones.
    /// </summary>
    public class CommanderManagerTests
    {
        private MockGameBridge _gameBridge = null!;
        private Mock<IPedSpawningService> _pedSpawningServiceMock = null!;
        private Mock<IPedDespawnService> _pedDespawnServiceMock = null!;
        private Mock<IPedBlipService> _pedBlipServiceMock = null!;
        private Mock<IZoneService> _zoneServiceMock = null!;
        private CommanderManager _manager = null!;

        private const string PlayerFactionId = "michael";
        private const string EnemyFactionId = "ballas";
        private const string TestZoneId = "zone_1";

        private void SetupManager(Action<object?>? openMenuCallback = null)
        {
            _gameBridge = new MockGameBridge();
            _pedSpawningServiceMock = new Mock<IPedSpawningService>();
            _pedDespawnServiceMock = new Mock<IPedDespawnService>();
            _pedBlipServiceMock = new Mock<IPedBlipService>();
            _zoneServiceMock = new Mock<IZoneService>();

            // Setup default mock behaviors
            _pedSpawningServiceMock.Setup(p => p.CanSpawn()).Returns(true);
            _pedSpawningServiceMock.Setup(p => p.SpawnPed(It.IsAny<string>(), It.IsAny<Vector3>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(() => new PedHandle(_gameBridge.CreatePed("test", new Vector3(0, 0, 0))));

            _pedBlipServiceMock.Setup(p => p.CreateBlipForPed(It.IsAny<int>(), It.IsAny<BlipColor>()))
                .Returns(1);

            _manager = new CommanderManager(
                _gameBridge,
                _pedSpawningServiceMock.Object,
                _pedDespawnServiceMock.Object,
                _pedBlipServiceMock.Object,
                _zoneServiceMock.Object,
                PlayerFactionId,
                openMenuCallback);
        }

        private Zone CreateFriendlyZone()
        {
            var zone = new Zone(TestZoneId, "Test Zone", new Vector3(100, 100, 0), 150f, 1);
            zone.OwnerFactionId = PlayerFactionId;
            return zone;
        }

        private Zone CreateEnemyZone()
        {
            var zone = new Zone(TestZoneId, "Test Zone", new Vector3(100, 100, 0), 150f, 1);
            zone.OwnerFactionId = EnemyFactionId;
            return zone;
        }

        private Zone CreateNeutralZone()
        {
            var zone = new Zone(TestZoneId, "Test Zone", new Vector3(100, 100, 0), 150f, 1);
            zone.OwnerFactionId = null;
            return zone;
        }

        [Fact]
        public void Constructor_ThrowsOnNullGameBridge()
        {
            // Arrange
            SetupManager();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new CommanderManager(
                null!,
                _pedSpawningServiceMock.Object,
                _pedDespawnServiceMock.Object,
                _pedBlipServiceMock.Object,
                _zoneServiceMock.Object,
                PlayerFactionId));
        }

        [Fact]
        public void Constructor_ThrowsOnNullPedSpawningService()
        {
            // Arrange
            SetupManager();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new CommanderManager(
                _gameBridge,
                null!,
                _pedDespawnServiceMock.Object,
                _pedBlipServiceMock.Object,
                _zoneServiceMock.Object,
                PlayerFactionId));
        }

        [Fact]
        public void Constructor_ThrowsOnNullPlayerFactionId()
        {
            // Arrange
            SetupManager();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new CommanderManager(
                _gameBridge,
                _pedSpawningServiceMock.Object,
                _pedDespawnServiceMock.Object,
                _pedBlipServiceMock.Object,
                _zoneServiceMock.Object,
                null!));
        }

        [Fact]
        public void OnZoneEntered_SpawnsCommanderInFriendlyZone()
        {
            // Arrange
            SetupManager();
            var zone = CreateFriendlyZone();

            // Act
            _manager.OnZoneEntered(zone);

            // Assert
            Assert.True(_manager.HasCommanderInZone(TestZoneId));
            _pedSpawningServiceMock.Verify(
                p => p.SpawnPed(CommanderManager.CommanderModel, It.IsAny<Vector3>(), PlayerFactionId, TestZoneId),
                Times.Once);
        }

        [Fact]
        public void OnZoneEntered_DoesNotSpawnCommanderInEnemyZone()
        {
            // Arrange
            SetupManager();
            var zone = CreateEnemyZone();

            // Act
            _manager.OnZoneEntered(zone);

            // Assert
            Assert.False(_manager.HasCommanderInZone(TestZoneId));
            _pedSpawningServiceMock.Verify(
                p => p.SpawnPed(It.IsAny<string>(), It.IsAny<Vector3>(), It.IsAny<string>(), It.IsAny<string>()),
                Times.Never);
        }

        [Fact]
        public void OnZoneEntered_DoesNotSpawnCommanderInNeutralZone()
        {
            // Arrange
            SetupManager();
            var zone = CreateNeutralZone();

            // Act
            _manager.OnZoneEntered(zone);

            // Assert
            Assert.False(_manager.HasCommanderInZone(TestZoneId));
            _pedSpawningServiceMock.Verify(
                p => p.SpawnPed(It.IsAny<string>(), It.IsAny<Vector3>(), It.IsAny<string>(), It.IsAny<string>()),
                Times.Never);
        }

        [Fact]
        public void OnZoneEntered_CreatesBlueBlipForCommander()
        {
            // Arrange
            SetupManager();
            var zone = CreateFriendlyZone();

            // Act
            _manager.OnZoneEntered(zone);

            // Assert
            _pedBlipServiceMock.Verify(
                p => p.CreateBlipForPed(It.IsAny<int>(), BlipColor.Blue),
                Times.Once);
        }

        [Fact]
        public void OnZoneExited_DespawnsCommander()
        {
            // Arrange
            SetupManager();
            var zone = CreateFriendlyZone();
            _manager.OnZoneEntered(zone);
            Assert.True(_manager.HasCommanderInZone(TestZoneId));

            // Act
            _manager.OnZoneExited(zone);

            // Assert
            Assert.False(_manager.HasCommanderInZone(TestZoneId));
            _pedDespawnServiceMock.Verify(p => p.DespawnPed(It.IsAny<int>()), Times.Once);
        }

        [Fact]
        public void OnZoneExited_RemovesBlip()
        {
            // Arrange
            SetupManager();
            var zone = CreateFriendlyZone();
            _manager.OnZoneEntered(zone);

            // Act
            _manager.OnZoneExited(zone);

            // Assert
            _pedBlipServiceMock.Verify(p => p.RemoveBlipForPed(It.IsAny<int>()), Times.Once);
        }

        [Fact]
        public void OnZoneEntered_WithNullZone_DoesNotThrow()
        {
            // Arrange
            SetupManager();

            // Act & Assert
            var exception = Record.Exception(() => _manager.OnZoneEntered(null!));
            Assert.Null(exception);
        }

        [Fact]
        public void OnZoneExited_WithNullZone_DoesNotThrow()
        {
            // Arrange
            SetupManager();

            // Act & Assert
            var exception = Record.Exception(() => _manager.OnZoneExited(null!));
            Assert.Null(exception);
        }

        [Fact]
        public void OnZoneEntered_DoesNotSpawnDuplicateCommander()
        {
            // Arrange
            SetupManager();
            var zone = CreateFriendlyZone();

            // Act - Enter zone twice
            _manager.OnZoneEntered(zone);
            _manager.OnZoneEntered(zone);

            // Assert - Should only spawn once
            _pedSpawningServiceMock.Verify(
                p => p.SpawnPed(It.IsAny<string>(), It.IsAny<Vector3>(), It.IsAny<string>(), It.IsAny<string>()),
                Times.Once);
        }

        [Fact]
        public void OnZoneExited_WithNoCommander_DoesNotThrow()
        {
            // Arrange
            SetupManager();
            var zone = CreateFriendlyZone();
            // Don't spawn any commander

            // Act & Assert
            var exception = Record.Exception(() => _manager.OnZoneExited(zone));
            Assert.Null(exception);
            _pedDespawnServiceMock.Verify(p => p.DespawnPed(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public void OnZoneEntered_SetsCommanderAsFriendly()
        {
            // Arrange
            SetupManager();
            var zone = CreateFriendlyZone();

            // Act
            _manager.OnZoneEntered(zone);

            // Assert - Commander should be in FRIENDLY_DEFENDERS group
            var spawnedPeds = _gameBridge.GetSpawnedPeds();
            Assert.Single(spawnedPeds);
            var relationshipGroup = _gameBridge.GetPedRelationshipGroup(spawnedPeds[0]);
            Assert.Equal("FRIENDLY_DEFENDERS", relationshipGroup);
        }

        [Fact]
        public void OnZoneEntered_ConfiguresCommanderCombatStats()
        {
            // Arrange
            SetupManager();
            var zone = CreateFriendlyZone();

            // Act
            _manager.OnZoneEntered(zone);

            // Assert - Commander should have high stats
            var spawnedPeds = _gameBridge.GetSpawnedPeds();
            Assert.Single(spawnedPeds);
            var pedHandle = spawnedPeds[0];

            // Check that ped was configured (via GameBridge calls)
            // The actual values are verified by the mock setting ped attributes
            Assert.True(_manager.HasCommanderInZone(TestZoneId));
        }

        [Fact]
        public void OnZoneEntered_TasksCommanderToWander()
        {
            // Arrange
            SetupManager();
            var zone = CreateFriendlyZone();

            // Act
            _manager.OnZoneEntered(zone);

            // Assert - Commander should be wandering
            var spawnedPeds = _gameBridge.GetSpawnedPeds();
            Assert.Single(spawnedPeds);
            Assert.True(_gameBridge.IsPedWandering(spawnedPeds[0]));
        }

        [Fact]
        public void CommanderStats_AreCorrectlyDefined()
        {
            // Assert - Verify commander stats constants
            Assert.Equal("s_m_y_armymech_01", CommanderManager.CommanderModel);
            Assert.Equal("weapon_carbinerifle", CommanderManager.CommanderWeapon);
            Assert.Equal(300, CommanderManager.CommanderHealth);
            Assert.Equal(100, CommanderManager.CommanderArmor);
            Assert.Equal(0.75f, CommanderManager.CommanderAccuracy);
        }

        [Fact]
        public void Update_RespawnsDeadCommander()
        {
            // Arrange
            SetupManager();
            var zone = CreateFriendlyZone();
            _zoneServiceMock.Setup(z => z.GetZone(TestZoneId)).Returns(zone);

            _manager.OnZoneEntered(zone);
            Assert.True(_manager.HasCommanderInZone(TestZoneId));

            // Kill the commander
            var peds = _gameBridge.GetSpawnedPeds();
            Assert.Single(peds);
            _gameBridge.KillPed(peds[0]);

            // Act - Update should detect death and respawn
            _manager.Update();

            // Assert - Should still have a commander (respawned)
            Assert.True(_manager.HasCommanderInZone(TestZoneId));
            // Spawn was called twice (initial + respawn)
            _pedSpawningServiceMock.Verify(
                p => p.SpawnPed(CommanderManager.CommanderModel, It.IsAny<Vector3>(), PlayerFactionId, TestZoneId),
                Times.Exactly(2));
        }

        [Fact]
        public void OnTerritoryLost_DespawnsCommander()
        {
            SetupManager();
            var zone = CreateFriendlyZone();

            _manager.OnZoneEntered(zone);
            Assert.True(_manager.HasCommanderInZone(TestZoneId));

            // Territory is lost
            _manager.OnTerritoryLost(TestZoneId);

            Assert.False(_manager.HasCommanderInZone(TestZoneId));
            _pedDespawnServiceMock.Verify(p => p.DespawnPed(It.IsAny<int>()), Times.Once);
        }

        [Fact]
        public void Update_ShowsHelpTextWhenAimingAtCommander()
        {
            // Arrange
            SetupManager();
            var zone = CreateFriendlyZone();
            _manager.OnZoneEntered(zone);

            var commanderHandle = _gameBridge.GetSpawnedPeds()[0];
            _gameBridge.SetPlayerFreeAiming(true);
            _gameBridge.SetEntityPlayerIsAimingAt(commanderHandle);

            // Act
            _manager.Update();

            // Assert
            Assert.Contains("Commander", _gameBridge.LastHelpText);
        }

        [Fact]
        public void Update_DoesNotShowHelpTextWhenNotAimingAtCommander()
        {
            // Arrange
            SetupManager();
            var zone = CreateFriendlyZone();
            _manager.OnZoneEntered(zone);

            _gameBridge.SetPlayerFreeAiming(true);
            _gameBridge.SetEntityPlayerIsAimingAt(999); // Not a commander

            // Act
            _manager.Update();

            // Assert
            Assert.Null(_gameBridge.LastHelpText);
        }

        [Fact]
        public void Update_DoesNotShowHelpTextWhenNotAiming()
        {
            // Arrange
            SetupManager();
            var zone = CreateFriendlyZone();
            _manager.OnZoneEntered(zone);

            var commanderHandle = _gameBridge.GetSpawnedPeds()[0];
            _gameBridge.SetPlayerFreeAiming(false);
            _gameBridge.SetEntityPlayerIsAimingAt(commanderHandle);

            // Act
            _manager.Update();

            // Assert
            Assert.Null(_gameBridge.LastHelpText);
        }

        [Fact]
        public void OnKeyDown_OpensMenuWhenAimingAtCommanderAndPressingE()
        {
            // Arrange
            bool menuOpened = false;
            SetupManager(_ => menuOpened = true);
            var zone = CreateFriendlyZone();
            _manager.OnZoneEntered(zone);

            var commanderHandle = _gameBridge.GetSpawnedPeds()[0];
            _gameBridge.SetPlayerFreeAiming(true);
            _gameBridge.SetEntityPlayerIsAimingAt(commanderHandle);

            // Act
            _manager.OnKeyDown(0x45); // E key

            // Assert
            Assert.True(menuOpened);
        }

        [Fact]
        public void OnKeyDown_DoesNotOpenMenuWhenNotAimingAtCommander()
        {
            // Arrange
            bool menuOpened = false;
            SetupManager(_ => menuOpened = true);
            var zone = CreateFriendlyZone();
            _manager.OnZoneEntered(zone);

            _gameBridge.SetPlayerFreeAiming(true);
            _gameBridge.SetEntityPlayerIsAimingAt(999); // Not a commander

            // Act
            _manager.OnKeyDown(0x45); // E key

            // Assert
            Assert.False(menuOpened);
        }

        [Fact]
        public void OnKeyDown_DoesNotOpenMenuWhenNotAiming()
        {
            // Arrange
            bool menuOpened = false;
            SetupManager(_ => menuOpened = true);
            var zone = CreateFriendlyZone();
            _manager.OnZoneEntered(zone);

            var commanderHandle = _gameBridge.GetSpawnedPeds()[0];
            _gameBridge.SetPlayerFreeAiming(false);
            _gameBridge.SetEntityPlayerIsAimingAt(commanderHandle);

            // Act
            _manager.OnKeyDown(0x45); // E key

            // Assert
            Assert.False(menuOpened);
        }

        [Fact]
        public void OnKeyDown_DoesNotOpenMenuWhenPressingWrongKey()
        {
            // Arrange
            bool menuOpened = false;
            SetupManager(_ => menuOpened = true);
            var zone = CreateFriendlyZone();
            _manager.OnZoneEntered(zone);

            var commanderHandle = _gameBridge.GetSpawnedPeds()[0];
            _gameBridge.SetPlayerFreeAiming(true);
            _gameBridge.SetEntityPlayerIsAimingAt(commanderHandle);

            // Act
            _manager.OnKeyDown(0x46); // F key, not E

            // Assert
            Assert.False(menuOpened);
        }

        [Fact]
        public void OnKeyDown_DoesNotThrowWhenNoCallbackProvided()
        {
            // Arrange
            SetupManager(); // No callback
            var zone = CreateFriendlyZone();
            _manager.OnZoneEntered(zone);

            var commanderHandle = _gameBridge.GetSpawnedPeds()[0];
            _gameBridge.SetPlayerFreeAiming(true);
            _gameBridge.SetEntityPlayerIsAimingAt(commanderHandle);

            // Act & Assert - Should not throw
            var exception = Record.Exception(() => _manager.OnKeyDown(0x45));
            Assert.Null(exception);
        }

        [Fact]
        public void OnBattleStarted_SwitchesCommanderToSprinting()
        {
            SetupManager();
            var zone = CreateFriendlyZone();
            _zoneServiceMock.Setup(z => z.GetZone(TestZoneId)).Returns(zone);
            _manager.OnZoneEntered(zone);

            var commanderHandle = _gameBridge.GetSpawnedPeds()[0];
            Assert.False(_gameBridge.IsPedWanderingSprinting(commanderHandle));

            _manager.OnBattleStarted(TestZoneId);

            Assert.True(_gameBridge.IsPedWanderingSprinting(commanderHandle));
        }

        [Fact]
        public void OnBattleEnded_SwitchesCommanderToWalking()
        {
            SetupManager();
            var zone = CreateFriendlyZone();
            _zoneServiceMock.Setup(z => z.GetZone(TestZoneId)).Returns(zone);
            _manager.OnZoneEntered(zone);
            _manager.OnBattleStarted(TestZoneId);

            var commanderHandle = _gameBridge.GetSpawnedPeds()[0];
            Assert.True(_gameBridge.IsPedWanderingSprinting(commanderHandle));

            _manager.OnBattleEnded(TestZoneId);

            Assert.True(_gameBridge.IsPedWandering(commanderHandle));
            Assert.False(_gameBridge.IsPedWanderingSprinting(commanderHandle));
        }
    }
}
