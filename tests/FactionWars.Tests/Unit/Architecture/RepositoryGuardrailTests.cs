using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using FactionWars.AI.Interfaces;
using FactionWars.Combat.Interfaces;
using FactionWars.Configuration;
using FactionWars.Core.Interfaces;
using FactionWars.Economy.Interfaces;
using FactionWars.Factions.Interfaces;
using FactionWars.Persistence;
using FactionWars.ScriptHookV;
using FactionWars.ScriptHookV.Persistence;
using FactionWars.Telemetry.Interfaces;
using FactionWars.Territory.Interfaces;
using FactionWars.Tests.Mocks;
using FactionWars.UI.Interfaces;
using Moq;
using Xunit;

namespace FactionWars.Tests.Unit.Architecture
{
    public class RepositoryGuardrailTests
    {
        private static readonly Regex NativeReferencePattern = new Regex(
            @"^\s*using\s+(GTA|NativeUI)(\.|;)|\bGTA\.",
            RegexOptions.Compiled | RegexOptions.Multiline);

        [Fact]
        public void NativeGameReferences_ShouldStayInScriptHookVLayer()
        {
            var violations = SourceFiles()
                .Where(file => !PathContainsSegment(file, "ScriptHookV"))
                .Where(file => NativeReferencePattern.IsMatch(File.ReadAllText(file)))
                .Select(ToRelativePath)
                .ToList();

            AssertNoViolations(
                violations,
                "Only the ScriptHookV layer should reference GTA/NativeUI APIs directly. Put native calls behind IGameBridge or renderer interfaces.");
        }

        [Fact]
        public void CoreLayer_ShouldNotDependOnScriptHookVLayer()
        {
            var coreRoot = Path.Combine(SolutionRoot(), "src", "FactionWars", "Core");
            var violations = Directory.GetFiles(coreRoot, "*.cs", SearchOption.AllDirectories)
                .Where(file => ContainsOrdinal(File.ReadAllText(file), "FactionWars.ScriptHookV"))
                .Select(ToRelativePath)
                .ToList();

            AssertNoViolations(
                violations,
                "Core must stay portable and testable. Depend on Core interfaces instead of ScriptHookV implementations.");
        }

        [Fact]
        public void ProductionCode_ShouldNotReferenceTestOnlyDependencies()
        {
            var testOnlyReferences = new[] { "FactionWars.Tests", "using Moq", "using Xunit" };

            var violations = SourceFiles()
                .Where(file => testOnlyReferences.Any(reference =>
                    ContainsOrdinal(File.ReadAllText(file), reference)))
                .Select(ToRelativePath)
                .ToList();

            AssertNoViolations(
                violations,
                "Production code must not depend on test assemblies or test-only packages.");
        }

        [Fact]
        public void RuntimeServiceContracts_ShouldBeRegisteredInCompositionRoot()
        {
            var gameBridge = CreateGameBridge();
            var container = ServiceContainerFactory.Create(gameBridge, new MockMenuProvider());

            var requiredRegistrations = new Action<ServiceContainer>[]
            {
                AssertRegistered<IConfigLoader>,
                AssertRegistered<GameConfig>,
                AssertRegistered<IGameBridge>,
                AssertRegistered<ITimeProvider>,
                AssertRegistered<IPlayerFactionDetector>,
                AssertRegistered<IZoneRepository>,
                AssertRegistered<IFactionRepository>,
                AssertRegistered<IPedPool>,
                AssertRegistered<IDifficultyService>,
                AssertRegistered<IZoneService>,
                AssertRegistered<IFactionService>,
                AssertRegistered<IDefenderTierService>,
                AssertRegistered<IFollowerService>,
                AssertRegistered<IZoneDefenderAllocationRepository>,
                AssertRegistered<IZoneDefenderAllocationService>,
                AssertRegistered<IPlayerContext>,
                AssertRegistered<IVictoryConditionService>,
                AssertRegistered<ISpawnPositionCalculator>,
                AssertRegistered<IPedSpawningService>,
                AssertRegistered<IPedDespawnService>,
                AssertRegistered<IPedRecyclingService>,
                AssertRegistered<IWaveSpawnerService>,
                AssertRegistered<IDefenderScalingService>,
                AssertRegistered<IDefenderCasualtyService>,
                AssertRegistered<IZoneBattleManager>,
                AssertRegistered<IPersistenceService>,
                AssertRegistered<ISidecarStore>,
                AssertRegistered<LegacyBackupTask>,
                AssertRegistered<NativeSaveWatcher>,
                AssertRegistered<IGameStateManager>,
                AssertRegistered<IZoneTraitResourceModifier>,
                AssertRegistered<ISupplyLineService>,
                AssertRegistered<IResourceTickService>,
                AssertRegistered<ITroopPurchaseService>,
                AssertRegistered<IMenuProvider>,
                AssertRegistered<IPedBlipService>,
                AssertRegistered<INotificationRenderer>,
                AssertRegistered<INotificationService>,
                AssertRegistered<ITerritoryIndicatorRenderer>,
                AssertRegistered<ITerritoryIndicatorService>,
                AssertRegistered<IFactionColorService>,
                AssertRegistered<IEventAlertService>,
                AssertRegistered<IEventFeedService>,
                AssertRegistered<IVehicleThreatService>,
                AssertRegistered<IAntiVehicleResponseService>,
                AssertRegistered<IAggressionResponseService>,
                AssertRegistered<IBattleSimulationService>,
                AssertRegistered<IAIBudgetService>,
                AssertRegistered<ICapitalDeploymentService>,
                AssertRegistered<IDictionary<string, IAIStrategy>>,
                AssertRegistered<IAIRecruitmentService>,
                AssertRegistered<IAIController>,
                AssertRegistered<ITelemetrySink>
            };

            foreach (var assertRegistration in requiredRegistrations)
            {
                assertRegistration(container);
            }
        }

        private static IEnumerable<string> SourceFiles()
        {
            var sourceRoot = Path.Combine(SolutionRoot(), "src", "FactionWars");
            return Directory.GetFiles(sourceRoot, "*.cs", SearchOption.AllDirectories)
                .Where(file => !PathContainsSegment(file, "bin"))
                .Where(file => !PathContainsSegment(file, "obj"));
        }

        private static IGameBridge CreateGameBridge()
        {
            var mock = new Mock<IGameBridge>();
            mock.Setup(x => x.GetScriptsDirectory()).Returns(Path.GetTempPath());
            return mock.Object;
        }

        private static void AssertRegistered<TService>(ServiceContainer container) where TService : class
        {
            Assert.True(container.IsRegistered<TService>(), typeof(TService).FullName + " should be registered.");
            Assert.NotNull(container.Resolve<TService>());
        }

        private static bool PathContainsSegment(string path, string segment)
        {
            return path
                .Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                .Any(part => string.Equals(part, segment, StringComparison.OrdinalIgnoreCase));
        }

        private static string SolutionRoot()
        {
            var directory = AppContext.BaseDirectory;
            while (directory != null)
            {
                if (File.Exists(Path.Combine(directory, "FactionWars.sln")))
                {
                    return directory;
                }

                directory = Path.GetDirectoryName(directory);
            }

            throw new InvalidOperationException("Could not find FactionWars.sln.");
        }

        private static string ToRelativePath(string path)
        {
            var root = SolutionRoot();
            if (!path.StartsWith(root, StringComparison.OrdinalIgnoreCase))
            {
                return path;
            }

            return path.Substring(root.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }

        private static bool ContainsOrdinal(string value, string search)
        {
            return value.IndexOf(search, StringComparison.Ordinal) >= 0;
        }

        private static void AssertNoViolations(IReadOnlyCollection<string> violations, string message)
        {
            Assert.True(
                violations.Count == 0,
                message + Environment.NewLine + string.Join(Environment.NewLine, violations.Select(v => "  " + v)));
        }
    }
}
