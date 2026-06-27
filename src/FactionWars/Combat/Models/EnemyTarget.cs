using FactionWars.Core.Interfaces;

namespace FactionWars.Combat.Models
{
    /// <summary>A known hostile ped's live world position, input to target assignment.</summary>
    public readonly struct EnemyTarget
    {
        public int Handle { get; }
        public Vector3 Position { get; }

        public EnemyTarget(int handle, Vector3 position)
        {
            Handle = handle;
            Position = position;
        }
    }
}
