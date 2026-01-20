using System;
using System.Drawing;
using FactionWars.Factions.Models;
using FactionWars.UI.Interfaces;
using FactionWars.UI.Models;
using GTA.UI;

namespace FactionWars.ScriptHookV.UI
{
    /// <summary>
    /// ScriptHookVDotNet implementation of the territory indicator renderer.
    /// Draws zone information at the top of the screen.
    /// </summary>
    public class TerritoryIndicatorRenderer : ITerritoryIndicatorRenderer
    {
        private const float IndicatorY = 0.02f; // Near top of screen
        private const float IndicatorX = 0.5f;  // Centered horizontally
        private const float TextScale = 0.45f;
        private const float SmallTextScale = 0.35f;

        private TerritoryIndicatorData? _currentData;
        private bool _isVisible;

        /// <inheritdoc />
        public bool IsVisible => _isVisible;

        /// <inheritdoc />
        public void Render(TerritoryIndicatorData data)
        {
            _currentData = data ?? throw new ArgumentNullException(nameof(data));
            _isVisible = true;
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

            // Choose color based on ownership
            Color zoneNameColor;
            if (data.IsNeutral)
            {
                zoneNameColor = Color.White;
            }
            else if (data.IsPlayerOwned)
            {
                zoneNameColor = Color.FromArgb(100, 200, 100); // Green for friendly
            }
            else
            {
                zoneNameColor = Color.FromArgb(255, 100, 100); // Red for enemy
            }

            // Use faction color if available
            if (data.OwnerFactionColor.HasValue)
            {
                var fc = data.OwnerFactionColor.Value;
                zoneNameColor = Color.FromArgb(fc.R, fc.G, fc.B);
            }

            // Draw zone name (main text)
            DrawText(data.ZoneName, IndicatorX, IndicatorY, TextScale, zoneNameColor, true);

            // Build and draw status line
            string statusText;
            Color statusColor;

            if (data.IsNeutral)
            {
                statusText = "Neutral Territory";
                statusColor = Color.Gray;
            }
            else if (data.IsContested)
            {
                statusText = $"{data.OwnerFactionName} - CONTESTED ({data.ControlPercentage:F0}%)";
                statusColor = Color.Yellow;
            }
            else
            {
                string ownershipText = data.IsPlayerOwned ? "Your Territory" : $"{data.OwnerFactionName}";
                statusText = $"{ownershipText} - {data.ControlPercentage:F0}%";
                statusColor = data.IsPlayerOwned ? Color.LightGreen : Color.FromArgb(255, 150, 150);
            }

            DrawText(statusText, IndicatorX, IndicatorY + 0.025f, SmallTextScale, statusColor, true);
        }

        /// <summary>
        /// Draws text on screen using GTA V's native text rendering.
        /// </summary>
        private void DrawText(string text, float x, float y, float scale, Color color, bool centered)
        {
            var textElement = new TextElement(text, new PointF(x * 1920f, y * 1080f), scale, color)
            {
                Alignment = centered ? Alignment.Center : Alignment.Left,
                Shadow = true,
                Outline = true
            };

            // Use screen resolution scaling for proper positioning
            textElement.ScaledDraw();
        }
    }
}
