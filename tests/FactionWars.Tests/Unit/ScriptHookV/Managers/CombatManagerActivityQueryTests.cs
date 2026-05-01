using FactionWars.Combat.Interfaces;
using FactionWars.Core.Utils;
using FactionWars.ScriptHookV;
using FactionWars.Territory.Interfaces;
using FactionWars.Tests.Mocks;
using Xunit;

namespace FactionWars.Tests.Unit.ScriptHookV.Managers
{
    public class CombatManagerActivityQueryTests
    {
        [Fact]
        public void HasActiveEncounter_NoCombat_ReturnsFalse()
        {
            var bridge = new MockGameBridge();
            var container = ServiceContainerFactory.Create(bridge, new MockMenuProvider());
            var controller = new GameLoopController(container);
            controller.OnTick();

            ICombatActivityQuery query = controller.CombatManager!;
            Assert.False(query.HasActiveEncounter);
        }

        [Fact]
        public void HasActiveEncounter_AfterStartCombat_ReturnsTrue()
        {
            var bridge = new MockGameBridge();
            var container = ServiceContainerFactory.Create(bridge, new MockMenuProvider());
            var controller = new GameLoopController(container);
            controller.OnTick();

            var zoneRepo = container.Resolve<IZoneRepository>();
            var zone = zoneRepo.GetById("vinewood_hills")!;
            zone.OwnerFactionId = "trevor";

            controller.CombatManager!.StartCombat(zone, attackingFactionId: "michael");

            ICombatActivityQuery query = controller.CombatManager!;
            Assert.True(query.HasActiveEncounter);
        }
    }
}
