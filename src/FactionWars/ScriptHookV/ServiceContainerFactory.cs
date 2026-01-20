using System;
using System.Collections.Generic;
using System.IO;
using FactionWars.AI.Interfaces;
using FactionWars.AI.Services;
using FactionWars.AI.Strategies;
using FactionWars.Combat.Interfaces;
using FactionWars.Combat.Pools;
using FactionWars.Combat.Services;
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
using FactionWars.ScriptHookV.Persistence;
using FactionWars.ScriptHookV.UI;
using FactionWars.UI.Interfaces;
using FactionWars.UI.Services;

namespace FactionWars.ScriptHookV
{
    /// <summary>
    /// Factory for creating and wiring up the ServiceContainer with all game services.
    /// This is the composition root for the FactionWars mod.
    /// </summary>
    public static class ServiceContainerFactory
    {
        /// <summary>
        /// Default tick interval for resource generation in seconds.
        /// </summary>
        private const int DefaultResourceTickInterval = 60;

        /// <summary>
        /// Default save directory for game saves (relative to user's documents folder).
        /// </summary>
        private const string DefaultSaveDirectoryName = "FactionWars";

        /// <summary>
        /// Creates a fully configured ServiceContainer with all services wired together.
        /// </summary>
        /// <param name="gameBridge">The game bridge implementation for native calls.</param>
        /// <returns>A configured ServiceContainer ready for use.</returns>
        /// <exception cref="ArgumentNullException">Thrown if gameBridge is null.</exception>
        public static ServiceContainer Create(IGameBridge gameBridge)
        {
            if (gameBridge == null)
                throw new ArgumentNullException(nameof(gameBridge));

            var container = new ServiceContainer();

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
            RegisterUIServices(container);

            // Register AI services
            RegisterAIServices(container);

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

            // Faction relationship repository - singleton
            container.RegisterSingleton<IFactionRelationshipRepository>(() =>
                new InMemoryFactionRelationshipRepository());

            // Ped pool - singleton (tracks all spawned peds)
            container.RegisterSingleton<IPedPool>(() => new InMemoryPedPool());
        }

        private static void RegisterDomainServices(ServiceContainer container)
        {
            // Zone service depends on zone repository
            container.RegisterSingleton<IZoneService>(() =>
                new ZoneService(container.Resolve<IZoneRepository>()));

            // Faction service depends on faction repository
            container.RegisterSingleton<IFactionService>(() =>
                new FactionService(container.Resolve<IFactionRepository>()));

            // Faction relationship service depends on faction repository and relationship repository
            container.RegisterSingleton<IFactionRelationshipService>(() =>
                new FactionRelationshipService(
                    container.Resolve<IFactionRepository>(),
                    container.Resolve<IFactionRelationshipRepository>()));

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

            // Control percentage calculator - no dependencies
            container.RegisterSingleton<IControlPercentageCalculator>(() =>
                new ControlPercentageCalculator());

            // Takeover detector - uses default config
            container.RegisterSingleton<ITakeoverDetector>(() =>
                new TakeoverDetector());

            // Combat result handler depends on zone service
            container.RegisterSingleton<ICombatResultHandler>(() =>
                new CombatResultHandler(container.Resolve<IZoneService>()));

            // Wave spawner service - no dependencies
            container.RegisterSingleton<IWaveSpawnerService>(() =>
                new WaveSpawnerService());
        }

        private static void RegisterPersistenceServices(ServiceContainer container)
        {
            // JSON persistence service - handles JSON serialization/deserialization of game state
            container.RegisterSingleton<IPersistenceService>(() =>
                new JsonPersistenceService());

            // Save slot manager - manages multiple save slots using persistence service
            // Uses user's Documents folder for save files (GTA V convention)
            container.RegisterSingleton<ISaveSlotManager>(() =>
            {
                var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                var saveDirectory = Path.Combine(documentsPath, DefaultSaveDirectoryName, "Saves");
                return new SaveSlotManager(
                    container.Resolve<IPersistenceService>(),
                    saveDirectory);
            });

            // Game state manager - coordinates save/load between domain repositories and persistence
            container.RegisterSingleton<IGameStateManager>(() =>
                new GameStateManager(
                    container.Resolve<ISaveSlotManager>(),
                    container.Resolve<IZoneRepository>(),
                    container.Resolve<IFactionRepository>(),
                    container.Resolve<IFactionRelationshipRepository>()));

            // Game state coordinator - provides simplified interface for UI save/load operations
            container.RegisterSingleton<IGameStateCoordinator>(() =>
                new GameStateCoordinator(container.Resolve<IGameStateManager>()));

            // Auto-save service - automatically saves game state at intervals
            container.RegisterSingleton<IAutoSaveService>(() =>
            {
                var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                var saveDirectory = Path.Combine(documentsPath, DefaultSaveDirectoryName, "Saves");
                return new AutoSaveService(
                    container.Resolve<IPersistenceService>(),
                    container.Resolve<IGameStateManager>(),
                    saveDirectory);
            });
        }

        private static void RegisterEconomyServices(ServiceContainer container)
        {
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
                    DefaultResourceTickInterval));

            // Troop purchase service depends on game bridge, defender tier service, and faction service
            container.RegisterSingleton<ITroopPurchaseService>(() =>
                new TroopPurchaseService(
                    container.Resolve<IGameBridge>(),
                    container.Resolve<IDefenderTierService>(),
                    container.Resolve<IFactionService>()));
        }

        private static void RegisterUIServices(ServiceContainer container)
        {
            // Menu provider - NativeUI implementation for game menus
            container.RegisterSingleton<IMenuProvider>(() =>
                new NativeUIMenuProvider());

            // Map blip service depends on game bridge, zone service, and faction repository
            container.RegisterSingleton<IMapBlipService>(() =>
                new MapBlipService(
                    container.Resolve<IGameBridge>(),
                    container.Resolve<IZoneService>(),
                    container.Resolve<IFactionRepository>()));

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
                    container.Resolve<ITerritoryIndicatorRenderer>()));
        }

        private static void RegisterAIServices(ServiceContainer container)
        {
            // AI strategies dictionary - maps faction IDs to their strategies
            container.RegisterSingleton<IDictionary<string, IAIStrategy>>(() =>
                new Dictionary<string, IAIStrategy>
                {
                    { "michael", new MichaelAIStrategy() },
                    { "trevor", new TrevorAIStrategy() },
                    { "franklin", new FranklinAIStrategy() }
                });

            // Zone evaluation service - calculates zone attractiveness for AI decisions
            container.RegisterSingleton<IZoneEvaluationService>(() =>
                new ZoneEvaluationService());

            // Resource allocation service - determines troop/cash distribution for operations
            container.RegisterSingleton<IResourceAllocationService>(() =>
                new ResourceAllocationService());

            // Aggression response service - tracks aggression and determines AI responses
            container.RegisterSingleton<IAggressionResponseService>(() =>
                new AggressionResponseService());
        }
    }
}
