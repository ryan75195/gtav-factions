using System;

namespace FactionWars.Tension.Models
{
    /// <summary>
    /// Represents the active effects of a diplomatic action between two factions.
    /// Provides modifiers and restrictions based on the action type.
    /// </summary>
    public class DiplomaticActionEffect
    {
        /// <summary>
        /// The first faction in this diplomatic effect.
        /// </summary>
        public string FactionId1 { get; }

        /// <summary>
        /// The second faction in this diplomatic effect.
        /// </summary>
        public string FactionId2 { get; }

        /// <summary>
        /// The type of diplomatic action providing this effect.
        /// </summary>
        public DiplomaticActionType ActionType { get; }

        /// <summary>
        /// The time when this effect started.
        /// </summary>
        public DateTime StartTime { get; }

        /// <summary>
        /// The duration of this effect in seconds. 0 means permanent.
        /// </summary>
        public int DurationSeconds { get; }

        /// <summary>
        /// Creates a new diplomatic action effect between two factions.
        /// </summary>
        /// <param name="factionId1">The first faction.</param>
        /// <param name="factionId2">The second faction.</param>
        /// <param name="actionType">The type of diplomatic action.</param>
        /// <param name="durationSeconds">Duration in seconds. 0 for permanent.</param>
        /// <exception cref="ArgumentNullException">Thrown when faction IDs are null.</exception>
        /// <exception cref="ArgumentException">Thrown when faction IDs are empty or identical.</exception>
        public DiplomaticActionEffect(string factionId1, string factionId2, DiplomaticActionType actionType, int durationSeconds = 0)
        {
            if (factionId1 == null)
                throw new ArgumentNullException(nameof(factionId1));
            if (factionId2 == null)
                throw new ArgumentNullException(nameof(factionId2));
            if (string.IsNullOrWhiteSpace(factionId1))
                throw new ArgumentException("Faction ID 1 cannot be empty or whitespace.", nameof(factionId1));
            if (string.IsNullOrWhiteSpace(factionId2))
                throw new ArgumentException("Faction ID 2 cannot be empty or whitespace.", nameof(factionId2));
            if (factionId1 == factionId2)
                throw new ArgumentException("Faction IDs cannot be the same.", nameof(factionId2));

            FactionId1 = factionId1;
            FactionId2 = factionId2;
            ActionType = actionType;
            StartTime = DateTime.UtcNow;
            DurationSeconds = durationSeconds;
        }

        /// <summary>
        /// Gets the combat modifier provided by this effect.
        /// Positive values increase combat effectiveness when defending together.
        /// </summary>
        public float CombatModifier => ActionType switch
        {
            DiplomaticActionType.MutualDefense => 0.15f,
            DiplomaticActionType.Alliance => 0.25f,
            _ => 0f
        };

        /// <summary>
        /// Gets the resource modifier provided by this effect.
        /// Positive values increase resource generation.
        /// </summary>
        public float ResourceModifier => ActionType switch
        {
            DiplomaticActionType.TradeAgreement => 0.1f,
            DiplomaticActionType.MutualDefense => 0.05f,
            DiplomaticActionType.Alliance => 0.15f,
            _ => 0f
        };

        /// <summary>
        /// Gets the tension decay modifier provided by this effect.
        /// Values greater than 1 accelerate tension decay.
        /// </summary>
        public float TensionDecayModifier => ActionType switch
        {
            DiplomaticActionType.Ceasefire => 1.5f,
            DiplomaticActionType.NonAggressionPact => 1.25f,
            DiplomaticActionType.TradeAgreement => 1.1f,
            DiplomaticActionType.MutualDefense => 1.25f,
            DiplomaticActionType.Alliance => 2.0f,
            DiplomaticActionType.DeclarationOfWar => 0f,
            DiplomaticActionType.PeaceTreaty => 2.5f,
            DiplomaticActionType.TerritorialConcession => 1.0f,
            _ => 1.0f
        };

        /// <summary>
        /// Gets whether this effect prevents combat between the factions.
        /// </summary>
        public bool PreventsCombat => ActionType switch
        {
            DiplomaticActionType.Ceasefire => true,
            DiplomaticActionType.NonAggressionPact => true,
            DiplomaticActionType.PeaceTreaty => true,
            _ => false
        };

        /// <summary>
        /// Gets whether this effect requires factions to support each other in defense.
        /// </summary>
        public bool RequiresDefenseSupport => ActionType switch
        {
            DiplomaticActionType.MutualDefense => true,
            DiplomaticActionType.Alliance => true,
            _ => false
        };

        /// <summary>
        /// Gets whether this effect prevents covert operations between the factions.
        /// </summary>
        public bool PreventsCovertOperations => ActionType switch
        {
            DiplomaticActionType.NonAggressionPact => true,
            DiplomaticActionType.MutualDefense => true,
            DiplomaticActionType.Alliance => true,
            DiplomaticActionType.PeaceTreaty => true,
            _ => false
        };

        /// <summary>
        /// Gets whether this effect allows territory access between the factions.
        /// </summary>
        public bool AllowsTerritoryAccess => ActionType switch
        {
            DiplomaticActionType.TradeAgreement => true,
            DiplomaticActionType.MutualDefense => true,
            DiplomaticActionType.Alliance => true,
            _ => false
        };

        /// <summary>
        /// Gets whether this effect is still active (not expired).
        /// Permanent effects are always active.
        /// </summary>
        public bool IsActive
        {
            get
            {
                if (IsPermanent)
                    return true;

                var elapsed = (DateTime.UtcNow - StartTime).TotalSeconds;
                return elapsed < DurationSeconds;
            }
        }

        /// <summary>
        /// Gets whether this effect is permanent (has no duration).
        /// </summary>
        public bool IsPermanent => DurationSeconds == 0;

        /// <summary>
        /// Gets the remaining duration in seconds.
        /// Returns null for permanent effects.
        /// </summary>
        public int? RemainingDurationSeconds
        {
            get
            {
                if (IsPermanent)
                    return null;

                var elapsed = (int)(DateTime.UtcNow - StartTime).TotalSeconds;
                var remaining = DurationSeconds - elapsed;
                return remaining > 0 ? remaining : 0;
            }
        }

        /// <summary>
        /// Checks if the specified faction is involved in this effect.
        /// </summary>
        /// <param name="factionId">The faction ID to check.</param>
        /// <returns>True if the faction is part of this effect.</returns>
        public bool ContainsFaction(string factionId)
        {
            if (string.IsNullOrEmpty(factionId))
                return false;

            return factionId == FactionId1 || factionId == FactionId2;
        }

        /// <summary>
        /// Checks if both specified factions are involved in this effect (order independent).
        /// </summary>
        /// <param name="factionId1">The first faction ID.</param>
        /// <param name="factionId2">The second faction ID.</param>
        /// <returns>True if both factions are involved in this effect.</returns>
        public bool InvolvesBothFactions(string factionId1, string factionId2)
        {
            return ContainsFaction(factionId1) && ContainsFaction(factionId2);
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            var duration = IsPermanent ? "permanent" : $"{RemainingDurationSeconds}s remaining";
            return $"{ActionType} effect between {FactionId1} and {FactionId2} ({duration})";
        }
    }
}
