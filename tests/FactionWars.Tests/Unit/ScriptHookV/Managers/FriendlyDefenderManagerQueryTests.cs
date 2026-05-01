using FactionWars.Combat.Interfaces;
using FactionWars.Combat.Models;
using FactionWars.Core.Utils;
using FactionWars.ScriptHookV;
using FactionWars.ScriptHookV.Managers;
using FactionWars.Tests.Mocks;
using Xunit;

namespace FactionWars.Tests.Unit.ScriptHookV.Managers
{
    /// <summary>
    /// FriendlyDefenderManager exposes its spawned-ped tracking via IFriendlyDefenderQuery
    /// so DefenderRallyController can list the defenders to rally without depending on the
    /// concrete manager.
    /// </summary>
    public class FriendlyDefenderManagerQueryTests
    {
        [Fact]
        public void GetDefendersInZone_UnknownZone_ReturnsEmpty()
        {
            // FriendlyDefenderManager is constructed inside GameLoopController.InitializeGameData,
            // so we reach it via the controller (same pattern as GameLoopControllerCombatTests).
            var bridge = new MockGameBridge();
            var container = ServiceContainerFactory.Create(bridge, new MockMenuProvider());
            var controller = new GameLoopController(container);
            controller.OnTick(); // Initialize.

            IFriendlyDefenderQuery query = controller.FriendlyDefenderManager!;
            var result = query.GetDefendersInZone("does_not_exist");

            Assert.Empty(result);
        }
    }
}
