using System;
using System.Collections.Generic;
using System.Linq;
using FactionWars.AI.Interfaces;
using FactionWars.Combat.Interfaces;
using FactionWars.Combat.Models;
using FactionWars.Core.Interfaces;
using FactionWars.Core.Models;
using FactionWars.Territory.Models;
using FactionWars.ScriptHookV.Logging;

namespace FactionWars.ScriptHookV.Managers
{
    /// <summary>
    /// Manages combat encounters between factions in zones.
    /// Coordinates ped tracking, control percentage calculations, and takeover detection.
    /// Supports wave-based spawning where Heavy defenders spawn first, then Medium, then Basic.
    /// </summary>
    public class CombatManager
    {
        private readonly IGameBridge _gameBridge;
        private readonly IPedPool _pedPool;
        private readonly IPedSpawningService _pedSpawningService;
        private readonly ISpawnPositionCalculator _spawnPositionCalculator;
        private readonly IControlPercentageCalculator _controlCalculator;
        private readonly ITakeoverDetector _takeoverDetector;
        private readonly ICombatResultHandler _combatResultHandler;
        private readonly IWaveSpawnerService _waveSpawnerService;
        private readonly IFollowerService _followerService;
        private readonly IAggressionResponseService _aggressionResponseService;

        private CombatEncounter? _currentEncounter;
        private WaveState? _currentWaveState;
        private int _nextEncounterId = 1;

        /// <summary>
        /// Gets the current combat encounter, or null if not in combat.
        /// </summary>
        public CombatEncounter? CurrentEncounter => _currentEncounter;

        /// <summary>
        /// Gets whether the player is currently in combat.
        /// </summary>
        public bool IsInCombat => _currentEncounter != null;

        /// <summary>
        /// Gets the current wave state for wave-based spawning, or null if not in combat.
        /// </summary>
        public WaveState? CurrentWaveState => _currentWaveState;

        /// <summary>
        /// Raised when a combat encounter starts.
        /// </summary>
        public event EventHandler<CombatEncounter>? CombatStarted;

        /// <summary>
        /// Raised when a combat encounter ends.
        /// </summary>
        public event EventHandler<CombatEncounter>? CombatEnded;

        /// <summary>
        /// Creates a new CombatManager.
        /// </summary>
        /// <param name="gameBridge">The game bridge for game interactions.</param>
        /// <param name="pedPool">The ped pool for tracking peds.</param>
        /// <param name="pedSpawningService">Service for spawning peds.</param>
        /// <param name="spawnPositionCalculator">Calculator for spawn positions.</param>
        /// <param name="controlCalculator">Calculator for control percentages.</param>
        /// <param name="takeoverDetector">Detector for takeover thresholds.</param>
        /// <param name="combatResultHandler">Handler for processing combat results.</param>
        /// <param name="waveSpawnerService">Service for wave-based defender spawning.</param>
        /// <param name="followerService">Service for tracking followers who count as attackers.</param>
        /// <param name="aggressionResponseService">Service for recording player aggression for AI retaliation.</param>
        /// <exception cref="ArgumentNullException">Thrown if any parameter is null.</exception>
        public CombatManager(
            IGameBridge gameBridge,
            IPedPool pedPool,
            IPedSpawningService pedSpawningService,
            ISpawnPositionCalculator spawnPositionCalculator,
            IControlPercentageCalculator controlCalculator,
            ITakeoverDetector takeoverDetector,
            ICombatResultHandler combatResultHandler,
            IWaveSpawnerService waveSpawnerService,
            IFollowerService followerService,
            IAggressionResponseService aggressionResponseService)
        {
            _gameBridge = gameBridge ?? throw new ArgumentNullException(nameof(gameBridge));
            _pedPool = pedPool ?? throw new ArgumentNullException(nameof(pedPool));
            _pedSpawningService = pedSpawningService ?? throw new ArgumentNullException(nameof(pedSpawningService));
            _spawnPositionCalculator = spawnPositionCalculator ?? throw new ArgumentNullException(nameof(spawnPositionCalculator));
            _controlCalculator = controlCalculator ?? throw new ArgumentNullException(nameof(controlCalculator));
            _takeoverDetector = takeoverDetector ?? throw new ArgumentNullException(nameof(takeoverDetector));
            _combatResultHandler = combatResultHandler ?? throw new ArgumentNullException(nameof(combatResultHandler));
            _waveSpawnerService = waveSpawnerService ?? throw new ArgumentNullException(nameof(waveSpawnerService));
            _followerService = followerService ?? throw new ArgumentNullException(nameof(followerService));
            _aggressionResponseService = aggressionResponseService ?? throw new ArgumentNullException(nameof(aggressionResponseService));
        }

        /// <summary>
        /// Starts a combat encounter in the specified zone.
        /// If already in combat, returns the existing encounter.
        /// </summary>
        /// <param name="zone">The zone where combat will take place.</param>
        /// <param name="attackingFactionId">The ID of the attacking faction.</param>
        /// <returns>The combat encounter (new or existing).</returns>
        /// <exception cref="ArgumentNullException">Thrown if zone or attackingFactionId is null.</exception>
        /// <exception cref="ArgumentException">Thrown if zone has no owner or attacker is the same as defender.</exception>
        public CombatEncounter StartCombat(Zone zone, string attackingFactionId)
        {
            FileLogger.Combat($"CombatManager.StartCombat: zone={zone?.Id}, attacker={attackingFactionId}, defender={zone?.OwnerFactionId}");

            if (zone == null)
                throw new ArgumentNullException(nameof(zone));
            if (attackingFactionId == null)
                throw new ArgumentNullException(nameof(attackingFactionId));

            // If already in combat, return existing encounter
            if (_currentEncounter != null)
            {
                FileLogger.Combat($"CombatManager.StartCombat: Already in combat (encounter={_currentEncounter.Id}), returning existing");
                return _currentEncounter;
            }

            // Validate zone has an owner
            if (zone.OwnerFactionId == null)
                throw new ArgumentException("Cannot start combat in a neutral zone with no owner.", nameof(zone));

            // Validate attacker is not the same as defender
            if (zone.OwnerFactionId == attackingFactionId)
                throw new ArgumentException("Attacking faction cannot be the same as defending faction.", nameof(attackingFactionId));

            // Create new encounter
            var encounterId = $"combat_{_nextEncounterId++}";
            _currentEncounter = new CombatEncounter(
                encounterId,
                zone.Id,
                attackingFactionId,
                zone.OwnerFactionId);

            FileLogger.Combat($"CombatManager.StartCombat: Created encounter {encounterId}, attacker={attackingFactionId}, defender={zone.OwnerFactionId}");

            // Record player aggression for AI retaliation tracking
            _aggressionResponseService.RecordAggression(
                attackingFactionId,
                zone.Id,
                damage: 1);  // Base damage value

            FileLogger.Combat($"CombatManager.StartCombat: Recorded aggression for attacker={attackingFactionId} against zone={zone.Id}");

            // Raise event
            CombatStarted?.Invoke(this, _currentEncounter);

            return _currentEncounter;
        }

        /// <summary>
        /// Ends the current combat encounter with the specified status.
        /// Processes the combat result unless combat is aborted.
        /// </summary>
        /// <param name="status">The final status of the combat.</param>
        public void EndCombat(CombatStatus status)
        {
            if (_currentEncounter == null)
            {
                FileLogger.Combat("CombatManager.EndCombat: No current encounter to end");
                return;
            }

            FileLogger.Combat($"CombatManager.EndCombat: Ending encounter {_currentEncounter.Id} with status={status}");
            FileLogger.Combat($"CombatManager.EndCombat: Final state - attackers={_currentEncounter.AttackerPedCount}, defenders={_currentEncounter.DefenderPedCount}, control={_currentEncounter.AttackerControlPercentage:F1}%/{_currentEncounter.DefenderControlPercentage:F1}%");

            var encounter = _currentEncounter;
            encounter.End(status);

            // Only process result for non-aborted/non-retreat combat
            if (status != CombatStatus.Aborted && status != CombatStatus.Stalemate && status != CombatStatus.PlayerRetreat)
            {
                FileLogger.Combat($"CombatManager.EndCombat: Processing combat result");
                _combatResultHandler.ProcessCombatResult(encounter);
            }
            else
            {
                FileLogger.Combat($"CombatManager.EndCombat: Skipping result processing for status={status}");
            }

            _currentEncounter = null;
            _currentWaveState = null;

            // Raise event
            CombatEnded?.Invoke(this, encounter);
            FileLogger.Combat($"CombatManager.EndCombat: Combat ended, CombatEnded event raised");
        }

        /// <summary>
        /// Aborts the current combat encounter.
        /// The zone ownership remains unchanged.
        /// </summary>
        public void AbortCombat()
        {
            if (_currentEncounter == null)
                return;

            var encounter = _currentEncounter;
            encounter.End(CombatStatus.Aborted);
            _currentEncounter = null;
            _currentWaveState = null;

            // Raise event
            CombatEnded?.Invoke(this, encounter);
        }

        /// <summary>
        /// Forces the player to retreat from the current combat encounter.
        /// Used when the player dies in a contested zone.
        /// The zone ownership remains unchanged.
        /// </summary>
        public void Retreat()
        {
            if (_currentEncounter == null)
                return;

            var encounter = _currentEncounter;
            encounter.End(CombatStatus.PlayerRetreat);
            _currentEncounter = null;
            _currentWaveState = null;

            // Raise event
            CombatEnded?.Invoke(this, encounter);
        }

        /// <summary>
        /// Updates the combat state. Should be called each game tick.
        /// Updates ped counts, control percentages, and checks for takeover.
        /// Also checks for player death which triggers a retreat.
        /// </summary>
        public void Update()
        {
            if (_currentEncounter == null)
            {
                return;
            }

            FileLogger.Debug($"CombatManager.Update: Encounter={_currentEncounter.Id}, Zone={_currentEncounter.ZoneId}");

            // Check for player death first - if dead, retreat from combat
            if (_gameBridge.IsPlayerDead())
            {
                FileLogger.Combat("CombatManager.Update: Player is dead, triggering retreat");
                Retreat();
                return;
            }

            // Count attackers: Player (always 1) + followers from attacking faction
            // Player is always counted as an attacker when in combat
            int attackerCount = 1; // Player
            int followerCount = _followerService.GetFollowerCount(_currentEncounter.AttackingFactionId);
            attackerCount += followerCount;

            // Also count any attacker peds from the pool (in case of AI attackers in future)
            var attackerPeds = _pedPool.GetByFactionAndZone(
                _currentEncounter.AttackingFactionId,
                _currentEncounter.ZoneId).ToList();
            attackerCount += attackerPeds.Count;

            // Count defenders from ped pool (spawned NPCs)
            var defenderPeds = _pedPool.GetByFactionAndZone(
                _currentEncounter.DefendingFactionId,
                _currentEncounter.ZoneId).ToList();

            FileLogger.Debug($"CombatManager.Update: Attackers - player=1, followers={followerCount}, pedPool={attackerPeds.Count}, total={attackerCount}");
            FileLogger.Debug($"CombatManager.Update: Defenders - pedPool={defenderPeds.Count} (faction={_currentEncounter.DefendingFactionId})");

            // Update ped counts on encounter
            _currentEncounter.AttackerPedCount = attackerCount;
            _currentEncounter.DefenderPedCount = defenderPeds.Count;

            // Calculate control percentages using total attacker count (player + followers + NPCs)
            var controlResult = _controlCalculator.Calculate(attackerCount, defenderPeds.Count);
            _currentEncounter.AttackerControlPercentage = controlResult.AttackerPercentage;
            _currentEncounter.DefenderControlPercentage = controlResult.DefenderPercentage;

            FileLogger.Combat($"CombatManager.Update: Control - attacker={controlResult.AttackerPercentage:F1}%, defender={controlResult.DefenderPercentage:F1}%, total={controlResult.TotalPeds}");

            // Don't check for takeover until defenders have had a chance to spawn
            // This prevents instant victory when entering a zone before any defenders appear
            bool defendersStillSpawning = _currentWaveState != null && !IsWaveSpawningComplete() && defenderPeds.Count == 0;
            if (defendersStillSpawning)
            {
                FileLogger.Debug($"CombatManager.Update: Skipping takeover check - defenders still spawning (waveComplete={IsWaveSpawningComplete()}, defenderCount={defenderPeds.Count})");
                return;
            }

            // Check for takeover
            var takeoverResult = _takeoverDetector.CheckTakeover(
                controlResult.AttackerPercentage,
                controlResult.DefenderPercentage,
                _currentEncounter.AttackingFactionId,
                _currentEncounter.DefendingFactionId);

            FileLogger.Debug($"CombatManager.Update: Takeover check - status={takeoverResult.Status}, complete={takeoverResult.IsTakeoverComplete}");

            // End combat if takeover detected
            if (takeoverResult.IsTakeoverComplete)
            {
                var status = takeoverResult.Status == TakeoverStatus.AttackerVictory
                    ? CombatStatus.AttackerVictory
                    : CombatStatus.DefenderVictory;
                FileLogger.Combat($"CombatManager.Update: COMBAT ENDING with status={status}");
                EndCombat(status);
            }
        }

        /// <summary>
        /// Spawns defenders at natural positions behind the player.
        /// Uses the spawn position calculator for natural-feeling spawns.
        /// </summary>
        /// <param name="modelName">The model name for the defender peds.</param>
        /// <param name="factionId">The faction ID for the defenders.</param>
        /// <param name="count">The number of defenders to spawn.</param>
        /// <returns>A list of spawned ped handles.</returns>
        /// <exception cref="InvalidOperationException">Thrown if not currently in combat.</exception>
        public IList<PedHandle> SpawnDefenders(string modelName, string factionId, int count)
        {
            if (_currentEncounter == null)
                throw new InvalidOperationException("Cannot spawn defenders when not in combat.");

            var spawnedPeds = new List<PedHandle>();

            if (count <= 0 || !_pedSpawningService.CanSpawn())
                return spawnedPeds;

            // Get natural spawn positions behind the player
            var spawnPositions = _spawnPositionCalculator.CalculateNaturalSpawnPositions(count);

            // Spawn peds at each position
            foreach (var position in spawnPositions)
            {
                if (!_pedSpawningService.CanSpawn())
                    break;

                var ped = _pedSpawningService.SpawnPed(modelName, position, factionId, _currentEncounter.ZoneId);
                if (ped.IsValid)
                {
                    spawnedPeds.Add(ped);
                }
            }

            return spawnedPeds;
        }

        /// <summary>
        /// Spawns a single defender at a natural position behind the player.
        /// </summary>
        /// <param name="modelName">The model name for the defender ped.</param>
        /// <param name="factionId">The faction ID for the defender.</param>
        /// <returns>The spawned ped handle, or PedHandle.Invalid if spawn failed.</returns>
        /// <exception cref="InvalidOperationException">Thrown if not currently in combat.</exception>
        public PedHandle SpawnDefender(string modelName, string factionId)
        {
            if (_currentEncounter == null)
                throw new InvalidOperationException("Cannot spawn defender when not in combat.");

            if (!_pedSpawningService.CanSpawn())
                return PedHandle.Invalid;

            var spawnPosition = _spawnPositionCalculator.CalculateNaturalSpawnPosition();
            return _pedSpawningService.SpawnPed(modelName, spawnPosition, factionId, _currentEncounter.ZoneId);
        }

        /// <summary>
        /// Gets whether the ped spawning service can spawn more peds.
        /// </summary>
        /// <returns>True if at least one ped can be spawned.</returns>
        public bool CanSpawnDefenders() => _pedSpawningService.CanSpawn();

        /// <summary>
        /// Gets the number of defenders that can currently be spawned.
        /// </summary>
        /// <returns>The number of available slots in the ped pool.</returns>
        public int CanSpawnDefendersCount() => _pedSpawningService.CanSpawnCount();

        #region Wave-Based Spawning

        /// <summary>
        /// Initializes wave-based spawning for the current combat with a spawn plan.
        /// Waves spawn in order: Heavy → Medium → Basic.
        /// </summary>
        /// <param name="spawnPlan">The spawn plan defining how many peds of each tier to spawn.</param>
        /// <exception cref="InvalidOperationException">Thrown if not currently in combat.</exception>
        /// <exception cref="ArgumentNullException">Thrown if spawnPlan is null.</exception>
        public void InitializeWaveSpawning(DefenderSpawnPlan spawnPlan)
        {
            FileLogger.Combat($"CombatManager.InitializeWaveSpawning: plan={spawnPlan?.TotalPeds ?? 0} total peds");

            if (_currentEncounter == null)
                throw new InvalidOperationException("Cannot initialize wave spawning when not in combat.");
            if (spawnPlan == null)
                throw new ArgumentNullException(nameof(spawnPlan));

            _currentWaveState = _waveSpawnerService.CreateWaveState(spawnPlan);
            FileLogger.Combat($"CombatManager.InitializeWaveSpawning: WaveState created, Heavy={spawnPlan.GetPedCount(DefenderTier.Heavy)}, Medium={spawnPlan.GetPedCount(DefenderTier.Medium)}, Basic={spawnPlan.GetPedCount(DefenderTier.Basic)}");
        }

        /// <summary>
        /// Gets the next defender tier that should spawn in the current wave sequence.
        /// Returns null if all waves are complete or wave spawning is not initialized.
        /// </summary>
        /// <returns>The next tier to spawn, or null if complete.</returns>
        public DefenderTier? GetNextWaveTier()
        {
            if (_currentWaveState == null)
                return null;

            return _waveSpawnerService.GetNextWaveTier(_currentWaveState);
        }

        /// <summary>
        /// Spawns defenders for the next wave, using the appropriate model for the tier.
        /// Spawns Heavy first, then Medium, then Basic.
        /// </summary>
        /// <param name="modelsByTier">Dictionary mapping tiers to ped model names.</param>
        /// <param name="factionId">The faction ID for the defenders.</param>
        /// <param name="maxPerTick">Maximum peds to spawn in this tick.</param>
        /// <returns>A list of spawned ped handles.</returns>
        /// <exception cref="InvalidOperationException">Thrown if not in combat or wave spawning not initialized.</exception>
        /// <exception cref="ArgumentNullException">Thrown if modelsByTier or factionId is null.</exception>
        public IList<PedHandle> SpawnNextWave(Dictionary<DefenderTier, string> modelsByTier, string factionId, int maxPerTick)
        {
            FileLogger.Spawn($"CombatManager.SpawnNextWave: factionId={factionId}, maxPerTick={maxPerTick}");

            if (_currentEncounter == null)
                throw new InvalidOperationException("Cannot spawn wave when not in combat.");
            if (_currentWaveState == null)
                throw new InvalidOperationException("Wave spawning not initialized. Call InitializeWaveSpawning first.");
            if (modelsByTier == null)
                throw new ArgumentNullException(nameof(modelsByTier));
            if (string.IsNullOrEmpty(factionId))
                throw new ArgumentNullException(nameof(factionId));

            var spawnedPeds = new List<PedHandle>();

            // Get the next tier to spawn
            var nextTier = _waveSpawnerService.GetNextWaveTier(_currentWaveState);
            if (nextTier == null)
            {
                FileLogger.Spawn("CombatManager.SpawnNextWave: All waves complete, no more tiers to spawn");
                return spawnedPeds; // All waves complete
            }

            var tier = nextTier.Value;
            FileLogger.Spawn($"CombatManager.SpawnNextWave: Next tier={tier}");

            // Get model name for this tier
            if (!modelsByTier.TryGetValue(tier, out var modelName) || string.IsNullOrEmpty(modelName))
            {
                FileLogger.Warn($"CombatManager.SpawnNextWave: No model configured for tier {tier}");
                return spawnedPeds; // No model configured for this tier
            }

            FileLogger.Spawn($"CombatManager.SpawnNextWave: Using model '{modelName}' for tier {tier}");

            // Calculate how many to spawn this tick
            int toSpawn = _waveSpawnerService.GetSpawnCountForWave(_currentWaveState, tier, maxPerTick);
            if (toSpawn <= 0)
            {
                FileLogger.Spawn($"CombatManager.SpawnNextWave: No peds to spawn (toSpawn={toSpawn})");
                return spawnedPeds;
            }

            FileLogger.Spawn($"CombatManager.SpawnNextWave: Will spawn {toSpawn} peds");

            // Get spawn positions
            var spawnPositions = _spawnPositionCalculator.CalculateNaturalSpawnPositions(toSpawn);
            FileLogger.Spawn($"CombatManager.SpawnNextWave: Got {spawnPositions.Count} spawn positions");

            // Spawn peds and track how many we actually spawned
            int actuallySpawned = 0;
            int positionIndex = 0;
            foreach (var position in spawnPositions)
            {
                if (!_pedSpawningService.CanSpawn())
                {
                    FileLogger.Spawn($"CombatManager.SpawnNextWave: CanSpawn=false, stopping spawn loop");
                    break;
                }

                FileLogger.Spawn($"CombatManager.SpawnNextWave: Spawning ped {positionIndex + 1} at ({position.X:F1}, {position.Y:F1}, {position.Z:F1})");
                var ped = _pedSpawningService.SpawnPed(modelName, position, factionId, _currentEncounter.ZoneId);
                if (ped.IsValid)
                {
                    FileLogger.Spawn($"CombatManager.SpawnNextWave: Spawned ped handle={ped.Handle}");
                    spawnedPeds.Add(ped);
                    actuallySpawned++;
                }
                else
                {
                    FileLogger.Warn($"CombatManager.SpawnNextWave: Failed to spawn ped at position {positionIndex}");
                }
                positionIndex++;
            }

            // Record how many we spawned in the wave state
            if (actuallySpawned > 0)
            {
                _currentWaveState.RecordSpawned(tier, actuallySpawned);
                FileLogger.Spawn($"CombatManager.SpawnNextWave: Recorded {actuallySpawned} spawned for tier {tier}");
            }

            FileLogger.Spawn($"CombatManager.SpawnNextWave: Completed - spawned {actuallySpawned} peds, remaining={_currentWaveState.TotalRemaining}");
            return spawnedPeds;
        }

        /// <summary>
        /// Checks if wave-based spawning is complete (all tiers fully spawned).
        /// </summary>
        /// <returns>True if all waves are complete or wave spawning is not initialized.</returns>
        public bool IsWaveSpawningComplete()
        {
            return _currentWaveState == null || _currentWaveState.IsComplete;
        }

        /// <summary>
        /// Gets the total remaining defenders to spawn across all tiers.
        /// </summary>
        /// <returns>The number of remaining defenders, or 0 if wave spawning is not initialized.</returns>
        public int GetRemainingDefendersToSpawn()
        {
            return _currentWaveState?.TotalRemaining ?? 0;
        }

        /// <summary>
        /// Gets the wave spawn order used by this manager.
        /// </summary>
        /// <returns>A list of tiers in spawn order (Heavy → Medium → Basic).</returns>
        public IReadOnlyList<DefenderTier> GetWaveSpawnOrder()
        {
            return _waveSpawnerService.GetWaveOrder();
        }

        #endregion
    }
}
