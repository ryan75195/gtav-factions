using FactionWars.Territory.Models;

namespace FactionWars.ScriptHookV.Managers.Interfaces
{
    /// <summary>UI-facing surface of <see cref="Managers.SupportSquadManager"/> for calling in a support squad.</summary>
    public interface ISupportSquadManager
    {
        /// <summary>True from the moment a squad is called until every ally is dead/streamed out.</summary>
        bool HasActiveSquad { get; }

        /// <summary>Calls the support squad into the given zone.</summary>
        void CallSupportSquad(Zone zone);
    }
}
