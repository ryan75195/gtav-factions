using FactionWars.UI.Models;
using Xunit;

namespace FactionWars.Tests.Unit.UI
{
    /// <summary>
    /// Tests for the tap-vs-hold state machine behind the squad key (d-pad left):
    /// release before the threshold is a tap; crossing it starts a hold that ends on release.
    /// </summary>
    public class HoldTapDetectorTests
    {
        private const long Threshold = HoldTapDetector.HoldThresholdMs;
        private readonly HoldTapDetector _detector = new HoldTapDetector();

        [Fact]
        public void Update_WhenIdle_ReturnsNone()
        {
            Assert.Equal(HoldTapResult.None, _detector.Update(false, 0));
            Assert.Equal(HoldTapResult.None, _detector.Update(false, 100));
        }

        [Fact]
        public void Update_OnInitialPress_ReturnsNone()
        {
            Assert.Equal(HoldTapResult.None, _detector.Update(true, 0));
        }

        [Fact]
        public void Update_ReleasedBeforeThreshold_ReturnsTap()
        {
            _detector.Update(true, 0);
            _detector.Update(true, Threshold - 1);

            Assert.Equal(HoldTapResult.Tap, _detector.Update(false, Threshold - 1));
        }

        [Fact]
        public void Update_HeldToExactlyThreshold_ReturnsHoldStart()
        {
            _detector.Update(true, 0);

            Assert.Equal(HoldTapResult.HoldStart, _detector.Update(true, Threshold));
        }

        [Fact]
        public void Update_HeldUnderThreshold_ReturnsNoneAndIsNotHolding()
        {
            _detector.Update(true, 0);

            Assert.Equal(HoldTapResult.None, _detector.Update(true, Threshold - 1));
            Assert.False(_detector.IsHolding);
        }

        [Fact]
        public void Update_HeldPastThreshold_ReturnsHoldingAfterHoldStart()
        {
            _detector.Update(true, 0);
            _detector.Update(true, Threshold);

            Assert.Equal(HoldTapResult.Holding, _detector.Update(true, Threshold + 50));
            Assert.True(_detector.IsHolding);
        }

        [Fact]
        public void Update_ReleasedAfterHold_ReturnsHoldEndNotTap()
        {
            _detector.Update(true, 0);
            _detector.Update(true, Threshold);

            Assert.Equal(HoldTapResult.HoldEnd, _detector.Update(false, Threshold + 100));
            Assert.False(_detector.IsHolding);
        }

        [Fact]
        public void Update_AfterHoldEnd_ReturnsNoneWhileIdle()
        {
            _detector.Update(true, 0);
            _detector.Update(true, Threshold);
            _detector.Update(false, Threshold + 100);

            Assert.Equal(HoldTapResult.None, _detector.Update(false, Threshold + 200));
        }

        [Fact]
        public void Update_TapAfterHold_MeasuresFromNewPress()
        {
            // Hold cycle
            _detector.Update(true, 0);
            _detector.Update(true, Threshold);
            _detector.Update(false, Threshold + 100);

            // New press much later must not inherit the old press start
            long newPress = 10_000;
            _detector.Update(true, newPress);

            Assert.Equal(HoldTapResult.Tap, _detector.Update(false, newPress + 50));
        }

        [Fact]
        public void Update_TwoConsecutiveTaps_BothDetected()
        {
            _detector.Update(true, 0);
            Assert.Equal(HoldTapResult.Tap, _detector.Update(false, 50));

            _detector.Update(true, 500);
            Assert.Equal(HoldTapResult.Tap, _detector.Update(false, 560));
        }

        [Fact]
        public void Update_PressAndReleaseSameFrameTime_ReturnsTap()
        {
            _detector.Update(true, 100);

            Assert.Equal(HoldTapResult.Tap, _detector.Update(false, 100));
        }
    }
}
