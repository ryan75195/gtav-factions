using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FactionWars.AI.Interfaces;
using FactionWars.AI.Models;
using FactionWars.Combat.Interfaces;
using FactionWars.Combat.Models;
using FactionWars.Configuration;
using FactionWars.Core.Interfaces;
using FactionWars.Core.Models;
using FactionWars.Core.Services;
using FactionWars.Persistence.Models;
using FactionWars.Economy.Interfaces;
using FactionWars.Factions.Interfaces;
using FactionWars.ScriptHookV.Data;
using FactionWars.ScriptHookV.Logging;
using FactionWars.ScriptHookV.Managers;
using FactionWars.ScriptHookV.Models;
using FactionWars.ScriptHookV.Persistence;
using FactionWars.ScriptHookV.UI;
using FactionWars.Telemetry.Interfaces;
using FactionWars.Telemetry.Services;
using FactionWars.Territory.Interfaces;
using FactionWars.Territory.Models;
using FactionWars.UI.Interfaces;
using FactionWars.UI.Models;
using GTA.Native;

namespace FactionWars.ScriptHookV
{
    public partial class GameLoopController
    {
        private void UpdateAndDrawBattleHud()
        {
            if (_zoneBattleManager == null || _battleHudRenderer == null)
                return;

            var battles = _zoneBattleManager.GetAllActiveBattles();
            if (battles.Count == 0)
            {
                _battleHudRenderer.Hide();
                return;
            }

            // Clamp index
            if (_currentBattleHudIndex >= battles.Count)
                _currentBattleHudIndex = 0;

            var battle = battles[_currentBattleHudIndex];
            var zone = _zoneService?.GetZone(battle.ZoneId);
            var attackerFaction = _factionService.GetFaction(battle.AttackerFactionId);
            var defenderFaction = _factionService.GetFaction(battle.DefenderFactionId);
            var zoneName = zone?.Name ?? battle.ZoneId;
            var attackerName = attackerFaction?.Name ?? battle.AttackerFactionId;
            var defenderName = defenderFaction?.Name ?? battle.DefenderFactionId;

            var hudData = new BattleHudData(
                zoneName,
                attackerName,
                battle.TotalAttackerTroops,
                defenderName,
                battle.TotalDefenderTroops,
                _currentBattleHudIndex + 1,
                battles.Count);

            _battleHudRenderer.SetData(hudData);
            _battleHudRenderer.Draw();
        }

        /// <summary>
        /// Initializes game data including zones and factions.
        /// Called on the first tick to ensure the game is ready.
        /// </summary>
    }
}
