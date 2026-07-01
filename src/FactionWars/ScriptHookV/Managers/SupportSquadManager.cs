using System;
using System.Collections.Generic;
using FactionWars.Combat.Interfaces;
using FactionWars.Combat.Models;
using FactionWars.Combat.Services;
using FactionWars.Core.Interfaces;
using FactionWars.Core.Models;
using FactionWars.ScriptHookV.Combat.Interfaces;
using FactionWars.ScriptHookV.Logging;
using FactionWars.ScriptHookV.Managers.Interfaces;
using FactionWars.ScriptHookV.Models;
using FactionWars.Territory.Interfaces;
using FactionWars.Territory.Models;
using FactionWars.UI.Interfaces;

namespace FactionWars.ScriptHookV.Managers
{
    /// <summary>
    /// Spawns a callable "support squad" — an FBI SUV loaded with 8 friendly allies — that
    /// drives to the player, dismounts within range, and hunts enemies in Search &amp; Destroy.
    /// The allies are friendly-but-not-a-follower: they share the player's faction relationship
    /// group (so the matrix makes them companions of the player and hostile to enemies) but are
    /// never added to the player's follower group. Owns a private <see cref="SquadStanceController"/>
    /// so this squad's stance state never interacts with the player's own bodyguard squad.
    /// </summary>
    public partial class SupportSquadManager : ISupportSquadManager
    {
        /// <summary>Vehicle model used for the support squad's transport.</summary>
        public const string Model = "fbi2";

        /// <summary>Distance (metres) to the player at which the squad dismounts and engages.</summary>
        public const float DismountRange = 30f;

        /// <summary>Distance beyond the zone radius the squad spawns at, before snapping to a road.</summary>
        public const float SpawnMargin = 40f;

        /// <summary>Drive speed (m/s) used for the inbound drive task.</summary>
        public const float DriveSpeed = 20f;

        /// <summary>Stop range (metres) used for the inbound drive task.</summary>
        public const float DriveStopRange = 10f;

        // 2 Sniper + 2 Gunner + 4 Rifleman = 8. First entry rides the driver seat (index 0).
        private static readonly DefenderRole[] Composition =
        {
            DefenderRole.Sniper,
            DefenderRole.Sniper,
            DefenderRole.Gunner,
            DefenderRole.Gunner,
            DefenderRole.Rifleman,
            DefenderRole.Rifleman,
            DefenderRole.Rifleman,
            DefenderRole.Rifleman
        };

        private enum Phase
        {
            None,
            Inbound,
            Engaging
        }

        private readonly IGameBridge _gameBridge;
        private readonly IZoneCombatantSpawner _spawner;
        private readonly ICombatantStatsProvider _statsProvider;
        private readonly IZoneService _zoneService;
        private readonly IPedDespawnService _pedDespawn;
        private readonly IPedBlipService _pedBlip;
        private readonly string _playerFactionId;
        private readonly SquadStanceController _stance;
        private readonly Dictionary<int, DefenderRole> _rolesByHandle = new Dictionary<int, DefenderRole>();

        private Phase _phase = Phase.None;
        private Zone? _activeZone;
        private int _suv = -1;

        public SupportSquadManager(SupportSquadManagerDependencies dependencies, string playerFactionId)
        {
            if (dependencies == null) throw new ArgumentNullException(nameof(dependencies));
            _gameBridge = dependencies.GameBridge ?? throw new ArgumentNullException(nameof(dependencies.GameBridge));
            _spawner = dependencies.Spawner ?? throw new ArgumentNullException(nameof(dependencies.Spawner));
            _statsProvider = dependencies.StatsProvider ?? throw new ArgumentNullException(nameof(dependencies.StatsProvider));
            _zoneService = dependencies.ZoneService ?? throw new ArgumentNullException(nameof(dependencies.ZoneService));
            _pedDespawn = dependencies.PedDespawn ?? throw new ArgumentNullException(nameof(dependencies.PedDespawn));
            _pedBlip = dependencies.PedBlip ?? throw new ArgumentNullException(nameof(dependencies.PedBlip));
            _playerFactionId = playerFactionId ?? throw new ArgumentNullException(nameof(playerFactionId));

            _stance = new SquadStanceController(
                _gameBridge,
                new SquadStanceResolver(),
                new TargetAssignmentResolver(),
                new PedIntentReconciler(_gameBridge),
                new SquadEngagementResolver(new EngageRangeProvider()));
        }

        public SupportSquadManager(params object?[] dependencies)
            : this(
                new SupportSquadManagerDependencies
                {
                    GameBridge = (IGameBridge?)dependencies[0],
                    Spawner = (IZoneCombatantSpawner?)dependencies[1],
                    StatsProvider = (ICombatantStatsProvider?)dependencies[2],
                    ZoneService = (IZoneService?)dependencies[3],
                    PedDespawn = (IPedDespawnService?)dependencies[4],
                    PedBlip = (IPedBlipService?)dependencies[5]
                },
                (string)dependencies[6]!)
        {
        }

        /// <summary>True from the moment a squad is called until every ally is dead/streamed out.</summary>
        public bool HasActiveSquad { get; private set; }

        /// <summary>
        /// Calls the support squad into the given zone: spawns an FBI SUV just outside the zone
        /// boundary, seats 8 friendly allies, and tasks the SUV to drive to the player. No-op if a
        /// support squad is already active. Returns false (and consumes nothing spawn-side) if a
        /// squad is already active or the SUV failed to spawn, so callers can avoid charging the
        /// player for a call that produced no squad.
        /// </summary>
        public bool CallSupportSquad(Zone zone)
        {
            if (zone == null) throw new ArgumentNullException(nameof(zone));
            if (HasActiveSquad) return false;

            var edgePoint = ResolveSpawnEdgePoint(zone);
            var spawnPos = _gameBridge.GetNearestRoadPosition(edgePoint);
            var suv = _gameBridge.CreateVehicle(Model, spawnPos);
            if (suv == -1)
            {
                FileLogger.Warn($"SupportSquadManager.CallSupportSquad: CreateVehicle({Model}) failed for zone {zone.Id}");
                return false;
            }

            FileLogger.Spawn($"SupportSquadManager.CallSupportSquad: FBI SUV {suv} spawned for zone {zone.Id} at ({spawnPos.X:F1}, {spawnPos.Y:F1}, {spawnPos.Z:F1})");

            _rolesByHandle.Clear();
            SpawnAndSeatAllies(suv, spawnPos, zone.Id);

            _gameBridge.TaskVehicleDriveToCoord(suv, _gameBridge.GetPlayerPosition(), DriveSpeed, DriveStopRange);

            _activeZone = zone;
            _suv = suv;
            _phase = Phase.Inbound;
            HasActiveSquad = true;

            _stance.SetStance(SquadStance.SearchAndDestroy, AliveHandles());
            FileLogger.AI($"SupportSquadManager.CallSupportSquad: {_rolesByHandle.Count} allies inbound to zone {zone.Id} in SUV {suv}");
            return true;
        }

        private static Vector3 ResolveSpawnEdgePoint(Zone zone)
        {
            // Fixed outward direction keeps spawn placement deterministic; the caller snaps this
            // point to the nearest road.
            var distance = zone.Radius + SpawnMargin;
            return new Vector3(zone.Center.X + distance, zone.Center.Y, zone.Center.Z);
        }

        private List<int> AliveHandles() => new List<int>(_rolesByHandle.Keys);
    }
}
