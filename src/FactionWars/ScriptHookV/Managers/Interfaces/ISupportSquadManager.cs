using FactionWars.Territory.Models;

namespace FactionWars.ScriptHookV.Managers.Interfaces
{
    /// <summary>UI-facing surface of <see cref="Managers.SupportSquadManager"/> for calling in a support squad.</summary>
    public interface ISupportSquadManager
    {
        /// <summary>True from the moment a squad is called until every ally is dead/streamed out.</summary>
        bool HasActiveSquad { get; }

        /// <summary>
        /// Calls the support squad into the given zone. Returns true if a squad was actually
        /// spawned (so callers can consume a package only on success), false if a squad was
        /// already active or the spawn attempt failed.
        /// </summary>
        bool CallSupportSquad(Zone zone);
    }
}
