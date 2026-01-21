using System;
using System.Drawing;
using FactionWars.UI.Interfaces;
using FactionWars.UI.Models;
using GTA.UI;

namespace FactionWars.ScriptHookV.UI
{
    /// <summary>
    /// ScriptHookVDotNet implementation of the territory indicator renderer.
    /// Draws compact zone information box at top-right of screen.
    /// </summary>
    public class TerritoryIndicatorRenderer : ITerritoryIndicatorRenderer
    {
        // Position constants - top right corner
        private const float BoxX = 0.92f;       // Right side of screen
        private const float BoxY = 0.02f;       // Near top
        private const float BoxWidth = 0.14f;   // Compact width
        private const float BoxPadding = 0.005f;
        private const float AccentBarWidth = 0.003f;

        // Text scales - compact
        private const float TitleScale = 0.35f;
        private const float SubtitleScale = 0.28f;
        private const float DetailScale = 0.26f;

        // Colors
        private static readonly Color BackgroundColor = Color.FromArgb(100, 0, 0, 0);
        private static readonly Color FriendlyAccent = Color.FromArgb(255, 100, 200, 100);
        private static readonly Color EnemyAccent = Color.FromArgb(255, 255, 100, 100);

        // Throttling
        private DateTime _lastDataUpdate = DateTime.MinValue;
        private static readonly TimeSpan UpdateThrottle = TimeSpan.FromMilliseconds(500);

        private TerritoryIndicatorData? _currentData;
        private bool _isVisible;

        /// <inheritdoc />
        public bool IsVisible => _isVisible;

        /// <inheritdoc />
        public void Render(TerritoryIndicatorData data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));

            // Always accept new data but throttle updates unless significant change
            var now = DateTime.UtcNow;
            bool shouldUpdate = _currentData == null ||
                                now - _lastDataUpdate >= UpdateThrottle ||
                                DataChangedSignificantly(_currentData, data);

            if (shouldUpdate)
            {
                _currentData = data;
                _lastDataUpdate = now;
            }

            _isVisible = true;
        }

        private static bool DataChangedSignificantly(TerritoryIndicatorData old, TerritoryIndicatorData current)
        {
            // Always update if zone changed
            if (old.ZoneName != current.ZoneName) return true;

            // Always update if ownership changed
            if (old.IsPlayerOwned != current.IsPlayerOwned) return true;

            // Always update if contest state changed
            if (old.IsContested != current.IsContested) return true;

            // Update if troop counts changed
            if (old.DeployedDefenderCount != current.DeployedDefenderCount) return true;
            if (old.ReserveDefenderCount != current.ReserveDefenderCount) return true;
            if (old.PlayerTroopCount != current.PlayerTroopCount) return true;
            if (old.EnemyDefenderCount != current.EnemyDefenderCount) return true;

            // Update if control percentage changed by more than 1%
            if (Math.Abs(old.ControlPercentage - current.ControlPercentage) >= 1f) return true;

            return false;
        }

        /// <inheritdoc />
        public void Hide()
        {
            _currentData = null;
            _isVisible = false;
        }

        /// <summary>
        /// Draws the territory indicator on screen.
        /// Should be called each frame from the OnTick handler.
        /// </summary>
        public void Draw()
        {
            if (!_isVisible || _currentData == null)
                return;

            var data = _currentData;

            // Don't show for neutral zones
            if (data.IsNeutral)
                return;

            if (data.IsPlayerOwned)
            {
                DrawFriendlyTerritoryHud(data);
            }
            else
            {
                DrawEnemyTerritoryHud(data);
            }
        }

        private void DrawFriendlyTerritoryHud(TerritoryIndicatorData data)
        {
            float boxHeight = 0.055f;
            float centerY = BoxY + (boxHeight / 2);

            // Draw box
            DrawBox(BoxX, centerY, BoxWidth, boxHeight, FriendlyAccent);

            // Zone name
            float textX = BoxX - (BoxWidth / 2) + BoxPadding + AccentBarWidth + 0.005f;
            DrawTextLeft(data.ZoneName.ToUpperInvariant(), textX, BoxY + 0.005f, TitleScale, FriendlyAccent);

            // Status
            DrawTextLeft("Your Territory", textX, BoxY + 0.022f, SubtitleScale, Color.LightGray);

            // Troop counts: "8 deployed · 14 reserve"
            string troopText = $"{data.DeployedDefenderCount} deployed \u00B7 {data.ReserveDefenderCount} reserve";
            DrawTextLeft(troopText, textX, BoxY + 0.038f, DetailScale, Color.White);
        }

        private void DrawEnemyTerritoryHud(TerritoryIndicatorData data)
        {
            float boxHeight = data.IsContested ? 0.072f : 0.055f;
            float centerY = BoxY + (boxHeight / 2);

            // Draw box
            DrawBox(BoxX, centerY, BoxWidth, boxHeight, EnemyAccent);

            // Zone name
            float textX = BoxX - (BoxWidth / 2) + BoxPadding + AccentBarWidth + 0.005f;
            DrawTextLeft(data.ZoneName.ToUpperInvariant(), textX, BoxY + 0.005f, TitleScale, EnemyAccent);

            // Status
            string status = data.IsContested ? "Capturing..." : $"{data.OwnerFactionName}";
            DrawTextLeft(status, textX, BoxY + 0.022f, SubtitleScale, Color.LightGray);

            if (data.IsContested)
            {
                // Progress bar
                float barY = BoxY + 0.042f;
                float barWidth = BoxWidth - 0.02f;
                DrawProgressBar(BoxX, barY, barWidth, 0.008f, data.ControlPercentage, FriendlyAccent);

                // Percentage and troop counts on same line
                string statsText = $"{data.ControlPercentage:F0}%  |  {data.PlayerTroopCount} vs {data.EnemyDefenderCount}";
                DrawTextLeft(statsText, textX, BoxY + 0.054f, DetailScale, Color.White);
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

        /// <summary>
        /// Draws a filled rectangle.
        /// </summary>
        private void DrawRect(float x, float y, float width, float height, Color color)
        {
            GTA.Native.Function.Call(
                GTA.Native.Hash.DRAW_RECT,
                x, y, width, height,
                color.R, color.G, color.B, color.A);
        }

        /// <summary>
        /// Draws a progress bar.
        /// </summary>
        private void DrawProgressBar(float x, float y, float width, float height, float percent, Color fillColor)
        {
            // Background
            DrawRect(x, y, width, height, Color.FromArgb(80, 50, 50, 50));

            // Fill
            float fillWidth = (percent / 100f) * width;
            if (fillWidth > 0.001f)
            {
                float fillX = x - (width / 2) + (fillWidth / 2);
                DrawRect(fillX, y, fillWidth, height, fillColor);
            }
        }

        private void DrawTextLeft(string text, float x, float y, float scale, Color color)
        {
            // ScaledDraw uses 1280x720 as base reference resolution
            var textElement = new TextElement(text, new PointF(x * 1280f, y * 720f), scale, color)
            {
                Alignment = Alignment.Left,
                Shadow = true
            };
            textElement.ScaledDraw();
        }
    }
}
