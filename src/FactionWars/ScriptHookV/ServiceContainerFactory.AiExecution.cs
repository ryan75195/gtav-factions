using System;
using System.Collections.Generic;
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
            var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            var telemetryRoot = Path.Combine(documentsPath, config.Persistence.SaveDirectoryName, "Telemetry");

            container.RegisterSingleton<ITelemetrySink>(() => new CsvTelemetrySink(telemetryRoot));
        }
    }
}
