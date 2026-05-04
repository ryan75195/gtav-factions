using FactionWars.Core.Interfaces;
using FactionWars.ScriptHookV.Logging;
using FactionWars.Territory.Models;
using System;
using System.Collections.Generic;

namespace FactionWars.ScriptHookV
{
    public partial class GameLoopController
    {
        private Vector3 GetOwnedTerritoryLandingPosition(Zone targetZone)
        {
            var center = targetZone.Center;
            var candidates = GetLandingCandidates(targetZone);

            foreach (var candidate in candidates)
            {
                if (!targetZone.Boundary.Contains(candidate))
                    continue;

                var groundCandidate = new Vector3(
                    candidate.X,
                    candidate.Y,
                    _gameBridge.GetGroundZ(candidate.X, candidate.Y, candidate.Z));
                if (targetZone.Boundary.Contains(groundCandidate))
                    return groundCandidate;

                var safeCandidate = _gameBridge.GetSafeCoordForPed(candidate);
                if (targetZone.Boundary.Contains(safeCandidate))
                    return safeCandidate;
            }

            return new Vector3(center.X, center.Y, _gameBridge.GetGroundZ(center.X, center.Y, center.Z));
        }

        private static List<Vector3> GetLandingCandidates(Zone targetZone)
        {
            var center = targetZone.Center;
            var candidates = new List<Vector3>
            {
                center
            };

            float inwardDistance = Math.Max(10f, Math.Min(targetZone.Radius * 0.2f, 40f));
            float[] distanceFractions = { 0.1f, 0.15f, 0.2f, 0.25f };
            double[] angles =
            {
                0.0,
                Math.PI / 4.0,
                Math.PI / 2.0,
                3.0 * Math.PI / 4.0,
                Math.PI,
                5.0 * Math.PI / 4.0,
                3.0 * Math.PI / 2.0,
                7.0 * Math.PI / 4.0
            };

            foreach (float fraction in distanceFractions)
            {
                float radius = Math.Min(inwardDistance, targetZone.Radius * fraction);
                foreach (double angle in angles)
                {
                    candidates.Add(new Vector3(
                        center.X + (float)(Math.Cos(angle) * radius),
                        center.Y + (float)(Math.Sin(angle) * radius),
                        center.Z));
                }
            }

            return candidates;
        }

        private void UpdatePlayerRespawnPlacement()
        {
            bool isDead;
            try
            {
                isDead = _gameBridge.IsPlayerDead();
            }
            catch (Exception ex)
            {
                FileLogger.Error("UpdatePlayerRespawnPlacement: failed to read player death state", ex);
                return;
            }

            if (!_hasReadPlayerDeathState)
            {
                _wasPlayerDead = isDead;
                _hasReadPlayerDeathState = true;
                return;
            }

            if (_wasPlayerDead && !isDead)
            {
                RequestOwnedTerritoryPlacement(CurrentPlayerFactionId, "respawn");
            }

            _wasPlayerDead = isDead;
        }

        private enum OwnedTerritoryPlacementResult
        {
            AlreadyOwned,
            Moved,
            NoTarget,
            NoPlayerPed
        }

        /// <summary>
        /// Gets the display name for a character based on their faction ID.
        /// </summary>
        private static string GetCharacterDisplayName(string? factionId)
        {
            return factionId switch
            {
                CharacterModelFactionDetector.MichaelFactionId => "Michael",
                CharacterModelFactionDetector.FranklinFactionId => "Franklin",
                CharacterModelFactionDetector.TrevorFactionId => "Trevor",
                _ => "Unknown"
            };
        }

        /// <summary>
        /// Called when the player enters a zone.
        /// Triggers combat if entering enemy territory.
        /// </summary>
    }
}
