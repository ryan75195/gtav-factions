using FactionWars.Core.Models;

namespace FactionWars.Persistence.Models
{
    public sealed class SavedFollowerState
    {
        public string FactionId { get; set; } = string.Empty;

        public DefenderRole Role { get; set; }

        public PlayerPosition Position { get; set; } = new PlayerPosition();

        public int VehicleSeatIndex { get; set; } = -1;
    }
}
