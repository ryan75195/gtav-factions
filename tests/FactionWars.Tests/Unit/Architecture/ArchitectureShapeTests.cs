using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using FactionWars.ScriptHookV;
using Xunit;

namespace FactionWars.Tests.Unit.Architecture
{
    public class ArchitectureShapeTests
    {
        [Fact]
        public void Interfaces_ShouldLiveInInterfacesNamespace()
        {
            var violations = ProductionTypes()
                .Where(type => type.IsInterface)
                .Where(type => !ContainsNamespaceSegment(type, "Interfaces"))
                .Where(type => !LegacyInterfaceLocations().Contains(type.FullName))
                .Select(FormatType)
                .ToList();

            AssertNoViolations(violations, "Interfaces should live in an Interfaces namespace.");
        }

        [Fact]
        public void ServiceClasses_ShouldImplementOwnInterface()
        {
            var violations = ProductionTypes()
                .Where(IsServiceClass)
                .Where(type => !ImplementsExpectedInterface(type))
                .Select(FormatType)
                .ToList();

            AssertNoViolations(violations, "Service classes should implement their matching interface.");
        }

        [Fact]
        public void Models_ShouldNotDependOnRuntimeImplementationLayers()
        {
            var runtimeLayerNames = new[] { "FactionWars.ScriptHookV", "NativeUI", "GTA" };
            var violations = ProductionTypes()
                .Where(type => ContainsNamespaceSegment(type, "Models"))
                .Where(type => !ContainsNamespaceSegment(type, "ScriptHookV"))
                .SelectMany(type => ReferencedTypes(type)
                    .Where(reference => runtimeLayerNames.Any(layer =>
                        reference.FullName?.StartsWith(layer, StringComparison.Ordinal) == true))
                    .Select(reference => FormatType(type) + " -> " + FormatType(reference)))
                .Distinct()
                .OrderBy(value => value)
                .ToList();

            AssertNoViolations(violations, "Model types should not depend on runtime implementation layers.");
        }

        [Fact]
        public void PublicApi_ShouldPreferCollectionsOverArrayReturns()
        {
            var allowedArrayReturnTypes = new[]
            {
                "FactionWars.Core.Interfaces.IGameBridge",
                "FactionWars.Core.Interfaces.IVehicleSeatPriorityService",
                "FactionWars.Core.Services.VehicleSeatPriorityService",
                "FactionWars.Core.Utils.MockGameBridge",
                "FactionWars.ScriptHookV.GameBridge"
            };

            var violations = ProductionTypes()
                .Where(type => !allowedArrayReturnTypes.Contains(type.FullName))
                .SelectMany(type => PublicOrdinaryMethods(type)
                    .Where(method => method.ReturnType.IsArray)
                    .Select(method => FormatType(type) + "." + method.Name))
                .OrderBy(value => value)
                .ToList();

            AssertNoViolations(violations, "Public methods should prefer IReadOnlyList<T> or IEnumerable<T> over arrays.");
        }

        [Fact]
        public void CoreLayer_ShouldNotReferenceScriptHookVTypes()
        {
            var violations = ProductionTypes()
                .Where(type => ContainsNamespaceSegment(type, "Core"))
                .SelectMany(type => ReferencedTypes(type)
                    .Where(reference => reference.FullName?.StartsWith("FactionWars.ScriptHookV", StringComparison.Ordinal) == true)
                    .Select(reference => FormatType(type) + " -> " + FormatType(reference)))
                .Distinct()
                .OrderBy(value => value)
                .ToList();

            AssertNoViolations(violations, "Core types should not reference ScriptHookV implementation types.");
        }

        [Fact]
        public void TestFixtures_ShouldExistForRuntimeServicesAndManagers()
        {
            var testTypeNames = typeof(ArchitectureShapeTests).Assembly
                .GetTypes()
                .Where(type => type.IsClass)
                .Select(type => type.Name)
                .ToHashSet(StringComparer.Ordinal);

            var violations = ProductionTypes()
                .Where(IsRuntimeClassThatNeedsFixture)
                .Where(type => !HasMatchingTestFixture(testTypeNames, type))
                .Select(FormatType)
                .OrderBy(value => value)
                .ToList();

            AssertNoViolations(violations, "Runtime services and managers should have matching test fixtures.");
        }

        private static IEnumerable<Type> ProductionTypes()
        {
            return LoadableTypes(typeof(GameLoopController).Assembly)
                .Where(type => type.IsPublic || type.IsNestedPublic)
                .Where(type => type.Namespace?.StartsWith("FactionWars", StringComparison.Ordinal) == true);
        }

        private static IEnumerable<Type> LoadableTypes(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                return ex.Types.Where(type => type != null).Cast<Type>();
            }
        }

        private static bool IsServiceClass(Type type)
        {
            return type.IsClass
                && !type.IsAbstract
                && type.Name.EndsWith("Service", StringComparison.Ordinal)
                && !ContainsNamespaceSegment(type, "ScriptHookV");
        }

        private static bool IsRuntimeClassThatNeedsFixture(Type type)
        {
            if (!type.IsClass || type.IsAbstract || type.IsGenericTypeDefinition)
                return false;

            if (ContainsNamespaceSegment(type, "Models") || ContainsNamespaceSegment(type, "Data"))
                return false;

            return type.Name.EndsWith("Service", StringComparison.Ordinal)
                || type.Name.EndsWith("Manager", StringComparison.Ordinal)
                || type.Name.EndsWith("Controller", StringComparison.Ordinal);
        }

        private static bool ImplementsExpectedInterface(Type type)
        {
            var expectedName = "I" + type.Name;
            return type.GetInterfaces().Any(iface =>
                iface.Name == expectedName
                || (iface.Name.StartsWith("I", StringComparison.Ordinal)
                    && type.Name.EndsWith(iface.Name.Substring(1), StringComparison.Ordinal)));
        }

        private static bool HasMatchingTestFixture(ISet<string> testTypeNames, Type type)
        {
            return testTypeNames.Contains(type.Name + "Tests")
                || testTypeNames.Any(name => name.StartsWith(type.Name, StringComparison.Ordinal)
                    && name.EndsWith("Tests", StringComparison.Ordinal));
        }

        private static ISet<string> LegacyInterfaceLocations()
        {
            return new HashSet<string>(StringComparer.Ordinal)
            {
                "FactionWars.Configuration.IConfigLoader",
                "FactionWars.Persistence.ISidecarStore",
                "FactionWars.ScriptHookV.IServiceContainer",
                "FactionWars.ScriptHookV.Managers.IFollowerManager",
                "FactionWars.ScriptHookV.Managers.ITerritoryEvents",
                "FactionWars.ScriptHookV.Persistence.IGameStateManager"
            };
        }

        private static IEnumerable<MethodInfo> PublicOrdinaryMethods(Type type)
        {
            return type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                .Where(method => !method.IsSpecialName);
        }

        private static IEnumerable<Type> ReferencedTypes(Type type)
        {
            foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
                yield return UnwrapType(field.FieldType);

            foreach (var property in type.GetProperties(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
                yield return UnwrapType(property.PropertyType);

            foreach (var constructor in type.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                foreach (var parameter in constructor.GetParameters())
                    yield return UnwrapType(parameter.ParameterType);
            }

            foreach (var method in type.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
            {
                yield return UnwrapType(method.ReturnType);
                foreach (var parameter in method.GetParameters())
                    yield return UnwrapType(parameter.ParameterType);
            }
        }

        private static Type UnwrapType(Type type)
        {
            if (type.IsArray)
                return type.GetElementType() ?? type;

            if (type.IsGenericType)
                return type.GetGenericArguments().FirstOrDefault() ?? type;

            return type;
        }

        private static bool ContainsNamespaceSegment(Type type, string segment)
        {
            return (type.Namespace ?? string.Empty)
                .Split('.')
                .Any(part => string.Equals(part, segment, StringComparison.Ordinal));
        }

        private static string FormatType(Type type)
        {
            return type.FullName ?? type.Name;
        }

        private static void AssertNoViolations(IReadOnlyCollection<string> violations, string message)
        {
            Assert.True(
                violations.Count == 0,
                message + Environment.NewLine + string.Join(Environment.NewLine, violations.Select(v => "  " + v)));
        }
    }
}
