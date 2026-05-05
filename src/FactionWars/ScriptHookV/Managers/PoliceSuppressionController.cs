using System;
using FactionWars.Combat.Interfaces;
using FactionWars.Core.Interfaces;
using FactionWars.ScriptHookV.Logging;

namespace FactionWars.ScriptHookV.Managers
{
    /// <summary>
    /// Suppresses ambient police response while the player is participating in a faction battle.
    /// </summary>
    public sealed class PoliceSuppressionController : IDisposable
    {
        private readonly IGameBridge _bridge;
        private readonly IZoneBattleManager _battleManager;
        private bool _suppressionEnabled;
        private bool _disposed;

        public PoliceSuppressionController(IGameBridge bridge, IZoneBattleManager battleManager)
        {
            _bridge = bridge ?? throw new ArgumentNullException(nameof(bridge));
            _battleManager = battleManager ?? throw new ArgumentNullException(nameof(battleManager));
        }

        public bool IsSuppressionEnabled => _suppressionEnabled;

        public void Update()
        {
            if (_disposed) return;

            var shouldSuppress = _battleManager.IsPlayerInBattle();
            if (shouldSuppress != _suppressionEnabled)
            {
                _suppressionEnabled = shouldSuppress;
                _bridge.SetPoliceSuppressionEnabled(shouldSuppress);
                FileLogger.Info("Police suppression " + (shouldSuppress ? "enabled" : "disabled"));
            }

            if (_suppressionEnabled)
            {
                _bridge.ClearWantedLevel();
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            if (_suppressionEnabled)
            {
                _suppressionEnabled = false;
                _bridge.SetPoliceSuppressionEnabled(false);
            }
        }
    }
}
