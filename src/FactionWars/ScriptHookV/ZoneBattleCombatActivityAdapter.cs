using System;
using FactionWars.Combat.Interfaces;

namespace FactionWars.ScriptHookV
{
    /// <summary>
    /// Adapts <see cref="IZoneBattleManager"/> to <see cref="ICombatActivityQuery"/>
    /// so that consumers like <c>DefenderRallyController</c> can query "is the player
    /// in combat right now?" without taking a direct dependency on the manager's full
    /// lifecycle API.
    /// </summary>
    public sealed class ZoneBattleCombatActivityAdapter : ICombatActivityQuery
    {
        private readonly IZoneBattleManager _battleManager;

        public ZoneBattleCombatActivityAdapter(IZoneBattleManager battleManager)
        {
            _battleManager = battleManager ?? throw new ArgumentNullException(nameof(battleManager));
        }

        /// <inheritdoc />
        public bool HasActiveEncounter => _battleManager.IsPlayerInBattle();
    }
}
