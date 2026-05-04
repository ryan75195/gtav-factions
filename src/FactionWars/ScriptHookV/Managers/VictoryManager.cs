using System;
using FactionWars.Core.Interfaces;
using FactionWars.Factions.Interfaces;
using FactionWars.UI.Interfaces;
using FactionWars.UI.Models;

namespace FactionWars.ScriptHookV.Managers
{
    /// <summary>
    /// Manages victory condition detection and victory screen display.
    /// Periodically checks if any faction has achieved 100% zone control
    /// and displays a victory notification when this occurs.
    /// </summary>
    public class VictoryManager
    {
        private readonly IVictoryConditionService _victoryConditionService;
        private readonly IFactionService _factionService;
        private readonly INotificationService _notificationService;

        private bool _isRunning;
        private bool _isVictoryAchieved;
        private string? _winningFactionId;
        private float _timeSinceLastCheck;
        private float _checkIntervalSeconds;

        private const float DefaultCheckInterval = 1.0f;
        private const float MinCheckInterval = 0.1f;
        private const float VictoryNotificationDuration = 10.0f;

        /// <summary>
        /// Event raised when a faction achieves victory.
        /// </summary>
        public event EventHandler<VictoryEventArgs>? OnVictory;

        /// <summary>
        /// Gets whether the victory manager is currently running.
        /// </summary>
        public bool IsRunning => _isRunning;

        /// <summary>
        /// Gets whether a victory has been achieved.
        /// </summary>
        public bool IsVictoryAchieved => _isVictoryAchieved;

        /// <summary>
        /// Gets the interval between victory condition checks in seconds.
        /// </summary>
        public float CheckIntervalSeconds => _checkIntervalSeconds;

        /// <summary>
        /// Creates a new VictoryManager.
        /// </summary>
        /// <param name="victoryConditionService">Service for checking victory conditions.</param>
        /// <param name="factionService">Service for retrieving faction information.</param>
        /// <param name="notificationService">Service for displaying notifications.</param>
        /// <exception cref="ArgumentNullException">Thrown if any parameter is null.</exception>
        public VictoryManager(
            IVictoryConditionService victoryConditionService,
            IFactionService factionService,
            INotificationService notificationService)
        {
            _victoryConditionService = victoryConditionService ?? throw new ArgumentNullException(nameof(victoryConditionService));
            _factionService = factionService ?? throw new ArgumentNullException(nameof(factionService));
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));

            _isRunning = false;
            _isVictoryAchieved = false;
            _winningFactionId = null;
            _timeSinceLastCheck = 0f;
            _checkIntervalSeconds = DefaultCheckInterval;
        }

        /// <summary>
        /// Starts the victory manager. Victory condition checks will begin.
        /// </summary>
        public void Start()
        {
            _isRunning = true;
            _timeSinceLastCheck = 0f;
        }

        /// <summary>
        /// Stops the victory manager. Victory condition checks will stop.
        /// </summary>
        public void Stop()
        {
            _isRunning = false;
        }

        /// <summary>
        /// Sets the interval between victory condition checks.
        /// </summary>
        /// <param name="intervalSeconds">The interval in seconds (minimum 0.1).</param>
        public void SetCheckInterval(float intervalSeconds)
        {
            _checkIntervalSeconds = Math.Max(MinCheckInterval, intervalSeconds);
        }

        /// <summary>
        /// Updates the victory manager with elapsed time.
        /// Should be called each frame/update cycle.
        /// </summary>
        /// <param name="deltaTimeSeconds">The time elapsed since the last update in seconds.</param>
        public void Update(float deltaTimeSeconds)
        {
            if (!_isRunning)
                return;

            // Don't check again if victory already achieved
            if (_isVictoryAchieved)
                return;

            _timeSinceLastCheck += deltaTimeSeconds;

            if (_timeSinceLastCheck >= _checkIntervalSeconds)
            {
                _timeSinceLastCheck = 0f;
                CheckForVictory();
            }
        }

        /// <summary>
        /// Gets the ID of the winning faction, if victory has been achieved.
        /// </summary>
        /// <returns>The winning faction ID, or null if no victory.</returns>
        public string? GetWinningFactionId()
        {
            return _winningFactionId;
        }

        /// <summary>
        /// Resets the victory state, allowing victory to be detected again.
        /// Used when starting a new game or loading a save.
        /// </summary>
        public void Reset()
        {
            _isVictoryAchieved = false;
            _winningFactionId = null;
            _timeSinceLastCheck = 0f;
        }

        /// <summary>
        /// Checks if any faction has achieved victory.
        /// </summary>
        private void CheckForVictory()
        {
            if (!_victoryConditionService.IsGameOver())
                return;

            var winningFactionId = _victoryConditionService.GetWinningFactionId();
            if (winningFactionId == null)
                return;

            var faction = _factionService.GetFaction(winningFactionId);
            if (faction == null)
                return;

            _isVictoryAchieved = true;
            _winningFactionId = winningFactionId;

            // Display victory notification
            DisplayVictoryScreen(faction.Name);

            // Raise victory event
            OnVictory?.Invoke(this, new VictoryEventArgs(winningFactionId, faction.Name));
        }

        /// <summary>
        /// Displays the victory notification to the player.
        /// </summary>
        /// <param name="factionName">The name of the winning faction.</param>
        private void DisplayVictoryScreen(string factionName)
        {
            _notificationService.ShowSuccess(
                "Victory!",
                $"{factionName} has achieved total control of Los Santos!",
                NotificationPriority.Critical,
                VictoryNotificationDuration);
        }
    }
}
