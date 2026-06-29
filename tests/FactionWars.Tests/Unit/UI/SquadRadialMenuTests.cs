using FactionWars.Combat.Models;
using FactionWars.UI.Models;
using Xunit;

namespace FactionWars.Tests.Unit.UI
{
    public class SquadRadialMenuTests
    {
        // Segments are ordered Escort(top/0), HoldArea(1), SearchAndDestroy(2).
        private readonly SquadRadialMenu _menu = new SquadRadialMenu();

        [Fact]
        public void Open_DefaultsSelectionToCurrentStance()
        {
            _menu.Open(SquadStance.HoldArea);

            Assert.True(_menu.IsOpen);
            Assert.Equal(SquadStance.HoldArea, _menu.SelectedStance);
        }

        [Fact]
        public void UpdatePointer_Up_SelectsEscort()
        {
            _menu.Open(SquadStance.SearchAndDestroy);

            _menu.UpdatePointer(0f, -1f); // straight up -> segment 0

            Assert.Equal(SquadStance.Escort, _menu.SelectedStance);
        }

        [Fact]
        public void UpdatePointer_InsideDeadzone_KeepsPreviousSelection()
        {
            _menu.Open(SquadStance.Escort);
            _menu.UpdatePointer(0f, 1f); // down -> SearchAndDestroy (segment 2)
            Assert.Equal(SquadStance.SearchAndDestroy, _menu.SelectedStance);

            _menu.UpdatePointer(0.01f, 0.01f); // inside deadzone -> unchanged

            Assert.Equal(SquadStance.SearchAndDestroy, _menu.SelectedStance);
        }

        [Fact]
        public void UpdatePointer_WhenClosed_DoesNothing()
        {
            _menu.UpdatePointer(0f, -1f);

            Assert.False(_menu.IsOpen);
        }

        [Fact]
        public void Close_ReturnsSelectionAndCloses()
        {
            _menu.Open(SquadStance.Escort);
            _menu.UpdatePointer(0f, 1f); // down -> SearchAndDestroy

            var chosen = _menu.Close();

            Assert.Equal(SquadStance.SearchAndDestroy, chosen);
            Assert.False(_menu.IsOpen);
        }
    }
}
