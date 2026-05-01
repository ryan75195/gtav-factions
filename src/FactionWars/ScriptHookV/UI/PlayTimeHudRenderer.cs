using System.Drawing;
using GTA.UI;

namespace FactionWars.ScriptHookV.UI
{
    /// <summary>
    /// Renders the play time counter at the top-middle of the screen.
    /// Shows elapsed time since game started in HH:MM:SS format.
    /// </summary>
    public class PlayTimeHudRenderer
    {
        // Position constants - top center
        private const float TextX = 0.5f;           // Center of screen
        private const float TextY = 0.015f;         // Near top
        private const float TextScale = 0.35f;

        // Colors
        private static readonly Color TextColor = Color.FromArgb(200, 255, 255, 255);
        private static readonly Color ShadowColor = Color.FromArgb(150, 0, 0, 0);

        private long _totalPlayTimeSeconds;
        private bool _isVisible = true;

        /// <summary>
        /// Gets or sets whether the play time HUD is visible.
        /// </summary>
        public bool IsVisible
        {
            get => _isVisible;
            set => _isVisible = value;
        }

        /// <summary>
        /// Updates the displayed play time.
        /// </summary>
        /// <param name="totalSeconds">Total play time in seconds.</param>
        public void SetPlayTime(long totalSeconds)
        {
            _totalPlayTimeSeconds = totalSeconds;
        }

        /// <summary>
        /// Draws the play time HUD. Should be called each frame.
        /// </summary>
        public void Draw()
        {
            if (!_isVisible)
                return;

            // Format time as HH:MM:SS
            var hours = _totalPlayTimeSeconds / 3600;
            var minutes = (_totalPlayTimeSeconds % 3600) / 60;
            var seconds = _totalPlayTimeSeconds % 60;

            string timeText = $"{hours:D2}:{minutes:D2}:{seconds:D2}";

            DrawTextCentered(timeText, TextX, TextY, TextScale, TextColor);
        }

        private void DrawTextCentered(string text, float x, float y, float scale, Color color)
        {
            var textElement = new TextElement(text, new PointF(x * 1280f, y * 720f), scale, color)
            {
                Alignment = Alignment.Center,
                Shadow = true
            };
            textElement.ScaledDraw();
        }
    }
}
