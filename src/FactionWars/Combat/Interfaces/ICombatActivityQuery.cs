namespace FactionWars.Combat.Interfaces
{
    /// <summary>
    /// Read-only query for "is the player currently in an active battle?".
    /// Implemented by <see cref="ZoneBattleCombatActivityAdapter"/> over
    /// <see cref="IZoneBattleManager"/>. Used by DefenderRallyController as one of
    /// the composite "should defenders rally?" signals.
    /// </summary>
    public interface ICombatActivityQuery
    {
        /// <summary>True if the player is currently a participant in any active zone battle.</summary>
        bool HasActiveEncounter { get; }
    }
}
