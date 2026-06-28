using System.Collections.Generic;
using FactionWars.AI.Interfaces;
using FactionWars.AI.Models;
using FactionWars.AI.Services;
using FactionWars.Combat.Interfaces;
using FactionWars.Configuration;
using FactionWars.Core.Interfaces;
using FactionWars.Core.Models;
using FactionWars.Core.Services;
using FactionWars.Economy.Interfaces;
using FactionWars.Factions.Interfaces;
using FactionWars.ScriptHookV.Managers;
using FactionWars.ScriptHookV.Models;
using FactionWars.ScriptHookV.UI;
using FactionWars.Territory.Interfaces;
using FactionWars.UI.Interfaces;
using FactionWars.UI.Services;

namespace FactionWars.ScriptHookV
{
    public static partial class ServiceContainerFactory
    {
        private static void RegisterUIServices(ServiceContainer container, IMenuProvider? menuProvider)
        {
            // Menu provider - use provided instance for testing, or NativeUI for production
            if (menuProvider != null)
            {
                container.Register<IMenuProvider>(menuProvider);
            }
            else
            {
                container.RegisterSingleton<IMenuProvider>(() =>
                    new NativeUIMenuProvider());
            }

            // Ped blip service - manages minimap blips for peds (followers, defenders)
            container.RegisterSingleton<IPedBlipService>(() =>
                new PedBlipService(container.Resolve<IGameBridge>()));

            // Notification renderer - use a simple implementation that delegates to game bridge
            container.RegisterSingleton<INotificationRenderer>(() =>
                new GameBridgeNotificationRenderer(container.Resolve<IGameBridge>()));

            // Notification service depends on notification renderer
            container.RegisterSingleton<INotificationService>(() =>
                new NotificationService(container.Resolve<INotificationRenderer>()));

            // Territory indicator renderer - ScriptHookV implementation for zone HUD display
            container.RegisterSingleton<ITerritoryIndicatorRenderer>(() =>
                new TerritoryIndicatorRenderer());

            // Territory indicator service - manages zone status HUD
            container.RegisterSingleton<ITerritoryIndicatorService>(() =>
                new TerritoryIndicatorService(
                    container.Resolve<IFactionRepository>(),
                    container.Resolve<ITerritoryIndicatorRenderer>(),
                    container.Resolve<IZoneBattleManager>()));

            // Faction color service - manages faction color assignments
            container.RegisterSingleton<IFactionColorService>(() =>
                new FactionColorService());

            // Event alert service - raises and manages game event alerts
            container.RegisterSingleton<IEventAlertService>(() =>
                new EventAlertService(
                    container.Resolve<INotificationService>()));

            // Event feed service - manages the scrolling event feed display
            container.RegisterSingleton<IEventFeedService>(() =>
                new EventFeedService(container.Resolve<ITimeProvider>()));
        }

        private static void RegisterAIServices(ServiceContainer container)
        {
            var config = container.Resolve<GameConfig>();
            RegisterAiSupportServices(container, config);
            RegisterAiExecutionServices(container);
        }

        private static void RegisterAiSupportServices(ServiceContainer container, GameConfig config)
        {
            container.RegisterSingleton<IVehicleThreatService>(() =>
                new VehicleThreatService());

            // Anti-vehicle response service - deploys Elite units against vehicle threats
            container.RegisterSingleton<IAntiVehicleResponseService>(() =>
                new AntiVehicleResponseService(
                    container.Resolve<IFactionService>(),
                    container.Resolve<IZoneDefenderAllocationService>(),
                    container.Resolve<IVehicleThreatService>(),
                    container.Resolve<IDefenderRoleService>()));

            // Aggression response service - tracks aggression and determines AI responses
            container.RegisterSingleton<IAggressionResponseService>(() =>
                new AggressionResponseService());

            // Battle simulation service - simulates AI vs AI battles
            container.RegisterSingleton<IBattleSimulationService>(() =>
                new BattleSimulationService());

            // Background battle simulator for AI vs AI combat
            container.RegisterSingleton<BackgroundBattleSimulator>(() =>
                new BackgroundBattleSimulator(new BackgroundBattleSimulatorDependencies
                {
                    BattleSimulationService = container.Resolve<IBattleSimulationService>(),
                    FactionService = container.Resolve<IFactionService>(),
                    ZoneService = container.Resolve<IZoneService>(),
                    AllocationService = container.Resolve<IZoneDefenderAllocationService>(),
                    EventAlertService = container.Resolve<IEventAlertService>(),
                    EventFeedService = container.Resolve<IEventFeedService>()
                }));

            // Register AI budget service - costs from config
            container.RegisterSingleton<IAIBudgetService>(() => new AIBudgetService(
                costPerTroop: config.AI.AttackCostPerTroop,
                recruitCostPerTroop: config.AI.RecruitCostPerTroop));

            // Register capital deployment service - intelligent decision-making for AI spending
            container.RegisterSingleton<ICapitalDeploymentService>(() => new CapitalDeploymentService(
                container.Resolve<IAIBudgetService>(),
                container.Resolve<IZoneDefenderAllocationService>()));

            // AI strategies dictionary - maps faction IDs to their strategies
            // Each strategy gets CapitalDeploymentService injected for intelligent decision-making
            container.RegisterSingleton<IDictionary<string, IAIStrategy>>(() => CreateStrategies(container, config));

            // Register AI recruitment service with capital deployment service for scaled recruitment
            container.RegisterSingleton<IAIRecruitmentService>(() => new AIRecruitmentService(
                container.Resolve<IFactionService>(),
                container.Resolve<IAIBudgetService>(),
                container.Resolve<IDefenderRoleService>(),
                container.Resolve<ICapitalDeploymentService>()));
        }

    }
}
