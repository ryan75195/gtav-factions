using System;
using FactionWars.Core.Interfaces;
using FactionWars.ScriptHookV.Logging;

namespace FactionWars.ScriptHookV.Managers
{
    public partial class CommanderManager
    {
        private void RespawnCommander(string zoneId)
        {
            // Remove old commander
            if (_commanderByZone.TryGetValue(zoneId, out var oldHandle))
            {
                _pedBlipService.RemoveBlipForPed(oldHandle);
                _gameBridge.DeletePed(oldHandle);
                _commanderByZone.Remove(zoneId);
            }

            // Get zone and spawn new commander
            var zone = _zoneService.GetZone(zoneId);
            if (zone != null && zone.OwnerFactionId == _playerFactionId)
            {
                SpawnCommander(zone);
            }
        }

        /// <summary>
        /// Calculates a random spawn position around the zone center at ground level.
        /// Uses the zone's full radius for spawn area (30%-100%) and navmesh-based safe
        /// coordinates to avoid spawning on rooftops.
        /// </summary>
        private Vector3 CalculateRandomSpawnPosition(Vector3 center, float zoneRadius)
        {
            var angle = _random.NextDouble() * 2 * Math.PI;
            var minRadius = zoneRadius * MinSpawnRadiusFraction;
            var distance = minRadius + (float)(_random.NextDouble() * (zoneRadius - minRadius));
            var x = center.X + (float)(Math.Cos(angle) * distance);
            var y = center.Y + (float)(Math.Sin(angle) * distance);

            var targetPos = new Vector3(x, y, center.Z);
            return _gameBridge.GetSafeCoordForPed(targetPos);
        }

        /// <summary>
        /// Handles key press events. Opens menu when E is pressed near a commander.
        /// </summary>
        /// <param name="keyCode">The key code of the pressed key.</param>
    }
}
