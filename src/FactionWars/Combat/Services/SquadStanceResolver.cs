using FactionWars.Combat.Interfaces;
using FactionWars.Combat.Models;
using FactionWars.Core.Interfaces;

namespace FactionWars.Combat.Services
{
    public class SquadStanceResolver : ISquadStanceResolver
    {
        private const float RingFraction = 0.6f;

        public BodyguardOrder Resolve(SquadStance stance, Vector3 anchorCenter, float anchorRadius, int bodyguardIndex, int bodyguardCount)
        {
            switch (stance)
            {
                case SquadStance.HoldArea:
                    return BodyguardOrder.HoldAtPoint(RingPoint(anchorCenter, anchorRadius, bodyguardIndex, bodyguardCount));
                case SquadStance.SearchAndDestroy:
                    return BodyguardOrder.SeekInRadius(anchorCenter, anchorRadius);
                default:
                    return BodyguardOrder.FollowPlayer();
            }
        }

        private static Vector3 RingPoint(Vector3 center, float radius, int index, int count)
        {
            if (count <= 0) count = 1;
            double angle = 2.0 * System.Math.PI * index / count;
            float r = radius * RingFraction;
            float x = center.X + (float)(r * System.Math.Cos(angle));
            float y = center.Y + (float)(r * System.Math.Sin(angle));
            return new Vector3(x, y, center.Z);
        }
    }
}
