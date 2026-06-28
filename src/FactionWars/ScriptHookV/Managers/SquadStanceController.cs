using System;
using System.Collections.Generic;
using FactionWars.Combat.Interfaces;
using FactionWars.Combat.Models;
using FactionWars.Core.Interfaces;
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

        private SquadStance _currentStance = SquadStance.Escort;
        private readonly Dictionary<int, AppliedOrder> _lastApplied = new Dictionary<int, AppliedOrder>();
        private readonly Dictionary<int, int> _lastFollowReassertMs = new Dictionary<int, int>();

        private const int FollowerReassertIntervalMs = 2000;
        private const float HoldRadiusPerBodyguard = 8f;

        // HoldArea holds a tight ring around the PLAYER, not the zone. Anchoring on the
        // zone centre/radius scattered bodyguards ~50m apart across the whole zone.
        private const float HoldRingRadius = 10f;

        public SquadStanceController(IGameBridge gameBridge, ISquadStanceResolver stanceResolver, ITargetAssignmentResolver assignmentResolver, IPedIntentReconciler reconciler)
        {
            _gameBridge = gameBridge ?? throw new ArgumentNullException(nameof(gameBridge));
            _stanceResolver = stanceResolver ?? throw new ArgumentNullException(nameof(stanceResolver));
            _assignmentResolver = assignmentResolver ?? throw new ArgumentNullException(nameof(assignmentResolver));
            _reconciler = reconciler ?? throw new ArgumentNullException(nameof(reconciler));
        }

        public SquadStance CurrentStance => _currentStance;

        public void CycleStance(IReadOnlyList<int> onFootBodyguardHandles)
        {
            if (onFootBodyguardHandles == null || onFootBodyguardHandles.Count == 0)
            {
                FileLogger.AI("SquadStance.CycleStance: ignored (no on-foot bodyguards)");
                return;
            }

            var previous = _currentStance;
            _currentStance = _currentStance.Next();
            _lastApplied.Clear();
            _reconciler.Clear();
            FileLogger.AI($"SquadStance.CycleStance: {previous} -> {_currentStance} (party={onFootBodyguardHandles.Count})");
            _gameBridge.ShowNotification($"~b~Bodyguards:~w~ {StanceLabel(_currentStance)}");
        }

        public void Update(Vector3 anchorCenter, float anchorRadius, IReadOnlyList<int> onFootBodyguardHandles, IReadOnlyList<EnemyTarget> enemiesInRange)
        {
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
                    ApplySearchAndDestroy(anchorCenter, anchorRadius, onFootBodyguardHandles, enemiesInRange);
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
