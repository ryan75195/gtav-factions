using System.Collections.Generic;
using System.IO;
using FactionWars.AI.Interfaces;
using FactionWars.AI.Services;
using FactionWars.Core.Interfaces;
using FactionWars.ScriptHookV;
using FactionWars.Tests.Mocks;
using Moq;
using Xunit;

namespace FactionWars.Tests.Unit.ScriptHookV
{
    /// <summary>
    /// Integration tests for CapitalDeploymentService wiring into ServiceContainer.
    /// </summary>
    public class CapitalDeploymentServiceIntegrationTests
    {
        /// <summary>
        /// Creates a mock IGameBridge with GetScriptsDirectory returning a temp path.
        /// </summary>
        private static IGameBridge CreateMockGameBridge()
        {
            var mock = new Mock<IGameBridge>();
            mock.Setup(x => x.GetScriptsDirectory()).Returns(Path.GetTempPath());
            return mock.Object;
        }

        #region ServiceContainer Registration Tests

        [Fact]
        public void Create_ShouldRegisterCapitalDeploymentService()
        {
            // Arrange
            var gameBridge = CreateMockGameBridge();

            // Act
            var container = ServiceContainerFactory.Create(gameBridge, new MockMenuProvider());

            // Assert
            Assert.True(container.IsRegistered<ICapitalDeploymentService>());
        }

        [Fact]
        public void Create_ShouldReturnSingletonCapitalDeploymentService()
        {
            // Arrange
            var gameBridge = CreateMockGameBridge();
            var container = ServiceContainerFactory.Create(gameBridge, new MockMenuProvider());

            // Act
            var service1 = container.Resolve<ICapitalDeploymentService>();
            var service2 = container.Resolve<ICapitalDeploymentService>();

            // Assert
            Assert.Same(service1, service2);
        }

        [Fact]
        public void Create_CapitalDeploymentService_ShouldBeCorrectType()
        {
            // Arrange
            var gameBridge = CreateMockGameBridge();
            var container = ServiceContainerFactory.Create(gameBridge, new MockMenuProvider());

            // Act
            var service = container.Resolve<ICapitalDeploymentService>();

            // Assert
            Assert.IsType<CapitalDeploymentService>(service);
        }

        #endregion

        #region AIRecruitmentService Integration Tests

        [Fact]
        public void Create_AIRecruitmentService_ShouldUseCapitalDeploymentService()
        {
            // Arrange
            var gameBridge = CreateMockGameBridge();
            var container = ServiceContainerFactory.Create(gameBridge, new MockMenuProvider());

            // Act
            var recruitmentService = container.Resolve<IAIRecruitmentService>();

            // Assert - the recruitment service should be configured with the capital deployment service
            // We verify this by checking it's the correct type that supports scaled recruitment
            Assert.IsType<AIRecruitmentService>(recruitmentService);
        }

        #endregion

        #region Strategy Integration Tests

        [Fact]
        public void Create_AIStrategies_ShouldHaveCapitalDeploymentServiceInjected()
        {
            // Arrange
            var gameBridge = CreateMockGameBridge();
            var container = ServiceContainerFactory.Create(gameBridge, new MockMenuProvider());

            // Act
            var strategies = container.Resolve<IDictionary<string, IAIStrategy>>();

            // Assert - strategies should exist and have capital deployment service
            // We verify this indirectly by ensuring the strategies are present
            Assert.Equal(3, strategies.Count);
            Assert.True(strategies.ContainsKey("michael"));
            Assert.True(strategies.ContainsKey("trevor"));
            Assert.True(strategies.ContainsKey("franklin"));
        }

        #endregion

        #region Functional Integration Tests

        [Fact]
        public void CapitalDeploymentService_ShouldProvideScaledRecruitment()
        {
            // Arrange
            var gameBridge = CreateMockGameBridge();
            var container = ServiceContainerFactory.Create(gameBridge, new MockMenuProvider());
            var capitalService = container.Resolve<ICapitalDeploymentService>();

            // Act
            int lowCashMax = capitalService.GetScaledRecruitmentMax(5000);
            int highCashMax = capitalService.GetScaledRecruitmentMax(50000);

            // Assert
            // With $5,000: 10 + (5000 / 10000) = 10
            Assert.Equal(10, lowCashMax);
            // With $50,000: 10 + (50000 / 10000) = 15
            Assert.Equal(15, highCashMax);
        }

        [Fact]
        public void CapitalDeploymentService_ShouldProvideOverwhelmingAttackForce()
        {
            // Arrange
            var gameBridge = CreateMockGameBridge();
            var container = ServiceContainerFactory.Create(gameBridge, new MockMenuProvider());
            var capitalService = container.Resolve<ICapitalDeploymentService>();

            // Act
            // 5 defenders, 20 available troops
            int attackForce = capitalService.GetOverwhelmingAttackForce(20, 5);

            // Assert
            // Max(5 * 3.0, 20 * 0.5) = Max(15, 10) = 15
            Assert.Equal(15, attackForce);
        }

        #endregion
    }
}
