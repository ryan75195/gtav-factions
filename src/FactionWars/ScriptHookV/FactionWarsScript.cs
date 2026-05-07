using FactionWars.Core.Interfaces;
using FactionWars.Persistence;
using FactionWars.ScriptHookV.Logging;
using FactionWars.ScriptHookV.Persistence;
using GTA;
using System;
using System.Windows.Forms;

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

        private NativeSaveWatcher? _nativeSaveWatcher;
        private LoadDetector? _loadDetector;
        private IGameBridge? _gameBridge;
        private IGameStateManager? _gameStateManager;
        private NativeSaveSidecarWriter? _nativeSaveSidecarWriter;

        public FactionWarsScript()
        {
            Tick += OnTick;
            KeyDown += OnKeyDown;
            KeyUp += OnKeyUp;
            Aborted += OnAborted;
        }

        private void OnTick(object sender, EventArgs e)
        {
            EnsureInitialized();
            _controller?.OnTick();
            TickLoadDetector();
            ProcessPendingNativeSaves();
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            _controller?.OnKeyDown((int)e.KeyCode);
        }

        private void OnKeyUp(object sender, KeyEventArgs e)
        {
            _controller?.OnKeyUp((int)e.KeyCode);
        }

        private void OnAborted(object sender, EventArgs e)
        {
            try
            {
                if (_nativeSaveWatcher != null)
                {
                    _nativeSaveWatcher.OnNativeSaveWritten -= HandleNativeSaveWritten;
                    _nativeSaveWatcher.Dispose();
                    _nativeSaveWatcher = null;
                }
            }
            catch (Exception ex)
            {
                FileLogger.Error("OnAborted: failed to dispose NativeSaveWatcher", ex);
            }

            _controller?.OnAbort();
            _controller = null;
            _nativeSaveSidecarWriter = null;
        }

        private void TickLoadDetector()
        {
            if (_loadDetector == null) return;
            _loadDetector.Tick();
        }

        private void ProcessPendingNativeSaves()
        {
            _nativeSaveSidecarWriter?.ProcessPending();
        }

        private void EnsureInitialized()
        {
            if (_initializationAttempted)
                return;

            _initializationAttempted = true;

            try
            {
                _gameBridge = new GameBridge();

                var container = ServiceContainerFactory.Create((GameBridge)_gameBridge);

                _controller = new GameLoopController(container);

                container.Resolve<LegacyBackupTask>().Run();

                _gameStateManager = container.Resolve<IGameStateManager>();
                var sidecarStore = container.Resolve<ISidecarStore>();
                _nativeSaveSidecarWriter = new NativeSaveSidecarWriter(
                    _gameBridge,
                    _gameStateManager,
                    container.Resolve<IFollowerService>(),
                    container.Resolve<IPlayerFactionDetector>());

                _loadDetector = new LoadDetector(
                    _gameBridge,
                    sidecarStore,
                    onHydrate: sidecar => _gameStateManager.HydrateFromSidecar(sidecar),
                    onNewGame: () => _gameStateManager.NewGame());

                _nativeSaveWatcher = container.Resolve<NativeSaveWatcher>();
                _nativeSaveWatcher.OnNativeSaveWritten += HandleNativeSaveWritten;
                _nativeSaveWatcher.Start();

                GTA.UI.Notification.PostTicker("~b~FactionWars~w~ loaded successfully!", false, false);
            }
            catch (Exception ex)
            {
                FileLogger.Error("FactionWarsScript: initialization failed", ex);
                GTA.UI.Notification.PostTicker($"~r~FactionWars failed to load:~w~ {ex.Message}", false, false);
            }
        }

        private void HandleNativeSaveWritten(object? sender, SaveEvent e)
        {
            try
            {
                _nativeSaveSidecarWriter?.Enqueue(e);
            }
            catch (Exception ex)
            {
                FileLogger.Error("HandleNativeSaveWritten: failed", ex);
            }
        }
    }
}
