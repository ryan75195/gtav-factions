using System;
using FactionWars.Core.Interfaces;

namespace FactionWars.ScriptHookV.Services
{
    /// <summary>
    /// Pure-logic helper for the zone defender leash. Decides whether a ped
    /// has strayed too far from its zone and picks a sensible point to send
    /// it back to. Stateless — callers pass current positions in.
    /// </summary>
    public static class ZoneLeashEnforcer
    {
        /// <summary>
        /// Defenders past zoneRadius * this multiplier are leashed back.
        /// Above 1.0 to give hysteresis: peds legitimately walking near the
        /// boundary don't yo-yo back and forth.
        /// </summary>
        public const float LeashThresholdMultiplier = 1.2f;

        /// <summary>
        /// Leashed defenders are sent to a random point inside
        /// zoneRadius * this multiplier (the inner half of the zone) so
        /// multiple yanked peds don't pile up on the exact center.
        /// </summary>
        public const float LeashReturnRadiusMultiplier = 0.5f;

        /// <summary>
        /// How often each manager runs the leash sweep, in milliseconds.
        /// </summary>
        public const int LeashCheckIntervalMs = 2000;

        /// <summary>
        /// Returns true if the ped is far enough outside the zone that it
        /// should be retasked back inside.
        /// </summary>
        public static bool ShouldLeash(Vector3 pedPos, Vector3 zoneCenter, float zoneRadius)
        {
            float threshold = zoneRadius * LeashThresholdMultiplier;
            float dx = pedPos.X - zoneCenter.X;
            float dy = pedPos.Y - zoneCenter.Y;
            float dz = pedPos.Z - zoneCenter.Z;
            float distSq = dx * dx + dy * dy + dz * dz;
            return distSq > threshold * threshold;
        }

        /// <summary>
        /// Picks a random point inside zoneRadius * LeashReturnRadiusMultiplier
        /// of the zone center. Z is preserved from the center so peds don't
        /// end up underground or in the air. Uses sqrt-of-uniform for the
        /// radial distance so points are uniformly distributed in the disk
        /// (not biased toward the center).
        /// </summary>
        public static Vector3 PickReturnPoint(Vector3 zoneCenter, float zoneRadius, Random rng)
        {
            if (rng == null)
                throw new ArgumentNullException(nameof(rng));

            float maxRadius = zoneRadius * LeashReturnRadiusMultiplier;
            float angle = (float)(rng.NextDouble() * 2.0 * Math.PI);
            float dist = maxRadius * (float)Math.Sqrt(rng.NextDouble());

            return new Vector3(
                zoneCenter.X + dist * (float)Math.Cos(angle),
                zoneCenter.Y + dist * (float)Math.Sin(angle),
                zoneCenter.Z);
        }
    }
}
