using FactionWars.Core.Interfaces;
using FactionWars.Persistence.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FactionWars.Persistence
{
    /// <summary>
    /// Validates save file game state data for integrity and compatibility.
    /// </summary>
    public class SaveFileValidator : ISaveFileValidator
    {
        private readonly int _currentVersion;

        /// <summary>
        /// Creates a new SaveFileValidator with default current version.
        /// </summary>
        public SaveFileValidator() : this(currentVersion: 1)
        {
        }

        /// <summary>
        /// Creates a new SaveFileValidator with specified current version.
        /// </summary>
        /// <param name="currentVersion">The current supported save file version.</param>
        public SaveFileValidator(int currentVersion)
        {
            _currentVersion = currentVersion;
        }

        /// <summary>
        /// Validates a game state and returns the validation result.
        /// </summary>
        public SaveFileValidationResult Validate(GameState gameState)
        {
            var result = new SaveFileValidationResult();

            if (gameState == null)
            {
                result.AddError("GameState is null.");
                return result;
            }

            ValidateVersion(gameState, result);
            ValidateTimestamps(gameState, result);
            ValidateCollectionsNotNull(gameState, result);

            // If collections are null, we can't validate further
            if (gameState.Factions == null || gameState.FactionStates == null ||
                gameState.Zones == null || gameState.Relationships == null)
            {
                return result;
            }

            var factionIds = new HashSet<string>();
            ValidateFactions(gameState, result, factionIds);

            var zoneIds = new HashSet<string>();
            ValidateZones(gameState, result, zoneIds, factionIds);

            ValidateFactionStates(gameState, result, factionIds, zoneIds);
            ValidateRelationships(gameState, result, factionIds);

            return result;
        }

        private void ValidateVersion(GameState gameState, SaveFileValidationResult result)
        {
            if (gameState.Version <= 0)
            {
                result.AddError($"Version must be positive. Found: {gameState.Version}");
            }
            else if (gameState.Version > _currentVersion)
            {
                result.AddError($"Save file version {gameState.Version} is not supported. Current version is {_currentVersion}.");
            }
        }

        private void ValidateTimestamps(GameState gameState, SaveFileValidationResult result)
        {
            var now = DateTime.UtcNow;

            if (gameState.CreatedAt > now.AddMinutes(1)) // Small tolerance
            {
                result.AddError("CreatedAt timestamp is in the future.");
            }

            if (gameState.ModifiedAt < gameState.CreatedAt)
            {
                result.AddError("ModifiedAt timestamp is before CreatedAt timestamp.");
            }

            if (gameState.TotalPlayTimeSeconds < 0)
            {
                result.AddError("TotalPlayTimeSeconds cannot be negative.");
            }
        }

        private void ValidateCollectionsNotNull(GameState gameState, SaveFileValidationResult result)
        {
            if (gameState.Factions == null)
            {
                result.AddError("Factions collection is null.");
            }

            if (gameState.FactionStates == null)
            {
                result.AddError("FactionStates collection is null.");
            }

            if (gameState.Zones == null)
            {
                result.AddError("Zones collection is null.");
            }

            if (gameState.Relationships == null)
            {
                result.AddError("Relationships collection is null.");
            }
        }

        private void ValidateFactions(GameState gameState, SaveFileValidationResult result, HashSet<string> factionIds)
        {
            foreach (var faction in gameState.Factions)
            {
                if (string.IsNullOrEmpty(faction.Id))
                {
                    result.AddError("Faction has missing or empty Id.");
                    continue;
                }

                if (string.IsNullOrEmpty(faction.Name))
                {
                    result.AddError($"Faction '{faction.Id}' has missing or empty Name.");
                }

                if (!factionIds.Add(faction.Id))
                {
                    result.AddError($"Faction Id '{faction.Id}' is duplicate.");
                }
            }
        }

        private void ValidateZones(GameState gameState, SaveFileValidationResult result,
            HashSet<string> zoneIds, HashSet<string> factionIds)
        {
            foreach (var zone in gameState.Zones)
            {
                if (string.IsNullOrEmpty(zone.Id))
                {
                    result.AddError("Zone has missing or empty Id.");
                    continue;
                }

                if (string.IsNullOrEmpty(zone.Name))
                {
                    result.AddError($"Zone '{zone.Id}' has missing or empty Name.");
                }

                if (!zoneIds.Add(zone.Id))
                {
                    result.AddError($"Zone Id '{zone.Id}' is duplicate.");
                }

                if (zone.Radius < 0)
                {
                    result.AddError($"Zone '{zone.Id}' has negative Radius: {zone.Radius}");
                }

                if (zone.ControlPercentage < 0 || zone.ControlPercentage > 100)
                {
                    result.AddError($"Zone '{zone.Id}' has invalid ControlPercentage: {zone.ControlPercentage}. Must be 0-100.");
                }

                if (zone.OwnerFactionId != null && !factionIds.Contains(zone.OwnerFactionId))
                {
                    result.AddError($"Zone '{zone.Id}' references non-existent faction '{zone.OwnerFactionId}'.");
                }
            }
        }

        private void ValidateFactionStates(GameState gameState, SaveFileValidationResult result,
            HashSet<string> factionIds, HashSet<string> zoneIds)
        {
            foreach (var state in gameState.FactionStates)
            {
                if (string.IsNullOrEmpty(state.FactionId))
                {
                    result.AddError("FactionState has missing or empty FactionId.");
                    continue;
                }

                if (!factionIds.Contains(state.FactionId))
                {
                    result.AddError($"FactionState references non-existent faction '{state.FactionId}'.");
                }

                if (state.Cash < 0)
                {
                    result.AddError($"FactionState '{state.FactionId}' has negative Cash: {state.Cash}");
                }

                if (state.RecruitmentPoints < 0)
                {
                    result.AddError($"FactionState '{state.FactionId}' has negative RecruitmentPoints: {state.RecruitmentPoints}");
                }

                if (state.Weapons < 0)
                {
                    result.AddError($"FactionState '{state.FactionId}' has negative Weapons: {state.Weapons}");
                }

                if (state.TroopCount < 0)
                {
                    result.AddError($"FactionState '{state.FactionId}' has negative TroopCount: {state.TroopCount}");
                }

                if (state.OwnedZoneIds != null)
                {
                    foreach (var zoneId in state.OwnedZoneIds)
                    {
                        if (!zoneIds.Contains(zoneId))
                        {
                            result.AddError($"FactionState '{state.FactionId}' references zone '{zoneId}' that does not exist.");
                        }
                    }
                }
            }
        }

        private void ValidateRelationships(GameState gameState, SaveFileValidationResult result,
            HashSet<string> factionIds)
        {
            foreach (var relationship in gameState.Relationships)
            {
                if (string.IsNullOrEmpty(relationship.FactionId1))
                {
                    result.AddError("Relationship has missing or empty FactionId1.");
                    continue;
                }

                if (string.IsNullOrEmpty(relationship.FactionId2))
                {
                    result.AddError("Relationship has missing or empty FactionId2.");
                    continue;
                }

                if (relationship.FactionId1 == relationship.FactionId2)
                {
                    result.AddError($"Relationship has self-reference: faction '{relationship.FactionId1}' cannot have a relationship with itself.");
                }

                if (!factionIds.Contains(relationship.FactionId1))
                {
                    result.AddError($"Relationship references non-existent faction '{relationship.FactionId1}'.");
                }

                if (!factionIds.Contains(relationship.FactionId2))
                {
                    result.AddError($"Relationship references non-existent faction '{relationship.FactionId2}'.");
                }

                if (relationship.Value < -100 || relationship.Value > 100)
                {
                    result.AddError($"Relationship Value {relationship.Value} is out of range. Must be between -100 and 100.");
                }
            }
        }
    }
}
