using System;
using FactionWars.ScriptHookV.Logging;
using FactionWars.Territory.Models;

namespace FactionWars.ScriptHookV.Managers
{
    public partial class CommanderManager
    {
        public void OnKeyDown(int keyCode)
        {
            if (keyCode != InteractKeyCode) return;

            // Check if player is near any commander (proximity-based)
            var nearbyCommander = GetNearbyCommander();
            if (nearbyCommander == null) return;

            _openMenuCallback?.Invoke(null!);
        }

        /// <summary>
        /// Checks if a ped handle is a commander.
        /// </summary>
        private bool IsCommander(int pedHandle)
        {
            return _commanderByZone.ContainsValue(pedHandle);
        }

        /// <summary>
        /// Resumes behavior for a commander after the player moves away.
        /// Uses combat targeting if in battle, otherwise wanders.
        /// </summary>
        private void ResumeWandering(int commanderHandle)
        {
            // Find the zone this commander is in
            string? zoneId = null;
            foreach (var kvp in _commanderByZone)
            {
                if (kvp.Value == commanderHandle)
                {
                    zoneId = kvp.Key;
                    break;
                }
            }

            if (zoneId == null) return;

            var zone = _zoneService.GetZone(zoneId);
            if (zone == null) return;

            // Resume combat targeting if in battle, otherwise wander
            if (_zonesInBattle.Contains(zoneId))
            {
                _gameBridge.TaskCombatHatedTargetsAroundPed(commanderHandle, zone.Radius);
            }
            else
            {
                _gameBridge.TaskPedWanderInArea(commanderHandle, zone.Center, zone.Radius);
            }
        }

        /// <summary>
        /// Gets the first commander within interaction proximity of the player.
        /// Returns the ped handle of the nearby commander, or null if none are close enough.
        /// </summary>
        private int? GetNearbyCommander()
        {
            var playerPos = _gameBridge.GetPlayerPosition();

            foreach (var kvp in _commanderByZone)
            {
                var pedHandle = kvp.Value;
                var commanderPos = _gameBridge.GetPedPosition(pedHandle);

                // Calculate distance (2D distance is sufficient since they're on the same plane)
                var dx = playerPos.X - commanderPos.X;
                var dy = playerPos.Y - commanderPos.Y;
                var dz = playerPos.Z - commanderPos.Z;
                var distance = (float)Math.Sqrt(dx * dx + dy * dy + dz * dz);

                if (distance <= InteractionProximity)
                {
                    return pedHandle;
                }
            }

            return null;
        }

        /// <summary>
        /// Called when a battle starts in a zone. Tasks commander to actively engage enemies.
        /// </summary>
        public void OnBattleStarted(string zoneId)
        {
            _zonesInBattle.Add(zoneId);

            if (!_commanderByZone.TryGetValue(zoneId, out var pedHandle)) return;

            var zone = _zoneService.GetZone(zoneId);
            if (zone == null) return;

            _gameBridge.TaskCombatHatedTargetsAroundPed(pedHandle, zone.Radius);
        }

        /// <summary>
        /// Called when a battle ends in a zone. Switches commander back to walking wander.
        /// </summary>
        public void OnBattleEnded(string zoneId)
        {
            _zonesInBattle.Remove(zoneId);

            if (!_commanderByZone.TryGetValue(zoneId, out var pedHandle)) return;

            var zone = _zoneService.GetZone(zoneId);
            if (zone == null) return;

            _gameBridge.TaskPedWanderInArea(pedHandle, zone.Center, zone.Radius);
        }
    }
}
