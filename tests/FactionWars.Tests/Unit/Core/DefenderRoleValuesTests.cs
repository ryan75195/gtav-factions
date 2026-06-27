using FactionWars.Core.Models;
using Xunit;

namespace FactionWars.Tests.Unit.Core
{
    public class DefenderRoleValuesTests
    {
        [Theory]
        [InlineData(DefenderRole.Grunt, 0)]
        [InlineData(DefenderRole.Gunner, 1)]
        [InlineData(DefenderRole.Rifleman, 2)]
        [InlineData(DefenderRole.Rocketeer, 3)]
        public void Role_HasStablePersistedValue(DefenderRole role, int expected)
        {
            Assert.Equal(expected, (int)role);
        }
    }
}
