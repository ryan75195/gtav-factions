using System.Collections.Generic;
using FactionWars.Combat.Interfaces;
using FactionWars.Combat.Models;
using FactionWars.Core.Interfaces;
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
    }
}
