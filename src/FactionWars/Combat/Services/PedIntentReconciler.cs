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
                    _gameBridge.SetPedBlockPermanentEvents(pedHandle, false);
                    _gameBridge.TaskGuardArea(pedHandle, intent.Position, intent.Radius);
                    break;
                case PedIntentKind.CombatTarget:
                    _gameBridge.RemovePedFromFollowerGroup(pedHandle);
                    _gameBridge.SetPedBlockPermanentEvents(pedHandle, false);
                    _gameBridge.TaskCombatPed(pedHandle, intent.Discriminator);
                    break;
                case PedIntentKind.AdvanceOnTarget:
                    _gameBridge.RemovePedFromFollowerGroup(pedHandle);
                    _gameBridge.SetPedBlockPermanentEvents(pedHandle, false);
                    _gameBridge.TaskGoToEntity(pedHandle, intent.Discriminator, intent.Radius);
                    break;
                case PedIntentKind.RegroupOnPlayer:
                    ApplyRegroupOnPlayer(pedHandle, intent);
                    break;
                case PedIntentKind.SeekHatedTargets:
                    _gameBridge.RemovePedFromFollowerGroup(pedHandle);
                    _gameBridge.SetPedBlockPermanentEvents(pedHandle, false);
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

        // Group-follow can't drag a ped back from beyond its range; sprint to the moving player with
        // a persistent follow task instead (mirrors the rally controller). Detach from the group
        // first so group-follow doesn't fight it. While running back (BlockEvents) gunfire is ignored
        // so the bodyguard doesn't stall in a firefight before reaching the player; once gathered the
        // caller clears the flag so it reacts to and defends against nearby threats again.
        private void ApplyRegroupOnPlayer(int pedHandle, PedIntent intent)
        {
            _gameBridge.RemovePedFromFollowerGroup(pedHandle);
            _gameBridge.SetPedBlockPermanentEvents(pedHandle, intent.BlockEvents);
            _gameBridge.TaskFollowToOffsetFromEntity(
                pedHandle, intent.Discriminator, new Vector3(0f, 0f, 0f),
                moveBlendRatio: 3.0f, stoppingRadius: intent.Radius, persistFollowing: true);
        }
    }
}
