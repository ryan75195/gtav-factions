using FactionWars.Combat.Models;
using Xunit;

namespace FactionWars.Tests.Unit.Combat
{
    public class SquadStanceNextTests
    {
        [Fact]
        public void Next_CyclesEscortToHoldAreaToSearchAndDestroyToEscort()
        {
            Assert.Equal(SquadStance.HoldArea, SquadStance.Escort.Next());
            Assert.Equal(SquadStance.SearchAndDestroy, SquadStance.HoldArea.Next());
            Assert.Equal(SquadStance.Escort, SquadStance.SearchAndDestroy.Next());
        }
    }
}
