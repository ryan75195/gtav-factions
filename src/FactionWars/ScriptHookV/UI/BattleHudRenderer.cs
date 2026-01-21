using System.Drawing;
using FactionWars.UI.Models;
using GTA.UI;

namespace FactionWars.ScriptHookV.UI
{
    /// <summary>
    /// Renders the active battle HUD showing AI battle troop counts.
    /// Displays at top-left of screen, below minimap area.
    /// </summary>
    public class BattleHudRenderer
    {
        // Position constants - left side, below minimap
        private const float BoxX = 0.085f;      // Left side of screen
        private const float BoxY = 0.35f;       // Below minimap
        private const float BoxWidth = 0.15f;
        private const float BoxPadding = 0.005f;
        private const float AccentBarWidth = 0.003f;

        // Text scales
        private const float TitleScale = 0.32f;
        private const float TroopScale = 0.30f;
        private const float HintScale = 0.24f;

        // Colors
        private static readonly Color BackgroundColor = Color.FromArgb(120, 0, 0, 0);
        private static readonly Color AccentColor = Color.FromArgb(255, 255, 180, 50);  // Orange/amber

        private BattleHudData? _currentData;
        private bool _isVisible;

        /// <summary>
        /// Gets whether the HUD is visible.
        /// </summary>
        public bool IsVisible => _isVisible;

        /// <summary>
        /// Updates the HUD with new battle data.
        /// </summary>
        public void SetData(BattleHudData? data)
        {
            _currentData = data;
            _isVisible = data != null;
        }

        /// <summary>
        /// Hides the battle HUD.
        /// </summary>
        public void Hide()
        {
            _currentData = null;
            _isVisible = false;
        }

        /// <summary>
        /// Draws the battle HUD. Should be called each frame.
        /// </summary>
        public void Draw()
        {
            if (!_isVisible || _currentData == null)
                return;

            var data = _currentData;
            float boxHeight = 0.065f;
            float centerY = BoxY + (boxHeight / 2);

            // Draw background box with accent
            DrawBox(BoxX, centerY, BoxWidth, boxHeight, AccentColor);

            float textX = BoxX - (BoxWidth / 2) + BoxPadding + AccentBarWidth + 0.005f;

            // Zone name as title
            DrawTextLeft(data.ZoneName.ToUpperInvariant(), textX, BoxY + 0.005f, TitleScale, AccentColor);

            // Troop counts: "Attacker 12 vs Defender 8"
            string troopText = $"{data.AttackerName} {data.AttackerTroops} vs {data.DefenderName} {data.DefenderTroops}";
            DrawTextLeft(troopText, textX, BoxY + 0.024f, TroopScale, Color.White);

            // Battle count indicator and hint
            if (data.HasMultipleBattles)
            {
                string hintText = $"Battle {data.CurrentBattleIndex}/{data.TotalBattles} - Press B to cycle";
                DrawTextLeft(hintText, textX, BoxY + 0.044f, HintScale, Color.LightGray);
            }
        }

        /// <summary>
        /// Draws a styled box with accent bar.
        /// </summary>
        private void DrawBox(float x, float y, float width, float height, Color accentColor)
        {
            // Background
            DrawRect(x, y, width, height, BackgroundColor);

            // Left accent bar
            float accentX = x - (width / 2) + (AccentBarWidth / 2);
            DrawRect(accentX, y, AccentBarWidth, height, accentColor);
        }

        private void DrawRect(float x, float y, float width, float height, Color color)
        {
            GTA.Native.Function.Call(
                GTA.Native.Hash.DRAW_RECT,
                x, y, width, height,
                color.R, color.G, color.B, color.A);
        }

        private void DrawTextLeft(string text, float x, float y, float scale, Color color)
        {
            var textElement = new TextElement(text, new PointF(x * 1280f, y * 720f), scale, color)
            {
                Alignment = Alignment.Left,
                Shadow = true
            };
            textElement.ScaledDraw();
        }
    }
}
