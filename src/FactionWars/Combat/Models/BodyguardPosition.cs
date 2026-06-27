using FactionWars.Core.Interfaces;

namespace FactionWars.Combat.Models
{
    /// <summary>A bodyguard's live world position, input to target assignment.</summary>
    public readonly struct BodyguardPosition
    {
        public int Handle { get; }
        public Vector3 Position { get; }

        public BodyguardPosition(int handle, Vector3 position)
        {
            Handle = handle;
            Position = position;
        }
    }
}
