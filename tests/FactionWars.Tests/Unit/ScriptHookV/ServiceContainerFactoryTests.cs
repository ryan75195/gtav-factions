using System;
using System.Collections.Generic;
using System.IO;
using FactionWars.AI.Interfaces;
using FactionWars.Combat.Interfaces;
using FactionWars.Core.Interfaces;
using FactionWars.Economy.Interfaces;
using FactionWars.Factions.Interfaces;
using FactionWars.Factions.Models;
using FactionWars.ScriptHookV;
using FactionWars.ScriptHookV.Managers;
using FactionWars.Territory.Interfaces;
using FactionWars.Tests.Mocks;
using FactionWars.UI.Interfaces;
using Moq;
using Xunit;

namespace FactionWars.Tests.Unit.ScriptHookV
{
    public class ServiceContainerFactoryTests
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

        [Fact]
        public void Create_ShouldReturnServiceContainer()
        {
            // Arrange
            var gameBridge = CreateMockGameBridge();

            // Act
            var container = ServiceContainerFactory.Create(gameBridge, new MockMenuProvider());

            // Assert
            Assert.NotNull(container);
            Assert.IsType<ServiceContainer>(container);
        }

        [Fact]
        public void Create_ShouldRegisterGameBridge()
        {
            // Arrange
            var gameBridge = CreateMockGameBridge();

            // Act
            var container = ServiceContainerFactory.Create(gameBridge, new MockMenuProvider());

            // Assert
            var resolved = container.Resolve<IGameBridge>();
            Assert.Same(gameBridge, resolved);
        }

        [Fact]
        public void Create_ShouldRegisterTimeProvider()
        {
            // Arrange
            var gameBridge = CreateMockGameBridge();

            // Act
            var container = ServiceContainerFactory.Create(gameBridge, new MockMenuProvider());

            // Assert
            Assert.True(container.IsRegistered<ITimeProvider>());
        }

        [Fact]
        public void Create_ShouldRegisterZoneRepository()
        {
            // Arrange
            var gameBridge = CreateMockGameBridge();

            // Act
            var container = ServiceContainerFactory.Create(gameBridge, new MockMenuProvider());

            // Assert
            Assert.True(container.IsRegistered<IZoneRepository>());
        }

        [Fact]
        public void Create_ShouldRegisterZoneService()
        {
            // Arrange
            var gameBridge = CreateMockGameBridge();

            // Act
            var container = ServiceContainerFactory.Create(gameBridge, new MockMenuProvider());

            // Assert
            Assert.True(container.IsRegistered<IZoneService>());
        }

        [Fact]
        public void Create_ShouldRegisterFactionRepository()
        {
            // Arrange
            var gameBridge = CreateMockGameBridge();

            // Act
            var container = ServiceContainerFactory.Create(gameBridge, new MockMenuProvider());

            // Assert
            Assert.True(container.IsRegistered<IFactionRepository>());
        }

        [Fact]
        public void Create_ShouldRegisterFactionService()
        {
            // Arrange
            var gameBridge = CreateMockGameBridge();

            // Act
            var container = ServiceContainerFactory.Create(gameBridge, new MockMenuProvider());

            // Assert
            Assert.True(container.IsRegistered<IFactionService>());
        }

        [Fact]
        public void Create_ShouldRegisterPedPool()
        {
            // Arrange
            var gameBridge = CreateMockGameBridge();

            // Act
            var container = ServiceContainerFactory.Create(gameBridge, new MockMenuProvider());

            // Assert
            Assert.True(container.IsRegistered<IPedPool>());
        }

        [Fact]
        public void Create_ShouldRegisterPedSpawningService()
        {
            // Arrange
            var gameBridge = CreateMockGameBridge();

            // Act
            var container = ServiceContainerFactory.Create(gameBridge, new MockMenuProvider());

            // Assert
            Assert.True(container.IsRegistered<IPedSpawningService>());
        }

        [Fact]
        public void Create_ShouldRegisterPedDespawnService()
        {
            // Arrange
            var gameBridge = CreateMockGameBridge();

            // Act
            var container = ServiceContainerFactory.Create(gameBridge, new MockMenuProvider());

            // Assert
            Assert.True(container.IsRegistered<IPedDespawnService>());
        }

        [Fact]
        public void Create_ShouldRegisterControlPercentageCalculator()
        {
            // Arrange
            var gameBridge = CreateMockGameBridge();

            // Act
            var container = ServiceContainerFactory.Create(gameBridge, new MockMenuProvider());

            // Assert
            Assert.True(container.IsRegistered<IControlPercentageCalculator>());
        }

        [Fact]
        public void Create_ShouldRegisterTakeoverDetector()
        {
            // Arrange
            var gameBridge = CreateMockGameBridge();

            // Act
            var container = ServiceContainerFactory.Create(gameBridge, new MockMenuProvider());

            // Assert
            Assert.True(container.IsRegistered<ITakeoverDetector>());
        }

        [Fact]
        public void Create_ShouldRegisterCombatResultHandler()
        {
            // Arrange
            var gameBridge = CreateMockGameBridge();

            // Act
            var container = ServiceContainerFactory.Create(gameBridge, new MockMenuProvider());

            // Assert
            Assert.True(container.IsRegistered<ICombatResultHandler>());
        }

        [Fact]
        public void Create_ShouldRegisterResourceTickService()
        {
            // Arrange
            var gameBridge = CreateMockGameBridge();

            // Act
            var container = ServiceContainerFactory.Create(gameBridge, new MockMenuProvider());

            // Assert
            Assert.True(container.IsRegistered<IResourceTickService>());
        }

        [Fact]
        public void Create_ShouldRegisterSupplyLineService()
        {
            // Arrange
            var gameBridge = CreateMockGameBridge();

            // Act
            var container = ServiceContainerFactory.Create(gameBridge, new MockMenuProvider());

            // Assert
            Assert.True(container.IsRegistered<ISupplyLineService>());
        }

        [Fact]
        public void Create_ShouldRegisterNotificationService()
        {
            // Arrange
            var gameBridge = CreateMockGameBridge();

            // Act
            var container = ServiceContainerFactory.Create(gameBridge, new MockMenuProvider());

            // Assert
            Assert.True(container.IsRegistered<INotificationService>());
        }

        [Fact]
        public void Create_ShouldRegisterPlayerFactionDetector()
        {
            // Arrange
            var gameBridge = CreateMockGameBridge();

            // Act
            var container = ServiceContainerFactory.Create(gameBridge, new MockMenuProvider());

            // Assert
            Assert.True(container.IsRegistered<IPlayerFactionDetector>());
        }

        [Fact]
        public void Create_ShouldReturnSingletonPlayerFactionDetector()
        {
            // Arrange
            var gameBridge = CreateMockGameBridge();
            var container = ServiceContainerFactory.Create(gameBridge, new MockMenuProvider());

            // Act
            var detector1 = container.Resolve<IPlayerFactionDetector>();
            var detector2 = container.Resolve<IPlayerFactionDetector>();

            // Assert
            Assert.Same(detector1, detector2);
        }

        [Fact]
        public void Create_ShouldReturnSingletonServices()
        {
            // Arrange
            var gameBridge = CreateMockGameBridge();
            var container = ServiceContainerFactory.Create(gameBridge, new MockMenuProvider());

            // Act
            var zoneRepo1 = container.Resolve<IZoneRepository>();
            var zoneRepo2 = container.Resolve<IZoneRepository>();

            // Assert - repositories should be singletons
            Assert.Same(zoneRepo1, zoneRepo2);
        }

        [Fact]
        public void Create_ShouldReturnSingletonFactionRepository()
        {
            // Arrange
            var gameBridge = CreateMockGameBridge();
            var container = ServiceContainerFactory.Create(gameBridge, new MockMenuProvider());

            // Act
            var repo1 = container.Resolve<IFactionRepository>();
            var repo2 = container.Resolve<IFactionRepository>();

            // Assert
            Assert.Same(repo1, repo2);
        }

        [Fact]
        public void Create_WithNullGameBridge_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => ServiceContainerFactory.Create(null!));
        }

        [Fact]
        public void Create_ShouldRegisterPedRecyclingService()
        {
            // Arrange
            var gameBridge = CreateMockGameBridge();

            // Act
            var container = ServiceContainerFactory.Create(gameBridge, new MockMenuProvider());

            // Assert
            Assert.True(container.IsRegistered<IPedRecyclingService>());
        }

        [Fact]
        public void Create_ShouldReturnSingletonPedRecyclingService()
        {
            // Arrange
            var gameBridge = CreateMockGameBridge();
            var container = ServiceContainerFactory.Create(gameBridge, new MockMenuProvider());

            // Act
            var service1 = container.Resolve<IPedRecyclingService>();
            var service2 = container.Resolve<IPedRecyclingService>();

            // Assert
            Assert.Same(service1, service2);
        }

        [Fact]
        public void Create_ShouldRegisterWaveSpawnerService()
        {
            // Arrange
            var gameBridge = CreateMockGameBridge();

            // Act
            var container = ServiceContainerFactory.Create(gameBridge, new MockMenuProvider());

            // Assert
            Assert.True(container.IsRegistered<IWaveSpawnerService>());
        }

        [Fact]
        public void Create_ShouldReturnSingletonWaveSpawnerService()
        {
            // Arrange
            var gameBridge = CreateMockGameBridge();
            var container = ServiceContainerFactory.Create(gameBridge, new MockMenuProvider());

            // Act
            var service1 = container.Resolve<IWaveSpawnerService>();
            var service2 = container.Resolve<IWaveSpawnerService>();

            // Assert
            Assert.Same(service1, service2);
        }

        [Fact]
        public void Create_ShouldRegisterAIStrategies()
        {
            // Arrange
            var gameBridge = CreateMockGameBridge();

            // Act
            var container = ServiceContainerFactory.Create(gameBridge, new MockMenuProvider());

            // Assert
            Assert.True(container.IsRegistered<IDictionary<string, IAIStrategy>>());
        }

        [Fact]
        public void Create_ShouldReturnSingletonAIStrategies()
        {
            // Arrange
            var gameBridge = CreateMockGameBridge();
            var container = ServiceContainerFactory.Create(gameBridge, new MockMenuProvider());

            // Act
            var strategies1 = container.Resolve<IDictionary<string, IAIStrategy>>();
            var strategies2 = container.Resolve<IDictionary<string, IAIStrategy>>();

            // Assert
            Assert.Same(strategies1, strategies2);
        }

        [Fact]
        public void Create_ShouldRegisterMichaelAIStrategy()
        {
            // Arrange
            var gameBridge = CreateMockGameBridge();
            var container = ServiceContainerFactory.Create(gameBridge, new MockMenuProvider());

            // Act
            var strategies = container.Resolve<IDictionary<string, IAIStrategy>>();

            // Assert
            Assert.True(strategies.ContainsKey("michael"));
            Assert.Equal(FactionType.Michael, strategies["michael"].FactionType);
        }

        [Fact]
        public void Create_ShouldRegisterTrevorAIStrategy()
        {
            // Arrange
            var gameBridge = CreateMockGameBridge();
            var container = ServiceContainerFactory.Create(gameBridge, new MockMenuProvider());

            // Act
            var strategies = container.Resolve<IDictionary<string, IAIStrategy>>();

            // Assert
            Assert.True(strategies.ContainsKey("trevor"));
            Assert.Equal(FactionType.Trevor, strategies["trevor"].FactionType);
        }

        [Fact]
        public void Create_ShouldRegisterFranklinAIStrategy()
        {
            // Arrange
            var gameBridge = CreateMockGameBridge();
            var container = ServiceContainerFactory.Create(gameBridge, new MockMenuProvider());

            // Act
            var strategies = container.Resolve<IDictionary<string, IAIStrategy>>();

            // Assert
            Assert.True(strategies.ContainsKey("franklin"));
            Assert.Equal(FactionType.Franklin, strategies["franklin"].FactionType);
        }

        [Fact]
        public void Create_AIStrategies_ShouldContainExactlyThreeStrategies()
        {
            // Arrange
            var gameBridge = CreateMockGameBridge();
            var container = ServiceContainerFactory.Create(gameBridge, new MockMenuProvider());

            // Act
            var strategies = container.Resolve<IDictionary<string, IAIStrategy>>();

            // Assert
            Assert.Equal(3, strategies.Count);
        }

        [Fact]
        public void Create_ShouldRegisterAggressionResponseService()
        {
            // Arrange
            var gameBridge = CreateMockGameBridge();

            // Act
            var container = ServiceContainerFactory.Create(gameBridge, new MockMenuProvider());

            // Assert
            Assert.True(container.IsRegistered<IAggressionResponseService>());
        }

        [Fact]
        public void Create_ShouldReturnSingletonAggressionResponseService()
        {
            // Arrange
            var gameBridge = CreateMockGameBridge();
            var container = ServiceContainerFactory.Create(gameBridge, new MockMenuProvider());

            // Act
            var service1 = container.Resolve<IAggressionResponseService>();
            var service2 = container.Resolve<IAggressionResponseService>();

            // Assert
            Assert.Same(service1, service2);
        }

        [Fact]
        public void Create_ShouldRegisterNotificationRenderer()
        {
            // Arrange
            var gameBridge = CreateMockGameBridge();

            // Act
            var container = ServiceContainerFactory.Create(gameBridge, new MockMenuProvider());

            // Assert
            Assert.True(container.IsRegistered<INotificationRenderer>());
        }

        [Fact]
        public void Create_ShouldReturnSingletonNotificationRenderer()
        {
            // Arrange
            var gameBridge = CreateMockGameBridge();
            var container = ServiceContainerFactory.Create(gameBridge, new MockMenuProvider());

            // Act
            var renderer1 = container.Resolve<INotificationRenderer>();
            var renderer2 = container.Resolve<INotificationRenderer>();

            // Assert
            Assert.Same(renderer1, renderer2);
        }

        [Fact]
        public void Create_ShouldReturnSingletonNotificationService()
        {
            // Arrange
            var gameBridge = CreateMockGameBridge();
            var container = ServiceContainerFactory.Create(gameBridge, new MockMenuProvider());

            // Act
            var service1 = container.Resolve<INotificationService>();
            var service2 = container.Resolve<INotificationService>();

            // Assert
            Assert.Same(service1, service2);
        }

        [Fact]
        public void Create_ShouldRegisterPersistenceService()
        {
            // Arrange
            var gameBridge = CreateMockGameBridge();

            // Act
            var container = ServiceContainerFactory.Create(gameBridge, new MockMenuProvider());

            // Assert
            Assert.True(container.IsRegistered<IPersistenceService>());
        }

        [Fact]
        public void Create_ShouldReturnSingletonPersistenceService()
        {
            // Arrange
            var gameBridge = CreateMockGameBridge();
            var container = ServiceContainerFactory.Create(gameBridge, new MockMenuProvider());

            // Act
            var service1 = container.Resolve<IPersistenceService>();
            var service2 = container.Resolve<IPersistenceService>();

            // Assert
            Assert.Same(service1, service2);
        }

        [Fact]
        public void Create_ShouldRegisterSaveSlotManagerWithPersistenceService()
        {
            // Arrange
            var gameBridge = CreateMockGameBridge();

            // Act
            var container = ServiceContainerFactory.Create(gameBridge, new MockMenuProvider());

            // Assert
            Assert.True(container.IsRegistered<ISaveSlotManager>());
            var saveSlotManager = container.Resolve<ISaveSlotManager>();
            // Should be a real SaveSlotManager, not a stub
            Assert.IsType<FactionWars.Persistence.SaveSlotManager>(saveSlotManager);
        }

        [Fact]
        public void Create_ShouldReturnSingletonSaveSlotManager()
        {
            // Arrange
            var gameBridge = CreateMockGameBridge();
            var container = ServiceContainerFactory.Create(gameBridge, new MockMenuProvider());

            // Act
            var service1 = container.Resolve<ISaveSlotManager>();
            var service2 = container.Resolve<ISaveSlotManager>();

            // Assert
            Assert.Same(service1, service2);
        }

        [Fact]
        public void Create_ShouldRegisterGameStateManager()
        {
            // Arrange
            var gameBridge = CreateMockGameBridge();

            // Act
            var container = ServiceContainerFactory.Create(gameBridge, new MockMenuProvider());

            // Assert
            Assert.True(container.IsRegistered<FactionWars.ScriptHookV.Persistence.IGameStateManager>());
            var manager = container.Resolve<FactionWars.ScriptHookV.Persistence.IGameStateManager>();
            Assert.IsType<FactionWars.ScriptHookV.Persistence.GameStateManager>(manager);
        }

        [Fact]
        public void Create_ShouldReturnSingletonGameStateManager()
        {
            // Arrange
            var gameBridge = CreateMockGameBridge();
            var container = ServiceContainerFactory.Create(gameBridge, new MockMenuProvider());

            // Act
            var service1 = container.Resolve<FactionWars.ScriptHookV.Persistence.IGameStateManager>();
            var service2 = container.Resolve<FactionWars.ScriptHookV.Persistence.IGameStateManager>();

            // Assert
            Assert.Same(service1, service2);
        }

        [Fact]
        public void Create_ShouldRegisterGameStateCoordinatorWithRealImplementation()
        {
            // Arrange
            var gameBridge = CreateMockGameBridge();

            // Act
            var container = ServiceContainerFactory.Create(gameBridge, new MockMenuProvider());

            // Assert
            Assert.True(container.IsRegistered<IGameStateCoordinator>());
            var coordinator = container.Resolve<IGameStateCoordinator>();
            // Should be a real GameStateCoordinator, not a stub
            Assert.IsType<FactionWars.ScriptHookV.Persistence.GameStateCoordinator>(coordinator);
        }

        [Fact]
        public void Create_ShouldReturnSingletonGameStateCoordinator()
        {
            // Arrange
            var gameBridge = CreateMockGameBridge();
            var container = ServiceContainerFactory.Create(gameBridge, new MockMenuProvider());

            // Act
            var service1 = container.Resolve<IGameStateCoordinator>();
            var service2 = container.Resolve<IGameStateCoordinator>();

            // Assert
            Assert.Same(service1, service2);
        }

        [Fact]
        public void Create_ShouldRegisterAutoSaveService()
        {
            // Arrange
            var gameBridge = CreateMockGameBridge();

            // Act
            var container = ServiceContainerFactory.Create(gameBridge, new MockMenuProvider());

            // Assert
            Assert.True(container.IsRegistered<IAutoSaveService>());
            var autoSaveService = container.Resolve<IAutoSaveService>();
            Assert.IsType<FactionWars.Persistence.AutoSaveService>(autoSaveService);
        }

        [Fact]
        public void Create_ShouldReturnSingletonAutoSaveService()
        {
            // Arrange
            var gameBridge = CreateMockGameBridge();
            var container = ServiceContainerFactory.Create(gameBridge, new MockMenuProvider());

            // Act
            var service1 = container.Resolve<IAutoSaveService>();
            var service2 = container.Resolve<IAutoSaveService>();

            // Assert
            Assert.Same(service1, service2);
        }

        [Fact]
        public void Create_AutoSaveService_ShouldHaveCorrectSaveDirectory()
        {
            // Arrange
            var gameBridge = CreateMockGameBridge();
            var container = ServiceContainerFactory.Create(gameBridge, new MockMenuProvider());

            // Act
            var autoSaveService = container.Resolve<IAutoSaveService>();

            // Assert
            // The auto-save file path should be in the user's documents/FactionWars directory
            Assert.Contains("FactionWars", autoSaveService.AutoSaveFilePath);
            Assert.Contains("autosave.json", autoSaveService.AutoSaveFilePath);
        }

        [Fact]
        public void Create_AutoSaveService_ShouldHaveDefaultFiveMinuteInterval()
        {
            // Arrange
            var gameBridge = CreateMockGameBridge();
            var container = ServiceContainerFactory.Create(gameBridge, new MockMenuProvider());

            // Act
            var autoSaveService = container.Resolve<IAutoSaveService>();

            // Assert
            Assert.Equal(TimeSpan.FromMinutes(5), autoSaveService.Interval);
        }
    }
}
