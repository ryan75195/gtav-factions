using System;
using System.Windows.Forms;
using GTA;

namespace FactionWars.ScriptHookV
{
    /// <summary>
    /// Main entry point for the FactionWars mod. Extends GTA.Script to hook into
    /// the game's script system. This class is intentionally thin - all logic is
    /// delegated to GameLoopController for testability.
    /// </summary>
    public class FactionWarsScript : Script
    {
        private GameLoopController? _controller;
        private bool _initializationAttempted;

        /// <summary>
        /// Initializes the FactionWars script.
        /// </summary>
        public FactionWarsScript()
        {
            Tick += OnTick;
            KeyDown += OnKeyDown;
            KeyUp += OnKeyUp;
            Aborted += OnAborted;

            // Defer initialization to first tick to ensure game is ready
        }

        /// <summary>
        /// Called every frame by ScriptHookVDotNet.
        /// </summary>
        private void OnTick(object sender, EventArgs e)
        {
            EnsureInitialized();
            _controller?.OnTick();
        }

        /// <summary>
        /// Called when a key is pressed.
        /// </summary>
        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            _controller?.OnKeyDown((int)e.KeyCode);
        }

        /// <summary>
        /// Called when a key is released.
        /// </summary>
        private void OnKeyUp(object sender, KeyEventArgs e)
        {
            _controller?.OnKeyUp((int)e.KeyCode);
        }

        /// <summary>
        /// Called when the script is aborted/unloaded.
        /// </summary>
        private void OnAborted(object sender, EventArgs e)
        {
            _controller?.OnAbort();
            _controller = null;
        }

        /// <summary>
        /// Initializes the game controller and all services.
        /// Called on first tick to ensure the game is fully loaded.
        /// </summary>
        private void EnsureInitialized()
        {
            if (_initializationAttempted)
                return;

            _initializationAttempted = true;

            try
            {
                // Create the game bridge that connects to GTA V natives
                var gameBridge = new GameBridge();

                // Create the service container with all services wired up
                var container = ServiceContainerFactory.Create(gameBridge);

                // Create the game loop controller
                _controller = new GameLoopController(container);

                GTA.UI.Notification.Show("~b~FactionWars~w~ loaded successfully!");
            }
            catch (Exception ex)
            {
                GTA.UI.Notification.Show($"~r~FactionWars failed to load:~w~ {ex.Message}");
            }
        }
    }
}
