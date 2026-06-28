using FactionWars.Combat.Models;
using FactionWars.Core.Interfaces;

namespace FactionWars.ScriptHookV.Combat.Interfaces
{
    /// <summary>
    /// The single site that spawns a zone combatant. Derives relationship group, blip colour,
    /// and friend/foe from one faction identity (via <see cref="Combat.Interfaces.IAllegianceResolver"/>),
    /// so a ped can never wear one faction's colour while fighting in another's group.
    /// Performs no relationship-matrix mutation — that is wired once at init.
    /// </summary>
    public interface IZoneCombatantSpawner
    {
        PedHandle Spawn(string factionId, string playerFactionId, string model, Vector3 position, string zoneId);
    }
}
