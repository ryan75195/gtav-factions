using System;
using FactionWars.Combat.Interfaces;
using FactionWars.Core.Interfaces;

namespace FactionWars.Combat.Services
{
    /// <inheritdoc />
    public sealed class PerchResolver : IPerchResolver
    {
        public Vector3 Resolve(Vector3 center, float searchRadius, int sampleCount, Func<float, float, float> heightAt)
        {
            if (heightAt == null)
                throw new ArgumentNullException(nameof(heightAt));

            float bestX = center.X;
            float bestY = center.Y;
            float bestZ = heightAt(center.X, center.Y);

            for (int i = 0; i < sampleCount; i++)
            {
                double angle = 2.0 * Math.PI * i / sampleCount;
                float x = center.X + (float)Math.Cos(angle) * searchRadius;
                float y = center.Y + (float)Math.Sin(angle) * searchRadius;
                float z = heightAt(x, y);
                if (z > bestZ)
                {
                    bestX = x;
                    bestY = y;
                    bestZ = z;
                }
            }

            return new Vector3(bestX, bestY, bestZ);
        }
    }
}
