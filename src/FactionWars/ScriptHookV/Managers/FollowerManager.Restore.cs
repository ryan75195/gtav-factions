using FactionWars.Core.Interfaces;
using FactionWars.Persistence.Models;
using FactionWars.UI.Interfaces;
using System;
using System.Collections.Generic;

namespace FactionWars.ScriptHookV.Managers
{
    public partial class FollowerManager
    {
        public void RestoreFollowers(string factionId, IEnumerable<SavedFollowerState> followers, int vehicleHandle)
        {
            if (string.IsNullOrEmpty(factionId) || followers == null)
            {
                return;
            }

            DismissAllFollowers(factionId);

            foreach (var savedFollower in followers)
            {
                if (!string.Equals(savedFollower.FactionId, factionId, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                RestoreFollower(factionId, savedFollower, vehicleHandle);
            }
        }

        private void RestoreFollower(string factionId, SavedFollowerState savedFollower, int vehicleHandle)
        {
            var result = _followerService.Recruit(factionId, savedFollower.Tier);
            if (!result.Success || result.Follower == null)
            {
                return;
            }

            var modelName = _modelsByTier.TryGetValue(savedFollower.Tier, out var model) ? model : "g_m_y_lost_01";
            var spawnPos = new Vector3(savedFollower.Position.X, savedFollower.Position.Y, savedFollower.Position.Z);
            var pedHandle = _pedSpawningService.SpawnPed(modelName, spawnPos, factionId, null);
            if (!pedHandle.IsValid)
            {
                _followerService.DismissFollower(result.Follower.Id);
                return;
            }

            result.Follower.SetPedHandle(pedHandle.Handle);
            _gameBridge.SetPedAsFollower(pedHandle.Handle);
            ConfigureFollowerCombat(pedHandle.Handle, _defenderTierService.GetTierConfig(savedFollower.Tier));
            _pedBlipService.CreateBlipForPed(pedHandle.Handle, BlipColor.Yellow);

            if (vehicleHandle >= 0 && savedFollower.VehicleSeatIndex >= 0)
            {
                _gameBridge.SetPedIntoVehicle(pedHandle.Handle, vehicleHandle, savedFollower.VehicleSeatIndex);
            }
        }
    }
}
