using System.Linq;
using FactionWars.ScriptHookV.Combat;
using FactionWars.ScriptHookV.Logging;

namespace FactionWars.ScriptHookV
{
    public partial class GameLoopController
    {
        /// <summary>
        /// (Re)establishes every GTA relationship-group pairing for the given player faction.
        /// Called at startup and on character switch so a faction's combatants are companions of
        /// the player when that faction is the player's, and hostile otherwise. Combatants spawn
        /// into their faction group and derive allegiance from this matrix; nothing mutates
        /// relationships per spawn.
        /// </summary>
        private void ApplyRelationshipMatrix(string? playerFactionId)
        {
            var factionIds = _factionService.GetAllFactions().Select(f => f.Id).ToList();
            new RelationshipMatrixInitializer(_gameBridge).Initialize(playerFactionId ?? string.Empty, factionIds);
            FileLogger.Info($"ApplyRelationshipMatrix: wired {factionIds.Count} faction groups for player '{playerFactionId ?? "UNKNOWN"}'");
        }
    }
}
