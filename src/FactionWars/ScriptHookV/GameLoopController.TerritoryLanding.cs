using FactionWars.Core.Interfaces;
using FactionWars.ScriptHookV.Logging;
using FactionWars.Territory.Models;
using System;
using System.Collections.Generic;

namespace FactionWars.ScriptHookV
{
    public partial class GameLoopController
    {
        private const float OwnedTerritoryMaximumDropFromZoneCenter = 25f;
        private const float OwnedTerritoryMaximumRiseFromZoneCenter = 150f;

        private Vector3 GetOwnedTerritoryLandingPosition(Zone targetZone)
        {
            var center = targetZone.Center;
            var candidates = GetLandingCandidates(targetZone);

            foreach (var candidate in candidates)
            {
                if (!targetZone.Boundary.Contains(candidate))
                    continue;

                var safeCandidate = _gameBridge.GetSafeCoordForPed(candidate);
                if (TryGetValidLandingPosition(targetZone, candidate, safeCandidate, out var safeLanding))
                    return safeLanding;

                var groundCandidate = new Vector3(
                    candidate.X,
                    candidate.Y,
                    _gameBridge.GetGroundZ(candidate.X, candidate.Y, candidate.Z));
                if (TryGetValidLandingPosition(targetZone, candidate, groundCandidate, out var groundLanding))
                    return groundLanding;
            }

            var fallbackSafe = _gameBridge.GetSafeCoordForPed(center);
            if (TryGetValidLandingPosition(targetZone, center, fallbackSafe, out var fallbackLanding))
                return fallbackLanding;

            var fallbackGround = _gameBridge.GetGroundZ(center.X, center.Y, center.Z);
            return new Vector3(center.X, center.Y, fallbackGround);
        }

        private bool TryGetValidLandingPosition(Zone targetZone, Vector3 requestedPosition, Vector3 candidate, out Vector3 landingPosition)
        {
            landingPosition = default;
            if (!targetZone.Boundary.Contains(candidate))
                return false;

            if (candidate.Z < targetZone.Center.Z - OwnedTerritoryMaximumDropFromZoneCenter)
            {
                FileLogger.Warn($"GetOwnedTerritoryLandingPosition: rejected low candidate ({candidate.X:F1},{candidate.Y:F1},{candidate.Z:F1}) for zone {targetZone.Id}");
                return false;
            }

            if (candidate.Z > targetZone.Center.Z + OwnedTerritoryMaximumRiseFromZoneCenter)
            {
                FileLogger.Warn($"GetOwnedTerritoryLandingPosition: rejected high candidate ({candidate.X:F1},{candidate.Y:F1},{candidate.Z:F1}) for zone {targetZone.Id}");
                return false;
            }

            var groundZ = _gameBridge.GetGroundZ(candidate.X, candidate.Y, Math.Max(candidate.Z, requestedPosition.Z));
            var correctedZ = groundZ < candidate.Z ? groundZ : candidate.Z;
            if (correctedZ < targetZone.Center.Z - OwnedTerritoryMaximumDropFromZoneCenter)
            {
                FileLogger.Warn($"GetOwnedTerritoryLandingPosition: rejected low corrected Z {correctedZ:F1} for zone {targetZone.Id}");
                return false;
            }

            if (correctedZ > targetZone.Center.Z + OwnedTerritoryMaximumRiseFromZoneCenter)
            {
                FileLogger.Warn($"GetOwnedTerritoryLandingPosition: rejected high corrected Z {correctedZ:F1} for zone {targetZone.Id}");
                return false;
            }

            landingPosition = new Vector3(candidate.X, candidate.Y, correctedZ);
            return true;
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
