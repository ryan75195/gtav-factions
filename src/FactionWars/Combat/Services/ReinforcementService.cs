using System;
using System.Collections.Generic;
using FactionWars.Combat.Interfaces;
using FactionWars.Combat.Models;
using FactionWars.Core.Interfaces;

namespace FactionWars.Combat.Services
{
    /// <summary>
    /// Service for managing reinforcement mechanics during combat encounters.
    /// Coordinates ped spawning with cooldowns, wave limits, and resource constraints.
    /// </summary>
    public class ReinforcementService : IReinforcementService
    {
        private readonly IPedSpawningService _pedSpawningService;
        private readonly ITimeProvider _timeProvider;
        private readonly ReinforcementConfig _config;

        // Tracks last request time per faction per encounter: (encounterId, factionId) -> lastRequestTime
        private readonly Dictionary<(string EncounterId, string FactionId), DateTime> _cooldowns;

        // Tracks active wave counts per faction per encounter
        private readonly Dictionary<(string EncounterId, string FactionId), int> _activeWaves;

        // Default ped model to use for reinforcements
        private const string DefaultPedModel = "a_m_y_mexthug_01";

        /// <summary>
        /// Creates a new ReinforcementService.
        /// </summary>
        /// <param name="pedSpawningService">The service for spawning peds.</param>
        /// <param name="timeProvider">The time provider for cooldown tracking.</param>
        /// <param name="config">The reinforcement configuration.</param>
        /// <exception cref="ArgumentNullException">Thrown if any parameter is null.</exception>
        public ReinforcementService(
            IPedSpawningService pedSpawningService,
            ITimeProvider timeProvider,
            ReinforcementConfig config)
        {
            _pedSpawningService = pedSpawningService ?? throw new ArgumentNullException(nameof(pedSpawningService));
            _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _cooldowns = new Dictionary<(string, string), DateTime>();
            _activeWaves = new Dictionary<(string, string), int>();
        }

        /// <inheritdoc />
        public ReinforcementResult RequestReinforcements(ReinforcementRequest request, CombatEncounter encounter)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));
            if (encounter == null)
                throw new ArgumentNullException(nameof(encounter));

            // Check if encounter is still active
            if (!encounter.IsActive)
            {
                return ReinforcementResult.EncounterEnded();
            }

            // Check if pool can spawn any peds
            if (!_pedSpawningService.CanSpawn())
            {
                return ReinforcementResult.PoolFull();
            }

            var key = (encounter.Id, request.FactionId);

            // Check cooldown
            var remainingCooldown = GetRemainingCooldownInternal(key);
            if (remainingCooldown > 0)
            {
                return ReinforcementResult.OnCooldown(remainingCooldown);
            }

            // Determine spawn count
            int spawnCount = CalculateSpawnCount(request.RequestedCount);

            // Clamp to available pool slots
            int availableSlots = _pedSpawningService.CanSpawnCount();
            spawnCount = Math.Min(spawnCount, availableSlots);

            if (spawnCount <= 0)
            {
                return ReinforcementResult.PoolFull();
            }

            // Spawn the peds
            var spawnedPeds = _pedSpawningService.SpawnMultiplePeds(
                DefaultPedModel,
                request.SpawnPosition,
                request.FactionId,
                request.ZoneId,
                spawnCount);

            // Update cooldown
            _cooldowns[key] = _timeProvider.UtcNow;

            // Update active wave count
            if (!_activeWaves.ContainsKey(key))
            {
                _activeWaves[key] = 0;
            }
            _activeWaves[key]++;

            // Calculate resource cost
            int resourceCost = spawnedPeds.Count * _config.ResourceCostPerPed;

            // Determine result status
            if (spawnedPeds.Count == 0)
            {
                return ReinforcementResult.PoolFull();
            }
            else if (spawnedPeds.Count < spawnCount)
            {
                return ReinforcementResult.PartialSuccess(
                    (IList<PedHandle>)spawnedPeds,
                    request.RequestedCount,
                    resourceCost,
                    "Pool capacity reached");
            }
            else if (spawnedPeds.Count < request.RequestedCount)
            {
                return ReinforcementResult.PartialSuccess(
                    (IList<PedHandle>)spawnedPeds,
                    request.RequestedCount,
                    resourceCost,
                    "Spawned count limited by configuration or pool");
            }
            else
            {
                return ReinforcementResult.Success((IList<PedHandle>)spawnedPeds, resourceCost);
            }
        }

        /// <inheritdoc />
        public ReinforcementResult RequestReinforcements(ReinforcementRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            // This overload would require an encounter registry to look up encounters by ID
            // For now, throw not implemented - callers should use the overload with encounter
            throw new NotImplementedException(
                "Use the overload that accepts a CombatEncounter. Encounter lookup by ID is not yet implemented.");
        }

        /// <inheritdoc />
        public bool CanRequestReinforcements(string factionId, CombatEncounter encounter)
        {
            if (factionId == null)
                throw new ArgumentNullException(nameof(factionId));
            if (encounter == null)
                throw new ArgumentNullException(nameof(encounter));

            // Check if encounter is active
            if (!encounter.IsActive)
                return false;

            // Check if pool can spawn
            if (!_pedSpawningService.CanSpawn())
                return false;

            // Check cooldown
            var key = (encounter.Id, factionId);
            if (GetRemainingCooldownInternal(key) > 0)
                return false;

            return true;
        }

        /// <inheritdoc />
        public float GetRemainingCooldown(string factionId, CombatEncounter encounter)
        {
            if (factionId == null)
                throw new ArgumentNullException(nameof(factionId));
            if (encounter == null)
                throw new ArgumentNullException(nameof(encounter));

            var key = (encounter.Id, factionId);
            return GetRemainingCooldownInternal(key);
        }

        /// <inheritdoc />
        public int GetActiveWaveCount(string factionId, CombatEncounter encounter)
        {
            if (factionId == null)
                throw new ArgumentNullException(nameof(factionId));
            if (encounter == null)
                throw new ArgumentNullException(nameof(encounter));

            var key = (encounter.Id, factionId);
            return _activeWaves.TryGetValue(key, out var count) ? count : 0;
        }

        /// <inheritdoc />
        public void ResetCooldown(string factionId, CombatEncounter encounter)
        {
            if (factionId == null)
                throw new ArgumentNullException(nameof(factionId));
            if (encounter == null)
                throw new ArgumentNullException(nameof(encounter));

            var key = (encounter.Id, factionId);
            _cooldowns.Remove(key);
        }

        /// <inheritdoc />
        public ReinforcementConfig GetCurrentConfig()
        {
            // Return a copy to prevent external modification
            return new ReinforcementConfig
            {
                CooldownSeconds = _config.CooldownSeconds,
                MinPedsPerWave = _config.MinPedsPerWave,
                MaxPedsPerWave = _config.MaxPedsPerWave,
                MaxActiveWaves = _config.MaxActiveWaves,
                RequiresResources = _config.RequiresResources,
                ResourceCostPerPed = _config.ResourceCostPerPed
            };
        }

        private float GetRemainingCooldownInternal((string EncounterId, string FactionId) key)
        {
            if (!_cooldowns.TryGetValue(key, out var lastRequest))
            {
                return 0f;
            }

            var elapsed = (float)(_timeProvider.UtcNow - lastRequest).TotalSeconds;
            var remaining = _config.CooldownSeconds - elapsed;

            return remaining > 0 ? remaining : 0f;
        }

        private int CalculateSpawnCount(int requestedCount)
        {
            // Clamp to min/max per wave
            int count = Math.Max(requestedCount, _config.MinPedsPerWave);
            count = Math.Min(count, _config.MaxPedsPerWave);
            return count;
        }
    }
}
