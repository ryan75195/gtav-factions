using System;
using System.IO;
using System.Linq;
using FactionWars.Configuration;
using FactionWars.Core.Interfaces;
using FactionWars.Core.Models;
using FactionWars.Core.Services;
using FactionWars.Economy.Interfaces;
using FactionWars.Economy.Services;
using FactionWars.Factions.Interfaces;
using FactionWars.Persistence;
using FactionWars.ScriptHookV.Persistence;
using FactionWars.Territory.Interfaces;
using FactionWars.Territory.Services;

namespace FactionWars.ScriptHookV
{
    public static partial class ServiceContainerFactory
    {
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
                    container.Resolve<IDefenderRoleService>(),
                    container.Resolve<IFactionService>()));
        }

    }
}
