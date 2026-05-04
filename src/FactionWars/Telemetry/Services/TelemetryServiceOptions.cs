using System;
using FactionWars.AI.Interfaces;
using FactionWars.Combat.Interfaces;
using FactionWars.Core.Interfaces;
using FactionWars.Economy.Interfaces;
using FactionWars.ScriptHookV.Managers;
using FactionWars.ScriptHookV.Persistence;
using FactionWars.Territory.Interfaces;

namespace FactionWars.Telemetry.Services
{
    /// <summary>
    /// Bundles the optional dependencies for <see cref="TelemetryService"/>. Each property is
    /// independent — supplying one does not require any other (with one exception:
    /// <see cref="BattleAttackerManager"/> requires <see cref="GetPlayerPedHandle"/>).
    /// Properties left null mean the service will not subscribe to that source.
    /// </summary>
    public sealed class TelemetryServiceOptions
    {
        /// <summary>
        /// Returns the current player ped handle. Required when
        /// <see cref="BattleAttackerManager"/> is supplied (so player kills can be detected).
        /// The handle changes on respawn, so this is a delegate rather than a value.
        /// </summary>
        public Func<int>? GetPlayerPedHandle { get; init; }

        /// <summary>
        /// Returns whether the player is currently dead. When supplied, TelemetryService
        /// emits PlayerEventType.Death on the alive-to-dead transition and
        /// PlayerEventType.RespawnAtHospital on the dead-to-alive transition.
        /// </summary>
        public Func<bool>? IsPlayerDead { get; init; }

        /// <summary>
        /// Returns the player's current zone id, if any. Used to tag player death and
        /// respawn events with the zone context.
        /// </summary>
        public Func<string?>? GetCurrentZoneId { get; init; }

        /// <summary>
        /// Returns the player's current world position. Used to record death location
        /// in the player event details JSON.
        /// </summary>
        public Func<Vector3>? GetPlayerPosition { get; init; }

        /// <summary>
        /// Predicate returning true when the given save filename has never been seen by the
        /// telemetry system before (i.e. its per-save folder does not yet exist on disk).
        /// When supplied, the first OnGameLoaded for a never-seen save emits MatchStart.
        /// Tests pass a stub; the production wiring (Task 13) supplies a directory-existence check.
        /// Default null: MatchStart is never emitted.
        /// </summary>
        public Func<string, bool>? IsFirstTimeSeenSave { get; init; }

        public IZoneBattleManager? ZoneBattleManager { get; init; }
        public AIManager? AIManager { get; init; }
        public IAIController? AIController { get; init; }
        public IZoneDefenderAllocationService? AllocationService { get; init; }
        public IResourceTickService? ResourceTickService { get; init; }
        public BattleAttackerManager? BattleAttackerManager { get; init; }
        public VictoryManager? VictoryManager { get; init; }
        public IDifficultyService? DifficultyService { get; init; }
        public NativeSaveWatcher? NativeSaveWatcher { get; init; }
    }
}

// `init`-only setters require the IsExternalInit type. .NET Framework 4.8 doesn't ship
// it, but the C# compiler accepts an internal polyfill in the project's own assembly.
// Scoped to internal so it doesn't pollute the public API.
namespace System.Runtime.CompilerServices
{
    internal static class IsExternalInit { }
}
