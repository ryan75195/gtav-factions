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
    public static partial class ServiceContainerFactory
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

            container.RegisterSingleton<ICombatantStatsProvider>(() =>
                CombatantStatsProviderFactory.Create(container.Resolve<GameConfig>().Combatants));

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
            container.RegisterSingleton<IDefenderRoleService>(() =>
                new DefenderRoleService());

            // Follower service - manages player followers (bodyguards). Cap 9 fits a full squad
            // in one Barracks (driver + 9 seats).
            container.RegisterSingleton<IFollowerService>(() =>
                new FollowerService(9));

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

    }
}
