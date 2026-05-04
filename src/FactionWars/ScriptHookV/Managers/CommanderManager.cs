using System;
using System.Collections.Generic;
using FactionWars.Combat.Interfaces;
using FactionWars.Core.Interfaces;
using FactionWars.Core.Models;
using FactionWars.ScriptHookV.Models;
using FactionWars.Territory.Interfaces;
using FactionWars.Territory.Models;
using FactionWars.UI.Interfaces;

namespace FactionWars.ScriptHookV.Managers
{
    /// <summary>
    /// Manages Commander NPCs that spawn in player-owned zones.
    /// Commanders provide an immersive way to access the mod menu.
    /// </summary>
    public partial class CommanderManager
    {
        /// <summary>
        /// Military mechanic model used for commanders.
        /// </summary>
        public const string CommanderModel = "s_m_y_armymech_01";

        /// <summary>
        /// Weapon carried by commanders.
        /// </summary>
        public const string CommanderWeapon = "weapon_carbinerifle";

        /// <summary>
        /// Health value for commanders (high durability).
        /// </summary>
        public const int CommanderHealth = 300;

        /// <summary>
        /// Armor value for commanders.
        /// </summary>
        public const int CommanderArmor = 100;

        /// <summary>
        /// Accuracy for commanders (high skill).
        /// </summary>
        public const float CommanderAccuracy = 0.75f;

        private readonly IGameBridge _gameBridge;
        private readonly IPedSpawningService _pedSpawningService;
        private readonly IPedDespawnService _pedDespawnService;
        private readonly IPedBlipService _pedBlipService;
        private readonly IZoneService _zoneService;
        private string _playerFactionId;

        private readonly Dictionary<string, int> _commanderByZone; // zoneId -> pedHandle
        private readonly HashSet<string> _zonesInBattle;
        private readonly HashSet<int> _commandersFacingPlayer; // Commanders currently facing player
        private string? _currentZoneId;
        private readonly Random _random = new Random();

        /// <summary>
        /// Minimum spawn radius as a fraction of zone radius (30%).
        /// </summary>
        private const float MinSpawnRadiusFraction = 0.3f;

        /// <summary>
        /// Key code for the E key used for interaction.
        /// </summary>
        private const int InteractKeyCode = 0x45; // E key

        /// <summary>
        /// Proximity distance (in meters) for commander interaction.
        /// Player must be within this distance to interact with a commander.
        /// </summary>
        public const float InteractionProximity = 3.0f;

        private readonly Action<object?>? _openMenuCallback;

        /// <summary>
        /// Creates a new CommanderManager instance.
        /// </summary>
        /// <param name="gameBridge">The game bridge for native function calls.</param>
        /// <param name="pedSpawningService">Service for spawning peds.</param>
        /// <param name="pedDespawnService">Service for despawning peds.</param>
        /// <param name="pedBlipService">Service for managing ped blips.</param>
        /// <param name="zoneService">Service for zone operations.</param>
        /// <param name="playerFactionId">The player's current faction ID.</param>
        /// <param name="openMenuCallback">Optional callback invoked when player interacts with commander.</param>
        /// <exception cref="ArgumentNullException">Thrown if any required parameter is null.</exception>
        public CommanderManager(CommanderManagerDependencies dependencies, string playerFactionId, Action<object?>? openMenuCallback = null)
        {
            if (dependencies == null) throw new ArgumentNullException(nameof(dependencies));
            _gameBridge = dependencies.GameBridge ?? throw new ArgumentNullException(nameof(dependencies.GameBridge));
            _pedSpawningService = dependencies.PedSpawningService ?? throw new ArgumentNullException(nameof(dependencies.PedSpawningService));
            _pedDespawnService = dependencies.PedDespawnService ?? throw new ArgumentNullException(nameof(dependencies.PedDespawnService));
            _pedBlipService = dependencies.PedBlipService ?? throw new ArgumentNullException(nameof(dependencies.PedBlipService));
            _zoneService = dependencies.ZoneService ?? throw new ArgumentNullException(nameof(dependencies.ZoneService));
            _playerFactionId = playerFactionId ?? throw new ArgumentNullException(nameof(playerFactionId));
            _openMenuCallback = openMenuCallback;

            _commanderByZone = new Dictionary<string, int>();
            _zonesInBattle = new HashSet<string>();
            _commandersFacingPlayer = new HashSet<int>();
        }

        public CommanderManager(params object?[] dependencies)
            : this(
                new CommanderManagerDependencies
                {
                    GameBridge = (IGameBridge?)dependencies[0],
                    PedSpawningService = (IPedSpawningService?)dependencies[1],
                    PedDespawnService = (IPedDespawnService?)dependencies[2],
                    PedBlipService = (IPedBlipService?)dependencies[3],
                    ZoneService = (IZoneService?)dependencies[4]
                },
                (string)dependencies[5]!,
                dependencies.Length > 6 ? (Action<object?>?)dependencies[6] : null)
        {
        }

        /// <summary>
        /// Checks if a commander is present in the specified zone.
        /// </summary>
        /// <param name="zoneId">The zone ID to check.</param>
        /// <returns>True if a commander exists in the zone.</returns>
        public bool HasCommanderInZone(string zoneId) => _commanderByZone.ContainsKey(zoneId);

        /// <summary>
        /// Called when the player enters a zone. Spawns a commander if the zone
        /// belongs to the player's faction.
        /// </summary>
        /// <param name="zone">The zone that was entered.</param>
        public void OnZoneEntered(Zone zone)
        {
            if (zone == null) return;
            _currentZoneId = zone.Id;

            if (zone.OwnerFactionId != _playerFactionId) return;

            SpawnCommander(zone);
        }

        /// <summary>
        /// Called when the player exits a zone. Despawns the commander
        /// that was spawned for that zone.
        /// </summary>
        /// <param name="zone">The zone that was exited.</param>
        public void OnZoneExited(Zone zone)
        {
            if (zone == null) return;
            if (_currentZoneId == zone.Id)
                _currentZoneId = null;

            DespawnCommander(zone.Id);
        }

        /// <summary>
        /// Called when territory is lost (all defenders died). Despawns the commander.
        /// </summary>
        /// <param name="zoneId">The zone where territory was lost.</param>
        public void OnTerritoryLost(string zoneId)
        {
            DespawnCommander(zoneId);
        }

        /// <summary>
        /// Spawns a commander in the specified zone.
        /// </summary>
        private void SpawnCommander(Zone zone)
        {
            if (_commanderByZone.ContainsKey(zone.Id)) return;
            if (!_pedSpawningService.CanSpawn()) return;

            var spawnPos = CalculateRandomSpawnPosition(zone.Center, zone.Radius);
            var pedHandle = _pedSpawningService.SpawnPed(CommanderModel, spawnPos, _playerFactionId, zone.Id);

            if (!pedHandle.IsValid) return;

            ConfigureCommander(pedHandle.Handle, zone);
            _pedBlipService.CreateBlipForPed(pedHandle.Handle, BlipColor.Blue);
            _commanderByZone[zone.Id] = pedHandle.Handle;
        }

        /// <summary>
        /// Despawns the commander in the specified zone.
        /// </summary>
        private void DespawnCommander(string zoneId)
        {
            if (!_commanderByZone.TryGetValue(zoneId, out var pedHandle)) return;

            _pedBlipService.RemoveBlipForPed(pedHandle);
            _pedDespawnService.DespawnPed(pedHandle);
            _commanderByZone.Remove(zoneId);
        }

        /// <summary>
        /// Configures the commander's combat attributes, weapons, and behavior.
        /// </summary>
        private void ConfigureCommander(int pedHandle, Zone zone)
        {
            _gameBridge.SetPedAsFriendly(pedHandle);

            // Give pistol first as secondary weapon
            _gameBridge.GivePedWeapon(pedHandle, "weapon_pistol");
            // Give carbine rifle as primary weapon
            _gameBridge.GivePedWeapon(pedHandle, CommanderWeapon);

            _gameBridge.SetPedAccuracy(pedHandle, CommanderAccuracy);
            _gameBridge.SetPedArmor(pedHandle, CommanderArmor);
            _gameBridge.SetPedHealth(pedHandle, CommanderHealth);
            _gameBridge.SetPedCombatAttributes(pedHandle, canUseCover: true, willFightArmedPeds: true);

            // Combat targeting if in battle, otherwise wander in the zone
            if (_zonesInBattle.Contains(zone.Id))
            {
                _gameBridge.TaskCombatHatedTargetsAroundPed(pedHandle, zone.Radius);
            }
            else
            {
                _gameBridge.TaskPedWanderInArea(pedHandle, zone.Center, zone.Radius);
            }
        }

        /// <summary>
        /// Checks all commanders for death and respawns them immediately.
        /// Also shows help text when player is near a commander.
        /// Should be called every frame from the game loop.
        /// </summary>
        public void Update()
        {
            // Check for dead commanders
            var deadCommanders = new List<string>();

            foreach (var kvp in _commanderByZone)
            {
                var zoneId = kvp.Key;
                var pedHandle = kvp.Value;

                if (!_gameBridge.IsPedAlive(pedHandle))
                {
                    deadCommanders.Add(zoneId);
                }
            }

            foreach (var zoneId in deadCommanders)
            {
                RespawnCommander(zoneId);
            }

            // Handle proximity-based facing/wandering state
            var nearbyCommander = GetNearbyCommander();
            if (nearbyCommander != null)
            {
                var commanderHandle = nearbyCommander.Value;

                // Show help text
                _gameBridge.DisplayHelpText("Press ~INPUT_CONTEXT~ to talk to Commander");

                // If commander is not already facing player, stop wandering and face player
                if (!_commandersFacingPlayer.Contains(commanderHandle))
                {
                    _gameBridge.ClearPedTasks(commanderHandle);
                    var playerPos = _gameBridge.GetPlayerPosition();
                    _gameBridge.TaskPedTurnToFacePosition(commanderHandle, playerPos);
                    _commandersFacingPlayer.Add(commanderHandle);
                }
            }

            // Resume wandering for commanders no longer near player
            var commandersToResume = new List<int>();
            foreach (var commanderHandle in _commandersFacingPlayer)
            {
                if (nearbyCommander == null || nearbyCommander.Value != commanderHandle)
                {
                    commandersToResume.Add(commanderHandle);
                }
            }

            foreach (var commanderHandle in commandersToResume)
            {
                _commandersFacingPlayer.Remove(commanderHandle);
                ResumeWandering(commanderHandle);
            }
        }

        /// <summary>
        /// Respawns a commander in the specified zone.
        /// Removes the old commander and spawns a new one.
        /// </summary>
        /// <param name="zoneId">The zone ID to respawn the commander in.</param>
        private void RespawnCommander(string zoneId)
        {
            // Remove old commander
            if (_commanderByZone.TryGetValue(zoneId, out var oldHandle))
            {
                _pedBlipService.RemoveBlipForPed(oldHandle);
                _gameBridge.DeletePed(oldHandle);
                _commanderByZone.Remove(zoneId);
            }

            // Get zone and spawn new commander
            var zone = _zoneService.GetZone(zoneId);
            if (zone != null && zone.OwnerFactionId == _playerFactionId)
            {
                SpawnCommander(zone);
            }
        }

        /// <summary>
        /// Calculates a random spawn position around the zone center at ground level.
        /// Uses the zone's full radius for spawn area (30%-100%) and navmesh-based safe
        /// coordinates to avoid spawning on rooftops.
        /// </summary>
        private Vector3 CalculateRandomSpawnPosition(Vector3 center, float zoneRadius)
        {
            var angle = _random.NextDouble() * 2 * Math.PI;
            var minRadius = zoneRadius * MinSpawnRadiusFraction;
            var distance = minRadius + (float)(_random.NextDouble() * (zoneRadius - minRadius));
            var x = center.X + (float)(Math.Cos(angle) * distance);
            var y = center.Y + (float)(Math.Sin(angle) * distance);

            var targetPos = new Vector3(x, y, center.Z);
            return _gameBridge.GetSafeCoordForPed(targetPos);
        }

        /// <summary>
        /// Handles key press events. Opens menu when E is pressed near a commander.
        /// </summary>
        /// <param name="keyCode">The key code of the pressed key.</param>
    }
}
