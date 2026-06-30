using System;
using System.Collections.Generic;
using System.Drawing;
using FactionWars.Combat.Models;
using FactionWars.Core.Interfaces;
using FactionWars.ScriptHookV.Logging;
using FactionWars.UI.Models;
using GTA.Native;
using GTA.UI;

namespace FactionWars.ScriptHookV.UI
{
    /// <summary>
    /// Weapon-wheel-style radial for choosing the bodyguard squad stance. Held on the existing squad
    /// key (control 174 / INPUT_PHONE_LEFT): open while held, point with the right stick or mouse,
    /// release to apply the highlighted stance. Native draw/input/time-scale lives here (ScriptHookV);
    /// the selection state machine and angle math are tested in <see cref="SquadRadialMenu"/> and
    /// <see cref="RadialSelector"/>. Behaviour is logged because native calls cannot be unit-tested.
    /// </summary>
    public sealed class SquadRadialMenuRenderer
    {
        private const int OpenControl = 174;        // INPUT_PHONE_LEFT (the existing squad key)
        private const int RightStickX = 220;        // INPUT_SCRIPT_RIGHT_AXIS_X
        private const int RightStickY = 221;        // INPUT_SCRIPT_RIGHT_AXIS_Y
        private const int LookLeftRight = 1;        // INPUT_LOOK_LR (mouse delta on KB/M)
        private const int LookUpDown = 2;           // INPUT_LOOK_UD
        private const float StickDeadzoneSq = 0.0625f; // (0.25)^2
        private const float MouseSensitivity = 0.08f;
        private const float OpenTimeScale = 0.35f;

        // Controls suppressed while the wheel is open so the character does not run/aim/shoot/swap.
        private static readonly int[] SuppressedControls = { 21, 22, 24, 25, 30, 31, 37, 140 };

        private readonly IGameBridge _gameBridge;
        private readonly Func<SquadStance> _currentStance;
        private readonly Action<SquadStance, IReadOnlyList<int>> _applyStance;
        private readonly Func<IReadOnlyList<int>> _handles;
        private readonly SquadRadialMenu _menu = new SquadRadialMenu();

        private float _mouseX;
        private float _mouseY;

        // Embedded wheel art, extracted to disk on first draw. Null until extracted; if extraction
        // fails we fall back to the rectangular slabs so the wheel is never invisible.
        private const string RingResource = "FactionWars.Resources.wheel_ring.png";
        private const int WheelBaseW = 1920;   // NativeUI.Sprite.DrawTexture uses a 1080-height base.
        private const int WheelBaseH = 1080;
        private const int WheelSize = 480;      // square wheel, centred on screen
        private string? _ringTexturePath;
        private bool _textureLoadFailed;

        public SquadRadialMenuRenderer(
            IGameBridge gameBridge,
            Func<SquadStance> currentStance,
            Action<SquadStance, IReadOnlyList<int>> applyStance,
            Func<IReadOnlyList<int>> handles)
        {
            _gameBridge = gameBridge ?? throw new ArgumentNullException(nameof(gameBridge));
            _currentStance = currentStance ?? throw new ArgumentNullException(nameof(currentStance));
            _applyStance = applyStance ?? throw new ArgumentNullException(nameof(applyStance));
            _handles = handles ?? throw new ArgumentNullException(nameof(handles));
        }

        /// <summary>True while the radial wheel is currently open (the squad key is held).</summary>
        public bool IsOpen => _menu.IsOpen;

        /// <summary>Called once per frame from the HUD pass. Owns the open/point/apply lifecycle.</summary>
        public void Update()
        {
            bool held = _gameBridge.IsControlPressed(OpenControl);

            if (held)
            {
                if (!_menu.IsOpen)
                {
                    _menu.Open(_currentStance());
                    _mouseX = 0f;
                    _mouseY = 0f;
                    FileLogger.AI($"SquadRadial: opened (current={_currentStance()})");
                }

                Function.Call(Hash.SET_TIME_SCALE, OpenTimeScale);
                SuppressControls();
                ReadPointerIntoMenu();
                Draw();
                return;
            }

            if (_menu.IsOpen)
            {
                var chosen = _menu.Close();
                Function.Call(Hash.SET_TIME_SCALE, 1f);
                _applyStance(chosen, _handles());
                FileLogger.AI($"SquadRadial: closed -> {chosen}");
            }
        }

        /// <summary>Resets time scale; call when the script aborts while the wheel may be open.</summary>
        public void Reset()
        {
            Function.Call(Hash.SET_TIME_SCALE, 1f);
        }

        private void SuppressControls()
        {
            foreach (var control in SuppressedControls)
            {
                _gameBridge.DisableControlThisFrame(control);
            }

            // Lock the camera like the real weapon wheel: disable the look controls so the mouse /
            // right stick no longer pan the view. The pointing delta is still read below via
            // GET_DISABLED_CONTROL_NORMAL, so selection keeps working while the camera stays put.
            _gameBridge.DisableControlThisFrame(LookLeftRight);
            _gameBridge.DisableControlThisFrame(LookUpDown);
        }

        private void ReadPointerIntoMenu()
        {
            float rx = Function.Call<float>(Hash.GET_CONTROL_NORMAL, 0, RightStickX);
            float ry = Function.Call<float>(Hash.GET_CONTROL_NORMAL, 0, RightStickY);

            float dirX;
            float dirY;
            if ((rx * rx) + (ry * ry) >= StickDeadzoneSq)
            {
                dirX = rx;
                dirY = ry;
                _mouseX = rx;
                _mouseY = ry;
            }
            else
            {
                // Read the look delta from the DISABLED control so the camera stays frozen (the look
                // controls are disabled this frame in SuppressControls) while still steering selection.
                _mouseX += Function.Call<float>(Hash.GET_DISABLED_CONTROL_NORMAL, 0, LookLeftRight) * MouseSensitivity * 60f;
                _mouseY += Function.Call<float>(Hash.GET_DISABLED_CONTROL_NORMAL, 0, LookUpDown) * MouseSensitivity * 60f;
                _mouseX = Clamp(_mouseX, -1f, 1f);
                _mouseY = Clamp(_mouseY, -1f, 1f);
                dirX = _mouseX;
                dirY = _mouseY;
            }

            _menu.UpdatePointer(dirX, dirY);
        }

        private static float Clamp(float v, float min, float max)
            => v < min ? min : (v > max ? max : v);

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
