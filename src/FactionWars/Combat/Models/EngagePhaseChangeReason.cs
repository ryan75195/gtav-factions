namespace FactionWars.Combat.Models
{
    /// <summary>Why <see cref="FactionWars.Combat.Services.SquadEngagementResolver"/> chose its phase
    /// this tick. A non-<see cref="None"/> value occurs exactly when the phase changed, so it doubles
    /// as the trigger and label for an engagement transition event.</summary>
    public enum EngagePhaseChangeReason
    {
        /// <summary>Phase unchanged this tick (held Advance or held Engage).</summary>
        None = 0,

        /// <summary>Advance -> Engage: in range with line of sight.</summary>
        EngageAcquired,

        /// <summary>Engage -> Advance: target left the hysteresis band.</summary>
        RangeBroken,

        /// <summary>Engage -> Advance: line of sight stayed broken; push for a new vantage.</summary>
        LosReposition
    }
}
