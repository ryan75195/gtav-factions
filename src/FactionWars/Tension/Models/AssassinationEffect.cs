using System;

namespace FactionWars.Tension.Models
{
    /// <summary>
    /// Represents the effect of a successful assassination operation.
    /// Effects include tension increase, combat effectiveness reduction, and morale impact.
    /// </summary>
    public class AssassinationEffect
    {
        /// <summary>
        /// The faction whose member was assassinated.
        /// </summary>
        public string TargetFactionId { get; }

        /// <summary>
        /// The type of target that was assassinated.
        /// </summary>
        public AssassinationTargetType TargetType { get; }

        /// <summary>
        /// The specific ID of the target, if applicable.
        /// </summary>
        public string? TargetId { get; }

        /// <summary>
        /// The UTC time when the assassination occurred.
        /// </summary>
        public DateTime Timestamp { get; }

        /// <summary>
        /// The base tension increase caused by this assassination.
        /// </summary>
        public int BaseTensionIncrease
        {
            get
            {
                return TargetType switch
                {
                    AssassinationTargetType.Lieutenant => 25,
                    AssassinationTargetType.HighValueMember => 15,
                    AssassinationTargetType.Enforcer => 10,
                    _ => 10
                };
            }
        }

        /// <summary>
        /// Additional tension bonus if the operation was detected.
        /// </summary>
        public int DetectionTensionBonus => 20;

        /// <summary>
        /// The reduction to faction combat effectiveness (0.0 to 1.0).
        /// </summary>
        public float CombatEffectivenessReduction
        {
            get
            {
                return TargetType switch
                {
                    AssassinationTargetType.Lieutenant => 0.15f,
                    AssassinationTargetType.HighValueMember => 0.05f,
                    AssassinationTargetType.Enforcer => 0.02f,
                    _ => 0.02f
                };
            }
        }

        /// <summary>
        /// The duration in seconds that the combat effectiveness reduction lasts.
        /// </summary>
        public int EffectDurationSeconds
        {
            get
            {
                return TargetType switch
                {
                    AssassinationTargetType.Lieutenant => 600,
                    AssassinationTargetType.HighValueMember => 300,
                    AssassinationTargetType.Enforcer => 180,
                    _ => 180
                };
            }
        }

        /// <summary>
        /// The morale impact on the target faction.
        /// </summary>
        public int MoraleImpact
        {
            get
            {
                return TargetType switch
                {
                    AssassinationTargetType.Lieutenant => 20,
                    AssassinationTargetType.HighValueMember => 10,
                    AssassinationTargetType.Enforcer => 5,
                    _ => 5
                };
            }
        }

        /// <summary>
        /// Creates a new assassination effect.
        /// </summary>
        /// <param name="targetFactionId">The faction whose member was assassinated.</param>
        /// <param name="targetType">The type of target.</param>
        /// <param name="targetId">Optional specific target ID.</param>
        /// <exception cref="ArgumentNullException">Thrown if faction ID is null.</exception>
        /// <exception cref="ArgumentException">Thrown if faction ID is empty or whitespace.</exception>
        public AssassinationEffect(string targetFactionId, AssassinationTargetType targetType, string? targetId)
        {
            if (targetFactionId == null)
                throw new ArgumentNullException(nameof(targetFactionId));
            if (string.IsNullOrWhiteSpace(targetFactionId))
                throw new ArgumentException("Target faction ID cannot be empty or whitespace.", nameof(targetFactionId));

            TargetFactionId = targetFactionId;
            TargetType = targetType;
            TargetId = targetId;
            Timestamp = DateTime.UtcNow;
        }

        public override string ToString()
        {
            var targetInfo = TargetId != null ? $" ({TargetId})" : "";
            return $"Assassination[{TargetType}] on {TargetFactionId}{targetInfo}";
        }
    }
}
