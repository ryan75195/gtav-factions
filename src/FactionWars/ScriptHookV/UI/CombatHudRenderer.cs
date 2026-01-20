using System;
using System.Drawing;
using FactionWars.UI.Interfaces;
using FactionWars.UI.Models;
using GTA.UI;

namespace FactionWars.ScriptHookV.UI
{
    /// <summary>
    /// ScriptHookVDotNet implementation of the combat HUD renderer.
    /// Draws combat progress information including defender count and capture progress bar.
    /// </summary>
    public class CombatHudRenderer : ICombatHudRenderer
    {
        // Position constants (screen-space coordinates 0-1)
        private const float HudX = 0.5f;          // Centered horizontally
        private const float HudY = 0.12f;         // Below territory indicator
        private const float BarWidth = 0.25f;    // Progress bar width
        private const float BarHeight = 0.015f;  // Progress bar height
        private const float TextScale = 0.4f;
        private const float SmallTextScale = 0.3f;

        // Colors
        private static readonly Color PlayerColor = Color.FromArgb(100, 200, 100);    // Green for player
        private static readonly Color EnemyColor = Color.FromArgb(255, 100, 100);     // Red for enemy
        private static readonly Color NeutralColor = Color.FromArgb(180, 180, 180);   // Gray for neutral
        private static readonly Color BackgroundColor = Color.FromArgb(120, 0, 0, 0); // Semi-transparent black

        private CombatHudData? _currentData;
        private bool _isVisible;

        /// <inheritdoc />
        public bool IsVisible => _isVisible;

        /// <summary>
        /// Gets the current combat HUD data, if any.
        /// </summary>
        public CombatHudData? CurrentData => _currentData;

        /// <inheritdoc />
        public void RenderCombatHud(CombatHudData data)
        {
            _currentData = data ?? throw new ArgumentNullException(nameof(data));
            _isVisible = true;
        }

        /// <inheritdoc />
        public void HideCombatHud()
        {
            _currentData = null;
            _isVisible = false;
        }

        /// <summary>
        /// Draws the combat HUD on screen.
        /// Should be called each frame from the OnTick handler.
        /// </summary>
        public void Draw()
        {
            if (!_isVisible || _currentData == null)
                return;

            var data = _currentData;

            // Draw zone name and combat status
            string statusLabel = data.IsPlayerAttacker ? "ATTACKING" : "DEFENDING";
            Color statusColor = data.IsPlayerAttacker ? Color.Orange : Color.Cyan;
            DrawText($"{data.ZoneName} - {statusLabel}", HudX, HudY, TextScale, statusColor, true);

            // Draw capture progress bar
            DrawProgressBar(HudX, HudY + 0.025f, BarWidth, BarHeight,
                data.PlayerControlPercent, data.EnemyControlPercent);

            // Draw control percentages
            string controlText = $"{data.PlayerControlPercent:F0}%  vs  {data.EnemyControlPercent:F0}%";
            DrawText(controlText, HudX, HudY + 0.045f, SmallTextScale, Color.White, true);

            // Draw combatant counts
            string combatantText;
            if (data.IsPlayerAttacker)
            {
                combatantText = $"Attackers: {data.AttackerPedCount}  |  Defenders: {data.DefenderPedCount}";
            }
            else
            {
                combatantText = $"Defenders: {data.DefenderPedCount}  |  Attackers: {data.AttackerPedCount}";
            }
            DrawText(combatantText, HudX, HudY + 0.065f, SmallTextScale, NeutralColor, true);

            // Draw reinforcement cooldown if active
            if (data.IsReinforcementOnCooldown)
            {
                string cooldownText = $"Reinforcements: {data.ReinforcementCooldownSeconds:F0}s";
                DrawText(cooldownText, HudX, HudY + 0.085f, SmallTextScale, Color.Yellow, true);
            }

            // Draw combat duration
            string durationText = $"Time: {data.CombatDuration:mm\\:ss}";
            DrawText(durationText, HudX, HudY + (data.IsReinforcementOnCooldown ? 0.105f : 0.085f),
                SmallTextScale, NeutralColor, true);
        }

        /// <summary>
        /// Draws a progress bar showing control percentages.
        /// </summary>
        private void DrawProgressBar(float centerX, float y, float width, float height,
            float playerPercent, float enemyPercent)
        {
            float barLeft = centerX - (width / 2);

            // Draw background
            DrawRect(centerX, y, width + 0.005f, height + 0.005f, BackgroundColor);

            // Calculate bar segments
            float playerWidth = (playerPercent / 100f) * width;
            float enemyWidth = (enemyPercent / 100f) * width;

            // Draw player control (left side)
            if (playerPercent > 0)
            {
                float playerCenterX = barLeft + (playerWidth / 2);
                DrawRect(playerCenterX, y, playerWidth, height, PlayerColor);
            }

            // Draw enemy control (right side)
            if (enemyPercent > 0)
            {
                float enemyCenterX = barLeft + width - (enemyWidth / 2);
                DrawRect(enemyCenterX, y, enemyWidth, height, EnemyColor);
            }
        }

        /// <summary>
        /// Draws a filled rectangle on screen.
        /// </summary>
        private void DrawRect(float x, float y, float width, float height, Color color)
        {
            // Convert to screen coordinates (GTA uses 0-1 for both axes)
            // Use native drawing for rectangles
            GTA.Native.Function.Call(
                GTA.Native.Hash.DRAW_RECT,
                x, y, width, height,
                color.R, color.G, color.B, color.A);
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

            textElement.ScaledDraw();
        }
    }
}
