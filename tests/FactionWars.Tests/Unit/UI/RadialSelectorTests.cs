using FactionWars.UI.Models;
using Xunit;

namespace FactionWars.Tests.Unit.UI
{
    public class RadialSelectorTests
    {
        // Screen-space directions: +X right, +Y down. Segment 0 is centered at the top (12 o'clock);
        // indices increase clockwise. Three segments => 120 deg each (centers at 0, 120, 240 deg).
        private const float Deadzone = 0.25f;

        private static int Select(float x, float y) => RadialSelector.SelectIndex(3, x, y, Deadzone);

        [Fact]
        public void Up_SelectsTopSegment()
        {
            Assert.Equal(0, Select(0f, -1f));
        }

        [Fact]
        public void Right_SelectsSecondSegment()
        {
            // 90 deg clockwise from top falls in segment 1's arc (centered at 120 deg).
            Assert.Equal(1, Select(1f, 0f));
        }

        [Fact]
        public void LowerRight_SelectsSecondSegment()
        {
            // 135 deg -> segment 1 (centered at 120 deg).
            Assert.Equal(1, Select(1f, 1f));
        }

        [Fact]
        public void Down_SelectsThirdSegment()
        {
            // 180 deg -> nearest center is 120 (seg 1) at 60 deg vs 240 (seg 2) at 60 deg; the
            // standard-rounding boundary resolves the exact midpoint to segment 2.
            Assert.Equal(2, Select(0f, 1f));
        }

        [Fact]
        public void Left_SelectsThirdSegment()
        {
            // 270 deg -> segment 2 (centered at 240 deg).
            Assert.Equal(2, Select(-1f, 0f));
        }

        [Fact]
        public void WrapsPastTop_BackToZero()
        {
            // Just counter-clockwise of straight up wraps around to segment 0.
            Assert.Equal(0, Select(-0.01f, -1f));
        }

        [Fact]
        public void InsideDeadzone_SelectsNothing()
        {
            // magnitude ~0.14 < 0.25 deadzone.
            Assert.Equal(-1, Select(0.1f, -0.1f));
        }

        [Fact]
        public void NonPositiveSegmentCount_SelectsNothing()
        {
            Assert.Equal(-1, RadialSelector.SelectIndex(0, 0f, -1f, Deadzone));
        }
    }
}
