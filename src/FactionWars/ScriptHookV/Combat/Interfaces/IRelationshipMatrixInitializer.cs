using System.Collections.Generic;

namespace FactionWars.ScriptHookV.Combat.Interfaces
{
    /// <summary>
    /// Establishes every GTA relationship-group pairing once (at load and on character
    /// switch), so combatant allegiance is decided by faction-group membership rather than
    /// ad-hoc per-spawn mutation. Faction groups hate each other; the player's faction is a
    /// companion of the PLAYER group while rival factions hate it.
    /// </summary>
    public interface IRelationshipMatrixInitializer
    {
        void Initialize(string playerFactionId, IReadOnlyList<string> allFactionIds);
    }
}
