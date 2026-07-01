namespace FactionWars.UI.Models
{
    /// <summary>
    /// Distinguishes a tap from a hold on a single control, fed one (pressed, time) sample per
    /// frame — the same pattern as GTA's character wheel. A release before the threshold is a
    /// tap; crossing the threshold starts a hold that ends on release. Pure and game-agnostic;
    /// the ScriptHookV layer supplies the control state and clock.
    /// </summary>
    public sealed class HoldTapDetector
    {
        /// <summary>How long the control must stay pressed before it counts as a hold.</summary>
        public const long HoldThresholdMs = 250;

        private bool _wasPressed;
        private bool _holding;
        private long _pressStartMs;

        /// <summary>True while a hold (threshold crossed, not yet released) is in progress.</summary>
        public bool IsHolding => _holding;

        /// <summary>
        /// Advances the detector one frame. <paramref name="nowMs"/> must come from a
        /// monotonic millisecond clock (e.g. Environment.TickCount).
        /// </summary>
        public HoldTapResult Update(bool isPressed, long nowMs)
        {
            if (isPressed)
            {
                if (!_wasPressed)
                {
                    _wasPressed = true;
                    _pressStartMs = nowMs;
                    return HoldTapResult.None;
                }

                if (_holding)
                {
                    return HoldTapResult.Holding;
                }

                if (nowMs - _pressStartMs >= HoldThresholdMs)
                {
                    _holding = true;
                    return HoldTapResult.HoldStart;
                }

                return HoldTapResult.None;
            }

            if (!_wasPressed)
            {
                return HoldTapResult.None;
            }

            _wasPressed = false;
            if (_holding)
            {
                _holding = false;
                return HoldTapResult.HoldEnd;
            }

            return HoldTapResult.Tap;
        }
    }
}
