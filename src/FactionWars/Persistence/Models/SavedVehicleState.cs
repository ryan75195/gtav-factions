namespace FactionWars.Persistence.Models
{
    public sealed class SavedVehicleState
    {
        public string ModelName { get; set; } = string.Empty;

        public PlayerPosition Position { get; set; } = new PlayerPosition();
    }
}
