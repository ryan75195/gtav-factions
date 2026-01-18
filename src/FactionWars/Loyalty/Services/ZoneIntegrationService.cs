using FactionWars.Loyalty.Interfaces;
using FactionWars.Loyalty.Models;
using System;

namespace FactionWars.Loyalty.Services
{
    /// <summary>
    /// Service responsible for managing zone integration after capture.
    /// Handles difficulty calculation, progress tracking, and penalty application.
    /// </summary>
    public class ZoneIntegrationService : IZoneIntegrationService
    {
        // Daily progress rates by difficulty (percentage points per day)
        private const int EasyProgressRate = 8;
        private const int ModerateProgressRate = 5;
        private const int ChallengingProgressRate = 3;
        private const int SevereProgressRate = 2;
        private const int ExtremeProgressRate = 1;

        // Resource penalty settings
        private const float MinResourceMultiplier = 0.25f;
        private const float MaxResourceMultiplier = 1.0f;

        // Loyalty thresholds for difficulty calculation
        private const int HighLoyaltyThreshold = 60;
        private const int NeutralLoyaltyThreshold = 40;
        private const int ResistantLoyaltyThreshold = 25;

        // Transfer count thresholds
        private const int ManyTransfersThreshold = 3;

        // Insurgency setback values
        private const int LowInsurgencySetback = 5;
        private const int MediumInsurgencySetback = 10;
        private const int HighInsurgencySetback = 20;
        private const int CriticalInsurgencySetback = 35;

        // Loyalty sync thresholds
        private const int LoyaltyProgressThreshold = 25;
        private const int FullIntegrationLoyaltyBonus = 30;
        private const int SupportiveLoyaltyMin = 60;

        // Defense bonus settings
        private const int NeutralProgressPoint = 50;
        private const int MaxDefenseBonus = 15;
        private const int MaxDefensePenalty = -15;

        /// <inheritdoc />
        public IntegrationDifficulty CalculateDifficulty(ZoneLoyalty loyalty, int transferCount)
        {
            if (loyalty == null)
                throw new ArgumentNullException(nameof(loyalty));

            // Base difficulty from loyalty level
            IntegrationDifficulty baseDifficulty;

            if (loyalty.LoyaltyValue >= HighLoyaltyThreshold)
            {
                baseDifficulty = IntegrationDifficulty.Easy;
            }
            else if (loyalty.LoyaltyValue >= NeutralLoyaltyThreshold)
            {
                baseDifficulty = IntegrationDifficulty.Moderate;
            }
            else if (loyalty.LoyaltyValue >= ResistantLoyaltyThreshold)
            {
                baseDifficulty = IntegrationDifficulty.Challenging;
            }
            else
            {
                baseDifficulty = IntegrationDifficulty.Severe;
            }

            // Increase difficulty for multiple transfers
            if (transferCount >= ManyTransfersThreshold)
            {
                baseDifficulty = (IntegrationDifficulty)Math.Min((int)baseDifficulty + 1, (int)IntegrationDifficulty.Extreme);
            }

            // Extreme case: hostile loyalty with many transfers
            if (loyalty.LoyaltyValue < ResistantLoyaltyThreshold && transferCount >= ManyTransfersThreshold)
            {
                baseDifficulty = IntegrationDifficulty.Extreme;
            }

            return baseDifficulty;
        }

        /// <inheritdoc />
        public int CalculateDailyProgress(ZoneIntegrationState state)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));

            return state.BaseDifficulty switch
            {
                IntegrationDifficulty.Easy => EasyProgressRate,
                IntegrationDifficulty.Moderate => ModerateProgressRate,
                IntegrationDifficulty.Challenging => ChallengingProgressRate,
                IntegrationDifficulty.Severe => SevereProgressRate,
                IntegrationDifficulty.Extreme => ExtremeProgressRate,
                _ => ModerateProgressRate
            };
        }

        /// <inheritdoc />
        public void ApplyDailyProgress(ZoneIntegrationState state)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));

            int progress = CalculateDailyProgress(state);
            state.AddProgress(progress);
            state.AdvanceDay();
        }

        /// <inheritdoc />
        public float CalculateResourcePenalty(ZoneIntegrationState state)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));

            // Linear interpolation from min (at 0%) to max (at 100%)
            // Formula: min + (progress/100) * (max - min)
            float progressRatio = state.IntegrationProgress / 100f;
            return MinResourceMultiplier + progressRatio * (MaxResourceMultiplier - MinResourceMultiplier);
        }

        /// <inheritdoc />
        public void ApplyInsurgencySetback(ZoneIntegrationState state, InsurgencyLevel insurgencyLevel)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));

            int setback = insurgencyLevel switch
            {
                InsurgencyLevel.None => 0,
                InsurgencyLevel.Low => LowInsurgencySetback,
                InsurgencyLevel.Medium => MediumInsurgencySetback,
                InsurgencyLevel.High => HighInsurgencySetback,
                InsurgencyLevel.Critical => CriticalInsurgencySetback,
                _ => 0
            };

            state.ReduceProgress(setback);
        }

        /// <inheritdoc />
        public ZoneIntegrationState CreateIntegrationState(ZoneLoyalty loyalty)
        {
            if (loyalty == null)
                throw new ArgumentNullException(nameof(loyalty));

            if (string.IsNullOrEmpty(loyalty.PreviousFactionId))
                throw new InvalidOperationException("Cannot create integration state without a previous controlling faction.");

            var difficulty = CalculateDifficulty(loyalty, loyalty.TransferCount);

            return new ZoneIntegrationState(
                loyalty.ZoneId,
                loyalty.ControllingFactionId,
                loyalty.PreviousFactionId,
                initialProgress: 0,
                baseDifficulty: difficulty,
                transferCount: loyalty.TransferCount);
        }

        /// <inheritdoc />
        public void UpdateLoyaltyFromIntegration(ZoneLoyalty loyalty, ZoneIntegrationState state)
        {
            if (loyalty == null)
                throw new ArgumentNullException(nameof(loyalty));
            if (state == null)
                throw new ArgumentNullException(nameof(state));

            // Only update loyalty once integration has made significant progress
            if (state.IntegrationProgress < LoyaltyProgressThreshold)
                return;

            // Full integration guarantees at least Supportive level
            if (state.IsFullyIntegrated)
            {
                int targetLoyalty = Math.Max(loyalty.LoyaltyValue + FullIntegrationLoyaltyBonus, SupportiveLoyaltyMin);
                loyalty.SetLoyalty(targetLoyalty);
                return;
            }

            // Partial bonus for high integration progress
            if (state.IntegrationProgress >= 50)
            {
                int bonusAmount = (state.IntegrationProgress - 50) / 10; // +1 for every 10% above 50%
                loyalty.AdjustLoyalty(bonusAmount);
            }
        }

        /// <inheritdoc />
        public int CalculateDefenseBonus(ZoneIntegrationState state)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));

            // Linear scale: -15 at 0%, 0 at 50%, +15 at 100%
            int deviation = state.IntegrationProgress - NeutralProgressPoint;

            // Scale: each point of deviation from 50 gives 0.3% bonus/penalty
            return (int)(deviation * (MaxDefenseBonus / 50.0));
        }
    }
}
