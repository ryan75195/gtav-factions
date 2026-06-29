using System;
using System.Collections.Generic;
using FactionWars.Combat.Interfaces;
using FactionWars.Combat.Models;
using FactionWars.Core.Interfaces;
using FactionWars.Core.Models;
using FactionWars.ScriptHookV.Logging;

namespace FactionWars.ScriptHookV.Managers
{
    /// <summary>
    /// Owns the party-wide squad stance and issues the matching per-bodyguard native tasks.
    /// Escort reproduces the on-foot follow repair; HoldArea anchors bodyguards on a ring;
    /// SearchAndDestroy assigns each bodyguard a known enemy (or seeks when none are tracked).
    /// </summary>
    public partial class SquadStanceController
    {
        private readonly IGameBridge _gameBridge;
        private readonly ISquadStanceResolver _stanceResolver;
        private readonly ITargetAssignmentResolver _assignmentResolver;
        private readonly IPedIntentReconciler _reconciler;
        private readonly ISquadEngagementResolver _engagementResolver;

        private SquadStance _currentStance = SquadStance.Escort;
        private readonly Dictionary<int, AppliedOrder> _lastApplied = new Dictionary<int, AppliedOrder>();
        private readonly Dictionary<int, int> _lastFollowReassertMs = new Dictionary<int, int>();
        private readonly Dictionary<int, EngagePhase> _enginePhase = new Dictionary<int, EngagePhase>();
        // Game time (ms) at which each engaged ped last held line of sight to its target. Used to
        // measure how long LOS has stayed broken so a sustained loss forces a reposition.
        private readonly Dictionary<int, int> _lastLosMs = new Dictionary<int, int>();
        // Game time (ms) at which each ped last started a line-of-sight reposition. Feeds the
        // resolver's momentum window so a ped commits to its reposition instead of thrashing.
        private readonly Dictionary<int, int> _lastRepositionMs = new Dictionary<int, int>();
        private IReadOnlyDictionary<int, DefenderRole> _rolesByHandle = new Dictionary<int, DefenderRole>();

        private const int FollowerReassertIntervalMs = 2000;

        // Idle radius the run-to-player follow task settles at: tight enough that bodyguards
        // actually gather on the player rather than milling a formation-width away.
        private const float EscortFollowStopRadius = 4f;

        // Past this distance, running back is hopeless (a respawn/fast-travel left the bodyguard
        // far across the map), so the stranded ped is warped next to the player instead of sprinting.
        private const float EscortTeleportDistance = 150f;
        private const float HoldRadiusPerBodyguard = 8f;

        // HoldArea holds a tight ring around the PLAYER, not the zone. Anchoring on the
        // zone centre/radius scattered bodyguards ~50m apart across the whole zone.
        private const float HoldRingRadius = 10f;

        public SquadStanceController(IGameBridge gameBridge, ISquadStanceResolver stanceResolver, ITargetAssignmentResolver assignmentResolver, IPedIntentReconciler reconciler, ISquadEngagementResolver engagementResolver)
        {
            _gameBridge = gameBridge ?? throw new ArgumentNullException(nameof(gameBridge));
            _stanceResolver = stanceResolver ?? throw new ArgumentNullException(nameof(stanceResolver));
            _assignmentResolver = assignmentResolver ?? throw new ArgumentNullException(nameof(assignmentResolver));
            _reconciler = reconciler ?? throw new ArgumentNullException(nameof(reconciler));
            _engagementResolver = engagementResolver ?? throw new ArgumentNullException(nameof(engagementResolver));
        }

        public SquadStance CurrentStance => _currentStance;

        public void CycleStance(IReadOnlyList<int> onFootBodyguardHandles)
        {
            if (onFootBodyguardHandles == null || onFootBodyguardHandles.Count == 0)
            {
                FileLogger.AI("SquadStance.CycleStance: ignored (no on-foot bodyguards)");
                return;
            }

            SetStance(_currentStance.Next(), onFootBodyguardHandles);
        }

        // Applies a chosen stance directly (used by the radial menu). A no-op when the target is
        // already current, so re-selecting the same stance does not re-task the squad or re-notify.
        public void SetStance(SquadStance target, IReadOnlyList<int> onFootBodyguardHandles)
        {
            if (onFootBodyguardHandles == null || onFootBodyguardHandles.Count == 0)
            {
                FileLogger.AI("SquadStance.SetStance: ignored (no on-foot bodyguards)");
                return;
            }

            if (target == _currentStance)
            {
                return;
            }

            var previous = _currentStance;
            _currentStance = target;
            _lastApplied.Clear();
            _reconciler.Clear();
            _enginePhase.Clear();
            _lastLosMs.Clear();
            _lastRepositionMs.Clear();
            _engagementState.Clear();
            _transitions.Clear();
            FileLogger.AI($"SquadStance.SetStance: {previous} -> {_currentStance} (party={onFootBodyguardHandles.Count})");
            _gameBridge.ShowNotification($"~b~Bodyguards:~w~ {StanceLabel(_currentStance)}");
        }

        public void Update(Vector3 anchorCenter, float anchorRadius, IReadOnlyList<int> onFootBodyguardHandles, IReadOnlyList<EnemyTarget> enemiesInZone, IReadOnlyDictionary<int, DefenderRole> rolesByHandle)
        {
            _rolesByHandle = rolesByHandle ?? new Dictionary<int, DefenderRole>();
            PruneStale(onFootBodyguardHandles);
            if (onFootBodyguardHandles == null || onFootBodyguardHandles.Count == 0)
            {
                return;
            }

            switch (_currentStance)
            {
                case SquadStance.HoldArea:
                    ApplyHoldArea(onFootBodyguardHandles);
                    break;
                case SquadStance.SearchAndDestroy:
                    ApplySearchAndDestroy(anchorCenter, anchorRadius, onFootBodyguardHandles, enemiesInZone);
                    break;
                default:
                    ApplyEscort(onFootBodyguardHandles);
                    break;
            }
        }

        private void PruneStale(IReadOnlyList<int> currentHandles)
        {
            var keep = currentHandles == null ? new HashSet<int>() : new HashSet<int>(currentHandles);
            foreach (var handle in new List<int>(_lastApplied.Keys))
            {
                if (!keep.Contains(handle))
                {
                    _lastApplied.Remove(handle);
                    _reconciler.Forget(handle);
                    ForgetEngagement(handle);
                }
            }
            foreach (var handle in new List<int>(_lastFollowReassertMs.Keys))
            {
                if (!keep.Contains(handle)) _lastFollowReassertMs.Remove(handle);
            }
        }

        private static string StanceLabel(SquadStance stance)
        {
            switch (stance)
            {
                case SquadStance.HoldArea: return "Hold Area";
                case SquadStance.SearchAndDestroy: return "Search & Destroy";
                default: return "Escort";
            }
        }
    }
}
