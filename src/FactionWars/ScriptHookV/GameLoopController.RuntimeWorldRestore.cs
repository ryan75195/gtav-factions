using FactionWars.Core.Interfaces;
using FactionWars.Persistence.Models;
using FactionWars.ScriptHookV.Logging;
using System;

namespace FactionWars.ScriptHookV
{
    public partial class GameLoopController
    {
        private void TryRestoreRuntimeWorldState()
        {
            var state = _pendingRuntimeWorldRestore;
            if (state == null)
            {
                return;
            }

            if (!_gameBridge.CanControlCharacter() || _gameBridge.IsPlayerDead())
            {
                return;
            }

            _pendingRuntimeWorldRestore = null;
            RestoreRuntimeWorldState(state);
        }

        private void RestoreRuntimeWorldState(RuntimeWorldState state)
        {
            try
            {
                var playerPosition = ToVector(state.PlayerPosition);
                var vehicleHandle = RestorePlayerVehicle(state, playerPosition);

                _gameBridge.SetPlayerPosition(playerPosition);
                _gameBridge.SetPlayerHeading(state.PlayerPosition.Heading);

                if (vehicleHandle >= 0)
                {
                    _gameBridge.SetPlayerIntoVehicle(vehicleHandle, 0);
                }

                var factionId = CurrentPlayerFactionId;
                if (!string.IsNullOrEmpty(factionId))
                {
                    _followerManager?.RestoreFollowers(factionId!, state.Followers, vehicleHandle);
                }

                FileLogger.Info("Runtime world state restored from sidecar.");
            }
            catch (Exception ex)
            {
                FileLogger.Error("Runtime world state restore failed", ex);
            }
        }

        private int RestorePlayerVehicle(RuntimeWorldState state, Vector3 playerPosition)
        {
            var savedVehicle = state.PlayerVehicle;
            if (savedVehicle == null || string.IsNullOrWhiteSpace(savedVehicle.ModelName))
            {
                return -1;
            }

            var vehiclePosition = ToVector(savedVehicle.Position);
            var handle = _gameBridge.CreateVehicle(savedVehicle.ModelName, vehiclePosition);
            if (handle < 0)
            {
                return -1;
            }

            _gameBridge.SetVehicleHeading(handle, savedVehicle.Position.Heading);
            return handle;
        }

        private static Vector3 ToVector(PlayerPosition position)
        {
            return new Vector3(position.X, position.Y, position.Z);
        }
    }
}
