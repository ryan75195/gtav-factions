using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using FactionWars.AI.Controllers;
using FactionWars.AI.Interfaces;
using FactionWars.AI.Models;
using FactionWars.AI.Strategies;
using FactionWars.Combat.Interfaces;
using FactionWars.Configuration;
using FactionWars.Core.Interfaces;
using FactionWars.Factions.Interfaces;
using FactionWars.ScriptHookV.Managers;
using FactionWars.Telemetry.Interfaces;
using FactionWars.Telemetry.Sinks;
using FactionWars.Territory.Interfaces;

namespace FactionWars.ScriptHookV
{
    public static partial class ServiceContainerFactory
    {
        public const string TelemetryDirectoryEnvironmentVariable = "FACTIONWARS_TELEMETRY_DIR";

        private static void RegisterAiExecutionServices(ServiceContainer container)
        {
            // Register AI decision executor
            container.RegisterSingleton<AIDecisionExecutor>(() => new AIDecisionExecutor(
                container.Resolve<IFactionService>(),
                container.Resolve<IAIBudgetService>(),
                container.Resolve<IAIRecruitmentService>()));

            // Register consolidated AI controller with recruitment service for scaled recruitment
            container.RegisterSingleton<IAIController>(() => new AIController(
                new AIControllerDependencies
                {
                    FactionService = container.Resolve<IFactionService>(),
                    ZoneService = container.Resolve<IZoneService>(),
                    BattleSimulationService = container.Resolve<IBattleSimulationService>(),
                    AllocationService = container.Resolve<IZoneDefenderAllocationService>(),
                    GameBridge = container.Resolve<IGameBridge>(),
                    Strategies = container.Resolve<IDictionary<string, IAIStrategy>>(),
                    ZoneBattleManager = container.Resolve<IZoneBattleManager>()
                },
                container.Resolve<IAIRecruitmentService>()));
        }

        private static IDictionary<string, IAIStrategy> CreateStrategies(ServiceContainer container, GameConfig config)
        {
            var capitalDeploymentService = container.Resolve<ICapitalDeploymentService>();

            var michaelStrategy = new MichaelAIStrategy(config.AI.MichaelAggressiveness, config.AI.MichaelRiskTolerance);
            michaelStrategy.SetCapitalDeploymentService(capitalDeploymentService);

            var trevorStrategy = new TrevorAIStrategy(config.AI.TrevorAggressiveness, config.AI.TrevorRiskTolerance);
            trevorStrategy.SetCapitalDeploymentService(capitalDeploymentService);

            var franklinStrategy = new FranklinAIStrategy(config.AI.FranklinAggressiveness, config.AI.FranklinRiskTolerance);
            franklinStrategy.SetCapitalDeploymentService(capitalDeploymentService);

            return new Dictionary<string, IAIStrategy>
            {
                { "michael", michaelStrategy },
                { "trevor", trevorStrategy },
                { "franklin", franklinStrategy }
            };
        }

        private static void RegisterTelemetryServices(ServiceContainer container)
        {
            var config = container.Resolve<GameConfig>();
            var telemetryRoot = ResolveTelemetryDirectory(config);

            container.RegisterSingleton<ITelemetrySink>(() => new CsvTelemetrySink(telemetryRoot));
        }

        private static string ResolveTelemetryDirectory(GameConfig config)
        {
            var configuredTelemetryDir = Environment.GetEnvironmentVariable(TelemetryDirectoryEnvironmentVariable);
            if (!string.IsNullOrWhiteSpace(configuredTelemetryDir))
            {
                return configuredTelemetryDir;
            }

            if (IsRunningUnderTest())
            {
                return Path.Combine(Path.GetTempPath(), "FactionWars", "TestTelemetry");
            }

            var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            return Path.Combine(documentsPath, config.Persistence.SaveDirectoryName, "Telemetry");
        }

        private static bool IsRunningUnderTest()
        {
            var processName = Process.GetCurrentProcess().ProcessName;
            var appDomainName = System.AppDomain.CurrentDomain.FriendlyName;
            return ContainsTestHostSignal(processName) || ContainsTestHostSignal(appDomainName);
        }

        private static bool ContainsTestHostSignal(string? value)
        {
            return value != null
                && (value.IndexOf("testhost", System.StringComparison.OrdinalIgnoreCase) >= 0
                    || value.IndexOf("vstest", System.StringComparison.OrdinalIgnoreCase) >= 0);
        }
    }
}
