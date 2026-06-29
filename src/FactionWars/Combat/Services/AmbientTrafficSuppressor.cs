using System.Collections.Generic;
using FactionWars.Combat.Interfaces;
using FactionWars.Combat.Models;

namespace FactionWars.Combat.Services
{
    /// <summary>Evicts the driver of every non-persistent, occupied vehicle except the player's:
    /// ambient traffic stops driving while the mod's and the player's own vehicles are untouched.</summary>
    public sealed class AmbientTrafficSuppressor : IAmbientTrafficSuppressor
    {
        public IReadOnlyList<int> SelectDriversToEvict(IReadOnlyList<VehicleSnapshot> vehicles, int playerVehicleHandle)
        {
            var drivers = new List<int>();
            foreach (var v in vehicles)
            {
                if (v.IsPersistent || v.Driver == -1 || v.Handle == playerVehicleHandle)
                {
                    continue;
                }

                drivers.Add(v.Driver);
            }

            return drivers;
        }
    }
}
