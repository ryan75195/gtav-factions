using System.Collections.Generic;

namespace FactionWars.ScriptHookV.Managers.Interfaces
{
    /// <summary>
    /// Exposes the live hostile ped handles a manager currently tracks, so squad
    /// Search &amp; Destroy can target known enemies.
    /// </summary>
    public interface IHostilePedHandleSource
    {
        IReadOnlyList<int> GetHostilePedHandles();
    }
}
