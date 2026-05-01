namespace FactionWars.Combat.Interfaces
{
    /// <summary>
    /// Read-only query for "is there an active mod-managed combat encounter?".
    /// Implemented by CombatManager. Used by DefenderRallyController as one of the
    /// composite "should defenders rally?" signals.
    /// </summary>
    public interface ICombatActivityQuery
    {
        /// <summary>True if a CombatEncounter is currently active.</summary>
        bool HasActiveEncounter { get; }
    }
}
