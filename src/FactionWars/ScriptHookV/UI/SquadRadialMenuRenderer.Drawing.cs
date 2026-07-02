using System;
using System.Drawing;
using FactionWars.Combat.Models;
using FactionWars.ScriptHookV.Logging;
using GTA.Native;
using GTA.UI;

namespace FactionWars.ScriptHookV.UI
{
    /// <summary>
    /// Drawing half of the squad radial: wheel ring texture, fallback slabs, labels, and the
    /// centre readout. Split from the lifecycle/input half to keep each file focused.
    /// </summary>
    public sealed partial class SquadRadialMenuRenderer
    {
        // Embedded wheel art, extracted to disk on first draw. Null until extracted; if extraction
        // fails we fall back to the rectangular slabs so the wheel is never invisible.
        private const string RingResource = "FactionWars.Resources.wheel_ring.png";
        private const int WheelBaseW = 1920;   // NativeUI.Sprite.DrawTexture uses a 1080-height base.
        private const int WheelBaseH = 1080;
        private const int WheelSize = 480;      // square wheel, centred on screen
        private string? _ringTexturePath;
        private bool _textureLoadFailed;

        private void Draw()
        {
            // Darker full-screen backdrop so the wheel reads clearly against the world.
            DrawRect(0.5f, 0.5f, 1f, 1f, Color.FromArgb(150, 0, 0, 0));

            if (!TryDrawWheelTextures())
            {
                DrawFallbackSlabs();
            }

            DrawLabelsAndCenter();
        }

        // Draws the circular ring using the embedded PNG. The selected slice is conveyed by the
        // labels (bright + larger) and the centre readout, not a coloured wedge. Returns false (so the
        // caller draws the rectangular fallback) if the texture cannot be extracted — a wrong native
        // call must never leave the wheel invisible.
        private bool TryDrawWheelTextures()
        {
            if (_textureLoadFailed) return false;

            if (_ringTexturePath == null)
            {
                try
                {
                    var asm = typeof(SquadRadialMenuRenderer).Assembly;
                    _ringTexturePath = NativeUI.Sprite.WriteFileFromResources(asm, RingResource);
                }
                catch (Exception ex)
                {
                    _textureLoadFailed = true;
                    FileLogger.Error("SquadRadial: wheel texture load failed; using fallback slabs", ex);
                    return false;
                }
            }

            var pos = new Point((WheelBaseW - WheelSize) / 2, (WheelBaseH - WheelSize) / 2);
            var size = new Size(WheelSize, WheelSize);

            NativeUI.Sprite.DrawTexture(_ringTexturePath, pos, size, 0f, Color.FromArgb(235, 255, 255, 255));
            return true;
        }

        // Original rectangular wheel — only used if the textures fail to load in-game.
        private void DrawFallbackSlabs()
        {
            var segments = _menu.Segments;
            for (int i = 0; i < segments.Count; i++)
            {
                double angle = i * (2 * Math.PI / segments.Count);
                float px = 0.5f + (0.12f * (float)Math.Sin(angle));
                float py = 0.5f - (0.21f * (float)Math.Cos(angle));
                bool selected = i == _menu.SelectedIndex;
                DrawRect(px, py, 0.165f, 0.072f, selected
                    ? Color.FromArgb(235, 170, 195, 255)
                    : Color.FromArgb(185, 20, 20, 20));
            }
        }

        // Segment labels around the ring plus the center readout (title / pointed stance / hint).
        private void DrawLabelsAndCenter()
        {
            const float radiusY = 0.155f;          // on the ring band
            const float radiusX = radiusY * 0.5625f; // * 9/16 so labels sit on a true circle
            var segments = _menu.Segments;
            for (int i = 0; i < segments.Count; i++)
            {
                double angle = i * (2 * Math.PI / segments.Count);
                float px = 0.5f + (radiusX * (float)Math.Sin(angle));
                float py = 0.5f - (radiusY * (float)Math.Cos(angle));
                bool selected = i == _menu.SelectedIndex;
                // Selection is shown by a bright, larger label (drawn over the ring); unselected
                // labels are dimmer and smaller. No coloured highlight wedge.
                DrawCenteredText(Label(segments[i]), px, py - 0.018f, selected ? 0.5f : 0.40f,
                    selected ? Color.White : Color.FromArgb(255, 150, 150, 150));
            }

            var pointed = segments[_menu.SelectedIndex];
            DrawCenteredText("BODYGUARDS", 0.5f, 0.5f - 0.040f, 0.34f, Color.FromArgb(255, 170, 195, 255));
            DrawCenteredText(Label(pointed), 0.5f, 0.5f - 0.010f, 0.52f, Color.White);
            DrawCenteredText("release to apply", 0.5f, 0.5f + 0.028f, 0.30f, Color.FromArgb(205, 225, 225, 225));
        }

        private static void DrawRect(float x, float y, float width, float height, Color color)
        {
            Function.Call(Hash.DRAW_RECT, x, y, width, height, color.R, color.G, color.B, color.A);
        }

        private static void DrawCenteredText(string text, float x, float y, float scale, Color color)
        {
            var element = new TextElement(text, new PointF(x * 1280f, y * 720f), scale, color)
            {
                Alignment = Alignment.Center,
                Shadow = true
            };
            element.ScaledDraw();
        }

        private static string Label(SquadStance stance)
        {
            switch (stance)
            {
                case SquadStance.HoldArea: return "HOLD";
                case SquadStance.SearchAndDestroy: return "SEARCH";
                default: return "ESCORT";
            }
        }
    }
}
