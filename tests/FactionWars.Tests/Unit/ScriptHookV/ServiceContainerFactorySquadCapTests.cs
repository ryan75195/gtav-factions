using FactionWars.Core.Interfaces;
using FactionWars.Core.Utils;
using FactionWars.ScriptHookV;
using Xunit;

namespace FactionWars.Tests.Unit.ScriptHookV
{
    public class ServiceContainerFactorySquadCapTests
    {
        [Fact]
        public void Create_RegistersFollowerService_WithMaxNineFollowers()
        {
            var container = ServiceContainerFactory.Create(new MockGameBridge());
            var followerService = container.Resolve<IFollowerService>();
            Assert.Equal(9, followerService.GetMaxFollowers());
        }
    }
}
