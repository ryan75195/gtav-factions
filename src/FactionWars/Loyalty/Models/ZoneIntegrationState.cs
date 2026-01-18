using System;

namespace FactionWars.Loyalty.Models
{
    /// <summary>
    /// Tracks the integration state of a captured zone into the controlling faction.
    /// Integration progress affects resource production and defense capabilities.
    /// </summary>
    public class ZoneIntegrationState : IEquatable<ZoneIntegrationState>
    {
        private int _integrationProgress;

        private const int MinProgress = 0;
        private const int MaxProgress = 100;
        private const int DefaultTransferCount = 1;

        /// <summary>
        /// The ID of the zone being integrated.
        /// </summary>
        public string ZoneId { get; }

        /// <summary>
        /// The ID of the faction that now controls the zone.
        /// </summary>
        public string NewControllerFactionId { get; }

        /// <summary>
        /// The ID of the faction that previously controlled the zone.
        /// </summary>
        public string PreviousControllerFactionId { get; }

        /// <summary>
        /// Current integration progress (0-100).
        /// </summary>
        public int IntegrationProgress
        {
            get => _integrationProgress;
            private set => _integrationProgress = Math.Max(MinProgress, Math.Min(MaxProgress, value));
        }

        /// <summary>
        /// Number of days since the zone was captured.
        /// </summary>
        public int DaysSinceCapture { get; private set; }

        /// <summary>
        /// The base difficulty level for integrating this zone.
        /// </summary>
        public IntegrationDifficulty BaseDifficulty { get; }

        /// <summary>
        /// Number of times control has been transferred for this zone.
        /// </summary>
        public int TransferCount { get; }

        /// <summary>
        /// Indicates whether the zone is fully integrated (100% progress).
        /// </summary>
        public bool IsFullyIntegrated => IntegrationProgress >= MaxProgress;

        /// <summary>
        /// Creates a new zone integration state.
        /// </summary>
        /// <param name="zoneId">The ID of the zone.</param>
        /// <param name="newControllerFactionId">The ID of the new controlling faction.</param>
        /// <param name="previousControllerFactionId">The ID of the previous controlling faction.</param>
        /// <param name="initialProgress">Initial integration progress (default 0).</param>
        /// <param name="baseDifficulty">The base difficulty level (default Moderate).</param>
        /// <param name="transferCount">Number of times control has been transferred (default 1).</param>
        public ZoneIntegrationState(
            string zoneId,
            string newControllerFactionId,
            string previousControllerFactionId,
            int initialProgress = 0,
            IntegrationDifficulty baseDifficulty = IntegrationDifficulty.Moderate,
            int transferCount = DefaultTransferCount)
        {
            ValidateString(zoneId, nameof(zoneId));
            ValidateString(newControllerFactionId, nameof(newControllerFactionId));
            ValidateString(previousControllerFactionId, nameof(previousControllerFactionId));

            ZoneId = zoneId;
            NewControllerFactionId = newControllerFactionId;
            PreviousControllerFactionId = previousControllerFactionId;
            IntegrationProgress = initialProgress;
            BaseDifficulty = baseDifficulty;
            TransferCount = Math.Max(1, transferCount);
            DaysSinceCapture = 0;
        }

        private static void ValidateString(string value, string paramName)
        {
            if (value == null)
                throw new ArgumentNullException(paramName);
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException($"{paramName} cannot be empty or whitespace.", paramName);
        }

        /// <summary>
        /// Advances the day counter by one.
        /// </summary>
        public void AdvanceDay()
        {
            DaysSinceCapture++;
        }

        /// <summary>
        /// Adds progress to the integration.
        /// The value is clamped to 100.
        /// </summary>
        /// <param name="amount">Amount of progress to add.</param>
        public void AddProgress(int amount)
        {
            if (amount > 0)
                IntegrationProgress += amount;
        }

        /// <summary>
        /// Reduces progress from the integration.
        /// The value is clamped to 0.
        /// </summary>
        /// <param name="amount">Amount of progress to reduce.</param>
        public void ReduceProgress(int amount)
        {
            if (amount > 0)
                IntegrationProgress -= amount;
        }

        public bool Equals(ZoneIntegrationState? other)
        {
            if (other is null) return false;
            return ZoneId == other.ZoneId;
        }

        public override bool Equals(object? obj)
        {
            return obj is ZoneIntegrationState state && Equals(state);
        }

        public override int GetHashCode()
        {
            return ZoneId.GetHashCode();
        }

        public static bool operator ==(ZoneIntegrationState? left, ZoneIntegrationState? right)
        {
            if (left is null) return right is null;
            return left.Equals(right);
        }

        public static bool operator !=(ZoneIntegrationState? left, ZoneIntegrationState? right)
        {
            return !(left == right);
        }

        public override string ToString()
        {
            return $"ZoneIntegrationState[{ZoneId}]: {IntegrationProgress}% ({BaseDifficulty})";
        }
    }
}
