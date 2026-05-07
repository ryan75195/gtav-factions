using System.Collections.Generic;

namespace FactionWars.Persistence.Models
{
    public sealed class RuntimeWorldState
    {
        public PlayerPosition PlayerPosition { get; set; } = new PlayerPosition();

        public SavedVehicleState? PlayerVehicle { get; set; }

        public List<SavedFollowerState> Followers { get; set; } = new List<SavedFollowerState>();
    }
}
