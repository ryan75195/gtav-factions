using System;
using System.Collections.Generic;

namespace FactionWars.ScriptHookV
{
    /// <summary>
    /// Simple dependency injection container for service registration and resolution.
    /// Supports instance registration, factory registration, and singleton patterns.
    /// </summary>
    public class ServiceContainer
    {
        private readonly Dictionary<Type, object> _instances = new Dictionary<Type, object>();
        private readonly Dictionary<Type, Func<object>> _factories = new Dictionary<Type, Func<object>>();
        private readonly Dictionary<Type, Lazy<object>> _singletons = new Dictionary<Type, Lazy<object>>();

        /// <summary>
        /// Registers a pre-created instance for a service type.
        /// </summary>
        /// <typeparam name="TService">The service interface type.</typeparam>
        /// <param name="instance">The service instance.</param>
        public void Register<TService>(TService instance) where TService : class
        {
            var type = typeof(TService);
            _instances[type] = instance;
            // Remove any factory or singleton registration for this type
            _factories.Remove(type);
            _singletons.Remove(type);
        }

        /// <summary>
        /// Registers a factory function that creates a new instance on each resolve.
        /// </summary>
        /// <typeparam name="TService">The service interface type.</typeparam>
        /// <param name="factory">The factory function.</param>
        public void RegisterFactory<TService>(Func<TService> factory) where TService : class
        {
            var type = typeof(TService);
            _factories[type] = () => factory();
            // Remove any instance or singleton registration for this type
            _instances.Remove(type);
            _singletons.Remove(type);
        }

        /// <summary>
        /// Registers a factory function that creates a singleton instance on first resolve.
        /// </summary>
        /// <typeparam name="TService">The service interface type.</typeparam>
        /// <param name="factory">The factory function.</param>
        public void RegisterSingleton<TService>(Func<TService> factory) where TService : class
        {
            var type = typeof(TService);
            _singletons[type] = new Lazy<object>(() => factory());
            // Remove any instance or factory registration for this type
            _instances.Remove(type);
            _factories.Remove(type);
        }

        /// <summary>
        /// Resolves a service of the specified type.
        /// </summary>
        /// <typeparam name="TService">The service interface type.</typeparam>
        /// <returns>The service instance.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the service is not registered.</exception>
        public TService Resolve<TService>() where TService : class
        {
            var type = typeof(TService);

            // Check instances first (fastest)
            if (_instances.TryGetValue(type, out var instance))
            {
                return (TService)instance;
            }

            // Check singletons
            if (_singletons.TryGetValue(type, out var lazy))
            {
                return (TService)lazy.Value;
            }

            // Check factories
            if (_factories.TryGetValue(type, out var factory))
            {
                return (TService)factory();
            }

            throw new InvalidOperationException($"Service of type {type.FullName} is not registered.");
        }

        /// <summary>
        /// Tries to resolve a service of the specified type.
        /// </summary>
        /// <typeparam name="TService">The service interface type.</typeparam>
        /// <param name="service">The resolved service, or null if not registered.</param>
        /// <returns>True if the service was resolved, false otherwise.</returns>
        public bool TryResolve<TService>(out TService? service) where TService : class
        {
            var type = typeof(TService);

            // Check instances first
            if (_instances.TryGetValue(type, out var instance))
            {
                service = (TService)instance;
                return true;
            }

            // Check singletons
            if (_singletons.TryGetValue(type, out var lazy))
            {
                service = (TService)lazy.Value;
                return true;
            }

            // Check factories
            if (_factories.TryGetValue(type, out var factory))
            {
                service = (TService)factory();
                return true;
            }

            service = null;
            return false;
        }

        /// <summary>
        /// Checks if a service of the specified type is registered.
        /// </summary>
        /// <typeparam name="TService">The service interface type.</typeparam>
        /// <returns>True if the service is registered, false otherwise.</returns>
        public bool IsRegistered<TService>() where TService : class
        {
            var type = typeof(TService);
            return _instances.ContainsKey(type) ||
                   _singletons.ContainsKey(type) ||
                   _factories.ContainsKey(type);
        }
    }
}
