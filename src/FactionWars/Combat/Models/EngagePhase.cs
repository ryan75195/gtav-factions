namespace FactionWars.Combat.Models
{
    /// <summary>Per-follower combat phase in Search &amp; Destroy.</summary>
    public enum EngagePhase
    {
        /// <summary>Running toward the assigned enemy; not yet in range/LOS.</summary>
        Advance,

        /// <summary>In weapon range with line of sight; fighting the assigned enemy.</summary>
        Engage
    }
}
