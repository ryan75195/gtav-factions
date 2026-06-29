using System.Collections.Generic;
using FactionWars.Combat.Models;

namespace FactionWars.Combat.Interfaces
{
    /// <summary>Selects the drivers of ambient (non-persistent) vehicles to evict so they stop
    /// driving through an active battle, leaving the player's and mod-spawned vehicles alone.</summary>
    public interface IAmbientTrafficSuppressor
    {
        IReadOnlyList<int> SelectDriversToEvict(IReadOnlyList<VehicleSnapshot> vehicles, int playerVehicleHandle);
    }
}
