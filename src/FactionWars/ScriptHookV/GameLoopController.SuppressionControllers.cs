using FactionWars.Combat.Services;
using FactionWars.ScriptHookV.Managers;

namespace FactionWars.ScriptHookV
{
    public partial class GameLoopController
    {
        // Builds the two battle-gated ambient-suppression controllers: police response, and ambient
        // traffic (evicts drivers from nearby random cars so they stop driving through the fight).
        private void InitializeSuppressionControllers()
        {
            _policeSuppressionController = new PoliceSuppressionController(_gameBridge, _zoneBattleManager!);
            _ambientTrafficController = new AmbientTrafficController(
                _gameBridge,
                new AmbientTrafficSuppressor(),
                IsBattleActiveInPlayerZone);
        }

        // True while a faction battle is active in the zone the player currently occupies.
        private bool IsBattleActiveInPlayerZone()
        {
            var zone = _territoryManager?.CurrentZone;
            return zone != null && _zoneBattleManager?.GetBattleForZone(zone.Id) != null;
        }
    }
}
