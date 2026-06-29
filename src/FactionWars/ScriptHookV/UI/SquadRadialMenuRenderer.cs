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

            const float radiusX = 0.12f; // ~radiusY * 9/16 so the ring looks circular on 16:9.
            const float radiusY = 0.21f;
            var segments = _menu.Segments;
            for (int i = 0; i < segments.Count; i++)
            {
                double angle = i * (2 * Math.PI / segments.Count);
                float px = 0.5f + (radiusX * (float)Math.Sin(angle));
                float py = 0.5f - (radiusY * (float)Math.Cos(angle));
                bool selected = i == _menu.SelectedIndex;

                // Selected segment: bright amber, dark bold label. Unselected: dim slab, light label.
                DrawRect(px, py, 0.165f, 0.072f, selected
                    ? Color.FromArgb(235, 255, 176, 32)
                    : Color.FromArgb(185, 20, 20, 20));
                DrawCenteredText(Label(segments[i]), px, py - 0.026f, selected ? 0.62f : 0.46f,
                    selected ? Color.Black : Color.FromArgb(255, 235, 235, 235));
            }

            // Center readout: title, the stance about to be applied (big), and a release hint.
            var pointed = segments[_menu.SelectedIndex];
            DrawCenteredText("BODYGUARDS", 0.5f, 0.5f - 0.048f, 0.4f, Color.FromArgb(255, 170, 195, 255));
            DrawCenteredText(Label(pointed), 0.5f, 0.5f - 0.014f, 0.62f, Color.FromArgb(255, 255, 176, 32));
            DrawCenteredText("release to apply", 0.5f, 0.5f + 0.03f, 0.32f, Color.FromArgb(205, 225, 225, 225));
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
