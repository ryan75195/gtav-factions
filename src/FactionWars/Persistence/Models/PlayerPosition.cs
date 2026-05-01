namespace FactionWars.Persistence.Models
{
    /// <summary>
    /// Player position and heading at save time. Recorded in the sidecar but
    /// not consumed by load logic in v1 — reserved for a future "restore position" feature.
    /// </summary>
    public sealed class PlayerPosition
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
        public float Heading { get; set; }
    }
}
