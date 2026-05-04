using FactionWars.Core.Interfaces;
using FactionWars.Core.Models;

namespace FactionWars.ScriptHookV
{
    public partial class GameLoopController
    {
        private int GetBasicTroopCost()
        {
            var tierService = _container.Resolve<IDefenderTierService>();
            return tierService.GetTierConfig(DefenderTier.Basic).Cost;
        }

        /// <summary>
        /// Attempts to claim the current neutral zone by paying for a guard troop.
        /// </summary>
    }
}
