using FactionWars.Core.Utils;
using FactionWars.Economy.Interfaces;
using FactionWars.ScriptHookV;
using Xunit;

namespace FactionWars.Tests.Unit.ScriptHookV
{
    public class ServiceContainerFactoryDeploymentTests
    {
        [Fact]
        public void Create_RegistersDefenderDeploymentService()
        {
            var container = ServiceContainerFactory.Create(new MockGameBridge());
            Assert.NotNull(container.Resolve<IDefenderDeploymentService>());
        }
    }
}
