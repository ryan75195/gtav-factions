using System;
using FactionWars.ScriptHookV;
using Xunit;

namespace FactionWars.Tests.Unit.ScriptHookV
{
    public class ServiceContainerTests
    {
        [Fact]
        public void Register_ShouldStoreService()
        {
            // Arrange
            var container = new ServiceContainer();
            var service = new TestService();

            // Act
            container.Register<ITestService>(service);

            // Assert
            Assert.True(container.IsRegistered<ITestService>());
        }

        [Fact]
        public void Resolve_ShouldReturnRegisteredService()
        {
            // Arrange
            var container = new ServiceContainer();
            var service = new TestService();
            container.Register<ITestService>(service);

            // Act
            var resolved = container.Resolve<ITestService>();

            // Assert
            Assert.Same(service, resolved);
        }

        [Fact]
        public void Resolve_WhenNotRegistered_ShouldThrowException()
        {
            // Arrange
            var container = new ServiceContainer();

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => container.Resolve<ITestService>());
        }

        [Fact]
        public void TryResolve_WhenRegistered_ShouldReturnTrueAndService()
        {
            // Arrange
            var container = new ServiceContainer();
            var service = new TestService();
            container.Register<ITestService>(service);

            // Act
            var result = container.TryResolve<ITestService>(out var resolved);

            // Assert
            Assert.True(result);
            Assert.Same(service, resolved);
        }

        [Fact]
        public void TryResolve_WhenNotRegistered_ShouldReturnFalseAndNull()
        {
            // Arrange
            var container = new ServiceContainer();

            // Act
            var result = container.TryResolve<ITestService>(out var resolved);

            // Assert
            Assert.False(result);
            Assert.Null(resolved);
        }

        [Fact]
        public void RegisterFactory_ShouldCreateInstanceOnResolve()
        {
            // Arrange
            var container = new ServiceContainer();
            var callCount = 0;
            container.RegisterFactory<ITestService>(() =>
            {
                callCount++;
                return new TestService();
            });

            // Act
            var service1 = container.Resolve<ITestService>();
            var service2 = container.Resolve<ITestService>();

            // Assert
            Assert.Equal(2, callCount); // Factory called each time
            Assert.NotSame(service1, service2);
        }

        [Fact]
        public void RegisterSingleton_ShouldReturnSameInstance()
        {
            // Arrange
            var container = new ServiceContainer();
            var callCount = 0;
            container.RegisterSingleton<ITestService>(() =>
            {
                callCount++;
                return new TestService();
            });

            // Act
            var service1 = container.Resolve<ITestService>();
            var service2 = container.Resolve<ITestService>();

            // Assert
            Assert.Equal(1, callCount); // Factory called only once
            Assert.Same(service1, service2);
        }

        [Fact]
        public void Register_SameTypeTwice_ShouldOverwrite()
        {
            // Arrange
            var container = new ServiceContainer();
            var service1 = new TestService { Name = "First" };
            var service2 = new TestService { Name = "Second" };
            container.Register<ITestService>(service1);

            // Act
            container.Register<ITestService>(service2);
            var resolved = container.Resolve<ITestService>();

            // Assert
            Assert.Same(service2, resolved);
        }

        // Test interfaces and implementations
        private interface ITestService
        {
            string Name { get; }
        }

        private class TestService : ITestService
        {
            public string Name { get; set; } = "Default";
        }
    }
}
