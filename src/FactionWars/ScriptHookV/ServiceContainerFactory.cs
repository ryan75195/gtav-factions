using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FactionWars.AI.Controllers;
using FactionWars.AI.Interfaces;
using FactionWars.AI.Models;
using FactionWars.AI.Services;
using FactionWars.AI.Strategies;
using FactionWars.Combat.Interfaces;
using FactionWars.Combat.Pools;
using FactionWars.Combat.Services;
using FactionWars.Configuration;
using FactionWars.Core.Interfaces;
using FactionWars.Core.Services;
using FactionWars.Economy.Interfaces;
using FactionWars.Economy.Services;
using FactionWars.Factions.Interfaces;
using FactionWars.Factions.Repositories;
using FactionWars.Factions.Services;
using FactionWars.Persistence;
using FactionWars.Territory.Interfaces;
using FactionWars.Territory.Repositories;
using FactionWars.Territory.Services;
using FactionWars.ScriptHookV.Managers;
using FactionWars.ScriptHookV.Models;
using FactionWars.ScriptHookV.Persistence;
using FactionWars.ScriptHookV.UI;
using FactionWars.Telemetry.Interfaces;
using FactionWars.Telemetry.Sinks;
using FactionWars.UI.Interfaces;
using FactionWars.UI.Services;
using FactionWars.Combat.Models;
using FactionWars.Core.Models;

namespace FactionWars.ScriptHookV
{
    /// <summary>
    /// Factory for creating and wiring up the ServiceContainer with all game services.
    /// This is the composition root for the FactionWars mod.
    /// </summary>
    public static class ServiceContainerFactory
    {
        /// <summary>
        /// Creates a fully configured ServiceContainer with all services wired together.
        /// </summary>
        /// <param name="gameBridge">The game bridge implementation for native calls.</param>
        /// <param name="menuProvider">Optional menu provider for testing. If null, uses NativeUIMenuProvider.</param>
        /// <returns>A configured ServiceContainer ready for use.</returns>
        /// <exception cref="ArgumentNullException">Thrown if gameBridge is null.</exception>
        public static ServiceContainer Create(IGameBridge gameBridge, IMenuProvider? menuProvider = null)
        {
            if (gameBridge == null)
                throw new ArgumentNullException(nameof(gameBridge));

            var container = new ServiceContainer();

            // Load configuration first - other services depend on it
            var configPath = Path.Combine(
                gameBridge.GetScriptsDirectory(),
                "FactionWars",
                "config.json");
            var configLoader = new ConfigLoader(configPath);
            var config = configLoader.Load();
            container.Register<IConfigLoader>(configLoader);
            container.Register(config);

            // Register core infrastructure
            RegisterCoreServices(container, gameBridge);

            // Register repositories (singletons - data stores)
            RegisterRepositories(container);

            // Register domain services
            RegisterDomainServices(container);

            // Register combat services
            RegisterCombatServices(container);

            // Register economy services
            RegisterEconomyServices(container);

            // Register persistence services
            RegisterPersistenceServices(container);

            // Register UI services
            RegisterUIServices(container, menuProvider);

            // Register AI services
            RegisterAIServices(container);

            // Register telemetry services
            RegisterTelemetryServices(container);

            return container;
        }

        private static void RegisterCoreServices(ServiceContainer container, IGameBridge gameBridge)
        {
            // Game bridge is passed in from outside (ScriptHookV-specific)
            container.Register<IGameBridge>(gameBridge);

            // Time provider uses system time
            container.RegisterSingleton<ITimeProvider>(() => new SystemTimeProvider());

            // Player faction detector - maps character models to factions
            container.RegisterSingleton<IPlayerFactionDetector>(() => new CharacterModelFactionDetector());
        }

        private static void RegisterRepositories(ServiceContainer container)
        {
            // Zone repository - singleton
            container.RegisterSingleton<IZoneRepository>(() => new InMemoryZoneRepository());

            // Faction repository - singleton
            container.RegisterSingleton<IFactionRepository>(() => new InMemoryFactionRepository());

            // Ped pool - singleton (tracks all spawned peds)
            container.RegisterSingleton<IPedPool>(() => new InMemoryPedPool());
        }

        private static void RegisterDomainServices(ServiceContainer container)
        {
            // Difficulty service - manages game difficulty settings
            container.RegisterSingleton<IDifficultyService>(() => new DifficultyService());

            // Zone service depends on zone repository and faction repository (for syncing zone ownership)
            container.RegisterSingleton<IZoneService>(() =>
                new ZoneService(
                    container.Resolve<IZoneRepository>(),
                    container.Resolve<IFactionRepository>()));

            // Faction service depends on faction repository
            container.RegisterSingleton<IFactionService>(() =>
                new FactionService(container.Resolve<IFactionRepository>()));

            // Defender tier service - manages defender tier configurations
            container.RegisterSingleton<IDefenderTierService>(() =>
                new DefenderTierService());

            // Follower service - manages player followers (bodyguards)
            container.RegisterSingleton<IFollowerService>(() =>
                new FollowerService());

            // Zone defender allocation repository - stores allocations
            container.RegisterSingleton<IZoneDefenderAllocationRepository>(() =>
                new InMemoryZoneDefenderAllocationRepository());

            // Zone defender allocation service - manages troop deployment to zones
            container.RegisterSingleton<IZoneDefenderAllocationService>(() =>
                new ZoneDefenderAllocationService(
                    container.Resolve<IZoneDefenderAllocationRepository>()));

            // Player context - provides current player's faction
            container.RegisterSingleton<IPlayerContext>(() =>
                new PlayerContext(
                    container.Resolve<IGameBridge>(),
                    container.Resolve<IPlayerFactionDetector>()));

            // Victory condition service - checks for faction victory (100% control)
            container.RegisterSingleton<IVictoryConditionService>(() =>
                new VictoryConditionService(container.Resolve<IZoneService>()));
        }

        private static void RegisterCombatServices(ServiceContainer container)
        {
            // Spawn position calculator depends on game bridge
            container.RegisterSingleton<ISpawnPositionCalculator>(() =>
                new SpawnPositionCalculator(container.Resolve<IGameBridge>()));

            // Ped spawning service depends on game bridge and ped pool
            container.RegisterSingleton<IPedSpawningService>(() =>
                new PedSpawningService(
                    container.Resolve<IGameBridge>(),
                    container.Resolve<IPedPool>()));

            // Ped despawn service depends on game bridge and ped pool
            container.RegisterSingleton<IPedDespawnService>(() =>
                new PedDespawnService(
                    container.Resolve<IGameBridge>(),
                    container.Resolve<IPedPool>()));

            // Ped recycling service depends on game bridge and ped pool
            container.RegisterSingleton<IPedRecyclingService>(() =>
                new PedRecyclingService(
                    container.Resolve<IGameBridge>(),
                    container.Resolve<IPedPool>()));

            // Wave spawner service - no dependencies
            container.RegisterSingleton<IWaveSpawnerService>(() =>
                new WaveSpawnerService());

            // Defender scaling service - scales zone troops to spawnable peds
            container.RegisterSingleton<IDefenderScalingService>(() =>
                new DefenderScalingService());

            // Defender casualty service - processes defender casualties
            container.RegisterSingleton<IDefenderCasualtyService>(() =>
                new DefenderCasualtyService(
                    container.Resolve<IGameBridge>(),
                    container.Resolve<IPedPool>(),
                    container.Resolve<IZoneDefenderAllocationRepository>()));

            // Zone battle manager - unified manager for battle lifecycle. Takes the
            // allocation/faction services so simulated kills decrement source-of-truth
            // troop counts, not just per-battle state.
            container.RegisterSingleton<IZoneBattleManager>(() =>
                new ZoneBattleManager(
                    container.Resolve<IZoneDefenderAllocationService>(),
                    container.Resolve<IFactionService>(),
                    container.Resolve<IZoneService>()));
        }

        private static void RegisterPersistenceServices(ServiceContainer container)
        {
            var config = container.Resolve<GameConfig>();

            // JSON persistence service - handles JSON serialization/deserialization of game state
            container.RegisterSingleton<IPersistenceService>(() =>
                new JsonPersistenceService());

            // Sidecar store - persists mod state alongside GTA V's native saves.
            container.RegisterSingleton<ISidecarStore>(() =>
            {
                var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                var sidecarDirectory = Path.Combine(documentsPath, config.Persistence.SaveDirectoryName, "sidecars");
                return new SidecarStore(sidecarDirectory);
            });

            // Legacy backup - runs once at startup to migrate save_slot_*.json files.
            container.RegisterSingleton<LegacyBackupTask>(() =>
            {
                var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                var saveDirectory = Path.Combine(documentsPath, config.Persistence.SaveDirectoryName);
                return new LegacyBackupTask(saveDirectory);
            });

            // Native save watcher - points at the active Rockstar profile directory.
            container.RegisterSingleton<NativeSaveWatcher>(() =>
            {
                var profileDir = ResolveActiveRockstarProfileDir();
                return new NativeSaveWatcher(profileDir);
            });

            // Game state manager - coordinates save/load between domain repositories and persistence
            container.RegisterSingleton<IGameStateManager>(() =>
                new GameStateManager(
                    container.Resolve<ISidecarStore>(),
                    container.Resolve<IZoneRepository>(),
                    container.Resolve<IFactionRepository>(),
                    container.Resolve<IZoneDefenderAllocationRepository>()));
        }

        private static string ResolveActiveRockstarProfileDir()
        {
            var profilesRoot = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "Rockstar Games", "GTA V", "Profiles");

            if (!Directory.Exists(profilesRoot))
            {
                FactionWars.ScriptHookV.Logging.FileLogger.Warn($"Rockstar profile root not found: {profilesRoot}");
                return profilesRoot;
            }

            var best = Directory.EnumerateDirectories(profilesRoot)
                .Select(dir => new
                {
                    Dir = dir,
                    MostRecent = Directory.EnumerateFiles(dir, "SGTA*")
                        .Select(f => File.GetLastWriteTimeUtc(f))
                        .DefaultIfEmpty(DateTime.MinValue)
                        .Max(),
                })
                .OrderByDescending(x => x.MostRecent)
                .FirstOrDefault();

            var chosen = best?.Dir ?? profilesRoot;
            FactionWars.ScriptHookV.Logging.FileLogger.Info($"Resolved Rockstar profile dir: {chosen}");
            return chosen;
        }

        private static void RegisterEconomyServices(ServiceContainer container)
        {
            var config = container.Resolve<GameConfig>();

            // Zone trait resource modifier - no dependencies
            container.RegisterSingleton<IZoneTraitResourceModifier>(() =>
                new ZoneTraitResourceModifier());

            // Supply line service depends on zone service
            container.RegisterSingleton<ISupplyLineService>(() =>
                new SupplyLineService(container.Resolve<IZoneService>()));

            // Resource tick service depends on faction service, zone service, resource modifier, and supply line service
            container.RegisterSingleton<IResourceTickService>(() =>
                new ResourceTickService(
                    container.Resolve<IFactionService>(),
                    container.Resolve<IZoneService>(),
                    container.Resolve<IZoneTraitResourceModifier>(),
                    container.Resolve<ISupplyLineService>(),
                    config.Economy.ResourceTickIntervalSeconds));

            // Troop purchase service depends on game bridge, defender tier service, and faction service
            container.RegisterSingleton<ITroopPurchaseService>(() =>
                new TroopPurchaseService(
                    container.Resolve<IGameBridge>(),
                    container.Resolve<IDefenderTierService>(),
                    container.Resolve<IFactionService>()));
        }

        private static void RegisterUIServices(ServiceContainer container, IMenuProvider? menuProvider)
        {
            // Menu provider - use provided instance for testing, or NativeUI for production
            if (menuProvider != null)
            {
                container.Register<IMenuProvider>(menuProvider);
            }
            else
            {
                container.RegisterSingleton<IMenuProvider>(() =>
                    new NativeUIMenuProvider());
            }

            // Ped blip service - manages minimap blips for peds (followers, defenders)
            container.RegisterSingleton<IPedBlipService>(() =>
                new PedBlipService(container.Resolve<IGameBridge>()));

            // Notification renderer - use a simple implementation that delegates to game bridge
            container.RegisterSingleton<INotificationRenderer>(() =>
                new GameBridgeNotificationRenderer(container.Resolve<IGameBridge>()));

            // Notification service depends on notification renderer
            container.RegisterSingleton<INotificationService>(() =>
                new NotificationService(container.Resolve<INotificationRenderer>()));

            // Territory indicator renderer - ScriptHookV implementation for zone HUD display
            container.RegisterSingleton<ITerritoryIndicatorRenderer>(() =>
                new TerritoryIndicatorRenderer());

            // Territory indicator service - manages zone status HUD
            container.RegisterSingleton<ITerritoryIndicatorService>(() =>
                new TerritoryIndicatorService(
                    container.Resolve<IFactionRepository>(),
                    container.Resolve<ITerritoryIndicatorRenderer>(),
                    container.Resolve<IZoneBattleManager>()));

            // Faction color service - manages faction color assignments
            container.RegisterSingleton<IFactionColorService>(() =>
                new FactionColorService());

            // Event alert service - raises and manages game event alerts
            container.RegisterSingleton<IEventAlertService>(() =>
                new EventAlertService(
                    container.Resolve<INotificationService>()));

            // Event feed service - manages the scrolling event feed display
            container.RegisterSingleton<IEventFeedService>(() =>
                new EventFeedService(container.Resolve<ITimeProvider>()));
        }

        private static void RegisterAIServices(ServiceContainer container)
        {
            var config = container.Resolve<GameConfig>();

            // Vehicle threat service - classifies vehicles by threat level
            container.RegisterSingleton<IVehicleThreatService>(() =>
                new VehicleThreatService());

            // Anti-vehicle response service - deploys Elite units against vehicle threats
            container.RegisterSingleton<IAntiVehicleResponseService>(() =>
                new AntiVehicleResponseService(
                    container.Resolve<IFactionService>(),
                    container.Resolve<IZoneDefenderAllocationService>(),
                    container.Resolve<IVehicleThreatService>(),
                    container.Resolve<IDefenderTierService>()));

            // Aggression response service - tracks aggression and determines AI responses
            container.RegisterSingleton<IAggressionResponseService>(() =>
                new AggressionResponseService());

            // Battle simulation service - simulates AI vs AI battles
            container.RegisterSingleton<IBattleSimulationService>(() =>
                new BattleSimulationService());

            // Background battle simulator for AI vs AI combat
            container.RegisterSingleton<BackgroundBattleSimulator>(() =>
                new BackgroundBattleSimulator(new BackgroundBattleSimulatorDependencies
                {
                    BattleSimulationService = container.Resolve<IBattleSimulationService>(),
                    FactionService = container.Resolve<IFactionService>(),
                    ZoneService = container.Resolve<IZoneService>(),
                    AllocationService = container.Resolve<IZoneDefenderAllocationService>(),
                    EventAlertService = container.Resolve<IEventAlertService>(),
                    EventFeedService = container.Resolve<IEventFeedService>()
                }));

            // Register AI budget service - costs from config
            container.RegisterSingleton<IAIBudgetService>(() => new AIBudgetService(
                costPerTroop: config.AI.AttackCostPerTroop,
                recruitCostPerTroop: config.AI.RecruitCostPerTroop));

            // Register capital deployment service - intelligent decision-making for AI spending
            container.RegisterSingleton<ICapitalDeploymentService>(() => new CapitalDeploymentService(
                container.Resolve<IAIBudgetService>(),
                container.Resolve<IZoneDefenderAllocationService>()));

            // AI strategies dictionary - maps faction IDs to their strategies
            // Each strategy gets CapitalDeploymentService injected for intelligent decision-making
            container.RegisterSingleton<IDictionary<string, IAIStrategy>>(() =>
            {
                var capitalDeploymentService = container.Resolve<ICapitalDeploymentService>();

                var michaelStrategy = new MichaelAIStrategy(config.AI.MichaelAggressiveness, config.AI.MichaelRiskTolerance);
                michaelStrategy.SetCapitalDeploymentService(capitalDeploymentService);

                var trevorStrategy = new TrevorAIStrategy(config.AI.TrevorAggressiveness, config.AI.TrevorRiskTolerance);
                trevorStrategy.SetCapitalDeploymentService(capitalDeploymentService);

                var franklinStrategy = new FranklinAIStrategy(config.AI.FranklinAggressiveness, config.AI.FranklinRiskTolerance);
                franklinStrategy.SetCapitalDeploymentService(capitalDeploymentService);

                return new Dictionary<string, IAIStrategy>
                {
                    { "michael", michaelStrategy },
                    { "trevor", trevorStrategy },
                    { "franklin", franklinStrategy }
                };
            });

            // Register AI recruitment service with capital deployment service for scaled recruitment
            container.RegisterSingleton<IAIRecruitmentService>(() => new AIRecruitmentService(
                container.Resolve<IFactionService>(),
                container.Resolve<IAIBudgetService>(),
                container.Resolve<IDefenderTierService>(),
                container.Resolve<ICapitalDeploymentService>()));

            // Register AI decision executor
            container.RegisterSingleton<AIDecisionExecutor>(() => new AIDecisionExecutor(
                container.Resolve<IFactionService>(),
                container.Resolve<IAIBudgetService>(),
                container.Resolve<IAIRecruitmentService>()));

            // Register consolidated AI controller with recruitment service for scaled recruitment
            container.RegisterSingleton<IAIController>(() => new AIController(
                new AIControllerDependencies
                {
                    FactionService = container.Resolve<IFactionService>(),
                    ZoneService = container.Resolve<IZoneService>(),
                    BattleSimulationService = container.Resolve<IBattleSimulationService>(),
                    AllocationService = container.Resolve<IZoneDefenderAllocationService>(),
                    GameBridge = container.Resolve<IGameBridge>(),
                    Strategies = container.Resolve<IDictionary<string, IAIStrategy>>(),
                    ZoneBattleManager = container.Resolve<IZoneBattleManager>()
                },
                container.Resolve<IAIRecruitmentService>()));
        }

        private static void RegisterTelemetryServices(ServiceContainer container)
        {
            var config = container.Resolve<GameConfig>();
            var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            var telemetryRoot = Path.Combine(documentsPath, config.Persistence.SaveDirectoryName, "Telemetry");

            container.RegisterSingleton<ITelemetrySink>(() => new CsvTelemetrySink(telemetryRoot));
        }
    }
}
