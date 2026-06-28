using System.IO;
using FactionWars.Combat.Interfaces;
using FactionWars.Core.Interfaces;
using FactionWars.ScriptHookV.Data;
using FactionWars.ScriptHookV.Logging;
using FactionWars.ScriptHookV.Managers;
using FactionWars.Territory.Interfaces;
using FactionWars.UI.Interfaces;

namespace FactionWars.ScriptHookV
{
    public partial class GameLoopController
    {
        private void InitializeWorldDataAndEconomy()
        {
            var scriptsDir = _gameBridge.GetScriptsDirectory();
            var zonesFilePath = Path.Combine(scriptsDir, "FactionWars", "zones.json");
            FileLogger.Info($"Looking for zones at: {zonesFilePath}");
            _zoneDataLoader.LoadZonesWithFallback(zonesFilePath);
            FileLogger.Info($"Loaded {_zoneRepository.Count} zones");

            FileLogger.Info("Initializing factions...");
            _factionInitializer.Initialize();

            FileLogger.Separator("ZONE OWNERSHIP");
            foreach (var zone in _zoneRepository.GetAll())
                FileLogger.Zone($"Zone '{zone.Name}' (ID: {zone.Id}) -> Owner: {zone.OwnerFactionId ?? "NONE"}");

            _mapBlipManager = new MapBlipManager(_gameBridge, _zoneRepository, _factionService);
            _mapBlipManager.Initialize();
            _economyManager = new EconomyManager(_resourceTickService, _gameBridge);
            _economyManager.Start();
            _economyManager.SetPlayerFactionId(CurrentPlayerFactionId);
        }

        private SpawnServices ResolveSpawnServices()
        {
            return new SpawnServices
            {
                PedSpawning = _container.Resolve<IPedSpawningService>(),
                PedDespawn = _container.Resolve<IPedDespawnService>(),
                DefenderRole = _container.Resolve<IDefenderRoleService>(),
                PedBlip = _container.Resolve<IPedBlipService>()
            };
        }

        private sealed class SpawnServices
        {
            public IPedSpawningService PedSpawning { get; set; } = null!;
            public IPedDespawnService PedDespawn { get; set; } = null!;
            public IDefenderRoleService DefenderRole { get; set; } = null!;
            public IPedBlipService PedBlip { get; set; } = null!;
        }

    }
}
