using System;
using FactionWars.Core.Interfaces;
using FactionWars.Economy.Interfaces;

namespace FactionWars.ScriptHookV.Managers
{
    /// <summary>
    /// Manages economy resource ticks and coordinates with the game loop.
    /// Wraps the IResourceTickService and provides a simple interface for game loop integration.
    /// Integrates with GTA V's money system to add income to player's real cash.
    /// </summary>
    public class EconomyManager
    {
        private readonly IResourceTickService _resourceTickService;
        private readonly IGameBridge _gameBridge;
        private bool _isRunning;
        private string? _playerFactionId;

        /// <summary>
        /// Event raised when a resource tick occurs for any faction.
        /// </summary>
        public event EventHandler<ResourceTickEventArgs>? OnResourceTick;

        /// <summary>
        /// Gets whether the economy manager is currently running.
        /// </summary>
        public bool IsRunning => _isRunning;

        /// <summary>
        /// Gets the progress toward the next tick as a percentage (0-100).
        /// </summary>
        public float TickProgress => _resourceTickService.TickProgress;

        /// <summary>
        /// Gets the time remaining until the next tick in seconds.
        /// </summary>
        public float TimeUntilNextTick => _resourceTickService.TimeUntilNextTick;

        /// <summary>
        /// Gets the interval between resource ticks in seconds.
        /// </summary>
        public int TickIntervalSeconds => _resourceTickService.TickIntervalSeconds;

        /// <summary>
        /// Creates a new EconomyManager.
        /// </summary>
        /// <param name="resourceTickService">The resource tick service for managing resource generation.</param>
        /// <param name="gameBridge">The game bridge for game interactions.</param>
        /// <exception cref="ArgumentNullException">Thrown if any parameter is null.</exception>
        public EconomyManager(IResourceTickService resourceTickService, IGameBridge gameBridge)
        {
            _resourceTickService = resourceTickService ?? throw new ArgumentNullException(nameof(resourceTickService));
            _gameBridge = gameBridge ?? throw new ArgumentNullException(nameof(gameBridge));
            _isRunning = false;

            // Subscribe to resource tick events to forward them
            _resourceTickService.OnResourceTick += HandleResourceTick;
        }

        /// <summary>
        /// Starts the economy system. Resource ticks will begin processing.
        /// </summary>
        public void Start()
        {
            _isRunning = true;
            _resourceTickService.Start();
        }

        /// <summary>
        /// Stops the economy system. Resource ticks will stop processing.
        /// </summary>
        public void Stop()
        {
            _isRunning = false;
            _resourceTickService.Stop();
        }

        /// <summary>
        /// Updates the economy system with elapsed time.
        /// Should be called each frame/update cycle.
        /// </summary>
        /// <param name="deltaTimeSeconds">The time elapsed since the last update in seconds.</param>
        public void Update(float deltaTimeSeconds)
        {
            if (!_isRunning)
                return;

            _resourceTickService.Update(deltaTimeSeconds);
        }

        /// <summary>
        /// Forces an immediate resource tick for all factions, bypassing the timer.
        /// </summary>
        public void ForceTick()
        {
            _resourceTickService.ForceTick();
        }

        /// <summary>
        /// Resets the tick timer to zero elapsed time.
        /// </summary>
        public void Reset()
        {
            _resourceTickService.Reset();
        }

        /// <summary>
        /// Sets the player's faction ID for cash integration.
        /// When a resource tick occurs for this faction, the cash will be added to the player's GTA V money.
        /// </summary>
        /// <param name="factionId">The faction ID of the player, or null to clear.</param>
        public void SetPlayerFactionId(string? factionId)
        {
            _playerFactionId = factionId;
        }

        /// <summary>
        /// Gets the player's faction ID for cash integration.
        /// </summary>
        /// <returns>The player's faction ID, or null if not set.</returns>
        public string? GetPlayerFactionId()
        {
            return _playerFactionId;
        }

        /// <summary>
        /// Handles resource tick events from the resource tick service.
        /// Forwards the event to subscribers and adds cash to player's GTA V money if it's their faction.
        /// </summary>
        private void HandleResourceTick(object? sender, ResourceTickEventArgs e)
        {
            // If this is the player's faction and they earned cash, add it to their GTA V money
            if (_playerFactionId != null &&
                e.FactionId == _playerFactionId &&
                e.CashGenerated > 0)
            {
                _gameBridge.AddPlayerMoney(e.CashGenerated);
            }

            OnResourceTick?.Invoke(this, e);
        }
    }
}
