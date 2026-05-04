using System.Collections.Generic;
using FactionWars.ScriptHookV.Logging;

namespace FactionWars.ScriptHookV.Managers
{
    public partial class CommanderManager
    {
        public void Update()
        {
            // Check for dead commanders
            var deadCommanders = new List<string>();

            foreach (var kvp in _commanderByZone)
            {
                var zoneId = kvp.Key;
                var pedHandle = kvp.Value;

                if (!_gameBridge.IsPedAlive(pedHandle))
                {
                    deadCommanders.Add(zoneId);
                }
            }

            foreach (var zoneId in deadCommanders)
            {
                RespawnCommander(zoneId);
            }

            // Handle proximity-based facing/wandering state
            var nearbyCommander = GetNearbyCommander();
            if (nearbyCommander != null)
            {
                var commanderHandle = nearbyCommander.Value;

                // Show help text
                _gameBridge.DisplayHelpText("Press ~INPUT_CONTEXT~ to talk to Commander");

                // If commander is not already facing player, stop wandering and face player
                if (!_commandersFacingPlayer.Contains(commanderHandle))
                {
                    _gameBridge.ClearPedTasks(commanderHandle);
                    var playerPos = _gameBridge.GetPlayerPosition();
                    _gameBridge.TaskPedTurnToFacePosition(commanderHandle, playerPos);
                    _commandersFacingPlayer.Add(commanderHandle);
                }
            }

            // Resume wandering for commanders no longer near player
            var commandersToResume = new List<int>();
            foreach (var commanderHandle in _commandersFacingPlayer)
            {
                if (nearbyCommander == null || nearbyCommander.Value != commanderHandle)
                {
                    commandersToResume.Add(commanderHandle);
                }
            }

            foreach (var commanderHandle in commandersToResume)
            {
                _commandersFacingPlayer.Remove(commanderHandle);
                ResumeWandering(commanderHandle);
            }
        }

        /// <summary>
        /// Respawns a commander in the specified zone.
        /// Removes the old commander and spawns a new one.
        /// </summary>
        /// <param name="zoneId">The zone ID to respawn the commander in.</param>
    }
}
