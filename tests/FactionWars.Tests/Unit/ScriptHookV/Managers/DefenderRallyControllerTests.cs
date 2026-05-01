using System.Collections.Generic;
using FactionWars.Combat.Interfaces;
using FactionWars.Combat.Models;
using FactionWars.Core.Interfaces;
using FactionWars.Core.Models;
using FactionWars.Core.Utils;
using FactionWars.ScriptHookV.Managers;
using FactionWars.Territory.Models;
using Moq;
using Xunit;

namespace FactionWars.Tests.Unit.ScriptHookV.Managers
{
    public class DefenderRallyControllerTests
    {
        // --- Test fixtures shared by all tests --------------------------------

        private readonly MockGameBridge _bridge = new MockGameBridge();
        private readonly Mock<ITerritoryEvents> _territory = new Mock<ITerritoryEvents>();
        private readonly Mock<IFriendlyDefenderQuery> _defenders = new Mock<IFriendlyDefenderQuery>();
        private readonly Mock<ICombatActivityQuery> _combat = new Mock<ICombatActivityQuery>();
        private string? _playerFactionId = "michael";
        private long _now = 1_000_000;

        public DefenderRallyControllerTests()
        {
            // Default: all zones return an empty defender dictionary so SpawnDefenderInZone
            // can safely iterate the previous result before overriding it.
            _defenders.Setup(d => d.GetDefendersInZone(It.IsAny<string>()))
                      .Returns(new Dictionary<int, DefenderTier>());
        }

        private DefenderRallyController BuildSut()
        {
            return new DefenderRallyController(
                _bridge,
                _territory.Object,
                _defenders.Object,
                _combat.Object,
                () => _playerFactionId,
                () => _now);
        }

        // --- Skeleton test ----------------------------------------------------

        [Fact]
        public void Update_NoCurrentZone_DoesNothing()
        {
            _territory.Setup(t => t.CurrentZone).Returns((Zone?)null);

            var sut = BuildSut();
            sut.Update();

            // No tasking calls.
            _defenders.Verify(d => d.GetDefendersInZone(It.IsAny<string>()), Times.Never);
        }

        // ---- Helpers ---------------------------------------------------------

        private static Zone OwnedZone(string id, string ownerFactionId)
            => new Zone(id, id, new Vector3(0, 0, 0), radius: 100f, strategicValue: 1)
            {
                OwnerFactionId = ownerFactionId,
            };

        private void SetCurrentZone(Zone? zone) => _territory.Setup(t => t.CurrentZone).Returns(zone);

        private int SpawnDefenderInZone(string zoneId, DefenderTier tier = DefenderTier.Basic)
        {
            int handle = _bridge.CreatePed("a_m_y_business_01", new Vector3(0, 0, 0));
            var current = new Dictionary<int, DefenderTier>();
            // Preserve existing defenders if the test added more.
            var prev = _defenders.Object.GetDefendersInZone(zoneId);
            foreach (var kv in prev) current[kv.Key] = kv.Value;
            current[handle] = tier;
            _defenders.Setup(d => d.GetDefendersInZone(zoneId)).Returns(current);
            return handle;
        }

        // ---- Friendly rally: false -> true tests -----------------------------

        [Fact]
        public void Update_PlayerInOwnZone_NoThreat_DoesNotRally()
        {
            var zone = OwnedZone("vinewood_hills", "michael");
            SetCurrentZone(zone);
            int defender = SpawnDefenderInZone(zone.Id);

            var sut = BuildSut();
            sut.Update();

            Assert.False(_bridge.IsPedGoingToEntity(defender));
        }

        [Fact]
        public void Update_PlayerInOwnZone_WantedLevelOn_RalliesDefenders()
        {
            var zone = OwnedZone("vinewood_hills", "michael");
            SetCurrentZone(zone);
            int defender = SpawnDefenderInZone(zone.Id);
            _bridge.WantedLevel = 2;
            _bridge.PlayerPedHandle = 99;

            var sut = BuildSut();
            sut.Update();

            Assert.True(_bridge.IsPedGoingToEntity(defender));
            Assert.Equal(99, _bridge.GetGoToEntityTarget(defender));
            Assert.Equal(DefenderRallyController.RallyStoppingRangeM, _bridge.GetGoToEntityStoppingRange(defender));
            Assert.True(_bridge.IsPedCombatTargeting(defender));
            Assert.Equal(DefenderRallyController.RallyCombatRadiusM, _bridge.GetPedCombatTargetingRadius(defender));
        }

        [Fact]
        public void Update_PlayerInOwnZone_CombatActive_RalliesDefenders()
        {
            var zone = OwnedZone("vinewood_hills", "michael");
            SetCurrentZone(zone);
            int defender = SpawnDefenderInZone(zone.Id);
            _combat.Setup(c => c.HasActiveEncounter).Returns(true);

            var sut = BuildSut();
            sut.Update();

            Assert.True(_bridge.IsPedGoingToEntity(defender));
        }

        [Fact]
        public void Update_PlayerInOwnZone_PlayerDamagedByPed_RalliesDefenders()
        {
            var zone = OwnedZone("vinewood_hills", "michael");
            SetCurrentZone(zone);
            int defender = SpawnDefenderInZone(zone.Id);
            _bridge.PlayerDamagedByPed = true;

            var sut = BuildSut();
            sut.Update();

            Assert.True(_bridge.IsPedGoingToEntity(defender));
        }

        [Fact]
        public void Update_PlayerInOwnZone_RallyTransition_ClearsTasksFirst()
        {
            // Spec says ClearPedTasks is called before TaskGoToEntity so the previous
            // wander task doesn't fight the new go-to.
            var zone = OwnedZone("vinewood_hills", "michael");
            SetCurrentZone(zone);
            int defender = SpawnDefenderInZone(zone.Id);
            _bridge.WantedLevel = 1;

            // Pre-task the defender so we can verify ClearPedTasks happened.
            _bridge.TaskPedWanderInArea(defender, new Vector3(0, 0, 0), 50f);
            Assert.True(_bridge.IsPedWandering(defender));

            var sut = BuildSut();
            sut.Update();

            // After rally, no longer wandering (cleared) and now going-to-entity.
            Assert.False(_bridge.IsPedWandering(defender));
            Assert.True(_bridge.IsPedGoingToEntity(defender));
        }

        [Fact]
        public void Update_PlayerInOwnZone_AlreadyRallying_DoesNotReissueTasks()
        {
            var zone = OwnedZone("vinewood_hills", "michael");
            SetCurrentZone(zone);
            int defender = SpawnDefenderInZone(zone.Id);
            _bridge.WantedLevel = 2;

            var sut = BuildSut();
            sut.Update(); // Transition: false -> true. Issues tasks.

            // Manually simulate that something else has tasked the defender; the
            // controller must NOT re-issue on the second tick (steady state true -> true).
            _bridge.TaskPedWanderInArea(defender, new Vector3(0, 0, 0), 50f);
            sut.Update();

            // Wander task is still in place — controller did not re-issue go-to-entity.
            Assert.True(_bridge.IsPedWandering(defender));
        }
    }
}
