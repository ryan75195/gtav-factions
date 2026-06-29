using System;

namespace FactionWars.UI.Models
{
    /// <summary>
    /// Maps a pointer direction onto a radial menu segment. Pure geometry so the fiddly angle math
    /// is testable without the game. Screen-space convention: +X right, +Y down. Segment 0 is
    /// centred at the top (12 o'clock) and indices increase clockwise.
    /// </summary>
    public static class RadialSelector
    {
        /// <summary>
        /// Returns the index (0..segmentCount-1) of the segment the pointer points at, or -1 when the
        /// pointer is inside the deadzone (no clear direction) or the segment count is non-positive.
        /// </summary>
        public static int SelectIndex(int segmentCount, float dirX, float dirY, float deadzone)
        {
            if (segmentCount <= 0)
            {
                return -1;
            }

            float magnitude = (float)Math.Sqrt((dirX * dirX) + (dirY * dirY));
            if (magnitude < deadzone)
            {
                return -1;
            }

            // Clockwise angle from straight up. atan2(x, -y): up=(0,-1)->0, right=(1,0)->pi/2.
            double angle = Math.Atan2(dirX, -dirY);
            if (angle < 0)
            {
                angle += 2 * Math.PI;
            }

            double segment = (2 * Math.PI) / segmentCount;
            // Standard round-half-up (avoids Math.Round's banker's rounding at exact boundaries).
            int index = (int)Math.Floor((angle / segment) + 0.5);
            return index % segmentCount;
        }
    }
}
