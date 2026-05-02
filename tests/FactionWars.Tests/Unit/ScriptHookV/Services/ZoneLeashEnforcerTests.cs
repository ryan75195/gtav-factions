using System;
using FactionWars.Core.Interfaces;
using FactionWars.ScriptHookV.Services;
using Xunit;

namespace FactionWars.Tests.Unit.ScriptHookV.Services
{
    public class ZoneLeashEnforcerTests
    {
        private static readonly Vector3 ZoneCenter = new Vector3(100f, 200f, 0f);
        private const float ZoneRadius = 150f;

        [Fact]
        public void ShouldLeash_AtZoneCenter_ReturnsFalse()
        {
            Assert.False(ZoneLeashEnforcer.ShouldLeash(ZoneCenter, ZoneCenter, ZoneRadius));
        }

        [Fact]
        public void ShouldLeash_AtBoundary_ReturnsFalse()
        {
            var pedPos = new Vector3(ZoneCenter.X + ZoneRadius, ZoneCenter.Y, ZoneCenter.Z);

            Assert.False(ZoneLeashEnforcer.ShouldLeash(pedPos, ZoneCenter, ZoneRadius));
        }

        [Fact]
        public void ShouldLeash_JustInsideHysteresisBand_ReturnsFalse()
        {
            var pedPos = new Vector3(ZoneCenter.X + ZoneRadius * 1.19f, ZoneCenter.Y, ZoneCenter.Z);

            Assert.False(ZoneLeashEnforcer.ShouldLeash(pedPos, ZoneCenter, ZoneRadius));
        }

        [Fact]
        public void ShouldLeash_JustPastHysteresisBand_ReturnsTrue()
        {
            var pedPos = new Vector3(ZoneCenter.X + ZoneRadius * 1.21f, ZoneCenter.Y, ZoneCenter.Z);

            Assert.True(ZoneLeashEnforcer.ShouldLeash(pedPos, ZoneCenter, ZoneRadius));
        }

        [Fact]
        public void ShouldLeash_FarOutside_ReturnsTrue()
        {
            var pedPos = new Vector3(ZoneCenter.X + ZoneRadius * 5f, ZoneCenter.Y, ZoneCenter.Z);

            Assert.True(ZoneLeashEnforcer.ShouldLeash(pedPos, ZoneCenter, ZoneRadius));
        }

        [Fact]
        public void PickReturnPoint_AlwaysWithinReturnRadius()
        {
            var rng = new Random(12345);

            for (int i = 0; i < 200; i++)
            {
                var point = ZoneLeashEnforcer.PickReturnPoint(ZoneCenter, ZoneRadius, rng);
                float dx = point.X - ZoneCenter.X;
                float dy = point.Y - ZoneCenter.Y;
                float dist = (float)Math.Sqrt(dx * dx + dy * dy);

                float maxDist = ZoneRadius * ZoneLeashEnforcer.LeashReturnRadiusMultiplier;
                Assert.True(dist <= maxDist + 0.01f,
                    $"iter {i}: dist={dist:F2} exceeded {maxDist:F2}");
            }
        }

        [Fact]
        public void PickReturnPoint_PreservesCenterZ()
        {
            var rng = new Random(99);
            var center = new Vector3(0f, 0f, 50f);

            var point = ZoneLeashEnforcer.PickReturnPoint(center, 100f, rng);

            Assert.Equal(50f, point.Z);
        }

        [Fact]
        public void PickReturnPoint_DeterministicGivenSameRng()
        {
            var rng1 = new Random(42);
            var rng2 = new Random(42);

            var p1 = ZoneLeashEnforcer.PickReturnPoint(ZoneCenter, ZoneRadius, rng1);
            var p2 = ZoneLeashEnforcer.PickReturnPoint(ZoneCenter, ZoneRadius, rng2);

            Assert.Equal(p1, p2);
        }
    }
}
