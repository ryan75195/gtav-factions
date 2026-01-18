using FactionWars.Lieutenants.Interfaces;
using FactionWars.Lieutenants.Models;

namespace FactionWars.Lieutenants.Services
{
    /// <summary>
    /// Calculates zone bonuses provided by lieutenants based on their traits and level.
    /// All bonuses are multipliers where 1.0 = no change.
    /// </summary>
    public class LieutenantZoneEffects : ILieutenantZoneEffects
    {
        // Base bonus percentages per trait (at level 1)
        private const float AggressiveAttackBonus = 0.15f;
        private const float VeteranCombatBonus = 0.10f;
        private const float RuthlessAttackBonus = 0.10f;
        private const float DefensiveDefenseBonus = 0.15f;
        private const float ResourcefulResourceBonus = 0.20f;
        private const float CorruptCashBonus = 0.15f;
        private const float CharismaticLoyaltyBonus = 0.20f;
        private const float LoyalLoyaltyBonus = 0.15f;
        private const float ConnectedIntelBonus = 0.20f;
        private const float CunningIntelBonus = 0.15f;
        private const float CunningCovertBonus = 0.25f;
        private const float IntimidatingDeterrenceBonus = 0.20f;
        private const float RuthlessDeterrenceBonus = 0.15f;
        private const float VeteranExperienceBonus = 0.30f;

        // Level scaling: bonus increases by 10% per level above 1
        private const float LevelScalingFactor = 0.10f;

        /// <inheritdoc />
        public float GetAttackBonus(Lieutenant? lieutenant)
        {
            if (!IsEffective(lieutenant))
                return 1.0f;

            float bonus = 0f;
            float levelMultiplier = GetLevelMultiplier(lieutenant!.Level);

            if (lieutenant.HasTrait(LieutenantTrait.Aggressive))
                bonus += AggressiveAttackBonus;

            if (lieutenant.HasTrait(LieutenantTrait.Veteran))
                bonus += VeteranCombatBonus;

            if (lieutenant.HasTrait(LieutenantTrait.Ruthless))
                bonus += RuthlessAttackBonus;

            return 1.0f + (bonus * levelMultiplier);
        }

        /// <inheritdoc />
        public float GetDefenseBonus(Lieutenant? lieutenant)
        {
            if (!IsEffective(lieutenant))
                return 1.0f;

            float bonus = 0f;
            float levelMultiplier = GetLevelMultiplier(lieutenant!.Level);

            if (lieutenant.HasTrait(LieutenantTrait.Defensive))
                bonus += DefensiveDefenseBonus;

            if (lieutenant.HasTrait(LieutenantTrait.Veteran))
                bonus += VeteranCombatBonus;

            return 1.0f + (bonus * levelMultiplier);
        }

        /// <inheritdoc />
        public float GetResourceBonus(Lieutenant? lieutenant)
        {
            if (!IsEffective(lieutenant))
                return 1.0f;

            float bonus = 0f;
            float levelMultiplier = GetLevelMultiplier(lieutenant!.Level);

            if (lieutenant.HasTrait(LieutenantTrait.Resourceful))
                bonus += ResourcefulResourceBonus;

            if (lieutenant.HasTrait(LieutenantTrait.Corrupt))
                bonus += CorruptCashBonus;

            return 1.0f + (bonus * levelMultiplier);
        }

        /// <inheritdoc />
        public float GetLoyaltyBonus(Lieutenant? lieutenant)
        {
            if (!IsEffective(lieutenant))
                return 1.0f;

            float bonus = 0f;
            float levelMultiplier = GetLevelMultiplier(lieutenant!.Level);

            if (lieutenant.HasTrait(LieutenantTrait.Charismatic))
                bonus += CharismaticLoyaltyBonus;

            if (lieutenant.HasTrait(LieutenantTrait.Loyal))
                bonus += LoyalLoyaltyBonus;

            return 1.0f + (bonus * levelMultiplier);
        }

        /// <inheritdoc />
        public float GetIntelligenceBonus(Lieutenant? lieutenant)
        {
            if (!IsEffective(lieutenant))
                return 1.0f;

            float bonus = 0f;
            float levelMultiplier = GetLevelMultiplier(lieutenant!.Level);

            if (lieutenant.HasTrait(LieutenantTrait.Connected))
                bonus += ConnectedIntelBonus;

            if (lieutenant.HasTrait(LieutenantTrait.Cunning))
                bonus += CunningIntelBonus;

            return 1.0f + (bonus * levelMultiplier);
        }

        /// <inheritdoc />
        public float GetCovertOpsBonus(Lieutenant? lieutenant)
        {
            if (!IsEffective(lieutenant))
                return 1.0f;

            float bonus = 0f;
            float levelMultiplier = GetLevelMultiplier(lieutenant!.Level);

            if (lieutenant.HasTrait(LieutenantTrait.Cunning))
                bonus += CunningCovertBonus;

            return 1.0f + (bonus * levelMultiplier);
        }

        /// <inheritdoc />
        public float GetAttackDeterrenceBonus(Lieutenant? lieutenant)
        {
            if (!IsEffective(lieutenant))
                return 1.0f;

            float bonus = 0f;
            float levelMultiplier = GetLevelMultiplier(lieutenant!.Level);

            if (lieutenant.HasTrait(LieutenantTrait.Intimidating))
                bonus += IntimidatingDeterrenceBonus;

            if (lieutenant.HasTrait(LieutenantTrait.Ruthless))
                bonus += RuthlessDeterrenceBonus;

            return 1.0f + (bonus * levelMultiplier);
        }

        /// <inheritdoc />
        public float GetExperienceGainBonus(Lieutenant? lieutenant)
        {
            if (!IsEffective(lieutenant))
                return 1.0f;

            float bonus = 0f;
            float levelMultiplier = GetLevelMultiplier(lieutenant!.Level);

            if (lieutenant.HasTrait(LieutenantTrait.Veteran))
                bonus += VeteranExperienceBonus;

            return 1.0f + (bonus * levelMultiplier);
        }

        /// <inheritdoc />
        public ZoneEffectsSummary GetAllZoneEffects(Lieutenant? lieutenant)
        {
            return new ZoneEffectsSummary
            {
                AttackBonus = GetAttackBonus(lieutenant),
                DefenseBonus = GetDefenseBonus(lieutenant),
                ResourceBonus = GetResourceBonus(lieutenant),
                LoyaltyBonus = GetLoyaltyBonus(lieutenant),
                IntelligenceBonus = GetIntelligenceBonus(lieutenant),
                CovertOpsBonus = GetCovertOpsBonus(lieutenant),
                AttackDeterrenceBonus = GetAttackDeterrenceBonus(lieutenant),
                ExperienceGainBonus = GetExperienceGainBonus(lieutenant)
            };
        }

        /// <summary>
        /// Checks if a lieutenant is effective (active and not null).
        /// Deceased or captured lieutenants provide no bonuses.
        /// </summary>
        private bool IsEffective(Lieutenant? lieutenant)
        {
            if (lieutenant == null)
                return false;

            // Only active lieutenants provide bonuses
            return lieutenant.Status == LieutenantStatus.Active;
        }

        /// <summary>
        /// Gets the level multiplier for bonuses.
        /// Higher level lieutenants get stronger bonuses.
        /// Formula: 1.0 + (0.1 * (level - 1))
        /// </summary>
        private float GetLevelMultiplier(int level)
        {
            return 1.0f + (LevelScalingFactor * (level - 1));
        }
    }
}
