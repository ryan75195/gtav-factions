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
    /// Weapon-wheel-style radial for choosing the bodyguard squad stance. On the squad key
    /// (control 174 / INPUT_PHONE_LEFT): HOLD opens the wheel — point with the right stick or
    /// mouse, release to apply the highlighted stance; a quick TAP instead raises
    /// <see cref="Tapped"/> so the owner can open the squad hub menu. Native
    /// draw/input/time-scale lives here (ScriptHookV); the tap/hold, selection, and angle state
    /// machines are tested in <see cref="HoldTapDetector"/>, <see cref="SquadRadialMenu"/>, and
    /// <see cref="RadialSelector"/>. Behaviour is logged because native calls cannot be unit-tested.
    /// </summary>
    public sealed partial class SquadRadialMenuRenderer
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
        private readonly HoldTapDetector _holdTap = new HoldTapDetector();

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

        /// <summary>True while the radial wheel is currently open (the squad key is held).</summary>
        public bool IsOpen => _menu.IsOpen;

        /// <summary>
        /// Raised when the squad key is tapped (released before the hold threshold) instead of
        /// held. The owner opens the squad hub menu from this.
        /// </summary>
        public event Action? Tapped;

        /// <summary>Called once per frame from the HUD pass. Owns the tap/open/point/apply lifecycle.</summary>
        public void Update()
        {
            var result = _holdTap.Update(_gameBridge.IsControlPressed(OpenControl), Environment.TickCount);

            switch (result)
            {
                case HoldTapResult.Tap:
                    FileLogger.AI("SquadRadial: tapped -> requesting squad hub");
                    Tapped?.Invoke();
                    return;

                case HoldTapResult.HoldStart:
                case HoldTapResult.Holding:
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

                case HoldTapResult.HoldEnd:
                    if (_menu.IsOpen)
                    {
                        var chosen = _menu.Close();
                        Function.Call(Hash.SET_TIME_SCALE, 1f);
                        _applyStance(chosen, _handles());
                        FileLogger.AI($"SquadRadial: closed -> {chosen}");
                    }

                    return;
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
    }
}
