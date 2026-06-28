using System;
using FactionWars.Combat.Services;
using FactionWars.Core.Interfaces;
using Xunit;

namespace FactionWars.Tests.Unit.Combat
{
    public class PerchResolverTests
    {
        private readonly PerchResolver _resolver = new PerchResolver();

        [Fact]
        public void Resolve_PicksHighestSampledPoint()
        {
            var center = new Vector3(100f, 100f, 0f);
            // Height is high only near x = 125 (east sample at radius 25).
            float HeightAt(float x, float y) => Math.Abs(x - 125f) < 0.5f ? 40f : 5f;

            var perch = _resolver.Resolve(center, 25f, 8, HeightAt);

            Assert.Equal(125f, perch.X, 3);
            Assert.Equal(100f, perch.Y, 3);
            Assert.Equal(40f, perch.Z, 3);
        }

        [Fact]
        public void Resolve_NoHighGround_FallsBackToCenter()
        {
            var center = new Vector3(0f, 0f, 10f);
            float HeightAt(float x, float y) => 3f; // everything lower than center's own sample

            var perch = _resolver.Resolve(center, 25f, 8, HeightAt);

            Assert.Equal(0f, perch.X, 3);
            Assert.Equal(0f, perch.Y, 3);
            Assert.Equal(3f, perch.Z, 3); // center sampled height
        }
    }
}
