using System;
using FactionWars.Core.Interfaces;

namespace FactionWars.Combat.Interfaces
{
    /// <summary>Picks a high-ground perch position near a center point.</summary>
    public interface IPerchResolver
    {
        /// <summary>
        /// Samples a ring of <paramref name="sampleCount"/> points at
        /// <paramref name="searchRadius"/> around <paramref name="center"/>
        /// (plus the center itself) and returns the one with the greatest
        /// height as reported by <paramref name="heightAt"/>. Returns the
        /// center when no sample is higher.
        /// </summary>
        Vector3 Resolve(Vector3 center, float searchRadius, int sampleCount, Func<float, float, float> heightAt);
    }
}
