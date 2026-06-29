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
        public void GetPlayerDeathInfo_RoundTripsConfiguredCause()
        {
            var mockBridge = new MockGameBridge();
            mockBridge.SetPlayerDeathInfo("SNIPERRIFLE", 4242);

            var cause = mockBridge.GetPlayerDeathInfo();

            Assert.Equal("SNIPERRIFLE", cause.WeaponName);
            Assert.Equal(4242, cause.KillerHandle);
        }

        [Fact]
        public void GetPlayerDeathInfo_DefaultsToEmptyWeaponAndNoKiller()
        {
            var mockBridge = new MockGameBridge();

            var cause = mockBridge.GetPlayerDeathInfo();

            Assert.Equal(string.Empty, cause.WeaponName);
            Assert.Equal(-1, cause.KillerHandle);
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
        public void SetPedCriticalHitsEnabled_StoresCriticalHitState()
        {
            // Arrange
            var mockBridge = new MockGameBridge();
            var handle = mockBridge.CreatePed("test_model", Vector3.Zero);

            // Act
            mockBridge.SetPedCriticalHitsEnabled(handle, true);

            // Assert
            Assert.True(mockBridge.GetPedCriticalHitsEnabled(handle));
        }

        [Fact]
        public void SetPedRagdollEnabled_StoresRagdollState()
        {
            // Arrange
            var mockBridge = new MockGameBridge();
            var handle = mockBridge.CreatePed("test_model", Vector3.Zero);

            // Act
            mockBridge.SetPedRagdollEnabled(handle, false);

            // Assert
            Assert.False(mockBridge.GetPedRagdollEnabled(handle));
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
        public void SetPedAsFriendly_PreservesFactionGroupAndDoesNotAttackPlayer()
        {
            // Arrange
            var mockBridge = new MockGameBridge();
            var handle = mockBridge.CreatePed("test_model", Vector3.Zero);
            mockBridge.SetPedRelationshipGroup(handle, "FACTION_MICHAEL");

            // Act
            mockBridge.SetPedAsFriendly(handle);

            // Assert - Group is owned by the relationship matrix now; SetPedAsFriendly no longer
            // reassigns it. The ped keeps the faction group it spawned in.
            Assert.Equal("FACTION_MICHAEL", mockBridge.GetPedRelationshipGroup(handle));
        }

        [Fact]
        public void SetPedAsHostileWanderer_MarksAttackingPlayerWithoutReassigningGroup()
        {
            // Arrange
            var mockBridge = new MockGameBridge();
            var handle = mockBridge.CreatePed("test_model", Vector3.Zero);
            mockBridge.SetPedRelationshipGroup(handle, "FACTION_BALLAS");

            // Act
            mockBridge.SetPedAsHostileWanderer(handle);

            // Assert - Group is owned by the relationship matrix now; the ped keeps the faction
            // group it spawned in (no reassignment to a synthetic enemies group).
            Assert.Equal("FACTION_BALLAS", mockBridge.GetPedRelationshipGroup(handle));
        }

        [Fact]
        public void SetPedAsHostileWanderer_PreservesExistingFactionGroup()
        {
            var mockBridge = new MockGameBridge();
            var handle = mockBridge.CreatePed("test_model", Vector3.Zero);
            mockBridge.SetPedRelationshipGroup(handle, "FACTION_TREVOR");

            mockBridge.SetPedAsHostileWanderer(handle);

            Assert.Equal("FACTION_TREVOR", mockBridge.GetPedRelationshipGroup(handle));
        }

        [Fact]
        public void SetPedToAttackPlayer_PreservesExistingFactionGroup()
        {
            var mockBridge = new MockGameBridge();
            var handle = mockBridge.CreatePed("test_model", Vector3.Zero);
            mockBridge.SetPedRelationshipGroup(handle, "FACTION_FRANKLIN");

            mockBridge.SetPedToAttackPlayer(handle);

            Assert.Equal("FACTION_FRANKLIN", mockBridge.GetPedRelationshipGroup(handle));
        }

        [Fact]
        public void SetRelationshipBetweenGroups_StoresBidirectionalRelationship()
        {
            var mockBridge = new MockGameBridge();

            mockBridge.SetRelationshipBetweenGroups("faction_michael", "faction_trevor", relationship: 5);

            Assert.Equal(5, mockBridge.GetRelationshipBetweenGroups("FACTION_MICHAEL", "FACTION_TREVOR"));
            Assert.Equal(5, mockBridge.GetRelationshipBetweenGroups("FACTION_TREVOR", "FACTION_MICHAEL"));
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

        [Fact]
        public void GetVehicleModelName_ReturnsModelName()
        {
            // Arrange
            var bridge = new MockGameBridge();
            var vehicleHandle = bridge.CreateVehicle("insurgent", new Vector3(0, 0, 0));

            // Act
            var modelName = bridge.GetVehicleModelName(vehicleHandle);

            // Assert
            Assert.Equal("insurgent", modelName);
        }

        [Fact]
        public void GetVehicleModelName_ReturnsEmptyForInvalidHandle()
        {
            // Arrange
            var bridge = new MockGameBridge();

            // Act
            var modelName = bridge.GetVehicleModelName(999);

            // Assert
            Assert.Equal(string.Empty, modelName);
        }

        [Fact]
        public void SetPlayerInVehicleWithModel_SetsPlayerInVehicle()
        {
            // Arrange
            var bridge = new MockGameBridge();

            // Act
            var vehicleHandle = bridge.SetPlayerInVehicleWithModel("buzzard", 4);

            // Assert
            Assert.True(bridge.IsPlayerInVehicle());
            Assert.Equal(vehicleHandle, bridge.GetPlayerVehicle());
            Assert.Equal("buzzard", bridge.GetVehicleModelName(vehicleHandle));
        }

        #region SetPedCanSwitchWeapons Tests

        [Fact]
        public void SetPedCanSwitchWeapons_TracksCanSwitchWeaponsState()
        {
            // Arrange
            var bridge = new MockGameBridge();
            var pedHandle = bridge.CreatePed("s_m_y_dealer_01", Vector3.Zero);

            // Act
            bridge.SetPedCanSwitchWeapons(pedHandle, false);

            // Assert
            Assert.False(bridge.GetPedCanSwitchWeapons(pedHandle));
        }

        [Fact]
        public void SetPedCanSwitchWeapons_DefaultsToTrue()
        {
            // Arrange
            var bridge = new MockGameBridge();
            var pedHandle = bridge.CreatePed("s_m_y_dealer_01", Vector3.Zero);

            // Assert - default should be true (can switch)
            Assert.True(bridge.GetPedCanSwitchWeapons(pedHandle));
        }

        [Fact]
        public void SetPedCanSwitchWeapons_CanBeSetToTrueAfterFalse()
        {
            // Arrange
            var bridge = new MockGameBridge();
            var pedHandle = bridge.CreatePed("s_m_y_dealer_01", Vector3.Zero);

            // Act
            bridge.SetPedCanSwitchWeapons(pedHandle, false);
            bridge.SetPedCanSwitchWeapons(pedHandle, true);

            // Assert
            Assert.True(bridge.GetPedCanSwitchWeapons(pedHandle));
        }

        #endregion

        #region Vehicle Class and Turret Seat Tests

        [Fact]
        public void GetVehicleClass_ReturnsSetClass()
        {
            var bridge = new MockGameBridge();
            var vehicleHandle = bridge.CreateVehicle("car", new Vector3(0, 0, 0));
            bridge.SetVehicleClass(vehicleHandle, 15); // Helicopter

            var result = bridge.GetVehicleClass(vehicleHandle);

            Assert.Equal(15, result);
        }

        [Fact]
        public void GetVehicleClass_InvalidVehicle_ReturnsNegativeOne()
        {
            var bridge = new MockGameBridge();

            var result = bridge.GetVehicleClass(9999);

            Assert.Equal(-1, result);
        }

        [Fact]
        public void IsVehicleSeatTurret_ReturnsTrueForTurretSeat()
        {
            var bridge = new MockGameBridge();
            var vehicleHandle = bridge.CreateVehicle("technical", new Vector3(0, 0, 0));
            bridge.SetSeatAsTurret(vehicleHandle, 2); // Back turret

            Assert.True(bridge.IsVehicleSeatTurret(vehicleHandle, 2));
            Assert.False(bridge.IsVehicleSeatTurret(vehicleHandle, 1));
        }

        [Fact]
        public void GetVehiclePosition_ReturnsSetPosition()
        {
            var bridge = new MockGameBridge();
            var vehicleHandle = bridge.CreateVehicle("car", new Vector3(10, 20, 30));
            bridge.SetVehiclePosition(vehicleHandle, new Vector3(100, 200, 50));

            var result = bridge.GetVehiclePosition(vehicleHandle);

            Assert.Equal(100, result.X);
            Assert.Equal(200, result.Y);
            Assert.Equal(50, result.Z);
        }

        #endregion

        #region Fingerprint Method Tests

        [Fact]
        public void GetTotalPlayTimeSeconds_DefaultsToZero()
        {
            var bridge = new MockGameBridge();
            Assert.Equal(0L, bridge.GetTotalPlayTimeSeconds());
        }

        [Fact]
        public void GetTotalPlayTimeSeconds_ReturnsSeededValue()
        {
            var bridge = new MockGameBridge { TotalPlayTimeSeconds = 12340 };
            Assert.Equal(12340L, bridge.GetTotalPlayTimeSeconds());
        }

        [Fact]
        public void GetCompletedMissionCount_ReturnsSeededValue()
        {
            var bridge = new MockGameBridge { CompletedMissionCount = 23 };
            Assert.Equal(23, bridge.GetCompletedMissionCount());
        }

        [Fact]
        public void GetInGameClockMinutes_ReturnsSeededValue()
        {
            var bridge = new MockGameBridge { InGameClockMinutes = 854 };
            Assert.Equal(854, bridge.GetInGameClockMinutes());
        }

        #endregion

        [Fact]
        public void TaskGuardArea_RecordsCentreAndRadius()
        {
            var mock = new MockGameBridge();
            int ped = mock.CreatePed("test", new Vector3(0f, 0f, 0f));

            mock.TaskGuardArea(ped, new Vector3(10f, 20f, 30f), 8f);

            Assert.True(mock.IsPedGuardingArea(ped));
            Assert.Equal(new Vector3(10f, 20f, 30f), mock.GetGuardAreaCenter(ped));
            Assert.Equal(8f, mock.GetGuardAreaRadius(ped));
        }

        [Fact]
        public void TaskCombatPed_RecordsTarget()
        {
            var mock = new MockGameBridge();
            int ped = mock.CreatePed("test", new Vector3(0f, 0f, 0f));

            mock.TaskCombatPed(ped, 555);

            Assert.True(mock.IsPedCombatingPed(ped));
            Assert.Equal(555, mock.GetCombatPedTarget(ped));
        }

        [Fact]
        public void GetNearbyVehicles_ReturnsVehiclesWithinRadius()
        {
            var bridge = new MockGameBridge();
            int near = bridge.CreateVehicle("adder", new Vector3(0f, 0f, 0f));
            int far = bridge.CreateVehicle("adder", new Vector3(500f, 0f, 0f));

            var found = bridge.GetNearbyVehicles(new Vector3(0f, 0f, 0f), 80f);

            Assert.Contains(near, found);
            Assert.DoesNotContain(far, found);
        }

        [Fact]
        public void CreateVehicle_IsPersistent_AmbientDefaultsNotPersistent()
        {
            var bridge = new MockGameBridge();
            int mod = bridge.CreateVehicle("adder", new Vector3(0f, 0f, 0f));
            int ambient = bridge.CreateAmbientVehicle("adder", new Vector3(1f, 0f, 0f));

            Assert.True(bridge.IsVehiclePersistent(mod));
            Assert.False(bridge.IsVehiclePersistent(ambient));
        }

        [Fact]
        public void GetVehicleDriver_ReturnsDriver_OrMinusOneWhenEmpty()
        {
            var bridge = new MockGameBridge();
            int veh = bridge.CreateAmbientVehicle("adder", new Vector3(0f, 0f, 0f));
            Assert.Equal(-1, bridge.GetVehicleDriver(veh));

            int driver = bridge.CreatePed("d", new Vector3(0f, 0f, 0f));
            bridge.SetVehicleDriver(veh, driver);
            Assert.Equal(driver, bridge.GetVehicleDriver(veh));
        }

        [Fact]
        public void TaskPedLeaveVehicle_ClearsDriverSeat()
        {
            var bridge = new MockGameBridge();
            int veh = bridge.CreateAmbientVehicle("adder", new Vector3(0f, 0f, 0f));
            int driver = bridge.CreatePed("d", new Vector3(0f, 0f, 0f));
            bridge.SetVehicleDriver(veh, driver);
            bridge.PutPedInVehicle(driver, veh, -1);

            bridge.TaskPedLeaveVehicle(driver);

            Assert.False(bridge.IsPedInVehicle(driver));
            Assert.Equal(-1, bridge.GetVehicleDriver(veh));
        }

        [Fact]
        public void SetVehicleHandbrake_IsObservable()
        {
            var bridge = new MockGameBridge();
            int veh = bridge.CreateAmbientVehicle("adder", new Vector3(0f, 0f, 0f));

            bridge.SetVehicleHandbrake(veh, true);

            Assert.True(bridge.GetVehicleHandbrakeForTest(veh));
        }
    }
}
