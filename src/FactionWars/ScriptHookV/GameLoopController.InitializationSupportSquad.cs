using FactionWars.Combat.Services;
using FactionWars.Core.Interfaces;
using FactionWars.ScriptHookV.Combat;
using FactionWars.ScriptHookV.Managers;
using FactionWars.ScriptHookV.Models;

namespace FactionWars.ScriptHookV
{
    public partial class GameLoopController
    {
        private void InitializeSupportSquadManager(SpawnServices spawnServices)
        {
            var spawner = new ZoneCombatantSpawner(
                new AllegianceResolver(),
                spawnServices.PedSpawning,
                spawnServices.PedBlip,
                _gameBridge);

            _supportSquadManager = new SupportSquadManager(
                new SupportSquadManagerDependencies
                {
                    GameBridge = _gameBridge,
                    Spawner = spawner,
                    StatsProvider = _container.Resolve<ICombatantStatsProvider>(),
                    ZoneService = _zoneService
                },
                CurrentPlayerFactionId ?? "");
        }
    }
}
