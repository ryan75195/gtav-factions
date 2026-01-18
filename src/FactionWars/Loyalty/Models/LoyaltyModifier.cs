using System;

namespace FactionWars.Loyalty.Models
{
    /// <summary>
    /// Represents a factor that modifies zone population loyalty.
    /// Modifiers can be permanent or temporary, and positive or negative.
    /// </summary>
    public class LoyaltyModifier
    {
        #region Default Values

        /// <summary>
        /// Default daily passive loyalty gain (small incremental improvement).
        /// </summary>
        public const int DefaultTimeBasedGain = 2;

        /// <summary>
        /// Default bonus for winning a defensive battle.
        /// </summary>
        public const int DefaultCombatVictoryBonus = 7;

        /// <summary>
        /// Default penalty for losing a defensive battle.
        /// </summary>
        public const int DefaultCombatDefeatPenalty = -5;

        /// <summary>
        /// Default bonus per unit of resource invested.
        /// </summary>
        public const int DefaultResourceInvestmentBonus = 3;

        /// <summary>
        /// Default penalty per oppression incident.
        /// </summary>
        public const int DefaultOppressionPenalty = -2;

        /// <summary>
        /// Default bonus from propaganda operations.
        /// </summary>
        public const int DefaultPropagandaBonus = 5;

        /// <summary>
        /// Default penalty from recent conquest.
        /// </summary>
        public const int DefaultRecentConquestPenalty = -10;

        /// <summary>
        /// Default duration for propaganda effects (in days).
        /// </summary>
        public const int DefaultPropagandaDuration = 7;

        /// <summary>
        /// Default duration for recent conquest penalty (in days).
        /// </summary>
        public const int DefaultRecentConquestDuration = 14;

        #endregion

        /// <summary>
        /// The type of loyalty modifier.
        /// </summary>
        public LoyaltyModifierType Type { get; }

        /// <summary>
        /// The value of the modifier (positive for bonuses, negative for penalties).
        /// </summary>
        public int Value { get; }

        /// <summary>
        /// Optional description of the modifier's origin or reason.
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// When the modifier was created.
        /// </summary>
        public DateTime CreatedAt { get; }

        /// <summary>
        /// Duration in days before the modifier expires. 0 means permanent.
        /// </summary>
        public int DurationDays { get; }

        /// <summary>
        /// Indicates whether this modifier is permanent (no duration).
        /// </summary>
        public bool IsPermanent => DurationDays == 0;

        /// <summary>
        /// Indicates whether this modifier has a positive effect on loyalty.
        /// </summary>
        public bool IsPositive => Value > 0;

        /// <summary>
        /// Creates a new loyalty modifier.
        /// </summary>
        /// <param name="type">The type of modifier.</param>
        /// <param name="value">The modifier value (positive or negative).</param>
        /// <param name="description">Optional description of the modifier.</param>
        /// <param name="durationDays">Duration in days (0 = permanent).</param>
        public LoyaltyModifier(LoyaltyModifierType type, int value, string description = "", int durationDays = 0)
        {
            Type = type;
            Value = value;
            Description = description ?? string.Empty;
            DurationDays = Math.Max(0, durationDays);
            CreatedAt = DateTime.UtcNow;
        }

        #region Factory Methods

        /// <summary>
        /// Creates a time-based loyalty gain modifier.
        /// </summary>
        /// <param name="value">Optional custom value (default: DefaultTimeBasedGain).</param>
        public static LoyaltyModifier CreateTimeBasedGain(int? value = null)
        {
            return new LoyaltyModifier(
                LoyaltyModifierType.TimeBasedGain,
                value ?? DefaultTimeBasedGain,
                "Daily loyalty gain");
        }

        /// <summary>
        /// Creates a combat victory modifier.
        /// </summary>
        /// <param name="value">Optional custom value (default: DefaultCombatVictoryBonus).</param>
        public static LoyaltyModifier CreateCombatVictory(int? value = null)
        {
            return new LoyaltyModifier(
                LoyaltyModifierType.CombatVictory,
                value ?? DefaultCombatVictoryBonus,
                "Defensive victory");
        }

        /// <summary>
        /// Creates a combat defeat modifier.
        /// </summary>
        /// <param name="value">Optional custom value (default: DefaultCombatDefeatPenalty).</param>
        public static LoyaltyModifier CreateCombatDefeat(int? value = null)
        {
            return new LoyaltyModifier(
                LoyaltyModifierType.CombatDefeat,
                value ?? DefaultCombatDefeatPenalty,
                "Defensive defeat");
        }

        /// <summary>
        /// Creates a resource investment modifier.
        /// </summary>
        /// <param name="amount">Optional custom amount (default: DefaultResourceInvestmentBonus).</param>
        public static LoyaltyModifier CreateResourceInvestment(int? amount = null)
        {
            return new LoyaltyModifier(
                LoyaltyModifierType.ResourceInvestment,
                amount ?? DefaultResourceInvestmentBonus,
                "Community investment");
        }

        /// <summary>
        /// Creates an oppression penalty modifier.
        /// </summary>
        /// <param name="value">Optional custom value (default: DefaultOppressionPenalty).</param>
        public static LoyaltyModifier CreateOppression(int? value = null)
        {
            return new LoyaltyModifier(
                LoyaltyModifierType.Oppression,
                value ?? DefaultOppressionPenalty,
                "Civilian casualties");
        }

        /// <summary>
        /// Creates a propaganda bonus modifier with temporary duration.
        /// </summary>
        /// <param name="value">Optional custom value (default: DefaultPropagandaBonus).</param>
        /// <param name="durationDays">Optional duration (default: DefaultPropagandaDuration).</param>
        public static LoyaltyModifier CreatePropaganda(int? value = null, int? durationDays = null)
        {
            return new LoyaltyModifier(
                LoyaltyModifierType.Propaganda,
                value ?? DefaultPropagandaBonus,
                "Propaganda campaign",
                durationDays ?? DefaultPropagandaDuration);
        }

        /// <summary>
        /// Creates a neighbor influence modifier.
        /// </summary>
        /// <param name="value">The influence value (positive or negative based on neighbor loyalty).</param>
        public static LoyaltyModifier CreateNeighborInfluence(int value)
        {
            return new LoyaltyModifier(
                LoyaltyModifierType.NeighborInfluence,
                value,
                "Neighboring zone influence");
        }

        /// <summary>
        /// Creates a recent conquest penalty modifier with temporary duration.
        /// </summary>
        /// <param name="value">Optional custom value (default: DefaultRecentConquestPenalty).</param>
        /// <param name="durationDays">Optional duration (default: DefaultRecentConquestDuration).</param>
        public static LoyaltyModifier CreateRecentConquest(int? value = null, int? durationDays = null)
        {
            return new LoyaltyModifier(
                LoyaltyModifierType.RecentConquest,
                value ?? DefaultRecentConquestPenalty,
                "Recent conquest penalty",
                durationDays ?? DefaultRecentConquestDuration);
        }

        #endregion

        /// <summary>
        /// Returns a string representation of the modifier.
        /// </summary>
        public override string ToString()
        {
            var sign = Value >= 0 ? "+" : "";
            var duration = IsPermanent ? "" : $" ({DurationDays}d)";
            return $"{Type}: {sign}{Value}{duration}";
        }
    }
}
