using FactionWars.Combat.Models;

namespace FactionWars.Combat.Interfaces
{
    /// <summary>
    /// Single owner of per-ped native tasking. Producers submit a desired <see cref="PedIntent"/>
    /// each tick; the reconciler applies it via the game bridge only when it differs from what was
    /// last applied to that ped, so no system re-issues an unchanged task (the per-frame thrash that
    /// drives the engine-side pathfinding stall).
    /// </summary>
    public interface IPedIntentReconciler
    {
        /// <summary>Applies <paramref name="intent"/> to the ped iff it changed since the last submit.</summary>
        void Submit(int pedHandle, PedIntent intent);

        /// <summary>Drops one ped's last-applied state (e.g. on despawn) so its next submit re-applies.</summary>
        void Forget(int pedHandle);

        /// <summary>Drops all last-applied state (e.g. on a stance change) so every next submit re-applies.</summary>
        void Clear();
    }
}
