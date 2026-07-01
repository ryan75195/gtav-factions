namespace FactionWars.UI.Models
{
    /// <summary>
    /// Per-frame outcome of <see cref="HoldTapDetector.Update"/>.
    /// </summary>
    public enum HoldTapResult
    {
        /// <summary>Nothing to act on this frame (idle, or pressed but under the hold threshold).</summary>
        None,

        /// <summary>The control was released before the hold threshold — a tap.</summary>
        Tap,

        /// <summary>The control has just crossed the hold threshold — a hold begins this frame.</summary>
        HoldStart,

        /// <summary>The control is still held past the threshold.</summary>
        Holding,

        /// <summary>The control was released after a hold — the hold ends this frame.</summary>
        HoldEnd
    }
}
