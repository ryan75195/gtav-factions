using System;
using System.Collections.Generic;
using FactionWars.Combat.Interfaces;
using FactionWars.Combat.Models;
using FactionWars.Core.Interfaces;

namespace FactionWars.Combat.Services
{
    /// <summary>
    /// Default <see cref="IPedIntentReconciler"/>. Keeps the last-applied intent per ped and only
    /// touches the game bridge when the submitted intent differs, centralizing the
    /// detach-before-combat and clear-before-goto sequencing that controllers previously each owned.
    /// Single-threaded: driven from the script thread.
    /// </summary>
    public class PedIntentReconciler : IPedIntentReconciler
    {
        private readonly IGameBridge _gameBridge;
        private readonly Dictionary<int, PedIntent> _lastApplied = new Dictionary<int, PedIntent>();

        public PedIntentReconciler(IGameBridge gameBridge)
        {
            _gameBridge = gameBridge ?? throw new ArgumentNullException(nameof(gameBridge));
        }

        /// <inheritdoc />
        public void Submit(int pedHandle, PedIntent intent)
        {
            if (_lastApplied.TryGetValue(pedHandle, out var previous) && previous.Equals(intent))
                return;

            Apply(pedHandle, intent);
            _lastApplied[pedHandle] = intent;
        }

        /// <inheritdoc />
        public void Forget(int pedHandle) => _lastApplied.Remove(pedHandle);

        /// <inheritdoc />
        public void Clear() => _lastApplied.Clear();

        private void Apply(int pedHandle, PedIntent intent)
        {
            switch (intent.Kind)
            {
                case PedIntentKind.FollowPlayer:
                    _gameBridge.SetPedAsFollower(pedHandle);
                    break;
                case PedIntentKind.GuardArea:
                    _gameBridge.RemovePedFromFollowerGroup(pedHandle);
                    _gameBridge.TaskGuardArea(pedHandle, intent.Position, intent.Radius);
                    break;
                case PedIntentKind.CombatTarget:
                    _gameBridge.RemovePedFromFollowerGroup(pedHandle);
                    _gameBridge.TaskCombatPed(pedHandle, intent.Discriminator);
                    break;
                case PedIntentKind.SeekHatedTargets:
                    _gameBridge.RemovePedFromFollowerGroup(pedHandle);
                    _gameBridge.TaskCombatHatedTargetsAroundPed(pedHandle, intent.Radius);
                    break;
                case PedIntentKind.WanderArea:
                    _gameBridge.TaskPedWanderInBoundedArea(pedHandle, intent.Position, intent.Radius);
                    break;
                case PedIntentKind.GoToCoord:
                    _gameBridge.ClearPedTasks(pedHandle);
                    _gameBridge.TaskGoToCoord(pedHandle, intent.Position);
                    break;
                case PedIntentKind.LeaveVehicle:
                    _gameBridge.TaskPedLeaveVehicle(pedHandle);
                    break;
            }
        }
    }
}
