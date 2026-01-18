using System;
using FactionWars.Loyalty.Interfaces;
using FactionWars.Loyalty.Models;

namespace FactionWars.Loyalty.Services
{
    /// <summary>
    /// Service for managing insurgency risk and events in zones.
    /// </summary>
    public class InsurgencyService : IInsurgencyService
    {
        // Risk increase amounts by loyalty level
        private const int HostileRiskIncrease = 20;
        private const int ResistantRiskIncrease = 8;

        // Risk reduction amounts by loyalty level
        private const int SupportiveRiskReduction = 5;
        private const int FanaticalRiskReduction = 10;

        // Uprising strength constants
        private const int MinUprisingStrength = 5;
        private const int StrengthScaleFactor = 10;

        /// <inheritdoc />
        public int CalculateRiskFromLoyalty(ZoneLoyalty loyalty)
        {
            if (loyalty == null)
                throw new ArgumentNullException(nameof(loyalty));

            return loyalty.Level switch
            {
                LoyaltyLevel.Hostile => HostileRiskIncrease,
                LoyaltyLevel.Resistant => ResistantRiskIncrease,
                _ => 0
            };
        }

        /// <inheritdoc />
        public int CalculateRiskReduction(ZoneLoyalty loyalty)
        {
            if (loyalty == null)
                throw new ArgumentNullException(nameof(loyalty));

            return loyalty.Level switch
            {
                LoyaltyLevel.Supportive => SupportiveRiskReduction,
                LoyaltyLevel.Fanatical => FanaticalRiskReduction,
                _ => 0
            };
        }

        /// <inheritdoc />
        public void UpdateDailyRisk(InsurgencyRisk risk, ZoneLoyalty loyalty)
        {
            if (risk == null)
                throw new ArgumentNullException(nameof(risk));
            if (loyalty == null)
                throw new ArgumentNullException(nameof(loyalty));

            int riskIncrease = CalculateRiskFromLoyalty(loyalty);
            int riskReduction = CalculateRiskReduction(loyalty);

            int netChange = riskIncrease - riskReduction;
            if (netChange != 0)
            {
                risk.AdjustRisk(netChange);
            }

            risk.AdvanceDay();
        }

        /// <inheritdoc />
        public bool CheckForUprising(InsurgencyRisk risk, float rollValue)
        {
            if (risk == null)
                throw new ArgumentNullException(nameof(risk));

            // Reset day counter on check
            risk.ResetDayCounter();

            // No uprising possible at None risk level
            if (risk.Level == InsurgencyLevel.None)
                return false;

            return rollValue < risk.UprisingChance;
        }

        /// <inheritdoc />
        public void ApplySuppressionEffect(InsurgencyRisk risk, int suppressionStrength)
        {
            if (risk == null)
                throw new ArgumentNullException(nameof(risk));
            if (suppressionStrength < 0)
                throw new ArgumentOutOfRangeException(nameof(suppressionStrength), "Suppression strength cannot be negative.");

            risk.AdjustRisk(-suppressionStrength);
        }

        /// <inheritdoc />
        public InsurgencyEvent CreateUprisingEvent(InsurgencyRisk risk)
        {
            if (risk == null)
                throw new ArgumentNullException(nameof(risk));
            if (risk.InsurgentFactionId == null)
                throw new InvalidOperationException("Cannot create uprising event without an insurgent faction.");

            return new InsurgencyEvent(
                risk.ZoneId,
                risk.ControllingFactionId,
                risk.InsurgentFactionId,
                InsurgencyEventType.Uprising);
        }

        /// <inheritdoc />
        public int CalculateUprisingStrength(InsurgencyRisk risk)
        {
            if (risk == null)
                throw new ArgumentNullException(nameof(risk));

            // Base strength scales with risk level
            int baseStrength = risk.RiskLevel / StrengthScaleFactor;

            // Ensure minimum strength
            return Math.Max(MinUprisingStrength, baseStrength);
        }
    }
}
