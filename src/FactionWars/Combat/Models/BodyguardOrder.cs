using FactionWars.Core.Interfaces;

namespace FactionWars.Combat.Models
{
    /// <summary>
    /// One bodyguard's intent for the current tick. Pure data - no native dependency.
    /// </summary>
    public readonly struct BodyguardOrder
    {
        public BodyguardOrderKind Kind { get; }
        public Vector3 Point { get; }
        public float Radius { get; }
        public int TargetHandle { get; }

        private BodyguardOrder(BodyguardOrderKind kind, Vector3 point, float radius, int targetHandle)
        {
            Kind = kind;
            Point = point;
            Radius = radius;
            TargetHandle = targetHandle;
        }

        public static BodyguardOrder FollowPlayer()
            => new BodyguardOrder(BodyguardOrderKind.FollowPlayer, Vector3.Zero, 0f, 0);

        public static BodyguardOrder HoldAtPoint(Vector3 point)
            => new BodyguardOrder(BodyguardOrderKind.HoldAtPoint, point, 0f, 0);

        public static BodyguardOrder SeekInRadius(Vector3 center, float radius)
            => new BodyguardOrder(BodyguardOrderKind.SeekInRadius, center, radius, 0);

        public static BodyguardOrder AttackTarget(int targetHandle)
            => new BodyguardOrder(BodyguardOrderKind.AttackTarget, Vector3.Zero, 0f, targetHandle);
    }
}
