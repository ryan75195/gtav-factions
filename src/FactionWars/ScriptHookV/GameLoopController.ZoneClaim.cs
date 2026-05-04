using FactionWars.Core.Interfaces;
using FactionWars.Core.Models;
using FactionWars.Combat.Models;
using FactionWars.ScriptHookV.Logging;
using FactionWars.ScriptHookV.Managers;
using FactionWars.Territory.Interfaces;

namespace FactionWars.ScriptHookV
{
    public partial class GameLoopController
    {
        private void TryClaimNeutralZone()
        {
            if (_currentNeutralZone == null) return;

            var cost = GetBasicTroopCost();
            var playerMoney = _gameBridge.GetPlayerMoney();
            var playerFaction = CurrentPlayerFactionId;

            if (playerMoney < cost)
            {
                _gameBridge.ShowNotification($"~r~Not enough cash! Need ${cost}");
                return;
            }

            // Store zone ID before clearing
            var zoneId = _currentNeutralZone.Id;
            var zoneName = _currentNeutralZone.Name;

            // Cancel any existing AI simulated battle for this zone
            // This prevents AI battles from continuing after the player captures a zone
            var existingBattle = _zoneBattleManager?.GetBattleForZone(zoneId);
            if (existingBattle != null)
            {
                FileLogger.Combat($"Cancelling existing battle in {zoneName} - player claimed zone");
                _zoneBattleManager!.EndBattle(zoneId, BattleOutcome.Cancelled);
            }

            // Deduct cost
            _gameBridge.AddPlayerMoney(-cost);

            // Transfer ownership
            _zoneService!.TransferZoneOwnership(zoneId, playerFaction);

            // Allocate 1 Basic troop
            var allocationService = _container.Resolve<IZoneDefenderAllocationService>();
            allocationService.SetAllocation(playerFaction!, zoneId, DefenderTier.Basic, 1);

            _gameBridge.ShowNotification($"~g~You now control {zoneName}!");

            // Clear prompt state
            _currentNeutralZone = null;
            _showingClaimPrompt = false;

            // Spawn defender and commander immediately
            // Get the updated zone (with new OwnerFactionId) from the service
            var claimedZone = _zoneService.GetZone(zoneId);
            if (claimedZone != null)
            {
                // Spawn friendly defender(s)
                _friendlyDefenderManager?.OnZoneEntered(claimedZone);

                // Spawn commander
                _commanderManager?.OnZoneEntered(claimedZone);
            }
        }

        /// <summary>
        /// Handles AI faction decisions.
        /// Routes through the decision executor for budget enforcement.
        /// </summary>
        private void HandleAIDecision(object? sender, AIDecisionEventArgs e)
        {
            // Route through decision executor for budget enforcement
            _aiDecisionExecutor?.ProcessDecisionCycle(e.FactionId, e.Decision);
        }

        /// <summary>
        /// Handles troop killed events from ZoneBattleManager for the kill feed.
        /// </summary>
        private void OnZoneBattleTroopKilled(ZoneBattle battle, DefenderTier tier, string side)
        {
            // Determine killer and victim based on who got killed
            string killerFactionId = side == "attacker" ? battle.DefenderFactionId : battle.AttackerFactionId;
            string victimFactionId = side == "attacker" ? battle.AttackerFactionId : battle.DefenderFactionId;

            var killerFaction = _factionService.GetFaction(killerFactionId);
            var victimFaction = _factionService.GetFaction(victimFactionId);

            string killerName = killerFaction?.Name ?? killerFactionId;
            string victimName = victimFaction?.Name ?? victimFactionId;

            var zone = _zoneService?.GetZone(battle.ZoneId);
            string zoneName = zone?.Name ?? battle.ZoneId;

            // Format: "[Ballas] killed [Grove St] Basic in Davis"
            string message = $"~y~[{killerName}]~w~ killed ~r~[{victimName}]~w~ {tier} in {zoneName}";
            _gameBridge.ShowNotification(message);
        }

    }
}
