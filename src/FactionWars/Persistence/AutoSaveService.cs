using FactionWars.Core.Interfaces;
using FactionWars.Persistence.Models;
using System;

namespace FactionWars.Persistence
{
    /// <summary>
    /// Service that automatically saves game state at configurable intervals.
    /// </summary>
    public class AutoSaveService : IAutoSaveService
    {
        private const string DefaultAutoSaveFileName = "autosave.json";
        private static readonly TimeSpan DefaultInterval = TimeSpan.FromMinutes(5);

        private readonly IPersistenceService _persistenceService;
        private readonly IGameStateProvider _gameStateProvider;
        private readonly string _saveDirectory;
        private readonly string _autoSaveFileName;

        private TimeSpan _interval;
        private TimeSpan _timeSinceLastSave;
        private bool _isRunning;
        private bool _isEnabled;
        private DateTime? _lastSaveTime;
        private int _autoSaveCount;
        private bool _disposed;

        /// <inheritdoc />
        public bool IsRunning => _isRunning;

        /// <inheritdoc />
        public bool IsEnabled
        {
            get => _isEnabled;
            set => _isEnabled = value;
        }

        /// <inheritdoc />
        public TimeSpan Interval => _interval;

        /// <inheritdoc />
        public TimeSpan TimeSinceLastSave => _timeSinceLastSave;

        /// <inheritdoc />
        public DateTime? LastSaveTime => _lastSaveTime;

        /// <inheritdoc />
        public int AutoSaveCount => _autoSaveCount;

        /// <inheritdoc />
        public string AutoSaveFilePath { get; }

        /// <inheritdoc />
        public event EventHandler<EventArgs>? AutoSaveStarted;

        /// <inheritdoc />
        public event EventHandler<AutoSaveCompletedEventArgs>? AutoSaveCompleted;

        /// <summary>
        /// Creates a new AutoSaveService with the specified parameters.
        /// </summary>
        /// <param name="persistenceService">The persistence service for saving game state.</param>
        /// <param name="gameStateProvider">The provider for getting current game state.</param>
        /// <param name="saveDirectory">The directory to save auto-save files.</param>
        /// <param name="interval">The auto-save interval (default 5 minutes).</param>
        /// <param name="autoSaveFileName">The auto-save file name (default "autosave.json").</param>
        public AutoSaveService(
            IPersistenceService persistenceService,
            IGameStateProvider gameStateProvider,
            string saveDirectory,
            TimeSpan? interval = null,
            string autoSaveFileName = DefaultAutoSaveFileName)
        {
            if (persistenceService == null)
            {
                throw new ArgumentNullException(nameof(persistenceService));
            }

            if (gameStateProvider == null)
            {
                throw new ArgumentNullException(nameof(gameStateProvider));
            }

            if (saveDirectory == null)
            {
                throw new ArgumentNullException(nameof(saveDirectory));
            }

            if (string.IsNullOrEmpty(saveDirectory))
            {
                throw new ArgumentException("Save directory cannot be empty.", nameof(saveDirectory));
            }

            var effectiveInterval = interval ?? DefaultInterval;
            ValidateInterval(effectiveInterval);

            _persistenceService = persistenceService;
            _gameStateProvider = gameStateProvider;
            _saveDirectory = saveDirectory;
            _autoSaveFileName = autoSaveFileName ?? DefaultAutoSaveFileName;
            _interval = effectiveInterval;
            _timeSinceLastSave = TimeSpan.Zero;
            _isRunning = false;
            _isEnabled = true;
            _lastSaveTime = null;
            _autoSaveCount = 0;
            _disposed = false;

            AutoSaveFilePath = System.IO.Path.Combine(_saveDirectory, _autoSaveFileName);
        }

        /// <inheritdoc />
        public void Start()
        {
            _isRunning = true;
        }

        /// <inheritdoc />
        public void Stop()
        {
            _isRunning = false;
        }

        /// <inheritdoc />
        public void Update(TimeSpan deltaTime)
        {
            if (!_isRunning)
            {
                return;
            }

            _timeSinceLastSave += deltaTime;

            if (_isEnabled && _timeSinceLastSave >= _interval)
            {
                TriggerSave();
                // Keep remainder time for next cycle
                _timeSinceLastSave = _timeSinceLastSave - _interval;
            }
        }

        /// <inheritdoc />
        public void TriggerSave()
        {
            var gameState = _gameStateProvider.GetCurrentGameState();
            if (gameState == null)
            {
                return;
            }

            AutoSaveStarted?.Invoke(this, EventArgs.Empty);

            try
            {
                gameState.SaveName = "Auto Save";
                gameState.MarkModified();
                _persistenceService.Save(gameState, AutoSaveFilePath);
                _lastSaveTime = DateTime.UtcNow;
                _autoSaveCount++;
                AutoSaveCompleted?.Invoke(this, new AutoSaveCompletedEventArgs());
            }
            catch (Exception ex)
            {
                AutoSaveCompleted?.Invoke(this, new AutoSaveCompletedEventArgs(ex));
            }
        }

        /// <inheritdoc />
        public bool HasAutoSave()
        {
            return _persistenceService.Exists(AutoSaveFilePath);
        }

        /// <inheritdoc />
        public GameState LoadAutoSave()
        {
            if (!HasAutoSave())
            {
                throw new InvalidOperationException("No auto-save file exists.");
            }

            return _persistenceService.Load(AutoSaveFilePath);
        }

        /// <inheritdoc />
        public void DeleteAutoSave()
        {
            _persistenceService.Delete(AutoSaveFilePath);
        }

        /// <inheritdoc />
        public void SetInterval(TimeSpan interval)
        {
            ValidateInterval(interval);
            _interval = interval;
        }

        /// <inheritdoc />
        public void ResetTimer()
        {
            _timeSinceLastSave = TimeSpan.Zero;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            Stop();
            _disposed = true;
        }

        private static void ValidateInterval(TimeSpan interval)
        {
            if (interval <= TimeSpan.Zero)
            {
                throw new ArgumentException("Interval must be greater than zero.", nameof(interval));
            }
        }
    }
}
