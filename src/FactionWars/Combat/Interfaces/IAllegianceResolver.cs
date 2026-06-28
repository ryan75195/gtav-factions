using FactionWars.Combat.Models;

namespace FactionWars.Combat.Interfaces
{
    /// <summary>
    /// Single source of truth for a zone combatant's relationship group, blip colour, and
    /// stance toward the player. Because colour and allegiance derive from the same faction
    /// identity, a combatant can never be presented in one faction's colour while being
    /// hostile in another's group.
    /// </summary>
    public interface IAllegianceResolver
    {
        CombatantProfile Resolve(string combatantFactionId, string playerFactionId);
    }
}
