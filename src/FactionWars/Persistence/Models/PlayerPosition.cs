namespace FactionWars.Persistence.Models
{
    /// <summary>
    /// Player position and heading at save time.
    /// </summary>
    public sealed class PlayerPosition
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
        public float Heading { get; set; }
    }
}
