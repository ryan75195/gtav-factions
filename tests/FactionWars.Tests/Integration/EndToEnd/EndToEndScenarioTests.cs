using System;
using System.Collections.Generic;
using System.Linq;
using FactionWars.AI.Interfaces;
using FactionWars.AI.Models;
using FactionWars.AI.Strategies;
using FactionWars.Combat.Interfaces;
using FactionWars.Combat.Models;
using FactionWars.Combat.Pools;
using FactionWars.Combat.Services;
using FactionWars.Core.Interfaces;
using FactionWars.Core.Models;
using FactionWars.Core.Services;
using FactionWars.Core.Utils;
using FactionWars.Economy.Interfaces;
using FactionWars.Economy.Services;
using FactionWars.Factions.Interfaces;
using FactionWars.Factions.Models;
using FactionWars.Factions.Repositories;
using FactionWars.Factions.Services;
using FactionWars.ScriptHookV;
using FactionWars.ScriptHookV.Managers;
using FactionWars.ScriptHookV.Persistence;
using FactionWars.Territory.Interfaces;
using FactionWars.Territory.Models;
using FactionWars.Territory.Repositories;
using FactionWars.Territory.Services;
using FactionWars.Tests.Mocks;
using FactionWars.UI.Interfaces;
using FactionWars.UI.Models;
using FactionWars.UI.Services;
using Moq;
using Xunit;

namespace FactionWars.Tests.Integration.EndToEnd
{
    /// <summary>
    /// End-to-end scenario tests that verify complete game scenarios involving
    /// multiple systems: zones, factions, combat, economy, AI, and notifications.
    /// Uses real implementations (not mocks) to verify full integration.
    /// </summary>
    public class EndToEndScenarioTests
    {
        private const string MichaelFactionId = "faction-michael";
        private const string TrevorFactionId = "faction-trevor";
        private const string FranklinFactionId = "faction-franklin";

        #region Scenario: Complete Territory Conquest

        [Fact]
        public void Scenario_CompleteTerritoryConquest_FromAttackToCapture()
        {
            // Arrange: Set up a complete game world
            var (zoneRepo, zoneService, factionRepo, factionService, combatHandler, tickService) = CreateGameWorld();

            // Create factions
            SetupFaction(factionRepo, factionService, MichaelFactionId, "Michael's Crew", 10000, 50);
            SetupFaction(factionRepo, factionService, TrevorFactionId, "Trevor's Gang", 5000, 30);

            // Create zones - Trevor owns Downtown, Michael wants to capture it
            var downtown = CreateZone(zoneRepo, "zone-downtown", "Downtown", TrevorFactionId, 5);
            factionService.AddZoneToFaction(TrevorFactionId, downtown.Id);

            // Verify initial state
            Assert.Equal(TrevorFactionId, zoneService.GetZone(downtown.Id)!.OwnerFactionId);
            Assert.Equal(1, factionService.GetZoneCount(TrevorFactionId));
            Assert.Equal(0, factionService.GetZoneCount(MichaelFactionId));

            // Act: Michael initiates attack and wins
            var encounter = new CombatEncounter("combat-1", downtown.Id, MichaelFactionId, TrevorFactionId);
            downtown.IsContested = true;
            zoneRepo.Update(downtown);

            // Combat unfolds - Michael's forces dominate
            encounter.AttackerPedCount = 25;
            encounter.DefenderPedCount = 0; // All defenders eliminated

            var controlCalc = new ControlPercentageCalculator();
            controlCalc.ApplyToEncounter(encounter);

            var takeoverDetector = new TakeoverDetector();
            var result = takeoverDetector.CheckTakeover(encounter);

            Assert.Equal(TakeoverStatus.AttackerVictory, result.Status);

            // Process combat result
            encounter.End(CombatStatus.AttackerVictory);
            var combatResult = combatHandler.ProcessCombatResult(encounter);

            // Assert: Zone ownership transferred
            Assert.True(combatResult.IsSuccess);
            Assert.Equal(CombatResultOutcome.ZoneCaptured, combatResult.Outcome);
            Assert.Equal(MichaelFactionId, zoneService.GetZone(downtown.Id)!.OwnerFactionId);
            Assert.False(zoneService.GetZone(downtown.Id)!.IsContested);
        }

        [Fact]
        public void Scenario_FailedAttack_DefenderRepelsInvader()
        {
            // Arrange
            var (zoneRepo, zoneService, factionRepo, factionService, combatHandler, _) = CreateGameWorld();

            SetupFaction(factionRepo, factionService, MichaelFactionId, "Michael's Crew", 5000, 20);
            SetupFaction(factionRepo, factionService, TrevorFactionId, "Trevor's Gang", 10000, 50);

            var fortress = CreateZone(zoneRepo, "zone-fortress", "The Fortress", TrevorFactionId, 8);
            factionService.AddZoneToFaction(TrevorFactionId, fortress.Id);

            // Act: Michael's weaker force attacks Trevor's stronghold
            var encounter = new CombatEncounter("combat-1", fortress.Id, MichaelFactionId, TrevorFactionId);

            // Combat unfolds - Trevor's defenders crush the attack
            encounter.AttackerPedCount = 0; // All attackers eliminated
            encounter.DefenderPedCount = 20;

            var controlCalc = new ControlPercentageCalculator();
            controlCalc.ApplyToEncounter(encounter);

            var takeoverDetector = new TakeoverDetector();
            var result = takeoverDetector.CheckTakeover(encounter);

            Assert.Equal(TakeoverStatus.DefenderVictory, result.Status);

            encounter.End(CombatStatus.DefenderVictory);
            var combatResult = combatHandler.ProcessCombatResult(encounter);

            // Assert: Defender retains control
            Assert.True(combatResult.IsSuccess);
            Assert.Equal(CombatResultOutcome.ZoneDefended, combatResult.Outcome);
            Assert.Equal(TrevorFactionId, zoneService.GetZone(fortress.Id)!.OwnerFactionId);
            Assert.Equal(100f, zoneService.GetZone(fortress.Id)!.ControlPercentage);
        }

        #endregion

        #region Scenario: Economic Growth to Military Expansion

        [Fact]
        public void Scenario_EconomicGrowth_EnablesMilitaryExpansion()
        {
            // Arrange: Create a faction with economic zones
            var (zoneRepo, zoneService, factionRepo, factionService, _, tickService) = CreateGameWorld();

            SetupFaction(factionRepo, factionService, MichaelFactionId, "Michael's Crew", 0, 0);

            // Give Michael diverse economic zones
            var commercial = CreateZone(zoneRepo, "zone-1", "Shopping District", MichaelFactionId, 3);
            commercial.Traits = ZoneTrait.Commercial;
            zoneRepo.Update(commercial);
            factionService.AddZoneToFaction(MichaelFactionId, commercial.Id);

            var residential = CreateZone(zoneRepo, "zone-2", "Suburbs", MichaelFactionId, 3);
            residential.Traits = ZoneTrait.Residential;
            zoneRepo.Update(residential);
            factionService.AddZoneToFaction(MichaelFactionId, residential.Id);

            var industrial = CreateZone(zoneRepo, "zone-3", "Factory District", MichaelFactionId, 3);
            industrial.Traits = ZoneTrait.Industrial;
            zoneRepo.Update(industrial);
            factionService.AddZoneToFaction(MichaelFactionId, industrial.Id);

            // Initial state: no resources
            var initialState = factionService.GetFactionState(MichaelFactionId);
            Assert.NotNull(initialState);
            Assert.Equal(0, initialState.Cash);
            Assert.Equal(0, initialState.TroopCount);

            // Act: Simulate several resource ticks (economy runs)
            tickService.Start();
            for (int i = 0; i < 5; i++)
            {
                tickService.ForceTick();
            }

            // Faction accumulates resources
            var postEconomyState = factionService.GetFactionState(MichaelFactionId);
            Assert.NotNull(postEconomyState);
            Assert.True(postEconomyState.Cash > 0, "Should have accumulated cash");
            Assert.True(postEconomyState.RecruitmentPoints > 0, "Should have recruitment points");
            Assert.True(postEconomyState.Weapons > 0, "Should have weapons");

            // Use recruitment points to recruit troops
            int pointsAvailable = postEconomyState.RecruitmentPoints;
            int troopsToRecruit = pointsAvailable / 10; // 10 points per troop
            if (troopsToRecruit > 0)
            {
                factionService.RecruitTroops(MichaelFactionId, troopsToRecruit);
            }

            // Assert: Faction now has military capability
            var finalState = factionService.GetFactionState(MichaelFactionId);
            Assert.NotNull(finalState);
            Assert.True(finalState.TroopCount > 0, "Should have recruited troops");
            Assert.True(finalState.MilitaryStrength > 0, "Should have military strength");
        }

        [Fact]
        public void Scenario_LosingZones_ReducesEconomicOutput()
        {
            // Arrange
            var (zoneRepo, zoneService, factionRepo, factionService, combatHandler, tickService) = CreateGameWorld();

            SetupFaction(factionRepo, factionService, MichaelFactionId, "Michael's Crew", 0, 10);
            SetupFaction(factionRepo, factionService, TrevorFactionId, "Trevor's Gang", 0, 30);

            // Michael has one zone
            var cashCow = CreateZone(zoneRepo, "zone-1", "Cash Cow", MichaelFactionId, 10);
            cashCow.Traits = ZoneTrait.Commercial;
            zoneRepo.Update(cashCow);
            factionService.AddZoneToFaction(MichaelFactionId, cashCow.Id);

            // Generate income
            tickService.Start();
            tickService.ForceTick();
            var incomeWithZone = factionService.GetFactionState(MichaelFactionId)!.Cash;
            Assert.True(incomeWithZone > 0, "Should have income with zone");

            // Act: Trevor captures Michael's zone
            var encounter = new CombatEncounter("combat-1", cashCow.Id, TrevorFactionId, MichaelFactionId);
            encounter.AttackerPedCount = 20;
            encounter.DefenderPedCount = 0;

            var controlCalc = new ControlPercentageCalculator();
            controlCalc.ApplyToEncounter(encounter);

            encounter.End(CombatStatus.AttackerVictory);
            combatHandler.ProcessCombatResult(encounter);

            // Update faction zone ownership
            factionService.RemoveZoneFromFaction(MichaelFactionId, cashCow.Id);
            factionService.AddZoneToFaction(TrevorFactionId, cashCow.Id);

            // Reset Michael's cash to test income loss
            var michaelState = factionService.GetFactionState(MichaelFactionId);
            Assert.NotNull(michaelState);
            var cashBefore = michaelState.Cash;

            tickService.ForceTick();

            // Assert: Michael no longer receives income from lost zone
            var cashAfter = factionService.GetFactionState(MichaelFactionId)!.Cash;
            Assert.Equal(cashBefore, cashAfter); // No new income
            Assert.Equal(0, factionService.GetZoneCount(MichaelFactionId));
        }

        #endregion

        #region Scenario: AI Decision Making

        [Fact]
        public void Scenario_AIEvaluatesTargetZones()
        {
            // Arrange
            var (zoneRepo, zoneService, factionRepo, factionService, _, _) = CreateGameWorld();

            // Create Michael faction
            var michaelFaction = SetupFaction(factionRepo, factionService, MichaelFactionId, "Michael's Crew", 10000, 50);

            // Create Trevor faction (opponent)
            var trevorFaction = SetupFaction(factionRepo, factionService, TrevorFactionId, "Trevor's Gang", 5000, 20);

            // Set up zones
            var michaelHQ = CreateZone(zoneRepo, "zone-michael-hq", "Michael's HQ", MichaelFactionId, 5);
            factionService.AddZoneToFaction(MichaelFactionId, michaelHQ.Id);

            var targetZone = CreateZone(zoneRepo, "zone-target", "Valuable Target", TrevorFactionId, 8);
            targetZone.Traits = ZoneTrait.Commercial;
            zoneRepo.Update(targetZone);
            factionService.AddZoneToFaction(TrevorFactionId, targetZone.Id);

            // Create AI context
            var michaelState = factionService.GetFactionState(MichaelFactionId)!;
            var allZones = zoneRepo.GetAll().ToList();
            var ownedZones = allZones.Where(z => z.OwnerFactionId == MichaelFactionId).ToList();

            var context = new AIContext(
                michaelFaction,
                michaelState,
                ownedZones,
                allZones,
                new List<Faction> { trevorFaction });

            // Act: Use Michael's AI strategy to evaluate target
            var strategy = new MichaelAIStrategy();

            float zoneScore = strategy.EvaluateZone(targetZone, context);
            bool shouldAttack = strategy.ShouldAttack(targetZone, context);

            // Assert: AI recognizes the high-value target
            Assert.True(zoneScore > 0, "High-value commercial zone should have positive score");
            Assert.True(shouldAttack, "With superior forces, should recommend attack");
        }

        [Fact]
        public void Scenario_TrevorAI_IsMoreAggressive()
        {
            // Act: Compare Trevor's strategy with Michael's
            var trevorStrategy = new TrevorAIStrategy();
            var michaelStrategy = new MichaelAIStrategy();

            float trevorAggressiveness = trevorStrategy.GetAggressiveness();
            float michaelAggressiveness = michaelStrategy.GetAggressiveness();

            // Assert: Trevor is more aggressive
            Assert.True(trevorAggressiveness > michaelAggressiveness,
                "Trevor's strategy should be more aggressive than Michael's");
        }

        [Fact]
        public void Scenario_FranklinAI_EvaluatesOpportunities()
        {
            // Arrange
            var (zoneRepo, zoneService, factionRepo, factionService, _, _) = CreateGameWorld();

            // Create Franklin faction
            var franklinFaction = SetupFaction(factionRepo, factionService, FranklinFactionId, "Franklin's Crew", 3000, 25);

            // Other factions
            var michaelFaction = SetupFaction(factionRepo, factionService, MichaelFactionId, "Michael's Crew", 8000, 40);
            var trevorFaction = SetupFaction(factionRepo, factionService, TrevorFactionId, "Trevor's Gang", 7000, 35);

            // Create zones - one weakly defended
            var franklinHQ = CreateZone(zoneRepo, "zone-1", "Grove Street", FranklinFactionId, 3);
            factionService.AddZoneToFaction(FranklinFactionId, franklinHQ.Id);

            var strongholdZone = CreateZone(zoneRepo, "zone-2", "Fort Zancudo", MichaelFactionId, 10);
            factionService.AddZoneToFaction(MichaelFactionId, strongholdZone.Id);

            var weakZone = CreateZone(zoneRepo, "zone-3", "Undefended Territory", TrevorFactionId, 2);
            weakZone.ControlPercentage = 30f; // Weak control
            zoneRepo.Update(weakZone);
            factionService.AddZoneToFaction(TrevorFactionId, weakZone.Id);

            var franklinState = factionService.GetFactionState(FranklinFactionId)!;
            var allZones = zoneRepo.GetAll().ToList();
            var ownedZones = allZones.Where(z => z.OwnerFactionId == FranklinFactionId).ToList();

            var context = new AIContext(
                franklinFaction,
                franklinState,
                ownedZones,
                allZones,
                new List<Faction> { michaelFaction, trevorFaction });

            // Act: Franklin AI evaluates both zones
            var franklinStrategy = new FranklinAIStrategy();

            float strongholdScore = franklinStrategy.EvaluateZone(strongholdZone, context);
            float weakZoneScore = franklinStrategy.EvaluateZone(weakZone, context);

            // Assert: Franklin evaluates both zones
            Assert.True(weakZoneScore >= 0, "Franklin should evaluate weakly held territories");
            Assert.True(strongholdScore >= 0, "Franklin should evaluate all zones");
        }

        #endregion

        #region Scenario: Multi-Faction Warfare

        [Fact]
        public void Scenario_ThreeWayWar_SimultaneousBattles()
        {
            // Arrange
            var (zoneRepo, zoneService, factionRepo, factionService, combatHandler, _) = CreateGameWorld();

            SetupFaction(factionRepo, factionService, MichaelFactionId, "Michael's Crew", 8000, 40);
            SetupFaction(factionRepo, factionService, TrevorFactionId, "Trevor's Gang", 7000, 35);
            SetupFaction(factionRepo, factionService, FranklinFactionId, "Franklin's Crew", 6000, 30);

            // Each faction controls one zone
            var zone1 = CreateZone(zoneRepo, "zone-1", "Vinewood", MichaelFactionId, 5);
            factionService.AddZoneToFaction(MichaelFactionId, zone1.Id);

            var zone2 = CreateZone(zoneRepo, "zone-2", "Sandy Shores", TrevorFactionId, 4);
            factionService.AddZoneToFaction(TrevorFactionId, zone2.Id);

            var zone3 = CreateZone(zoneRepo, "zone-3", "Grove Street", FranklinFactionId, 3);
            factionService.AddZoneToFaction(FranklinFactionId, zone3.Id);

            // Act: Two simultaneous battles
            // Battle 1: Michael attacks Trevor's zone
            var encounter1 = new CombatEncounter("combat-1", zone2.Id, MichaelFactionId, TrevorFactionId);
            encounter1.AttackerPedCount = 20;
            encounter1.DefenderPedCount = 0; // Michael wins

            // Battle 2: Franklin attacks Michael's zone
            var encounter2 = new CombatEncounter("combat-2", zone1.Id, FranklinFactionId, MichaelFactionId);
            encounter2.AttackerPedCount = 0; // Franklin loses
            encounter2.DefenderPedCount = 15;

            var controlCalc = new ControlPercentageCalculator();
            controlCalc.ApplyToEncounter(encounter1);
            controlCalc.ApplyToEncounter(encounter2);

            var takeoverDetector = new TakeoverDetector();
            var result1 = takeoverDetector.CheckTakeover(encounter1);
            var result2 = takeoverDetector.CheckTakeover(encounter2);

            encounter1.End(CombatStatus.AttackerVictory);
            encounter2.End(CombatStatus.DefenderVictory);

            combatHandler.ProcessCombatResult(encounter1);
            combatHandler.ProcessCombatResult(encounter2);

            // Assert: Territory changes correctly
            Assert.Equal(TakeoverStatus.AttackerVictory, result1.Status);
            Assert.Equal(TakeoverStatus.DefenderVictory, result2.Status);

            Assert.Equal(MichaelFactionId, zoneService.GetZone(zone2.Id)!.OwnerFactionId); // Michael captures Sandy Shores
            Assert.Equal(MichaelFactionId, zoneService.GetZone(zone1.Id)!.OwnerFactionId); // Michael defends Vinewood
            Assert.Equal(FranklinFactionId, zoneService.GetZone(zone3.Id)!.OwnerFactionId); // Franklin keeps Grove Street
        }

        [Fact]
        public void Scenario_DominationVictory_OneFactionControlsAllZones()
        {
            // Arrange
            var (zoneRepo, zoneService, factionRepo, factionService, combatHandler, _) = CreateGameWorld();

            SetupFaction(factionRepo, factionService, MichaelFactionId, "Michael's Crew", 20000, 100);
            SetupFaction(factionRepo, factionService, TrevorFactionId, "Trevor's Gang", 2000, 10);

            // Create 5 zones - Trevor controls 4, Michael controls 1
            var michaelZone = CreateZone(zoneRepo, "zone-0", "Michael's Base", MichaelFactionId, 5);
            factionService.AddZoneToFaction(MichaelFactionId, michaelZone.Id);

            var trevorZones = new List<Zone>();
            for (int i = 1; i <= 4; i++)
            {
                var zone = CreateZone(zoneRepo, $"zone-{i}", $"Territory {i}", TrevorFactionId, 3);
                factionService.AddZoneToFaction(TrevorFactionId, zone.Id);
                trevorZones.Add(zone);
            }

            Assert.Equal(1, factionService.GetZoneCount(MichaelFactionId));
            Assert.Equal(4, factionService.GetZoneCount(TrevorFactionId));

            // Act: Michael conquers all Trevor's zones one by one
            foreach (var zone in trevorZones)
            {
                var encounter = new CombatEncounter($"combat-{zone.Id}", zone.Id, MichaelFactionId, TrevorFactionId);
                encounter.AttackerPedCount = 25;
                encounter.DefenderPedCount = 0;

                var controlCalc = new ControlPercentageCalculator();
                controlCalc.ApplyToEncounter(encounter);

                encounter.End(CombatStatus.AttackerVictory);
                combatHandler.ProcessCombatResult(encounter);

                // Update faction tracking
                factionService.RemoveZoneFromFaction(TrevorFactionId, zone.Id);
                factionService.AddZoneToFaction(MichaelFactionId, zone.Id);
            }

            // Assert: Michael controls everything
            Assert.Equal(5, factionService.GetZoneCount(MichaelFactionId));
            Assert.Equal(0, factionService.GetZoneCount(TrevorFactionId));

            // All zones are now Michael's
            foreach (var zone in zoneRepo.GetAll())
            {
                Assert.Equal(MichaelFactionId, zone.OwnerFactionId);
            }
        }

        #endregion

        #region Scenario: Event Alerts During Combat

        [Fact]
        public void Scenario_EventAlertsFireDuringCombat()
        {
            // Arrange
            var notificationService = new MockNotificationService();
            var eventAlertService = new EventAlertService(notificationService);

            var (zoneRepo, zoneService, factionRepo, factionService, combatHandler, _) = CreateGameWorld();

            SetupFaction(factionRepo, factionService, MichaelFactionId, "Michael's Crew", 10000, 50);
            SetupFaction(factionRepo, factionService, TrevorFactionId, "Trevor's Gang", 5000, 30);

            var downtown = CreateZone(zoneRepo, "zone-downtown", "Downtown", TrevorFactionId, 5);
            factionService.AddZoneToFaction(TrevorFactionId, downtown.Id);

            // Act: Simulate events during attack
            eventAlertService.RaiseAttackLaunched("Downtown", "Michael's Crew", "Trevor's Gang");
            eventAlertService.RaiseAttackIncoming("Downtown", "Trevor's Gang", "Michael's Crew");

            // Combat resolves - Michael wins
            var encounter = new CombatEncounter("combat-1", downtown.Id, MichaelFactionId, TrevorFactionId);
            encounter.AttackerPedCount = 25;
            encounter.DefenderPedCount = 0;

            var controlCalc = new ControlPercentageCalculator();
            controlCalc.ApplyToEncounter(encounter);

            encounter.End(CombatStatus.AttackerVictory);
            combatHandler.ProcessCombatResult(encounter);

            eventAlertService.RaiseZoneCaptured("Downtown", "Michael's Crew");
            eventAlertService.RaiseZoneLost("Downtown", "Trevor's Gang", "Michael's Crew");

            // Assert: All events were recorded
            Assert.Equal(4, eventAlertService.AlertHistory.Count);

            var alerts = eventAlertService.AlertHistory.ToList();
            Assert.Equal(EventAlertType.AttackLaunched, alerts[0].Type);
            Assert.Equal(EventAlertType.AttackIncoming, alerts[1].Type);
            Assert.Equal(EventAlertType.ZoneCaptured, alerts[2].Type);
            Assert.Equal(EventAlertType.ZoneLost, alerts[3].Type);

            // Notifications were displayed
            Assert.Equal(4, notificationService.DisplayedNotifications.Count);
        }

        #endregion

        #region Scenario: Faction Relationships Impact Combat

        [Fact]
        public void Scenario_FactionRelationships_AffectWarfare()
        {
            // Arrange
            var (zoneRepo, zoneService, factionRepo, factionService, _, _) = CreateGameWorld();
            var relationshipRepo = new InMemoryFactionRelationshipRepository();
            var relationshipService = new FactionRelationshipService(factionRepo, relationshipRepo);

            SetupFaction(factionRepo, factionService, MichaelFactionId, "Michael's Crew", 8000, 40);
            SetupFaction(factionRepo, factionService, TrevorFactionId, "Trevor's Gang", 7000, 35);
            SetupFaction(factionRepo, factionService, FranklinFactionId, "Franklin's Crew", 6000, 30);

            // Set up relationships: Michael and Franklin are allied, both at war with Trevor
            relationshipService.DeclareWar(MichaelFactionId, TrevorFactionId);
            relationshipService.DeclareWar(FranklinFactionId, TrevorFactionId);
            relationshipService.FormAlliance(MichaelFactionId, FranklinFactionId);

            // Verify relationships
            Assert.True(relationshipService.AreAtWar(MichaelFactionId, TrevorFactionId));
            Assert.True(relationshipService.AreAtWar(FranklinFactionId, TrevorFactionId));
            Assert.True(relationshipService.AreAllied(MichaelFactionId, FranklinFactionId));
        }

        #endregion

        #region Scenario: Supply Lines and Resource Flow

        [Fact]
        public void Scenario_SupplyLines_WithHeadquarters_AffectsResourceGeneration()
        {
            // Arrange
            var (zoneRepo, zoneService, factionRepo, factionService, _, tickService) = CreateGameWorld();

            SetupFaction(factionRepo, factionService, MichaelFactionId, "Michael's Crew", 0, 10);

            // Create adjacent zones (overlapping radii for adjacency)
            // Zone center positions that are within sum of their radii
            var hq = new Zone("zone-hq", "HQ", new Vector3(0, 0, 0), 200f, 3);
            hq.OwnerFactionId = MichaelFactionId;
            hq.ControlPercentage = 100f;
            zoneRepo.Add(hq);

            var adjacent = new Zone("zone-adjacent", "Adjacent Territory", new Vector3(300, 0, 0), 200f, 5);
            adjacent.OwnerFactionId = MichaelFactionId;
            adjacent.ControlPercentage = 100f;
            adjacent.Traits = ZoneTrait.Commercial;
            zoneRepo.Add(adjacent);

            factionService.AddZoneToFaction(MichaelFactionId, hq.Id);
            factionService.AddZoneToFaction(MichaelFactionId, adjacent.Id);

            var supplyLineService = new SupplyLineService(zoneService);

            // Set HQ for faction
            supplyLineService.SetHeadquarters(MichaelFactionId, hq.Id);

            // Act: Check if adjacent zone is connected to HQ
            bool hasSupplyLine = supplyLineService.HasSupplyLine(MichaelFactionId, adjacent.Id);

            // Assert: Supply line should exist (zones are adjacent)
            Assert.True(hasSupplyLine, "Adjacent zone should have supply line to HQ");

            // Generate resources
            tickService.Start();
            tickService.ForceTick();

            // Zone generates resources
            var state = factionService.GetFactionState(MichaelFactionId);
            Assert.NotNull(state);
            Assert.True(state.Cash > 0, "Should receive income from connected zones");
        }

        [Fact]
        public void Scenario_DisconnectedZone_HasReducedEfficiency()
        {
            // Arrange
            var (zoneRepo, zoneService, factionRepo, factionService, _, _) = CreateGameWorld();

            SetupFaction(factionRepo, factionService, MichaelFactionId, "Michael's Crew", 0, 10);

            // Create HQ
            var hq = new Zone("zone-hq", "HQ", new Vector3(0, 0, 0), 100f, 3);
            hq.OwnerFactionId = MichaelFactionId;
            hq.ControlPercentage = 100f;
            zoneRepo.Add(hq);

            // Create remote zone (not adjacent - far away)
            var remote = new Zone("zone-remote", "Remote Outpost", new Vector3(5000, 5000, 0), 100f, 5);
            remote.OwnerFactionId = MichaelFactionId;
            remote.ControlPercentage = 100f;
            remote.Traits = ZoneTrait.Commercial;
            zoneRepo.Add(remote);

            factionService.AddZoneToFaction(MichaelFactionId, hq.Id);
            factionService.AddZoneToFaction(MichaelFactionId, remote.Id);

            var supplyLineService = new SupplyLineService(zoneService);
            supplyLineService.SetHeadquarters(MichaelFactionId, hq.Id);

            // Act: Check efficiency of disconnected zone
            float efficiency = supplyLineService.GetSupplyLineEfficiency(MichaelFactionId, remote.Id);

            // Assert: Efficiency should be reduced for disconnected zone
            Assert.True(efficiency < 1.0f, "Disconnected zone should have reduced efficiency");
        }

        #endregion

        #region Scenario: Complete Game Turn Cycle

        [Fact]
        public void Scenario_CompleteGameTurn_AllSystemsIntegrate()
        {
            // Arrange: Full game world with all systems
            var (zoneRepo, zoneService, factionRepo, factionService, combatHandler, tickService) = CreateGameWorld();
            var notificationService = new MockNotificationService();
            var eventAlertService = new EventAlertService(notificationService);

            // Create all three factions
            var michael = SetupFaction(factionRepo, factionService, MichaelFactionId, "Michael's Crew", 5000, 30);
            var trevor = SetupFaction(factionRepo, factionService, TrevorFactionId, "Trevor's Gang", 4000, 25);
            var franklin = SetupFaction(factionRepo, factionService, FranklinFactionId, "Franklin's Crew", 3000, 20);

            // Create initial territory distribution
            // Michael owns high-value zones (for defense) and there are contested/high-value targets
            var michaelZone = CreateZone(zoneRepo, "zone-1", "Vinewood", MichaelFactionId, 8);
            michaelZone.Traits = ZoneTrait.Commercial;
            michaelZone.IsContested = true; // Contested, needs defense
            zoneRepo.Update(michaelZone);
            factionService.AddZoneToFaction(MichaelFactionId, michaelZone.Id);

            // High-value target (strategic value >= 7 for Michael to attack)
            var trevorZone = CreateZone(zoneRepo, "zone-2", "Sandy Shores", TrevorFactionId, 8);
            trevorZone.Traits = ZoneTrait.Industrial;
            zoneRepo.Update(trevorZone);
            factionService.AddZoneToFaction(TrevorFactionId, trevorZone.Id);

            var franklinZone = CreateZone(zoneRepo, "zone-3", "Grove Street", FranklinFactionId, 6);
            franklinZone.Traits = ZoneTrait.Residential;
            zoneRepo.Update(franklinZone);
            factionService.AddZoneToFaction(FranklinFactionId, franklinZone.Id);

            // TURN 1: Economy phase
            tickService.Start();
            tickService.ForceTick();

            // All factions should have resources
            Assert.True(factionService.GetFactionState(MichaelFactionId)!.Cash > 0);
            Assert.True(factionService.GetFactionState(TrevorFactionId)!.Weapons > 0);
            Assert.True(factionService.GetFactionState(FranklinFactionId)!.RecruitmentPoints > 0);

            // TURN 2: AI decision phase (simulated)
            var michaelStrategy = new MichaelAIStrategy();
            var michaelState = factionService.GetFactionState(MichaelFactionId)!;
            var allZones = zoneRepo.GetAll().ToList();
            var michaelOwnedZones = allZones.Where(z => z.OwnerFactionId == MichaelFactionId).ToList();

            var context = new AIContext(
                michael,
                michaelState,
                michaelOwnedZones,
                allZones,
                new List<Faction> { trevor, franklin });

            var decisions = michaelStrategy.MakeDecisions(context);
            // AI should generate at least defense decisions for the contested zone
            Assert.NotEmpty(decisions);

            // TURN 3: Combat phase (Michael attacks Trevor)
            if (decisions.Any(d => d.DecisionType == AIDecisionType.Attack))
            {
                var attackDecision = decisions.First(d => d.DecisionType == AIDecisionType.Attack);
                var targetZone = zoneRepo.GetById(attackDecision.TargetZoneId!);

                if (targetZone != null && targetZone.OwnerFactionId == TrevorFactionId)
                {
                    eventAlertService.RaiseAttackLaunched(targetZone.Name, "Michael's Crew", "Trevor's Gang");

                    var encounter = new CombatEncounter("combat-turn3", targetZone.Id, MichaelFactionId, TrevorFactionId);
                    encounter.AttackerPedCount = attackDecision.TroopsToCommit;
                    encounter.DefenderPedCount = 10;

                    var controlCalc = new ControlPercentageCalculator();
                    controlCalc.ApplyToEncounter(encounter);

                    // Combat resolves based on troop counts
                    var takeoverDetector = new TakeoverDetector();
                    var result = takeoverDetector.CheckTakeover(encounter);

                    if (result.Status == TakeoverStatus.AttackerVictory)
                    {
                        encounter.End(CombatStatus.AttackerVictory);
                        combatHandler.ProcessCombatResult(encounter);
                        eventAlertService.RaiseZoneCaptured(targetZone.Name, "Michael's Crew");
                    }
                }
            }

            // TURN 4: Another economy tick
            tickService.ForceTick();

            // Verify game state is consistent
            int totalZones = 3;
            int michaelZones = factionService.GetZoneCount(MichaelFactionId);
            int trevorZones = factionService.GetZoneCount(TrevorFactionId);
            int franklinZones = factionService.GetZoneCount(FranklinFactionId);

            // All zones should be accounted for
            Assert.True(michaelZones + trevorZones + franklinZones <= totalZones,
                "Zone counts should not exceed total zones");
        }

        #endregion

        #region Scenario: Full Combat Flow (Enter Zone → Fight → Capture)

        /// <summary>
        /// End-to-end test for the complete combat flow:
        /// 1. Player walks into enemy territory (TerritoryManager detects zone entry)
        /// 2. Combat begins (CombatManager starts encounter)
        /// 3. Defenders spawn in waves (Heavy → Medium → Basic)
        /// 4. Fighting occurs (control percentage updates)
        /// 5. All defenders eliminated → Zone captured
        /// </summary>
        [Fact]
        public void EndToEnd_FullCombatFlow_EnterZoneFightCapture()
        {
            // Arrange: Set up complete game world with all systems wired together
            var gameBridge = new MockGameBridge();
            var zoneRepo = new InMemoryZoneRepository();
            var zoneService = new ZoneService(zoneRepo);
            var factionRepo = new InMemoryFactionRepository();
            var factionService = new FactionService(factionRepo);

            // Set up factions
            SetupFaction(factionRepo, factionService, MichaelFactionId, "Michael's Crew", 10000, 50);
            SetupFaction(factionRepo, factionService, TrevorFactionId, "Trevor's Gang", 5000, 30);

            // Create an enemy zone at a specific location
            var enemyZone = new Zone("zone-downtown", "Downtown", new Vector3(100, 100, 0), 200f, 5);
            enemyZone.OwnerFactionId = TrevorFactionId;
            enemyZone.ControlPercentage = 100f;
            zoneRepo.Add(enemyZone);
            factionService.AddZoneToFaction(TrevorFactionId, enemyZone.Id);

            // Set up TerritoryManager
            var territoryManager = new TerritoryManager(gameBridge, zoneService);

            // Set up CombatManager with all dependencies
            var pedPool = new InMemoryPedPool(30);
            var pedSpawningService = new PedSpawningService(gameBridge, pedPool);
            var spawnPositionCalculator = new SpawnPositionCalculator(gameBridge);
            var controlCalculator = new ControlPercentageCalculator();
            var takeoverDetector = new TakeoverDetector();
            var combatResultHandler = new CombatResultHandler(zoneService);
            var waveSpawnerService = new WaveSpawnerService();
            var followerService = new FollowerService();

            var pedDespawnServiceMock = new Mock<IPedDespawnService>();
            pedDespawnServiceMock.Setup(p => p.DespawnPedsByZone(It.IsAny<string>())).Returns(new System.Collections.Generic.List<FactionWars.Combat.Models.PedHandle>());
            pedDespawnServiceMock.Setup(p => p.DespawnDeadPeds()).Returns(new System.Collections.Generic.List<FactionWars.Combat.Models.PedHandle>());
            var aggressionResponseServiceMock = new Mock<IAggressionResponseService>();
            var combatManager = new CombatManager(
                gameBridge,
                pedPool,
                pedSpawningService,
                pedDespawnServiceMock.Object,
                spawnPositionCalculator,
                controlCalculator,
                takeoverDetector,
                combatResultHandler,
                waveSpawnerService,
                followerService,
                aggressionResponseServiceMock.Object);

            // Track events
            bool zoneEnteredRaised = false;
            Zone? enteredZone = null;
            territoryManager.ZoneEntered += (sender, zone) =>
            {
                zoneEnteredRaised = true;
                enteredZone = zone;
            };

            bool combatStartedRaised = false;
            CombatEncounter? startedEncounter = null;
            combatManager.CombatStarted += (sender, encounter) =>
            {
                combatStartedRaised = true;
                startedEncounter = encounter;
            };

            bool combatEndedRaised = false;
            CombatEncounter? endedEncounter = null;
            combatManager.CombatEnded += (sender, encounter) =>
            {
                combatEndedRaised = true;
                endedEncounter = encounter;
            };

            // STEP 1: Player starts outside the zone
            gameBridge.PlayerPosition = new Vector3(500, 500, 0); // Far from zone
            territoryManager.Update();
            Assert.Null(territoryManager.CurrentZone);
            Assert.False(territoryManager.IsInEnemyTerritory(MichaelFactionId));

            // STEP 2: Player enters enemy zone
            gameBridge.PlayerPosition = new Vector3(100, 100, 0); // Inside zone
            territoryManager.Update();

            Assert.True(zoneEnteredRaised, "ZoneEntered event should be raised");
            Assert.NotNull(enteredZone);
            Assert.Equal(enemyZone.Id, enteredZone!.Id);
            Assert.True(territoryManager.IsInEnemyTerritory(MichaelFactionId), "Should detect enemy territory");

            // STEP 3: Start combat when entering enemy zone
            var encounter = combatManager.StartCombat(enemyZone, MichaelFactionId);

            Assert.True(combatStartedRaised, "CombatStarted event should be raised");
            Assert.True(combatManager.IsInCombat, "Should be in combat");
            Assert.Equal(enemyZone.Id, encounter.ZoneId);
            Assert.Equal(MichaelFactionId, encounter.AttackingFactionId);
            Assert.Equal(TrevorFactionId, encounter.DefendingFactionId);

            // STEP 4: Initialize wave-based spawning (2 Heavy, 2 Medium, 3 Basic = 7 total)
            var spawnPlan = new DefenderSpawnPlan(basicPeds: 3, mediumPeds: 2, heavyPeds: 2);
            combatManager.InitializeWaveSpawning(spawnPlan);
            Assert.Equal(7, combatManager.GetRemainingDefendersToSpawn());

            // STEP 5: Spawn defenders in waves (Heavy → Medium → Basic)
            var modelsByTier = new Dictionary<DefenderTier, string>
            {
                { DefenderTier.Heavy, "s_m_y_blackops_01" },
                { DefenderTier.Medium, "s_m_y_armymech_01" },
                { DefenderTier.Basic, "s_m_y_dealer_01" }
            };

            // First wave: Heavy
            Assert.Equal(DefenderTier.Heavy, combatManager.GetNextWaveTier());
            var heavyWave = combatManager.SpawnNextWave(modelsByTier, TrevorFactionId, 10);
            Assert.Equal(2, heavyWave.Count);

            // Second wave: Medium
            Assert.Equal(DefenderTier.Medium, combatManager.GetNextWaveTier());
            var mediumWave = combatManager.SpawnNextWave(modelsByTier, TrevorFactionId, 10);
            Assert.Equal(2, mediumWave.Count);

            // Third wave: Basic
            Assert.Equal(DefenderTier.Basic, combatManager.GetNextWaveTier());
            var basicWave = combatManager.SpawnNextWave(modelsByTier, TrevorFactionId, 10);
            Assert.Equal(3, basicWave.Count);

            Assert.True(combatManager.IsWaveSpawningComplete(), "All waves should be spawned");
            Assert.Null(combatManager.GetNextWaveTier());

            // STEP 6: Spawn attackers (player's allies)
            for (int i = 0; i < 5; i++)
            {
                pedSpawningService.SpawnPed("s_m_y_dealer_01", new Vector3(100, 100, 0), MichaelFactionId, enemyZone.Id);
            }

            // STEP 7: Combat update - both sides have peds, combat continues
            combatManager.Update();
            Assert.True(combatManager.IsInCombat, "Combat should continue with both sides present");
            Assert.Equal(5, encounter.AttackerPedCount);
            Assert.Equal(7, encounter.DefenderPedCount);

            // STEP 8: Simulate fighting - defenders get eliminated one by one
            var defenders = pedPool.GetByFaction(TrevorFactionId).ToList();
            foreach (var defender in defenders)
            {
                pedPool.Remove(defender);
            }

            // STEP 9: Final combat update - attackers win, zone captured
            combatManager.Update();

            // Assert: Combat ended with attacker victory
            Assert.True(combatEndedRaised, "CombatEnded event should be raised");
            Assert.False(combatManager.IsInCombat, "Combat should have ended");
            Assert.NotNull(endedEncounter);
            Assert.Equal(CombatStatus.AttackerVictory, endedEncounter!.Status);

            // Assert: Zone ownership transferred to attacker
            var capturedZone = zoneService.GetZone(enemyZone.Id);
            Assert.NotNull(capturedZone);
            Assert.Equal(MichaelFactionId, capturedZone!.OwnerFactionId);
            Assert.Equal(100f, capturedZone.ControlPercentage);
            Assert.False(capturedZone.IsContested);
        }

        /// <summary>
        /// End-to-end test verifying that control percentage updates correctly during combat.
        /// </summary>
        [Fact]
        public void EndToEnd_CombatFlow_ControlPercentageUpdatesCorrectly()
        {
            // Arrange
            var gameBridge = new MockGameBridge { PlayerPosition = new Vector3(100, 100, 0) };
            var zoneRepo = new InMemoryZoneRepository();
            var zoneService = new ZoneService(zoneRepo);

            var enemyZone = new Zone("zone-1", "Test Zone", new Vector3(100, 100, 0), 200f, 5);
            enemyZone.OwnerFactionId = TrevorFactionId;
            enemyZone.ControlPercentage = 100f;
            zoneRepo.Add(enemyZone);

            var pedPool = new InMemoryPedPool(30);
            var pedSpawningService = new PedSpawningService(gameBridge, pedPool);
            var spawnPositionCalculator = new SpawnPositionCalculator(gameBridge);
            var controlCalculator = new ControlPercentageCalculator();
            var takeoverDetector = new TakeoverDetector();
            var combatResultHandler = new CombatResultHandler(zoneService);
            var waveSpawnerService = new WaveSpawnerService();
            var followerService = new FollowerService();

            var pedDespawnServiceMock = new Mock<IPedDespawnService>();
            pedDespawnServiceMock.Setup(p => p.DespawnPedsByZone(It.IsAny<string>())).Returns(new System.Collections.Generic.List<FactionWars.Combat.Models.PedHandle>());
            pedDespawnServiceMock.Setup(p => p.DespawnDeadPeds()).Returns(new System.Collections.Generic.List<FactionWars.Combat.Models.PedHandle>());
            var aggressionResponseServiceMock = new Mock<IAggressionResponseService>();
            var combatManager = new CombatManager(
                gameBridge, pedPool, pedSpawningService, pedDespawnServiceMock.Object, spawnPositionCalculator,
                controlCalculator, takeoverDetector, combatResultHandler, waveSpawnerService,
                followerService, aggressionResponseServiceMock.Object);

            // Start combat
            var encounter = combatManager.StartCombat(enemyZone, MichaelFactionId);

            // Spawn equal forces on both sides (5 vs 5)
            for (int i = 0; i < 5; i++)
            {
                pedSpawningService.SpawnPed("model", new Vector3(100, 100, 0), MichaelFactionId, enemyZone.Id);
                pedSpawningService.SpawnPed("model", new Vector3(100, 100, 0), TrevorFactionId, enemyZone.Id);
            }

            // Act: Update combat
            combatManager.Update();

            // Assert: Control should be 50/50
            Assert.True(combatManager.IsInCombat);
            Assert.Equal(5, encounter.AttackerPedCount);
            Assert.Equal(5, encounter.DefenderPedCount);
            Assert.Equal(50f, encounter.AttackerControlPercentage);
            Assert.Equal(50f, encounter.DefenderControlPercentage);

            // Remove 3 defenders (5 attackers vs 2 defenders)
            var defendersToRemove = pedPool.GetByFaction(TrevorFactionId).Take(3).ToList();
            foreach (var ped in defendersToRemove)
            {
                pedPool.Remove(ped);
            }

            // Update combat again
            combatManager.Update();

            // Assert: Attackers now have advantage
            Assert.True(combatManager.IsInCombat);
            Assert.Equal(5, encounter.AttackerPedCount);
            Assert.Equal(2, encounter.DefenderPedCount);
            // Control percentages should favor attacker (approximately 71% vs 29%)
            Assert.True(encounter.AttackerControlPercentage > 50f);
            Assert.True(encounter.DefenderControlPercentage < 50f);
        }

        /// <summary>
        /// End-to-end test verifying that player's GTA V cash is used for troop purchases
        /// and income is added to player money.
        /// </summary>
        [Fact]
        public void EndToEnd_CombatFlow_ZoneCaptureUpdatesOwnership()
        {
            // Arrange: Set up a minimal combat scenario
            var gameBridge = new MockGameBridge { PlayerPosition = new Vector3(100, 100, 0) };
            var zoneRepo = new InMemoryZoneRepository();
            var zoneService = new ZoneService(zoneRepo);
            var factionRepo = new InMemoryFactionRepository();
            var factionService = new FactionService(factionRepo);

            // Set up factions
            SetupFaction(factionRepo, factionService, MichaelFactionId, "Michael's Crew", 10000, 50);
            SetupFaction(factionRepo, factionService, TrevorFactionId, "Trevor's Gang", 5000, 30);

            // Trevor owns 2 zones initially
            var zone1 = new Zone("zone-1", "Zone 1", new Vector3(100, 100, 0), 200f, 5);
            zone1.OwnerFactionId = TrevorFactionId;
            zone1.ControlPercentage = 100f;
            zoneRepo.Add(zone1);
            factionService.AddZoneToFaction(TrevorFactionId, zone1.Id);

            var zone2 = new Zone("zone-2", "Zone 2", new Vector3(500, 500, 0), 200f, 5);
            zone2.OwnerFactionId = TrevorFactionId;
            zone2.ControlPercentage = 100f;
            zoneRepo.Add(zone2);
            factionService.AddZoneToFaction(TrevorFactionId, zone2.Id);

            // Verify initial zone counts
            Assert.Equal(0, factionService.GetZoneCount(MichaelFactionId));
            Assert.Equal(2, factionService.GetZoneCount(TrevorFactionId));

            // Set up combat manager
            var pedPool = new InMemoryPedPool(30);
            var pedSpawningService = new PedSpawningService(gameBridge, pedPool);
            var spawnPositionCalculator = new SpawnPositionCalculator(gameBridge);
            var controlCalculator = new ControlPercentageCalculator();
            var takeoverDetector = new TakeoverDetector();
            var combatResultHandler = new CombatResultHandler(zoneService);
            var waveSpawnerService = new WaveSpawnerService();
            var followerService = new FollowerService();

            var pedDespawnServiceMock = new Mock<IPedDespawnService>();
            pedDespawnServiceMock.Setup(p => p.DespawnPedsByZone(It.IsAny<string>())).Returns(new System.Collections.Generic.List<FactionWars.Combat.Models.PedHandle>());
            pedDespawnServiceMock.Setup(p => p.DespawnDeadPeds()).Returns(new System.Collections.Generic.List<FactionWars.Combat.Models.PedHandle>());
            var aggressionResponseServiceMock = new Mock<IAggressionResponseService>();
            var combatManager = new CombatManager(
                gameBridge, pedPool, pedSpawningService, pedDespawnServiceMock.Object, spawnPositionCalculator,
                controlCalculator, takeoverDetector, combatResultHandler, waveSpawnerService,
                followerService, aggressionResponseServiceMock.Object);

            // Act: Capture zone 1
            combatManager.StartCombat(zone1, MichaelFactionId);

            // Spawn only attackers (instant win)
            for (int i = 0; i < 5; i++)
            {
                pedSpawningService.SpawnPed("model", new Vector3(100, 100, 0), MichaelFactionId, zone1.Id);
            }

            combatManager.Update();

            // Assert: Zone 1 captured, zone 2 unchanged
            Assert.False(combatManager.IsInCombat);
            Assert.Equal(MichaelFactionId, zoneService.GetZone(zone1.Id)!.OwnerFactionId);
            Assert.Equal(TrevorFactionId, zoneService.GetZone(zone2.Id)!.OwnerFactionId);
        }

        #endregion

        #region Scenario: Player Death in Combat (Retreat)

        /// <summary>
        /// End-to-end test for player death during combat:
        /// 1. Player enters enemy zone and combat starts
        /// 2. Combat is in progress with both attackers and defenders
        /// 3. Player dies during combat
        /// 4. Combat ends as "retreat" (PlayerRetreat status)
        /// 5. Zone ownership remains unchanged (defender keeps zone)
        /// </summary>
        [Fact]
        public void EndToEnd_PlayerDeathInCombat_RetreatZoneUnchanged()
        {
            // Arrange: Set up complete game world with all systems wired together
            var gameBridge = new MockGameBridge();
            var zoneRepo = new InMemoryZoneRepository();
            var zoneService = new ZoneService(zoneRepo);
            var factionRepo = new InMemoryFactionRepository();
            var factionService = new FactionService(factionRepo);

            // Set up factions
            SetupFaction(factionRepo, factionService, MichaelFactionId, "Michael's Crew", 10000, 50);
            SetupFaction(factionRepo, factionService, TrevorFactionId, "Trevor's Gang", 5000, 30);

            // Create an enemy zone - Trevor owns it
            var enemyZone = new Zone("zone-downtown", "Downtown", new Vector3(100, 100, 0), 200f, 5);
            enemyZone.OwnerFactionId = TrevorFactionId;
            enemyZone.ControlPercentage = 100f;
            zoneRepo.Add(enemyZone);
            factionService.AddZoneToFaction(TrevorFactionId, enemyZone.Id);

            // Set up TerritoryManager
            var territoryManager = new TerritoryManager(gameBridge, zoneService);

            // Set up CombatManager with all dependencies
            var pedPool = new InMemoryPedPool(30);
            var pedSpawningService = new PedSpawningService(gameBridge, pedPool);
            var spawnPositionCalculator = new SpawnPositionCalculator(gameBridge);
            var controlCalculator = new ControlPercentageCalculator();
            var takeoverDetector = new TakeoverDetector();
            var combatResultHandler = new CombatResultHandler(zoneService);
            var waveSpawnerService = new WaveSpawnerService();
            var followerService = new FollowerService();

            var pedDespawnServiceMock = new Mock<IPedDespawnService>();
            pedDespawnServiceMock.Setup(p => p.DespawnPedsByZone(It.IsAny<string>())).Returns(new System.Collections.Generic.List<FactionWars.Combat.Models.PedHandle>());
            pedDespawnServiceMock.Setup(p => p.DespawnDeadPeds()).Returns(new System.Collections.Generic.List<FactionWars.Combat.Models.PedHandle>());
            var aggressionResponseServiceMock = new Mock<IAggressionResponseService>();
            var combatManager = new CombatManager(
                gameBridge,
                pedPool,
                pedSpawningService,
                pedDespawnServiceMock.Object,
                spawnPositionCalculator,
                controlCalculator,
                takeoverDetector,
                combatResultHandler,
                waveSpawnerService,
                followerService,
                aggressionResponseServiceMock.Object);

            // Track combat events
            bool combatEndedRaised = false;
            CombatEncounter? endedEncounter = null;
            combatManager.CombatEnded += (sender, encounter) =>
            {
                combatEndedRaised = true;
                endedEncounter = encounter;
            };

            // Verify initial state: Trevor owns the zone
            Assert.Equal(TrevorFactionId, zoneService.GetZone(enemyZone.Id)!.OwnerFactionId);
            Assert.Equal(100f, zoneService.GetZone(enemyZone.Id)!.ControlPercentage);

            // STEP 1: Player enters enemy zone
            gameBridge.PlayerPosition = new Vector3(100, 100, 0); // Inside zone
            territoryManager.Update();

            Assert.True(territoryManager.IsInEnemyTerritory(MichaelFactionId), "Should detect enemy territory");

            // STEP 2: Start combat when entering enemy zone
            var encounter = combatManager.StartCombat(enemyZone, MichaelFactionId);

            Assert.True(combatManager.IsInCombat, "Should be in combat");
            Assert.Equal(enemyZone.Id, encounter.ZoneId);
            Assert.Equal(MichaelFactionId, encounter.AttackingFactionId);
            Assert.Equal(TrevorFactionId, encounter.DefendingFactionId);

            // STEP 3: Spawn peds on both sides (combat in progress)
            // Spawn attackers (player's allies)
            for (int i = 0; i < 5; i++)
            {
                pedSpawningService.SpawnPed("s_m_y_dealer_01", new Vector3(100, 100, 0), MichaelFactionId, enemyZone.Id);
            }

            // Spawn defenders (Trevor's peds)
            for (int i = 0; i < 5; i++)
            {
                pedSpawningService.SpawnPed("s_m_y_blackops_01", new Vector3(100, 100, 0), TrevorFactionId, enemyZone.Id);
            }

            // STEP 4: Combat update - both sides have peds, combat continues
            combatManager.Update();
            Assert.True(combatManager.IsInCombat, "Combat should continue with both sides present");
            Assert.Equal(5, encounter.AttackerPedCount);
            Assert.Equal(5, encounter.DefenderPedCount);
            Assert.Equal(50f, encounter.AttackerControlPercentage);
            Assert.Equal(50f, encounter.DefenderControlPercentage);

            // STEP 5: Player dies during combat
            gameBridge.IsPlayerDeadValue = true;

            // STEP 6: Combat update detects player death and triggers retreat
            combatManager.Update();

            // Assert: Combat ended with PlayerRetreat status
            Assert.True(combatEndedRaised, "CombatEnded event should be raised");
            Assert.False(combatManager.IsInCombat, "Combat should have ended");
            Assert.NotNull(endedEncounter);
            Assert.Equal(CombatStatus.PlayerRetreat, endedEncounter!.Status);

            // Assert: Zone ownership remains UNCHANGED - Trevor still owns it
            var zoneAfterCombat = zoneService.GetZone(enemyZone.Id);
            Assert.NotNull(zoneAfterCombat);
            Assert.Equal(TrevorFactionId, zoneAfterCombat!.OwnerFactionId);
            Assert.Equal(100f, zoneAfterCombat.ControlPercentage);
            Assert.False(zoneAfterCombat.IsContested);
        }

        /// <summary>
        /// Verifies that player death in combat does NOT transfer zone ownership,
        /// even when the attacker was winning (had more control percentage).
        /// </summary>
        [Fact]
        public void EndToEnd_PlayerDeathWhileWinning_StillRetreatsZoneUnchanged()
        {
            // Arrange
            var gameBridge = new MockGameBridge { PlayerPosition = new Vector3(100, 100, 0) };
            var zoneRepo = new InMemoryZoneRepository();
            var zoneService = new ZoneService(zoneRepo);
            var factionRepo = new InMemoryFactionRepository();
            var factionService = new FactionService(factionRepo);

            SetupFaction(factionRepo, factionService, MichaelFactionId, "Michael's Crew", 10000, 50);
            SetupFaction(factionRepo, factionService, TrevorFactionId, "Trevor's Gang", 5000, 30);

            // Trevor owns the zone
            var enemyZone = new Zone("zone-1", "Test Zone", new Vector3(100, 100, 0), 200f, 5);
            enemyZone.OwnerFactionId = TrevorFactionId;
            enemyZone.ControlPercentage = 100f;
            zoneRepo.Add(enemyZone);
            factionService.AddZoneToFaction(TrevorFactionId, enemyZone.Id);

            var pedPool = new InMemoryPedPool(30);
            var pedSpawningService = new PedSpawningService(gameBridge, pedPool);
            var spawnPositionCalculator = new SpawnPositionCalculator(gameBridge);
            var controlCalculator = new ControlPercentageCalculator();
            var takeoverDetector = new TakeoverDetector();
            var combatResultHandler = new CombatResultHandler(zoneService);
            var waveSpawnerService = new WaveSpawnerService();
            var followerService = new FollowerService();

            var pedDespawnServiceMock = new Mock<IPedDespawnService>();
            pedDespawnServiceMock.Setup(p => p.DespawnPedsByZone(It.IsAny<string>())).Returns(new System.Collections.Generic.List<FactionWars.Combat.Models.PedHandle>());
            pedDespawnServiceMock.Setup(p => p.DespawnDeadPeds()).Returns(new System.Collections.Generic.List<FactionWars.Combat.Models.PedHandle>());
            var aggressionResponseServiceMock = new Mock<IAggressionResponseService>();
            var combatManager = new CombatManager(
                gameBridge, pedPool, pedSpawningService, pedDespawnServiceMock.Object, spawnPositionCalculator,
                controlCalculator, takeoverDetector, combatResultHandler, waveSpawnerService,
                followerService, aggressionResponseServiceMock.Object);

            CombatEncounter? endedEncounter = null;
            combatManager.CombatEnded += (sender, encounter) => endedEncounter = encounter;

            // Start combat
            var encounter = combatManager.StartCombat(enemyZone, MichaelFactionId);

            // Spawn more attackers than defenders (player is winning: 8 vs 2)
            for (int i = 0; i < 8; i++)
            {
                pedSpawningService.SpawnPed("model", new Vector3(100, 100, 0), MichaelFactionId, enemyZone.Id);
            }
            for (int i = 0; i < 2; i++)
            {
                pedSpawningService.SpawnPed("model", new Vector3(100, 100, 0), TrevorFactionId, enemyZone.Id);
            }

            // Update combat - player is winning
            combatManager.Update();
            Assert.True(combatManager.IsInCombat);
            Assert.True(encounter.AttackerControlPercentage > 50f, "Attacker should be winning");

            // Player dies while winning
            gameBridge.IsPlayerDeadValue = true;

            // Act: Combat update detects player death
            combatManager.Update();

            // Assert: Even though player was winning, death = retreat, zone unchanged
            Assert.False(combatManager.IsInCombat);
            Assert.Equal(CombatStatus.PlayerRetreat, endedEncounter!.Status);
            Assert.Equal(TrevorFactionId, zoneService.GetZone(enemyZone.Id)!.OwnerFactionId);
        }

        /// <summary>
        /// Verifies the Retreat() method can be called directly and properly ends combat.
        /// </summary>
        [Fact]
        public void EndToEnd_ManualRetreat_EndsCombatZoneUnchanged()
        {
            // Arrange
            var gameBridge = new MockGameBridge { PlayerPosition = new Vector3(100, 100, 0) };
            var zoneRepo = new InMemoryZoneRepository();
            var zoneService = new ZoneService(zoneRepo);

            var enemyZone = new Zone("zone-1", "Test Zone", new Vector3(100, 100, 0), 200f, 5);
            enemyZone.OwnerFactionId = TrevorFactionId;
            enemyZone.ControlPercentage = 100f;
            zoneRepo.Add(enemyZone);

            var pedPool = new InMemoryPedPool(30);
            var pedSpawningService = new PedSpawningService(gameBridge, pedPool);
            var spawnPositionCalculator = new SpawnPositionCalculator(gameBridge);
            var controlCalculator = new ControlPercentageCalculator();
            var takeoverDetector = new TakeoverDetector();
            var combatResultHandler = new CombatResultHandler(zoneService);
            var waveSpawnerService = new WaveSpawnerService();
            var followerService = new FollowerService();

            var pedDespawnServiceMock = new Mock<IPedDespawnService>();
            pedDespawnServiceMock.Setup(p => p.DespawnPedsByZone(It.IsAny<string>())).Returns(new System.Collections.Generic.List<FactionWars.Combat.Models.PedHandle>());
            pedDespawnServiceMock.Setup(p => p.DespawnDeadPeds()).Returns(new System.Collections.Generic.List<FactionWars.Combat.Models.PedHandle>());
            var aggressionResponseServiceMock = new Mock<IAggressionResponseService>();
            var combatManager = new CombatManager(
                gameBridge, pedPool, pedSpawningService, pedDespawnServiceMock.Object, spawnPositionCalculator,
                controlCalculator, takeoverDetector, combatResultHandler, waveSpawnerService,
                followerService, aggressionResponseServiceMock.Object);

            CombatEncounter? endedEncounter = null;
            combatManager.CombatEnded += (sender, encounter) => endedEncounter = encounter;

            // Start combat
            combatManager.StartCombat(enemyZone, MichaelFactionId);
            Assert.True(combatManager.IsInCombat);

            // Act: Call Retreat() directly
            combatManager.Retreat();

            // Assert: Combat ended with PlayerRetreat, zone unchanged
            Assert.False(combatManager.IsInCombat);
            Assert.NotNull(endedEncounter);
            Assert.Equal(CombatStatus.PlayerRetreat, endedEncounter!.Status);
            Assert.Equal(TrevorFactionId, zoneService.GetZone(enemyZone.Id)!.OwnerFactionId);
        }

        #endregion

        #region Scenario: Economy Flow (Income → Purchase Troops → Allocate from Reserve)

        /// <summary>
        /// End-to-end test for the complete economy flow:
        /// 1. Player owns zones that generate income
        /// 2. Resource tick triggers, generating income for the faction
        /// 3. Income is added to player's GTA V cash
        /// 4. Player purchases troops with cash (goes to reserve pool)
        /// 5. Player allocates troops from reserve to specific zones for defense
        /// </summary>
        [Fact]
        public void EndToEnd_EconomyFlow_IncomeToTroopPurchaseToAllocation()
        {
            // Arrange: Set up game world with all economy services
            var gameBridge = new MockGameBridge();
            var zoneRepo = new InMemoryZoneRepository();
            var zoneService = new ZoneService(zoneRepo);
            var factionRepo = new InMemoryFactionRepository();
            var factionService = new FactionService(factionRepo);
            var resourceModifier = new ZoneTraitResourceModifier();
            var supplyLineService = new SupplyLineService(zoneService);
            var tickService = new ResourceTickService(
                factionService, zoneService, resourceModifier, supplyLineService, 300);
            var economyManager = new EconomyManager(tickService, gameBridge);
            var defenderTierService = new DefenderTierService();
            var troopPurchaseService = new TroopPurchaseService(
                gameBridge, defenderTierService, factionService);
            var allocationRepo = new InMemoryZoneDefenderAllocationRepository();
            var allocationService = new ZoneDefenderAllocationService(allocationRepo);

            // Set up player faction (Michael)
            SetupFaction(factionRepo, factionService, MichaelFactionId, "Michael's Crew", 0, 0);

            // Create zones owned by the player - commercial zone generates +50% cash
            var downtown = new Zone("zone-downtown", "Downtown", new Vector3(100, 100, 0), 200f, 5);
            downtown.OwnerFactionId = MichaelFactionId;
            downtown.ControlPercentage = 100f;
            downtown.Traits = ZoneTrait.Commercial; // +50% cash bonus
            zoneRepo.Add(downtown);
            factionService.AddZoneToFaction(MichaelFactionId, downtown.Id);

            var industrial = new Zone("zone-industrial", "Industrial District", new Vector3(500, 100, 0), 200f, 3);
            industrial.OwnerFactionId = MichaelFactionId;
            industrial.ControlPercentage = 100f;
            industrial.Traits = ZoneTrait.Industrial; // +50% weapons
            zoneRepo.Add(industrial);
            factionService.AddZoneToFaction(MichaelFactionId, industrial.Id);

            // Configure economy manager for player
            economyManager.SetPlayerFactionId(MichaelFactionId);
            economyManager.Start();

            // Verify initial state
            Assert.Equal(0, gameBridge.GetPlayerMoney());
            var initialState = factionService.GetFactionState(MichaelFactionId);
            Assert.NotNull(initialState);
            Assert.Equal(0, initialState.Cash);
            Assert.Equal(0, initialState.TotalReserveTroops);

            // STEP 1: Generate income through resource ticks
            // Multiple ticks to accumulate sufficient funds for troop purchases
            for (int i = 0; i < 5; i++)
            {
                economyManager.ForceTick();
            }

            // Assert: Player has received GTA V cash
            int playerMoney = gameBridge.GetPlayerMoney();
            Assert.True(playerMoney > 0, $"Player should have received income, got ${playerMoney}");

            // Faction state should also reflect accumulated resources
            var stateAfterIncome = factionService.GetFactionState(MichaelFactionId);
            Assert.NotNull(stateAfterIncome);
            Assert.True(stateAfterIncome.Cash > 0, "Faction should have accumulated cash");

            // STEP 2: Purchase troops with earned money
            int basicCost = defenderTierService.GetCost(DefenderTier.Basic);
            int mediumCost = defenderTierService.GetCost(DefenderTier.Medium);
            int heavyCost = defenderTierService.GetCost(DefenderTier.Heavy);

            int moneyBeforePurchases = playerMoney;

            // Purchase mixed troops
            var basicPurchase = troopPurchaseService.PurchaseTroops(MichaelFactionId, DefenderTier.Basic, 3);
            var mediumPurchase = troopPurchaseService.PurchaseTroops(MichaelFactionId, DefenderTier.Medium, 2);
            var heavyPurchase = troopPurchaseService.PurchaseTroops(MichaelFactionId, DefenderTier.Heavy, 1);

            // Assert: Purchases succeeded
            Assert.True(basicPurchase.Success, "Basic troop purchase should succeed");
            Assert.True(mediumPurchase.Success, "Medium troop purchase should succeed");
            Assert.True(heavyPurchase.Success, "Heavy troop purchase should succeed");

            // Assert: Money was deducted
            int expectedSpent = (3 * basicCost) + (2 * mediumCost) + (1 * heavyCost);
            Assert.Equal(moneyBeforePurchases - expectedSpent, gameBridge.GetPlayerMoney());

            // Assert: Troops are in reserve pool
            var stateAfterPurchase = factionService.GetFactionState(MichaelFactionId);
            Assert.NotNull(stateAfterPurchase);
            Assert.Equal(3, stateAfterPurchase.GetReserveTroops(DefenderTier.Basic));
            Assert.Equal(2, stateAfterPurchase.GetReserveTroops(DefenderTier.Medium));
            Assert.Equal(1, stateAfterPurchase.GetReserveTroops(DefenderTier.Heavy));
            Assert.Equal(6, stateAfterPurchase.TotalReserveTroops);

            // STEP 3: Allocate troops from reserve to zones
            // Allocate to downtown (high-value zone): 2 Basic, 1 Medium
            bool downtownBasicAlloc = allocationService.AllocateTroops(
                stateAfterPurchase, downtown.Id, DefenderTier.Basic, 2);
            bool downtownMediumAlloc = allocationService.AllocateTroops(
                stateAfterPurchase, downtown.Id, DefenderTier.Medium, 1);

            // Allocate to industrial: 1 Basic, 1 Medium, 1 Heavy
            bool industrialBasicAlloc = allocationService.AllocateTroops(
                stateAfterPurchase, industrial.Id, DefenderTier.Basic, 1);
            bool industrialMediumAlloc = allocationService.AllocateTroops(
                stateAfterPurchase, industrial.Id, DefenderTier.Medium, 1);
            bool industrialHeavyAlloc = allocationService.AllocateTroops(
                stateAfterPurchase, industrial.Id, DefenderTier.Heavy, 1);

            // Assert: All allocations succeeded
            Assert.True(downtownBasicAlloc, "Downtown basic allocation should succeed");
            Assert.True(downtownMediumAlloc, "Downtown medium allocation should succeed");
            Assert.True(industrialBasicAlloc, "Industrial basic allocation should succeed");
            Assert.True(industrialMediumAlloc, "Industrial medium allocation should succeed");
            Assert.True(industrialHeavyAlloc, "Industrial heavy allocation should succeed");

            // Assert: Reserve pool was depleted
            Assert.Equal(0, stateAfterPurchase.GetReserveTroops(DefenderTier.Basic));
            Assert.Equal(0, stateAfterPurchase.GetReserveTroops(DefenderTier.Medium));
            Assert.Equal(0, stateAfterPurchase.GetReserveTroops(DefenderTier.Heavy));
            Assert.Equal(0, stateAfterPurchase.TotalReserveTroops);

            // Assert: Zone allocations are correct
            var downtownAllocation = allocationService.GetAllocation(MichaelFactionId, downtown.Id);
            Assert.NotNull(downtownAllocation);
            Assert.Equal(2, downtownAllocation!.GetTroopCount(DefenderTier.Basic));
            Assert.Equal(1, downtownAllocation.GetTroopCount(DefenderTier.Medium));
            Assert.Equal(0, downtownAllocation.GetTroopCount(DefenderTier.Heavy));
            Assert.Equal(3, downtownAllocation.TotalTroops);

            var industrialAllocation = allocationService.GetAllocation(MichaelFactionId, industrial.Id);
            Assert.NotNull(industrialAllocation);
            Assert.Equal(1, industrialAllocation!.GetTroopCount(DefenderTier.Basic));
            Assert.Equal(1, industrialAllocation.GetTroopCount(DefenderTier.Medium));
            Assert.Equal(1, industrialAllocation.GetTroopCount(DefenderTier.Heavy));
            Assert.Equal(3, industrialAllocation.TotalTroops);

            // Assert: Total allocated troops matches
            int totalAllocated = allocationService.GetTotalAllocatedTroops(MichaelFactionId);
            Assert.Equal(6, totalAllocated);
        }

        /// <summary>
        /// Verifies that troops can be withdrawn from zones back to reserve pool.
        /// </summary>
        [Fact]
        public void EndToEnd_EconomyFlow_WithdrawTroopsFromZoneToReserve()
        {
            // Arrange
            var gameBridge = new MockGameBridge();
            gameBridge.PlayerMoney = 5000;
            var factionRepo = new InMemoryFactionRepository();
            var factionService = new FactionService(factionRepo);
            var defenderTierService = new DefenderTierService();
            var troopPurchaseService = new TroopPurchaseService(
                gameBridge, defenderTierService, factionService);
            var allocationRepo = new InMemoryZoneDefenderAllocationRepository();
            var allocationService = new ZoneDefenderAllocationService(allocationRepo);

            // Set up faction
            SetupFaction(factionRepo, factionService, MichaelFactionId, "Michael's Crew", 0, 0);

            // Purchase troops
            troopPurchaseService.PurchaseTroops(MichaelFactionId, DefenderTier.Basic, 5);
            troopPurchaseService.PurchaseTroops(MichaelFactionId, DefenderTier.Medium, 3);

            var factionState = factionService.GetFactionState(MichaelFactionId);
            Assert.NotNull(factionState);

            // Allocate all troops to a zone
            allocationService.AllocateTroops(factionState, "zone-1", DefenderTier.Basic, 5);
            allocationService.AllocateTroops(factionState, "zone-1", DefenderTier.Medium, 3);

            // Verify all troops allocated
            Assert.Equal(0, factionState.TotalReserveTroops);
            var allocation = allocationService.GetAllocation(MichaelFactionId, "zone-1");
            Assert.Equal(8, allocation!.TotalTroops);

            // Act: Withdraw some troops back to reserve
            bool withdrawBasic = allocationService.WithdrawTroops(factionState, "zone-1", DefenderTier.Basic, 2);
            bool withdrawMedium = allocationService.WithdrawTroops(factionState, "zone-1", DefenderTier.Medium, 1);

            // Assert: Withdrawals succeeded
            Assert.True(withdrawBasic);
            Assert.True(withdrawMedium);

            // Assert: Reserve pool has withdrawn troops
            Assert.Equal(2, factionState.GetReserveTroops(DefenderTier.Basic));
            Assert.Equal(1, factionState.GetReserveTroops(DefenderTier.Medium));
            Assert.Equal(3, factionState.TotalReserveTroops);

            // Assert: Zone allocation reduced
            allocation = allocationService.GetAllocation(MichaelFactionId, "zone-1");
            Assert.Equal(3, allocation!.GetTroopCount(DefenderTier.Basic));
            Assert.Equal(2, allocation.GetTroopCount(DefenderTier.Medium));
            Assert.Equal(5, allocation.TotalTroops);
        }

        /// <summary>
        /// Verifies that insufficient funds prevent troop purchases.
        /// </summary>
        [Fact]
        public void EndToEnd_EconomyFlow_InsufficientFunds_CannotPurchaseTroops()
        {
            // Arrange
            var gameBridge = new MockGameBridge();
            gameBridge.PlayerMoney = 500; // Limited funds
            var factionRepo = new InMemoryFactionRepository();
            var factionService = new FactionService(factionRepo);
            var defenderTierService = new DefenderTierService();
            var troopPurchaseService = new TroopPurchaseService(
                gameBridge, defenderTierService, factionService);

            SetupFaction(factionRepo, factionService, MichaelFactionId, "Michael's Crew", 0, 0);

            // Heavy troops cost $1000 each
            int heavyCost = defenderTierService.GetCost(DefenderTier.Heavy);
            Assert.Equal(1000, heavyCost);

            // Act: Try to purchase heavy troop with insufficient funds
            var result = troopPurchaseService.PurchaseTroops(MichaelFactionId, DefenderTier.Heavy, 1);

            // Assert: Purchase failed
            Assert.False(result.Success);
            Assert.Equal(0, result.TroopsPurchased);
            Assert.Equal(500, gameBridge.GetPlayerMoney()); // Money unchanged

            var state = factionService.GetFactionState(MichaelFactionId);
            Assert.Equal(0, state!.TotalReserveTroops); // No troops added
        }

        /// <summary>
        /// Verifies that zone income accumulates correctly across multiple ticks
        /// and can be used to progressively build an army.
        /// </summary>
        [Fact]
        public void EndToEnd_EconomyFlow_ProgressiveArmyBuilding()
        {
            // Arrange
            var gameBridge = new MockGameBridge();
            var zoneRepo = new InMemoryZoneRepository();
            var zoneService = new ZoneService(zoneRepo);
            var factionRepo = new InMemoryFactionRepository();
            var factionService = new FactionService(factionRepo);
            var resourceModifier = new ZoneTraitResourceModifier();
            var supplyLineService = new SupplyLineService(zoneService);
            var tickService = new ResourceTickService(
                factionService, zoneService, resourceModifier, supplyLineService, 300);
            var economyManager = new EconomyManager(tickService, gameBridge);
            var defenderTierService = new DefenderTierService();
            var troopPurchaseService = new TroopPurchaseService(
                gameBridge, defenderTierService, factionService);

            SetupFaction(factionRepo, factionService, MichaelFactionId, "Michael's Crew", 0, 0);

            // Create a high-value commercial zone
            var zone = new Zone("zone-1", "Financial District", new Vector3(0, 0, 0), 200f, 8);
            zone.OwnerFactionId = MichaelFactionId;
            zone.ControlPercentage = 100f;
            zone.Traits = ZoneTrait.Commercial;
            zoneRepo.Add(zone);
            factionService.AddZoneToFaction(MichaelFactionId, zone.Id);

            economyManager.SetPlayerFactionId(MichaelFactionId);
            economyManager.Start();

            int basicCost = defenderTierService.GetCost(DefenderTier.Basic);
            int troopsPurchased = 0;

            // Act: Progressive army building over multiple ticks
            for (int tick = 0; tick < 10; tick++)
            {
                economyManager.ForceTick();

                // Try to buy a troop after each tick if affordable
                if (troopPurchaseService.CanAfford(DefenderTier.Basic, 1))
                {
                    var result = troopPurchaseService.PurchaseTroops(
                        MichaelFactionId, DefenderTier.Basic, 1);
                    if (result.Success)
                    {
                        troopsPurchased++;
                    }
                }
            }

            // Assert: Should have been able to purchase multiple troops over time
            var state = factionService.GetFactionState(MichaelFactionId);
            Assert.NotNull(state);
            Assert.True(troopsPurchased > 0, "Should have purchased at least one troop");
            Assert.Equal(troopsPurchased, state.GetReserveTroops(DefenderTier.Basic));
        }

        #endregion

        #region Scenario: AI Simulation (AI Attacks, AI Defends, AI Battles AI)

        /// <summary>
        /// End-to-end test for AI simulation:
        /// 1. Player is one faction, other two are AI-controlled
        /// 2. AI factions make attack decisions against enemy zones
        /// 3. AI factions make defend decisions for contested zones
        /// 4. AI battles AI through BattleSimulationService
        /// 5. Zone ownership changes based on battle outcomes
        /// 6. Troop casualties are applied to both sides
        /// </summary>
        [Fact]
        public void EndToEnd_AISimulation_AIAttacksDefendsAndBattlesAI()
        {
            // Arrange: Set up complete game world with all AI services
            var zoneRepo = new InMemoryZoneRepository();
            var zoneService = new ZoneService(zoneRepo);
            var factionRepo = new InMemoryFactionRepository();
            var factionService = new FactionService(factionRepo);
            var battleSimulationService = new BattleSimulationService();

            // Create all three factions
            var michael = SetupFaction(factionRepo, factionService, MichaelFactionId, "Michael's Crew", 10000, 100);
            var trevor = SetupFaction(factionRepo, factionService, TrevorFactionId, "Trevor's Gang", 8000, 80);
            var franklin = SetupFaction(factionRepo, factionService, FranklinFactionId, "Franklin's Family", 6000, 60);

            // Create zones - each faction owns some territories
            // Michael's zones (high value targets)
            var vinewood = CreateZone(zoneRepo, "zone-vinewood", "Vinewood", MichaelFactionId, 8);
            factionService.AddZoneToFaction(MichaelFactionId, vinewood.Id);

            var delPerro = CreateZone(zoneRepo, "zone-delperro", "Del Perro", MichaelFactionId, 7);
            factionService.AddZoneToFaction(MichaelFactionId, delPerro.Id);

            // Trevor's zones
            var sandyShores = CreateZone(zoneRepo, "zone-sandy", "Sandy Shores", TrevorFactionId, 5);
            factionService.AddZoneToFaction(TrevorFactionId, sandyShores.Id);

            var alamoSea = CreateZone(zoneRepo, "zone-alamo", "Alamo Sea", TrevorFactionId, 4);
            factionService.AddZoneToFaction(TrevorFactionId, alamoSea.Id);

            // Franklin's zones
            var groveStreet = CreateZone(zoneRepo, "zone-grove", "Grove Street", FranklinFactionId, 5);
            factionService.AddZoneToFaction(FranklinFactionId, groveStreet.Id);

            var davis = CreateZone(zoneRepo, "zone-davis", "Davis", FranklinFactionId, 4);
            factionService.AddZoneToFaction(FranklinFactionId, davis.Id);

            // Set up AI strategies
            var michaelStrategy = new MichaelAIStrategy();
            var trevorStrategy = new TrevorAIStrategy();
            var franklinStrategy = new FranklinAIStrategy();

            var strategies = new Dictionary<string, IAIStrategy>
            {
                { MichaelFactionId, michaelStrategy },
                { TrevorFactionId, trevorStrategy },
                { FranklinFactionId, franklinStrategy }
            };

            // Create AI Manager - Franklin is the player, so Michael and Trevor are AI
            var aiManager = new AIManager(factionService, zoneService, strategies);
            aiManager.SetPlayerFactionId(FranklinFactionId);
            aiManager.Start();

            // Track AI decisions
            var attackDecisions = new List<(string FactionId, AIDecision Decision)>();
            var defendDecisions = new List<(string FactionId, AIDecision Decision)>();

            aiManager.OnAIDecision += (sender, args) =>
            {
                if (args.Decision.DecisionType == AIDecisionType.Attack)
                    attackDecisions.Add((args.FactionId, args.Decision));
                else if (args.Decision.DecisionType == AIDecisionType.Defend)
                    defendDecisions.Add((args.FactionId, args.Decision));
            };

            // Verify initial state
            Assert.Equal(2, factionService.GetZoneCount(MichaelFactionId));
            Assert.Equal(2, factionService.GetZoneCount(TrevorFactionId));
            Assert.Equal(2, factionService.GetZoneCount(FranklinFactionId));

            // STEP 1: Setup contested zone to test defense
            // Make one of Michael's zones contested so he will defend
            vinewood.IsContested = true;
            zoneRepo.Update(vinewood);

            // STEP 2: AI makes decisions
            aiManager.ForceDecision();

            // Assert: Trevor (aggressive) should make attack decisions
            var trevorDecisions = aiManager.GetLastDecisions(TrevorFactionId);
            Assert.NotEmpty(trevorDecisions);

            // Trevor should target high-value zones (Vinewood or Del Perro owned by Michael)
            var trevorAttack = trevorDecisions.FirstOrDefault(d => d.DecisionType == AIDecisionType.Attack);
            Assert.NotNull(trevorAttack);
            Assert.True(trevorAttack!.TroopsToCommit > 0, "Trevor should commit troops to attack");

            // Assert: Michael (defensive) should defend the contested zone
            var michaelDecisions = aiManager.GetLastDecisions(MichaelFactionId);
            Assert.NotEmpty(michaelDecisions);

            // Assert: Franklin (player) should NOT make AI decisions
            var franklinDecisions = aiManager.GetLastDecisions(FranklinFactionId);
            Assert.Empty(franklinDecisions);

            // STEP 3: Verify Michael has a defend decision for the contested zone
            var michaelDefend = michaelDecisions.FirstOrDefault(d => d.DecisionType == AIDecisionType.Defend);
            Assert.NotNull(michaelDefend);
            Assert.Equal("zone-vinewood", michaelDefend!.TargetZoneId);

            // STEP 4: Simulate AI vs AI battle (Trevor attacks Michael's zone)
            var targetZoneId = trevorAttack.TargetZoneId!;
            var targetZone = zoneRepo.GetById(targetZoneId);
            Assert.NotNull(targetZone);
            string originalOwner = targetZone!.OwnerFactionId!;

            // Get troop compositions for battle
            var trevorState = factionService.GetFactionState(TrevorFactionId);
            var michaelState = factionService.GetFactionState(MichaelFactionId);
            Assert.NotNull(trevorState);
            Assert.NotNull(michaelState);

            // Trevor commits decided troops, Michael defends with available troops
            var attackerTroops = new TroopComposition(trevorAttack.TroopsToCommit, 0, 0);
            var defenderTroops = new TroopComposition(michaelState.TroopCount / 2, 0, 0); // Defenders commit half their troops

            // Simulate the battle
            var battleResult = battleSimulationService.SimulateBattle(
                TrevorFactionId,
                MichaelFactionId,
                targetZoneId,
                attackerTroops,
                defenderTroops);

            // Assert: Battle produced valid result
            Assert.NotNull(battleResult);
            Assert.Equal(targetZoneId, battleResult.ZoneId);
            Assert.True(battleResult.NewOwnerFactionId == TrevorFactionId || battleResult.NewOwnerFactionId == MichaelFactionId,
                "Zone should be owned by either attacker or defender");

            // STEP 5: Apply battle outcome - update zone ownership
            if (battleResult.AttackerWon)
            {
                // Trevor captured the zone
                targetZone.OwnerFactionId = TrevorFactionId;
                targetZone.ControlPercentage = 100f;
                targetZone.IsContested = false;
                zoneRepo.Update(targetZone);

                // Update faction zone tracking
                factionService.RemoveZoneFromFaction(originalOwner, targetZoneId);
                factionService.AddZoneToFaction(TrevorFactionId, targetZoneId);
            }
            else
            {
                // Michael defended successfully
                targetZone.IsContested = false;
                targetZone.ControlPercentage = 100f;
                zoneRepo.Update(targetZone);
            }

            // STEP 6: Apply casualties to both factions
            int attackerCasualties = battleResult.AttackerCasualties.TotalCount;
            int defenderCasualties = battleResult.DefenderCasualties.TotalCount;

            // Reduce troop counts
            if (attackerCasualties > 0)
            {
                factionService.LoseTroops(TrevorFactionId, attackerCasualties);
            }
            if (defenderCasualties > 0)
            {
                factionService.LoseTroops(MichaelFactionId, defenderCasualties);
            }

            // Assert: Casualties were applied
            var trevorFinalState = factionService.GetFactionState(TrevorFactionId);
            var michaelFinalState = factionService.GetFactionState(MichaelFactionId);

            Assert.True(trevorFinalState!.TroopCount < 80 || attackerCasualties == 0,
                "Trevor should have fewer troops after casualties");
            Assert.True(michaelFinalState!.TroopCount < 100 || defenderCasualties == 0,
                "Michael should have fewer troops after casualties");

            // Assert: Zone ownership reflects battle outcome
            var updatedZone = zoneRepo.GetById(targetZoneId);
            Assert.Equal(battleResult.NewOwnerFactionId, updatedZone!.OwnerFactionId);
        }

        /// <summary>
        /// Verifies that multiple AI vs AI battles can occur in sequence with proper
        /// troop deductions and zone ownership changes.
        /// </summary>
        [Fact]
        public void EndToEnd_AISimulation_MultipleAIBattlesInSequence()
        {
            // Arrange
            var zoneRepo = new InMemoryZoneRepository();
            var zoneService = new ZoneService(zoneRepo);
            var factionRepo = new InMemoryFactionRepository();
            var factionService = new FactionService(factionRepo);
            var battleSimulationService = new BattleSimulationService();

            // Create factions with varying troop counts
            var michael = SetupFaction(factionRepo, factionService, MichaelFactionId, "Michael's Crew", 10000, 50);
            var trevor = SetupFaction(factionRepo, factionService, TrevorFactionId, "Trevor's Gang", 8000, 100);
            var franklin = SetupFaction(factionRepo, factionService, FranklinFactionId, "Franklin's Family", 6000, 30);

            // Create zones - Trevor has many zones to attack from
            var zone1 = CreateZone(zoneRepo, "zone-1", "Zone 1", MichaelFactionId, 5);
            factionService.AddZoneToFaction(MichaelFactionId, zone1.Id);

            var zone2 = CreateZone(zoneRepo, "zone-2", "Zone 2", MichaelFactionId, 5);
            factionService.AddZoneToFaction(MichaelFactionId, zone2.Id);

            var zone3 = CreateZone(zoneRepo, "zone-3", "Zone 3", TrevorFactionId, 3);
            factionService.AddZoneToFaction(TrevorFactionId, zone3.Id);

            var zone4 = CreateZone(zoneRepo, "zone-4", "Zone 4", TrevorFactionId, 3);
            factionService.AddZoneToFaction(TrevorFactionId, zone4.Id);

            // Track initial troop counts
            int trevorInitialTroops = factionService.GetFactionState(TrevorFactionId)!.TroopCount;
            int michaelInitialTroops = factionService.GetFactionState(MichaelFactionId)!.TroopCount;

            int battlesFought = 0;
            int zonesChangedOwner = 0;

            // Act: Simulate multiple battles
            for (int i = 0; i < 3; i++)
            {
                var trevorState = factionService.GetFactionState(TrevorFactionId);
                var michaelState = factionService.GetFactionState(MichaelFactionId);

                // Skip if either faction has no troops
                if (trevorState!.TroopCount <= 0 || michaelState!.TroopCount <= 0)
                    break;

                // Trevor attacks a Michael zone
                var targetZone = zoneRepo.GetByOwner(MichaelFactionId).FirstOrDefault();
                if (targetZone == null)
                    break;

                int attackForce = Math.Min(20, trevorState.TroopCount);
                int defenseForce = Math.Min(15, michaelState.TroopCount);

                var attackerTroops = new TroopComposition(attackForce, 0, 0);
                var defenderTroops = new TroopComposition(defenseForce, 0, 0);

                var battleResult = battleSimulationService.SimulateBattle(
                    TrevorFactionId,
                    MichaelFactionId,
                    targetZone.Id,
                    attackerTroops,
                    defenderTroops);

                battlesFought++;

                // Apply battle result
                if (battleResult.AttackerWon)
                {
                    targetZone.OwnerFactionId = TrevorFactionId;
                    zoneRepo.Update(targetZone);
                    factionService.RemoveZoneFromFaction(MichaelFactionId, targetZone.Id);
                    factionService.AddZoneToFaction(TrevorFactionId, targetZone.Id);
                    zonesChangedOwner++;
                }

                // Apply casualties
                factionService.LoseTroops(TrevorFactionId, battleResult.AttackerCasualties.TotalCount);
                factionService.LoseTroops(MichaelFactionId, battleResult.DefenderCasualties.TotalCount);
            }

            // Assert: Multiple battles occurred
            Assert.True(battlesFought > 0, "At least one battle should have occurred");

            // Assert: Troops were reduced
            var trevorFinalState = factionService.GetFactionState(TrevorFactionId);
            var michaelFinalState = factionService.GetFactionState(MichaelFactionId);

            Assert.True(trevorFinalState!.TroopCount < trevorInitialTroops,
                "Trevor should have lost troops in battles");
            Assert.True(michaelFinalState!.TroopCount < michaelInitialTroops,
                "Michael should have lost troops in battles");
        }

        /// <summary>
        /// Verifies that AI strategies influence battle outcomes appropriately.
        /// Trevor (aggressive) should attack more, Michael (defensive) should defend more.
        /// </summary>
        [Fact]
        public void EndToEnd_AISimulation_StrategiesDetermineDecisionTypes()
        {
            // Arrange
            var zoneRepo = new InMemoryZoneRepository();
            var zoneService = new ZoneService(zoneRepo);
            var factionRepo = new InMemoryFactionRepository();
            var factionService = new FactionService(factionRepo);

            // Create factions with equal troops
            SetupFaction(factionRepo, factionService, MichaelFactionId, "Michael's Crew", 10000, 50);
            SetupFaction(factionRepo, factionService, TrevorFactionId, "Trevor's Gang", 10000, 50);
            SetupFaction(factionRepo, factionService, FranklinFactionId, "Franklin's Family", 10000, 50);

            // Create zones - one contested zone for each AI faction
            var michaelZone = CreateZone(zoneRepo, "zone-m", "Michael Zone", MichaelFactionId, 8);
            michaelZone.IsContested = true; // Contested - Michael should defend
            zoneRepo.Update(michaelZone);
            factionService.AddZoneToFaction(MichaelFactionId, michaelZone.Id);

            var trevorZone = CreateZone(zoneRepo, "zone-t", "Trevor Zone", TrevorFactionId, 5);
            factionService.AddZoneToFaction(TrevorFactionId, trevorZone.Id);

            var franklinZone = CreateZone(zoneRepo, "zone-f", "Franklin Zone", FranklinFactionId, 5);
            factionService.AddZoneToFaction(FranklinFactionId, franklinZone.Id);

            var strategies = new Dictionary<string, IAIStrategy>
            {
                { MichaelFactionId, new MichaelAIStrategy() },
                { TrevorFactionId, new TrevorAIStrategy() },
                { FranklinFactionId, new FranklinAIStrategy() }
            };

            var aiManager = new AIManager(factionService, zoneService, strategies);
            aiManager.SetPlayerFactionId(FranklinFactionId); // Franklin is player
            aiManager.Start();

            // Act
            aiManager.ForceDecision();

            // Assert: Michael (defensive) prioritizes defense when contested
            var michaelDecisions = aiManager.GetLastDecisions(MichaelFactionId);
            Assert.NotEmpty(michaelDecisions);
            var michaelDefend = michaelDecisions.FirstOrDefault(d => d.DecisionType == AIDecisionType.Defend);
            Assert.NotNull(michaelDefend);
            Assert.Equal("zone-m", michaelDefend!.TargetZoneId);

            // Assert: Trevor (aggressive) should have attack decisions
            var trevorDecisions = aiManager.GetLastDecisions(TrevorFactionId);
            Assert.NotEmpty(trevorDecisions);
            var trevorAttack = trevorDecisions.FirstOrDefault(d => d.DecisionType == AIDecisionType.Attack);
            Assert.NotNull(trevorAttack);

            // Assert: Verify strategy aggressiveness values
            var trevorStrategy = new TrevorAIStrategy();
            var michaelStrategy = new MichaelAIStrategy();
            Assert.True(trevorStrategy.GetAggressiveness() > michaelStrategy.GetAggressiveness(),
                "Trevor strategy should be more aggressive than Michael's");
        }

        /// <summary>
        /// Verifies that the battle simulation correctly handles tier-based troop composition
        /// with proper casualty distribution.
        /// </summary>
        [Fact]
        public void EndToEnd_AISimulation_BattleWithMixedTroopTiers()
        {
            // Arrange
            var battleSimulationService = new BattleSimulationService();

            // Attacker has mixed tiers: 30 Basic, 20 Medium, 10 Heavy
            var attackerTroops = new TroopComposition(30, 20, 10);
            // Defender has mostly Basic: 50 Basic, 5 Medium, 0 Heavy
            var defenderTroops = new TroopComposition(50, 5, 0);

            // Calculate strengths
            float attackerStrength = attackerTroops.TotalStrength; // 30*1 + 20*1.5 + 10*2 = 80
            float defenderStrength = defenderTroops.TotalStrength; // 50*1 + 5*1.5 + 0*2 = 57.5

            Assert.Equal(80f, attackerStrength, 1);
            Assert.Equal(57.5f, defenderStrength, 1);

            // Act
            var result = battleSimulationService.SimulateBattle(
                "attacker-faction",
                "defender-faction",
                "zone-1",
                attackerTroops,
                defenderTroops);

            // Assert: With higher strength, attacker should likely win
            // (Defender advantage is 1.2x, so 57.5 * 1.2 = 69 < 80)
            Assert.True(result.AttackerWon, "Attacker with higher strength should win");

            // Assert: Casualties are reasonable
            Assert.True(result.AttackerCasualties.TotalCount > 0, "Attacker should have some casualties");
            Assert.True(result.DefenderCasualties.TotalCount > 0, "Defender should have some casualties");
            Assert.True(result.AttackerCasualties.TotalCount <= attackerTroops.TotalCount,
                "Attacker casualties should not exceed attacker troops");
            Assert.True(result.DefenderCasualties.TotalCount <= defenderTroops.TotalCount,
                "Defender casualties should not exceed defender troops");

            // Assert: Basic troops take more casualties than heavy
            if (result.AttackerCasualties.Basic > 0 && result.AttackerCasualties.Heavy > 0)
            {
                float basicRate = (float)result.AttackerCasualties.Basic / attackerTroops.Basic;
                float heavyRate = (float)result.AttackerCasualties.Heavy / attackerTroops.Heavy;
                Assert.True(basicRate >= heavyRate,
                    "Basic troops should have higher or equal casualty rate than heavy troops");
            }
        }

        /// <summary>
        /// Verifies complete AI simulation cycle: decision → battle → outcome → state update.
        /// </summary>
        [Fact]
        public void EndToEnd_AISimulation_CompleteDecisionBattleOutcomeCycle()
        {
            // Arrange: Full game setup
            var zoneRepo = new InMemoryZoneRepository();
            var zoneService = new ZoneService(zoneRepo);
            var factionRepo = new InMemoryFactionRepository();
            var factionService = new FactionService(factionRepo);
            var battleSimulationService = new BattleSimulationService();

            // Trevor has overwhelming force against weak Michael
            var michael = SetupFaction(factionRepo, factionService, MichaelFactionId, "Michael's Crew", 5000, 15);
            var trevor = SetupFaction(factionRepo, factionService, TrevorFactionId, "Trevor's Gang", 10000, 100);
            var franklin = SetupFaction(factionRepo, factionService, FranklinFactionId, "Franklin's Family", 5000, 20);

            // Michael owns one high-value zone
            var vinewood = CreateZone(zoneRepo, "zone-vinewood", "Vinewood", MichaelFactionId, 9);
            factionService.AddZoneToFaction(MichaelFactionId, vinewood.Id);

            // Trevor owns one zone
            var sandy = CreateZone(zoneRepo, "zone-sandy", "Sandy Shores", TrevorFactionId, 4);
            factionService.AddZoneToFaction(TrevorFactionId, sandy.Id);

            // Franklin owns one zone
            var grove = CreateZone(zoneRepo, "zone-grove", "Grove Street", FranklinFactionId, 5);
            factionService.AddZoneToFaction(FranklinFactionId, grove.Id);

            var strategies = new Dictionary<string, IAIStrategy>
            {
                { MichaelFactionId, new MichaelAIStrategy() },
                { TrevorFactionId, new TrevorAIStrategy() },
                { FranklinFactionId, new FranklinAIStrategy() }
            };

            var aiManager = new AIManager(factionService, zoneService, strategies);
            aiManager.SetPlayerFactionId(FranklinFactionId);
            aiManager.Start();

            // Track state before
            int michaelZonesBefore = factionService.GetZoneCount(MichaelFactionId);
            int trevorZonesBefore = factionService.GetZoneCount(TrevorFactionId);
            int trevorTroopsBefore = factionService.GetFactionState(TrevorFactionId)!.TroopCount;

            // STEP 1: AI Decision
            aiManager.ForceDecision();

            var trevorDecisions = aiManager.GetLastDecisions(TrevorFactionId);
            var attackDecision = trevorDecisions.FirstOrDefault(d => d.DecisionType == AIDecisionType.Attack);

            Assert.NotNull(attackDecision);
            Assert.Equal("zone-vinewood", attackDecision!.TargetZoneId);

            // STEP 2: Battle Simulation
            var attackerTroops = new TroopComposition(attackDecision.TroopsToCommit, 0, 0);
            var defenderTroops = new TroopComposition(15, 0, 0); // Michael's full force

            var battleResult = battleSimulationService.SimulateBattle(
                TrevorFactionId,
                MichaelFactionId,
                "zone-vinewood",
                attackerTroops,
                defenderTroops);

            // STEP 3: Apply Outcome
            if (battleResult.AttackerWon)
            {
                vinewood.OwnerFactionId = TrevorFactionId;
                vinewood.IsContested = false;
                vinewood.ControlPercentage = 100f;
                zoneRepo.Update(vinewood);
                factionService.RemoveZoneFromFaction(MichaelFactionId, vinewood.Id);
                factionService.AddZoneToFaction(TrevorFactionId, vinewood.Id);
            }

            // Apply casualties
            factionService.LoseTroops(TrevorFactionId, battleResult.AttackerCasualties.TotalCount);
            factionService.LoseTroops(MichaelFactionId, battleResult.DefenderCasualties.TotalCount);

            // STEP 4: Verify Final State
            int michaelZonesAfter = factionService.GetZoneCount(MichaelFactionId);
            int trevorZonesAfter = factionService.GetZoneCount(TrevorFactionId);
            int trevorTroopsAfter = factionService.GetFactionState(TrevorFactionId)!.TroopCount;

            // With overwhelming force, Trevor should win
            Assert.True(battleResult.AttackerWon, "Trevor with overwhelming force should win");
            Assert.Equal(MichaelFactionId, battleResult.DefenderFactionId);
            Assert.Equal(TrevorFactionId, battleResult.NewOwnerFactionId);

            // Zone ownership changed
            Assert.Equal(michaelZonesBefore - 1, michaelZonesAfter);
            Assert.Equal(trevorZonesBefore + 1, trevorZonesAfter);

            // Troops reduced
            Assert.True(trevorTroopsAfter < trevorTroopsBefore, "Trevor should have lost some troops");

            // Zone is now owned by Trevor
            Assert.Equal(TrevorFactionId, zoneService.GetZone("zone-vinewood")!.OwnerFactionId);
        }

        #endregion

        #region Scenario: Follower System (Recruit → Follow → Fight → Die)

        /// <summary>
        /// End-to-end test for the complete follower system flow:
        /// 1. Player recruits a follower from the Army menu
        /// 2. Follower is tracked and counts toward limit
        /// 3. Follower participates in combat (simulated via service state)
        /// 4. Follower dies in combat → permanently lost
        /// </summary>
        [Fact]
        public void EndToEnd_FollowerSystem_RecruitFollowFightDie()
        {
            // Arrange: Set up faction and follower service
            var factionRepo = new InMemoryFactionRepository();
            var factionService = new FactionService(factionRepo);
            SetupFaction(factionRepo, factionService, MichaelFactionId, "Michael's Crew", 10000, 50);

            var followerService = new FollowerService(maxFollowers: 4);

            // STEP 1: Recruit a follower
            var recruitResult = followerService.Recruit(MichaelFactionId, DefenderTier.Basic);

            // Assert: Recruitment succeeded
            Assert.True(recruitResult.Success, "Recruitment should succeed");
            Assert.NotNull(recruitResult.Follower);
            Assert.Equal(MichaelFactionId, recruitResult.Follower!.FactionId);
            Assert.Equal(DefenderTier.Basic, recruitResult.Follower.Tier);
            Assert.True(recruitResult.Follower.IsAlive, "New follower should be alive");

            var followerId = recruitResult.Follower.Id;

            // STEP 2: Verify follower is tracked and counts toward limit
            Assert.Equal(1, followerService.GetFollowerCount(MichaelFactionId));
            Assert.Equal(4, followerService.GetMaxFollowers());

            var followers = followerService.GetFollowers(MichaelFactionId);
            Assert.Single(followers);
            Assert.Equal(followerId, followers[0].Id);

            // STEP 3: Recruit more followers up to the limit
            var medium1 = followerService.Recruit(MichaelFactionId, DefenderTier.Medium);
            var medium2 = followerService.Recruit(MichaelFactionId, DefenderTier.Medium);
            var heavy1 = followerService.Recruit(MichaelFactionId, DefenderTier.Heavy);

            Assert.True(medium1.Success, "Second follower should succeed");
            Assert.True(medium2.Success, "Third follower should succeed");
            Assert.True(heavy1.Success, "Fourth follower should succeed");
            Assert.Equal(4, followerService.GetFollowerCount(MichaelFactionId));

            // Try to exceed limit
            var overLimit = followerService.Recruit(MichaelFactionId, DefenderTier.Basic);
            Assert.False(overLimit.Success, "Should not exceed max followers");
            Assert.Equal(FollowerRecruitFailureReason.MaxFollowersReached, overLimit.FailureReason);

            // STEP 4: Simulate follower fighting alongside player (via ped handle assignment)
            var activeFollower = followerService.GetFollowerById(followerId);
            Assert.NotNull(activeFollower);
            activeFollower!.SetPedHandle(12345); // Simulating spawn
            Assert.Equal(12345, activeFollower.PedHandle);

            // STEP 5: Follower dies in combat - permanent loss
            followerService.HandleFollowerDeath(followerId);

            // Assert: Follower is removed
            Assert.Equal(3, followerService.GetFollowerCount(MichaelFactionId));
            var removedFollower = followerService.GetFollowerById(followerId);
            Assert.Null(removedFollower); // Follower no longer in service

            // Can now recruit a replacement
            var replacement = followerService.Recruit(MichaelFactionId, DefenderTier.Basic);
            Assert.True(replacement.Success, "Should be able to recruit replacement");
            Assert.Equal(4, followerService.GetFollowerCount(MichaelFactionId));
        }

        /// <summary>
        /// Tests that followers persist through multiple combat encounters until death.
        /// </summary>
        [Fact]
        public void EndToEnd_FollowerSystem_SurviveMultipleCombats()
        {
            // Arrange
            var followerService = new FollowerService(maxFollowers: 6);

            // Recruit a heavy follower (tougher, survives longer)
            var heavyResult = followerService.Recruit(MichaelFactionId, DefenderTier.Heavy);
            Assert.True(heavyResult.Success);
            var heavyFollower = heavyResult.Follower!;

            // Simulate multiple combat encounters where follower survives
            for (int combat = 1; combat <= 3; combat++)
            {
                // Start of each combat, verify follower is still available
                var currentFollower = followerService.GetFollowerById(heavyFollower.Id);
                Assert.NotNull(currentFollower);
                Assert.True(currentFollower!.IsAlive, $"Follower should be alive for combat #{combat}");

                // Simulate combat participation
                currentFollower.SetPedHandle(1000 + combat);
            }

            // Follower still counts after surviving multiple combats
            Assert.Equal(1, followerService.GetFollowerCount(MichaelFactionId));
            var survivingFollower = followerService.GetFollowerById(heavyFollower.Id);
            Assert.NotNull(survivingFollower);
            Assert.True(survivingFollower!.IsAlive);
            Assert.True(survivingFollower.GetServiceTime().TotalMilliseconds >= 0, "Service time should be tracked");
        }

        /// <summary>
        /// Tests that dismissing a follower removes them without death penalty.
        /// </summary>
        [Fact]
        public void EndToEnd_FollowerSystem_DismissFollower()
        {
            // Arrange
            var followerService = new FollowerService(maxFollowers: 4);

            // Recruit two followers
            var basic = followerService.Recruit(MichaelFactionId, DefenderTier.Basic);
            var medium = followerService.Recruit(MichaelFactionId, DefenderTier.Medium);

            Assert.Equal(2, followerService.GetFollowerCount(MichaelFactionId));

            // Act: Dismiss the basic follower
            bool dismissed = followerService.DismissFollower(basic.Follower!.Id);

            // Assert: Follower removed, can recruit new one
            Assert.True(dismissed, "Dismiss should succeed");
            Assert.Equal(1, followerService.GetFollowerCount(MichaelFactionId));

            // The medium follower is still there
            var remaining = followerService.GetFollowers(MichaelFactionId);
            Assert.Single(remaining);
            Assert.Equal(medium.Follower!.Id, remaining[0].Id);

            // Dismissed follower no longer found
            Assert.Null(followerService.GetFollowerById(basic.Follower.Id));
        }

        /// <summary>
        /// Tests that all followers are dismissed when player switches characters.
        /// </summary>
        [Fact]
        public void EndToEnd_FollowerSystem_DismissAllOnCharacterSwitch()
        {
            // Arrange: Recruit followers for Michael
            var followerService = new FollowerService(maxFollowers: 6);

            followerService.Recruit(MichaelFactionId, DefenderTier.Basic);
            followerService.Recruit(MichaelFactionId, DefenderTier.Medium);
            followerService.Recruit(MichaelFactionId, DefenderTier.Heavy);

            // Also recruit for Trevor (different faction)
            followerService.Recruit(TrevorFactionId, DefenderTier.Basic);
            followerService.Recruit(TrevorFactionId, DefenderTier.Medium);

            Assert.Equal(3, followerService.GetFollowerCount(MichaelFactionId));
            Assert.Equal(2, followerService.GetFollowerCount(TrevorFactionId));

            // Act: Player switches from Michael to Trevor (Michael's followers dismissed)
            followerService.DismissAllFollowers(MichaelFactionId);

            // Assert: Michael's followers gone, Trevor's remain
            Assert.Equal(0, followerService.GetFollowerCount(MichaelFactionId));
            Assert.Equal(2, followerService.GetFollowerCount(TrevorFactionId));
            Assert.Empty(followerService.GetFollowers(MichaelFactionId));
            Assert.Equal(2, followerService.GetFollowers(TrevorFactionId).Count);
        }

        /// <summary>
        /// Tests mixed tier follower squad in combat.
        /// </summary>
        [Fact]
        public void EndToEnd_FollowerSystem_MixedTierSquad()
        {
            // Arrange
            var followerService = new FollowerService(maxFollowers: 6);

            // Recruit a mixed squad: 2 Basic, 2 Medium, 2 Heavy
            var basicFollowers = new List<Follower>();
            var mediumFollowers = new List<Follower>();
            var heavyFollowers = new List<Follower>();

            for (int i = 0; i < 2; i++)
            {
                var basicResult = followerService.Recruit(MichaelFactionId, DefenderTier.Basic);
                Assert.True(basicResult.Success);
                basicFollowers.Add(basicResult.Follower!);

                var mediumResult = followerService.Recruit(MichaelFactionId, DefenderTier.Medium);
                Assert.True(mediumResult.Success);
                mediumFollowers.Add(mediumResult.Follower!);

                var heavyResult = followerService.Recruit(MichaelFactionId, DefenderTier.Heavy);
                Assert.True(heavyResult.Success);
                heavyFollowers.Add(heavyResult.Follower!);
            }

            Assert.Equal(6, followerService.GetFollowerCount(MichaelFactionId));

            // Simulate combat where lower tier followers die first
            // Basic followers die
            foreach (var basic in basicFollowers)
            {
                followerService.HandleFollowerDeath(basic.Id);
            }

            Assert.Equal(4, followerService.GetFollowerCount(MichaelFactionId));

            // Medium followers die
            foreach (var medium in mediumFollowers)
            {
                followerService.HandleFollowerDeath(medium.Id);
            }

            Assert.Equal(2, followerService.GetFollowerCount(MichaelFactionId));

            // Only heavy followers remain
            var survivors = followerService.GetFollowers(MichaelFactionId);
            Assert.All(survivors, f => Assert.Equal(DefenderTier.Heavy, f.Tier));
        }

        #endregion

        #region Scenario: Character Switching (Switch Character → Faction Changes, Followers Dismissed)

        /// <summary>
        /// End-to-end test for complete character switching flow:
        /// 1. Player starts as Michael with followers
        /// 2. Player switches to Trevor
        /// 3. Faction changes from Michael to Trevor
        /// 4. Michael's followers are automatically dismissed
        /// 5. Trevor can recruit new followers
        /// </summary>
        [Fact]
        public void EndToEnd_CharacterSwitching_FactionChangesAndFollowersDismissed()
        {
            // Arrange: Set up game world with full service wiring
            var gameBridge = new MockGameBridge();
            gameBridge.PlayerCharacterModel = "player_zero"; // Michael
            gameBridge.PlayerPosition = new Vector3(100, 100, 0);
            gameBridge.PlayerMoney = 50000; // Plenty of money for followers

            var container = ServiceContainerFactory.Create(gameBridge, new MockMenuProvider());
            var controller = new GameLoopController(container);

            // Initialize by calling first tick
            controller.OnTick();

            // STEP 1: Verify initial faction is Michael
            Assert.Equal("michael", controller.CurrentPlayerFactionId);
            Assert.True(controller.IsInitialized);

            // STEP 2: Recruit followers for Michael
            var followerService = container.Resolve<IFollowerService>();

            var basicResult = followerService.Recruit("michael", DefenderTier.Basic);
            var mediumResult = followerService.Recruit("michael", DefenderTier.Medium);
            var heavyResult = followerService.Recruit("michael", DefenderTier.Heavy);

            Assert.True(basicResult.Success, "Should recruit basic follower for Michael");
            Assert.True(mediumResult.Success, "Should recruit medium follower for Michael");
            Assert.True(heavyResult.Success, "Should recruit heavy follower for Michael");
            Assert.Equal(3, followerService.GetFollowerCount("michael"));

            // STEP 3: Subscribe to character switch event to verify it fires
            string? oldFactionFromEvent = null;
            string? newFactionFromEvent = null;
            int switchEventCount = 0;

            controller.OnCharacterSwitched += (oldFaction, newFaction) =>
            {
                oldFactionFromEvent = oldFaction;
                newFactionFromEvent = newFaction;
                switchEventCount++;
            };

            // STEP 4: Switch to Trevor
            gameBridge.PlayerCharacterModel = "player_two"; // Trevor
            controller.OnTick();

            // STEP 5: Verify faction changed
            Assert.Equal("trevor", controller.CurrentPlayerFactionId);
            Assert.Equal(1, switchEventCount);
            Assert.Equal("michael", oldFactionFromEvent);
            Assert.Equal("trevor", newFactionFromEvent);

            // STEP 6: Verify Michael's followers were dismissed
            Assert.Equal(0, followerService.GetFollowerCount("michael"));
            Assert.Empty(followerService.GetFollowers("michael"));

            // STEP 7: Verify a notification was shown
            Assert.True(gameBridge.Notifications.Count > 0, "Should show notification on character switch");
            Assert.True(gameBridge.Notifications.Any(n =>
                n.Contains("Trevor") || n.Contains("faction") || n.Contains("switch")),
                "Notification should mention Trevor or faction switch");

            // STEP 8: Trevor can recruit new followers
            var trevorBasic = followerService.Recruit("trevor", DefenderTier.Basic);
            var trevorMedium = followerService.Recruit("trevor", DefenderTier.Medium);

            Assert.True(trevorBasic.Success, "Trevor should be able to recruit basic follower");
            Assert.True(trevorMedium.Success, "Trevor should be able to recruit medium follower");
            Assert.Equal(2, followerService.GetFollowerCount("trevor"));
            Assert.Equal(0, followerService.GetFollowerCount("michael")); // Still 0 for Michael
        }

        /// <summary>
        /// Tests multiple character switches in sequence, verifying faction changes correctly each time.
        /// </summary>
        [Fact]
        public void EndToEnd_CharacterSwitching_MultipleSwitch_FactionUpdatesEachTime()
        {
            // Arrange
            var gameBridge = new MockGameBridge();
            gameBridge.PlayerCharacterModel = "player_zero"; // Michael
            gameBridge.PlayerPosition = new Vector3(100, 100, 0);

            var container = ServiceContainerFactory.Create(gameBridge, new MockMenuProvider());
            var controller = new GameLoopController(container);

            var factionHistory = new List<(string? old, string? @new)>();
            controller.OnCharacterSwitched += (old, @new) => factionHistory.Add((old, @new));

            // Initialize
            controller.OnTick();
            Assert.Equal("michael", controller.CurrentPlayerFactionId);

            // Act: Michael -> Franklin -> Trevor -> Michael
            gameBridge.PlayerCharacterModel = "player_one"; // Franklin
            controller.OnTick();
            Assert.Equal("franklin", controller.CurrentPlayerFactionId);

            gameBridge.PlayerCharacterModel = "player_two"; // Trevor
            controller.OnTick();
            Assert.Equal("trevor", controller.CurrentPlayerFactionId);

            gameBridge.PlayerCharacterModel = "player_zero"; // Michael
            controller.OnTick();
            Assert.Equal("michael", controller.CurrentPlayerFactionId);

            // Assert: All switches tracked correctly
            Assert.Equal(3, factionHistory.Count);
            Assert.Equal(("michael", "franklin"), factionHistory[0]);
            Assert.Equal(("franklin", "trevor"), factionHistory[1]);
            Assert.Equal(("trevor", "michael"), factionHistory[2]);
        }

        /// <summary>
        /// Tests that followers for all factions are dismissed independently when switching.
        /// </summary>
        [Fact]
        public void EndToEnd_CharacterSwitching_FollowersPerFaction_DismissedOnSwitch()
        {
            // Arrange
            var gameBridge = new MockGameBridge();
            gameBridge.PlayerCharacterModel = "player_zero"; // Michael
            gameBridge.PlayerPosition = new Vector3(100, 100, 0);
            gameBridge.PlayerMoney = 100000;

            var container = ServiceContainerFactory.Create(gameBridge, new MockMenuProvider());
            var controller = new GameLoopController(container);
            controller.OnTick();

            var followerService = container.Resolve<IFollowerService>();

            // STEP 1: Recruit followers for Michael
            followerService.Recruit("michael", DefenderTier.Basic);
            followerService.Recruit("michael", DefenderTier.Heavy);
            Assert.Equal(2, followerService.GetFollowerCount("michael"));

            // STEP 2: Switch to Trevor
            gameBridge.PlayerCharacterModel = "player_two"; // Trevor
            controller.OnTick();

            // Michael's followers dismissed
            Assert.Equal(0, followerService.GetFollowerCount("michael"));

            // STEP 3: Recruit followers for Trevor
            followerService.Recruit("trevor", DefenderTier.Medium);
            followerService.Recruit("trevor", DefenderTier.Heavy);
            followerService.Recruit("trevor", DefenderTier.Heavy);
            Assert.Equal(3, followerService.GetFollowerCount("trevor"));

            // STEP 4: Switch to Franklin
            gameBridge.PlayerCharacterModel = "player_one"; // Franklin
            controller.OnTick();

            // Trevor's followers dismissed, Michael's stay at 0
            Assert.Equal(0, followerService.GetFollowerCount("trevor"));
            Assert.Equal(0, followerService.GetFollowerCount("michael"));

            // STEP 5: Recruit followers for Franklin
            followerService.Recruit("franklin", DefenderTier.Basic);
            Assert.Equal(1, followerService.GetFollowerCount("franklin"));

            // STEP 6: Switch back to Michael
            gameBridge.PlayerCharacterModel = "player_zero"; // Michael
            controller.OnTick();

            // Franklin's followers dismissed
            Assert.Equal(0, followerService.GetFollowerCount("franklin"));

            // Michael can recruit again
            followerService.Recruit("michael", DefenderTier.Medium);
            Assert.Equal(1, followerService.GetFollowerCount("michael"));
        }

        /// <summary>
        /// Tests that no switch event fires when character stays the same.
        /// </summary>
        [Fact]
        public void EndToEnd_CharacterSwitching_NoSwitch_NoEventFired()
        {
            // Arrange
            var gameBridge = new MockGameBridge();
            gameBridge.PlayerCharacterModel = "player_zero"; // Michael
            gameBridge.PlayerPosition = new Vector3(100, 100, 0);
            gameBridge.PlayerMoney = 50000;

            var container = ServiceContainerFactory.Create(gameBridge, new MockMenuProvider());
            var controller = new GameLoopController(container);

            var followerService = container.Resolve<IFollowerService>();
            followerService.Recruit("michael", DefenderTier.Basic);
            followerService.Recruit("michael", DefenderTier.Heavy);

            int switchCount = 0;
            controller.OnCharacterSwitched += (_, _) => switchCount++;

            // Initialize
            controller.OnTick();
            Assert.Equal("michael", controller.CurrentPlayerFactionId);
            Assert.Equal(2, followerService.GetFollowerCount("michael"));

            // Act: Multiple ticks with same character
            for (int i = 0; i < 10; i++)
            {
                controller.OnTick();
            }

            // Assert: No switches, followers remain
            Assert.Equal(0, switchCount);
            Assert.Equal("michael", controller.CurrentPlayerFactionId);
            Assert.Equal(2, followerService.GetFollowerCount("michael"));
        }

        #endregion

        #region Scenario: Persistence (Save → Load → State Preserved)

        /// <summary>
        /// End-to-end test for complete persistence flow:
        /// 1. Player modifies game state (zones, factions, resources, relationships)
        /// 2. Player saves the game to a slot
        /// 3. Game state is cleared/reset
        /// 4. Player loads the game from the slot
        /// 5. All game state is correctly restored
        /// </summary>
        [Fact]
        public void EndToEnd_Persistence_SaveLoadStatePreserved()
        {
            // Arrange: Set up game world with all systems wired together
            var gameBridge = new MockGameBridge();
            gameBridge.PlayerCharacterModel = "player_zero"; // Michael
            gameBridge.PlayerPosition = new Vector3(100, 100, 0);
            gameBridge.PlayerMoney = 50000;

            var container = ServiceContainerFactory.Create(gameBridge, new MockMenuProvider());
            var controller = new GameLoopController(container);

            // Initialize by calling first tick
            controller.OnTick();

            // Get services for verification
            var factionService = container.Resolve<IFactionService>();
            var zoneRepo = container.Resolve<IZoneRepository>();
            var zoneService = container.Resolve<IZoneService>();
            var relationshipService = container.Resolve<IFactionRelationshipService>();
            var gameStateManager = container.Resolve<IGameStateManager>();

            // STEP 1: Modify game state - simulate gameplay
            // Michael captures territory from Trevor
            var trevorZone = zoneRepo.GetAll().First(z => z.OwnerFactionId == "trevor");
            string capturedZoneId = trevorZone.Id;
            string capturedZoneName = trevorZone.Name;

            // Transfer zone ownership
            trevorZone.OwnerFactionId = "michael";
            trevorZone.ControlPercentage = 100f;
            zoneRepo.Update(trevorZone);
            factionService.RemoveZoneFromFaction("trevor", capturedZoneId);
            factionService.AddZoneToFaction("michael", capturedZoneId);

            // Add resources to Michael's faction
            var michaelState = factionService.GetFactionState("michael");
            Assert.NotNull(michaelState);
            int originalCash = michaelState!.Cash;
            int bonusCash = 5000;
            michaelState.Cash += bonusCash;

            // Add troops to reserve pool by tier
            michaelState.AddReserveTroops(DefenderTier.Basic, 25);
            michaelState.AddReserveTroops(DefenderTier.Medium, 15);
            michaelState.AddReserveTroops(DefenderTier.Heavy, 5);

            // Track Trevor state for verification
            var trevorState = factionService.GetFactionState("trevor");
            Assert.NotNull(trevorState);
            int trevorOriginalCash = trevorState!.Cash;

            // Record state before save
            int michaelZonesBefore = factionService.GetZoneCount("michael");
            int trevorZonesBefore = factionService.GetZoneCount("trevor");
            int michaelCashBefore = factionService.GetFactionState("michael")!.Cash;

            // STEP 2: Save the game
            gameStateManager.NewGame(); // Mark as having a loaded game
            bool saveSuccess = false;
            gameStateManager.OnGameSaved += (s, e) => saveSuccess = e.Success;
            gameStateManager.SaveToSlot(0, "End-to-End Test Save");

            Assert.True(saveSuccess, "Save should succeed");
            Assert.Equal("End-to-End Test Save", gameStateManager.CurrentSaveName);

            // STEP 3: Modify state after save (simulating continued play)
            // Give Michael more resources that should NOT be preserved after load
            michaelState.Cash += 99999;
            michaelState.AddReserveTroops(DefenderTier.Heavy, 100);

            // Verify state changed
            Assert.Equal(michaelCashBefore + 99999, factionService.GetFactionState("michael")!.Cash);

            // STEP 4: Load the saved game
            bool loadSuccess = false;
            gameStateManager.OnGameLoaded += (s, e) => loadSuccess = e.Success;
            gameStateManager.LoadFromSlot(0);

            Assert.True(loadSuccess, "Load should succeed");

            // STEP 5: Verify ALL state is correctly restored

            // 5a. Zone ownership restored
            var restoredMichaelZones = factionService.GetZoneCount("michael");
            var restoredTrevorZones = factionService.GetZoneCount("trevor");
            Assert.Equal(michaelZonesBefore, restoredMichaelZones);
            Assert.Equal(trevorZonesBefore, restoredTrevorZones);

            // 5b. The captured zone is still owned by Michael
            var restoredCapturedZone = zoneService.GetZone(capturedZoneId);
            Assert.NotNull(restoredCapturedZone);
            Assert.Equal("michael", restoredCapturedZone!.OwnerFactionId);

            // 5c. Michael's cash restored to pre-load value (not post-save modifications)
            var restoredMichaelState = factionService.GetFactionState("michael");
            Assert.NotNull(restoredMichaelState);
            Assert.Equal(michaelCashBefore, restoredMichaelState!.Cash);

            // 5d. Reserve pool by tier is preserved
            Assert.Equal(25, restoredMichaelState.GetReserveTroops(DefenderTier.Basic));
            Assert.Equal(15, restoredMichaelState.GetReserveTroops(DefenderTier.Medium));
            Assert.Equal(5, restoredMichaelState.GetReserveTroops(DefenderTier.Heavy));

            // 5e. Save name preserved
            Assert.Equal("End-to-End Test Save", gameStateManager.CurrentSaveName);
        }

        /// <summary>
        /// Tests that multiple save/load cycles maintain data integrity.
        /// </summary>
        [Fact]
        public void EndToEnd_Persistence_MultipleSaveLoadCycles_DataIntegrity()
        {
            // Arrange
            var gameBridge = new MockGameBridge();
            gameBridge.PlayerCharacterModel = "player_zero";
            gameBridge.PlayerPosition = new Vector3(100, 100, 0);

            var container = ServiceContainerFactory.Create(gameBridge, new MockMenuProvider());
            var controller = new GameLoopController(container);
            controller.OnTick();

            var factionService = container.Resolve<IFactionService>();
            var gameStateManager = container.Resolve<IGameStateManager>();
            gameStateManager.NewGame();

            // Initial state
            var initialMichaelState = factionService.GetFactionState("michael");
            Assert.NotNull(initialMichaelState);
            int initialCash = initialMichaelState!.Cash;

            // Perform multiple save/load cycles with modifications
            for (int cycle = 1; cycle <= 3; cycle++)
            {
                // Modify state
                var state = factionService.GetFactionState("michael");
                state!.Cash += 1000 * cycle;
                state.AddReserveTroops(DefenderTier.Basic, cycle * 10);

                int expectedCash = state.Cash;
                int expectedBasicTroops = state.GetReserveTroops(DefenderTier.Basic);

                // Save
                gameStateManager.SaveToSlot(cycle - 1, $"Cycle {cycle} Save");

                // Modify after save
                state.Cash = 999999;
                state.AddReserveTroops(DefenderTier.Heavy, 999);

                // Load
                gameStateManager.LoadFromSlot(cycle - 1);

                // Verify restoration
                var restoredState = factionService.GetFactionState("michael");
                Assert.NotNull(restoredState);
                Assert.Equal(expectedCash, restoredState!.Cash);
                Assert.Equal(expectedBasicTroops, restoredState.GetReserveTroops(DefenderTier.Basic));
            }
        }

        /// <summary>
        /// Tests that AI faction state is preserved through save/load.
        /// </summary>
        [Fact]
        public void EndToEnd_Persistence_AIFactionStatePreserved()
        {
            // Arrange
            var gameBridge = new MockGameBridge();
            gameBridge.PlayerCharacterModel = "player_zero"; // Michael is player
            gameBridge.PlayerPosition = new Vector3(100, 100, 0);

            var container = ServiceContainerFactory.Create(gameBridge, new MockMenuProvider());
            var controller = new GameLoopController(container);
            controller.OnTick();

            var factionService = container.Resolve<IFactionService>();
            var gameStateManager = container.Resolve<IGameStateManager>();
            gameStateManager.NewGame();

            // Modify AI factions (Trevor and Franklin)
            var trevorState = factionService.GetFactionState("trevor");
            Assert.NotNull(trevorState);
            trevorState!.Cash = 77777;
            trevorState.AddReserveTroops(DefenderTier.Heavy, 42);

            var franklinState = factionService.GetFactionState("franklin");
            Assert.NotNull(franklinState);
            franklinState!.Cash = 33333;
            franklinState.AddReserveTroops(DefenderTier.Medium, 28);

            // Save
            gameStateManager.SaveToSlot(0, "AI State Test");

            // Corrupt the state
            trevorState.Cash = 0;
            franklinState.Cash = 0;
            Assert.Equal(0, factionService.GetFactionState("trevor")!.Cash);

            // Load
            gameStateManager.LoadFromSlot(0);

            // Verify AI states restored
            var restoredTrevor = factionService.GetFactionState("trevor");
            var restoredFranklin = factionService.GetFactionState("franklin");

            Assert.NotNull(restoredTrevor);
            Assert.NotNull(restoredFranklin);
            Assert.Equal(77777, restoredTrevor!.Cash);
            Assert.Equal(42, restoredTrevor.GetReserveTroops(DefenderTier.Heavy));
            Assert.Equal(33333, restoredFranklin!.Cash);
            Assert.Equal(28, restoredFranklin.GetReserveTroops(DefenderTier.Medium));
        }

        /// <summary>
        /// Tests that zone traits and contested state are preserved through save/load.
        /// </summary>
        [Fact]
        public void EndToEnd_Persistence_ZoneStatePreserved()
        {
            // Arrange
            var gameBridge = new MockGameBridge();
            gameBridge.PlayerCharacterModel = "player_zero";
            gameBridge.PlayerPosition = new Vector3(100, 100, 0);

            var container = ServiceContainerFactory.Create(gameBridge, new MockMenuProvider());
            var controller = new GameLoopController(container);
            controller.OnTick();

            var zoneRepo = container.Resolve<IZoneRepository>();
            var zoneService = container.Resolve<IZoneService>();
            var gameStateManager = container.Resolve<IGameStateManager>();
            gameStateManager.NewGame();

            // Modify zone states
            var zones = zoneRepo.GetAll().Take(3).ToList();

            zones[0].IsContested = true;
            zones[0].ControlPercentage = 65.5f;
            zoneRepo.Update(zones[0]);

            zones[1].Traits = ZoneTrait.Commercial | ZoneTrait.HighValue;
            zoneRepo.Update(zones[1]);

            zones[2].Traits = ZoneTrait.Fortified;
            zones[2].ControlPercentage = 100f;
            zoneRepo.Update(zones[2]);

            string zone0Id = zones[0].Id;
            string zone1Id = zones[1].Id;
            string zone2Id = zones[2].Id;

            // Save
            gameStateManager.SaveToSlot(0, "Zone State Test");

            // Corrupt zone state
            zones[0].IsContested = false;
            zones[0].ControlPercentage = 100f;
            zoneRepo.Update(zones[0]);

            // Load
            gameStateManager.LoadFromSlot(0);

            // Verify zone states restored
            var restored0 = zoneService.GetZone(zone0Id);
            var restored1 = zoneService.GetZone(zone1Id);
            var restored2 = zoneService.GetZone(zone2Id);

            Assert.NotNull(restored0);
            Assert.True(restored0!.IsContested, "Zone 0 should be contested");
            Assert.Equal(65.5f, restored0.ControlPercentage, 1);

            Assert.NotNull(restored1);
            Assert.True(restored1!.Traits.HasFlag(ZoneTrait.Commercial));
            Assert.True(restored1.Traits.HasFlag(ZoneTrait.HighValue));

            Assert.NotNull(restored2);
            Assert.True(restored2!.Traits.HasFlag(ZoneTrait.Fortified));
            Assert.Equal(100f, restored2.ControlPercentage);
        }

        /// <summary>
        /// Tests that gameplay can continue normally after loading a save.
        /// </summary>
        [Fact]
        public void EndToEnd_Persistence_ContinueGameplayAfterLoad()
        {
            // Arrange
            var gameBridge = new MockGameBridge();
            gameBridge.PlayerCharacterModel = "player_zero";
            gameBridge.PlayerPosition = new Vector3(100, 100, 0);
            gameBridge.PlayerMoney = 10000;

            var container = ServiceContainerFactory.Create(gameBridge, new MockMenuProvider());
            var controller = new GameLoopController(container);
            controller.OnTick();

            var factionService = container.Resolve<IFactionService>();
            var zoneRepo = container.Resolve<IZoneRepository>();
            var gameStateManager = container.Resolve<IGameStateManager>();
            var defenderTierService = container.Resolve<IDefenderTierService>();
            var troopPurchaseService = container.Resolve<ITroopPurchaseService>();

            gameStateManager.NewGame();

            // Setup initial state
            var michaelState = factionService.GetFactionState("michael");
            Assert.NotNull(michaelState);
            michaelState!.Cash = 5000;

            // Save game
            gameStateManager.SaveToSlot(0, "Pre-Purchase Save");

            // Simulate game reset (e.g., game crash/restart)
            michaelState.Cash = 0;
            michaelState.AddReserveTroops(DefenderTier.Basic, 999);

            // Load saved game
            gameStateManager.LoadFromSlot(0);

            // CONTINUE GAMEPLAY: Purchase troops after load
            gameBridge.PlayerMoney = 5000; // Sync player money

            var purchaseResult = troopPurchaseService.PurchaseTroops("michael", DefenderTier.Basic, 5);

            // Verify purchase succeeds after load
            Assert.True(purchaseResult.Success, "Should be able to purchase troops after loading save");
            Assert.Equal(5, purchaseResult.TroopsPurchased);

            var restoredState = factionService.GetFactionState("michael");
            Assert.NotNull(restoredState);
            Assert.Equal(5, restoredState!.GetReserveTroops(DefenderTier.Basic));
        }

        #endregion

        #region Scenario: Victory Condition (Capture All → Victory Screen)

        /// <summary>
        /// End-to-end test for complete victory condition flow:
        /// 1. Three factions start with distributed zones
        /// 2. One faction progressively captures all zones from other factions
        /// 3. VictoryConditionService detects 100% control
        /// 4. VictoryManager triggers victory screen notification
        /// </summary>
        [Fact]
        public void EndToEnd_VictoryCondition_CaptureAllZones_TriggersVictoryScreen()
        {
            // Arrange: Set up game world with 3 factions and zones
            var zoneRepo = new InMemoryZoneRepository();
            var zoneService = new ZoneService(zoneRepo);
            var factionRepo = new InMemoryFactionRepository();
            var factionService = new FactionService(factionRepo);

            // Create factions
            var michael = SetupFaction(factionRepo, factionService, MichaelFactionId, "Michael's Crew", 10000, 100);
            var trevor = SetupFaction(factionRepo, factionService, TrevorFactionId, "Trevor's Gang", 5000, 30);
            var franklin = SetupFaction(factionRepo, factionService, FranklinFactionId, "Franklin's Family", 5000, 30);

            // Create zones - distributed among factions
            var zone1 = CreateZone(zoneRepo, "zone-1", "Vinewood", MichaelFactionId, 8);
            factionService.AddZoneToFaction(MichaelFactionId, zone1.Id);

            var zone2 = CreateZone(zoneRepo, "zone-2", "Sandy Shores", TrevorFactionId, 5);
            factionService.AddZoneToFaction(TrevorFactionId, zone2.Id);

            var zone3 = CreateZone(zoneRepo, "zone-3", "Grove Street", FranklinFactionId, 5);
            factionService.AddZoneToFaction(FranklinFactionId, zone3.Id);

            var zone4 = CreateZone(zoneRepo, "zone-4", "Del Perro", MichaelFactionId, 6);
            factionService.AddZoneToFaction(MichaelFactionId, zone4.Id);

            // Create VictoryConditionService
            var victoryConditionService = new VictoryConditionService(zoneService);

            // Create mock notification service to track victory notifications
            var notificationService = new MockNotificationService();

            // Create VictoryManager
            var victoryManager = new VictoryManager(
                victoryConditionService,
                factionService,
                notificationService);

            // Track victory event
            bool victoryEventRaised = false;
            string? winningFactionIdFromEvent = null;
            string? winningFactionNameFromEvent = null;
            victoryManager.OnVictory += (sender, args) =>
            {
                victoryEventRaised = true;
                winningFactionIdFromEvent = args.WinningFactionId;
                winningFactionNameFromEvent = args.WinningFactionName;
            };

            // Verify initial state
            Assert.False(victoryConditionService.IsGameOver(), "Game should not be over initially");
            Assert.Null(victoryConditionService.GetWinningFactionId());
            Assert.Equal(2, factionService.GetZoneCount(MichaelFactionId));
            Assert.Equal(1, factionService.GetZoneCount(TrevorFactionId));
            Assert.Equal(1, factionService.GetZoneCount(FranklinFactionId));

            // Verify initial victory progress
            float initialProgress = victoryConditionService.GetVictoryProgress(MichaelFactionId);
            Assert.Equal(50f, initialProgress, 1); // 2 of 4 zones = 50%

            // Start VictoryManager
            victoryManager.Start();
            Assert.True(victoryManager.IsRunning);
            Assert.False(victoryManager.IsVictoryAchieved);

            // Update - should not trigger victory yet
            victoryManager.Update(1.0f);
            Assert.False(victoryEventRaised, "Victory should not trigger yet");
            Assert.False(victoryManager.IsVictoryAchieved);

            // STEP 2: Michael captures Trevor's zone (Sandy Shores)
            zone2.OwnerFactionId = MichaelFactionId;
            zone2.ControlPercentage = 100f;
            zoneRepo.Update(zone2);
            factionService.RemoveZoneFromFaction(TrevorFactionId, zone2.Id);
            factionService.AddZoneToFaction(MichaelFactionId, zone2.Id);

            // Verify intermediate state
            Assert.Equal(3, factionService.GetZoneCount(MichaelFactionId));
            Assert.Equal(0, factionService.GetZoneCount(TrevorFactionId));
            Assert.Equal(75f, victoryConditionService.GetVictoryProgress(MichaelFactionId), 1);
            Assert.False(victoryConditionService.IsGameOver(), "Game should not be over yet");

            // Update VictoryManager - should not trigger yet
            victoryManager.Update(1.0f);
            Assert.False(victoryEventRaised);
            Assert.False(victoryManager.IsVictoryAchieved);

            // STEP 3: Michael captures Franklin's zone (Grove Street) - final zone
            zone3.OwnerFactionId = MichaelFactionId;
            zone3.ControlPercentage = 100f;
            zoneRepo.Update(zone3);
            factionService.RemoveZoneFromFaction(FranklinFactionId, zone3.Id);
            factionService.AddZoneToFaction(MichaelFactionId, zone3.Id);

            // Verify final state - Michael owns all zones
            Assert.Equal(4, factionService.GetZoneCount(MichaelFactionId));
            Assert.Equal(0, factionService.GetZoneCount(TrevorFactionId));
            Assert.Equal(0, factionService.GetZoneCount(FranklinFactionId));
            Assert.Equal(100f, victoryConditionService.GetVictoryProgress(MichaelFactionId), 1);

            // Victory condition should be met
            Assert.True(victoryConditionService.IsGameOver(), "Game should be over");
            Assert.Equal(MichaelFactionId, victoryConditionService.GetWinningFactionId());

            // VictoryCheckResult should indicate victory
            var victoryResult = victoryConditionService.CheckVictoryCondition(MichaelFactionId);
            Assert.True(victoryResult.IsVictory);
            Assert.Equal(4, victoryResult.ZonesOwned);
            Assert.Equal(4, victoryResult.TotalZones);
            Assert.Equal(100f, victoryResult.ControlPercentage, 1);

            // STEP 4: VictoryManager should trigger victory screen on next update
            victoryManager.Update(1.0f);

            // Assert: Victory event was raised
            Assert.True(victoryEventRaised, "Victory event should have been raised");
            Assert.Equal(MichaelFactionId, winningFactionIdFromEvent);
            Assert.Equal("Michael's Crew", winningFactionNameFromEvent);

            // Assert: VictoryManager state updated
            Assert.True(victoryManager.IsVictoryAchieved);
            Assert.Equal(MichaelFactionId, victoryManager.GetWinningFactionId());

            // Assert: Victory notification was displayed
            Assert.True(notificationService.DisplayedNotifications.Count > 0,
                "Victory notification should have been displayed");
            var victoryNotification = notificationService.DisplayedNotifications
                .First(n => n.Title == "Victory!");
            Assert.NotNull(victoryNotification);
            Assert.Equal(NotificationType.Success, victoryNotification.Type);
            Assert.Equal(NotificationPriority.Critical, victoryNotification.Priority);
            Assert.Contains("Michael's Crew", victoryNotification.Message);
            Assert.Contains("total control", victoryNotification.Message);
        }

        /// <summary>
        /// End-to-end test verifying victory screen does not trigger when conditions not met.
        /// </summary>
        [Fact]
        public void EndToEnd_VictoryCondition_PartialControl_NoVictoryScreen()
        {
            // Arrange: Set up game world with distributed zones
            var zoneRepo = new InMemoryZoneRepository();
            var zoneService = new ZoneService(zoneRepo);
            var factionRepo = new InMemoryFactionRepository();
            var factionService = new FactionService(factionRepo);

            SetupFaction(factionRepo, factionService, MichaelFactionId, "Michael's Crew", 10000, 50);
            SetupFaction(factionRepo, factionService, TrevorFactionId, "Trevor's Gang", 8000, 40);
            SetupFaction(factionRepo, factionService, FranklinFactionId, "Franklin's Family", 6000, 30);

            // Create 5 zones - distributed so no one has majority
            var zone1 = CreateZone(zoneRepo, "zone-1", "Vinewood", MichaelFactionId, 8);
            factionService.AddZoneToFaction(MichaelFactionId, zone1.Id);

            var zone2 = CreateZone(zoneRepo, "zone-2", "Sandy Shores", TrevorFactionId, 5);
            factionService.AddZoneToFaction(TrevorFactionId, zone2.Id);

            var zone3 = CreateZone(zoneRepo, "zone-3", "Grove Street", FranklinFactionId, 5);
            factionService.AddZoneToFaction(FranklinFactionId, zone3.Id);

            var zone4 = CreateZone(zoneRepo, "zone-4", "Del Perro", MichaelFactionId, 6);
            factionService.AddZoneToFaction(MichaelFactionId, zone4.Id);

            var zone5 = CreateZone(zoneRepo, "zone-5", "Paleto Bay", TrevorFactionId, 4);
            factionService.AddZoneToFaction(TrevorFactionId, zone5.Id);

            var victoryConditionService = new VictoryConditionService(zoneService);
            var notificationService = new MockNotificationService();
            var victoryManager = new VictoryManager(
                victoryConditionService,
                factionService,
                notificationService);

            bool victoryEventRaised = false;
            victoryManager.OnVictory += (sender, args) => victoryEventRaised = true;

            victoryManager.Start();

            // Act: Update multiple times
            for (int i = 0; i < 10; i++)
            {
                victoryManager.Update(1.0f);
            }

            // Assert: No victory
            Assert.False(victoryEventRaised);
            Assert.False(victoryManager.IsVictoryAchieved);
            Assert.Null(victoryManager.GetWinningFactionId());
            Assert.False(victoryConditionService.IsGameOver());

            // No victory notifications
            Assert.DoesNotContain(notificationService.DisplayedNotifications,
                n => n.Title == "Victory!");
        }

        /// <summary>
        /// End-to-end test verifying victory can only be detected once.
        /// </summary>
        [Fact]
        public void EndToEnd_VictoryCondition_VictoryOnlyTriggersOnce()
        {
            // Arrange: Set up game world where Michael owns all zones
            var zoneRepo = new InMemoryZoneRepository();
            var zoneService = new ZoneService(zoneRepo);
            var factionRepo = new InMemoryFactionRepository();
            var factionService = new FactionService(factionRepo);

            var michael = SetupFaction(factionRepo, factionService, MichaelFactionId, "Michael's Crew", 10000, 100);

            // Michael starts owning all zones
            var zone1 = CreateZone(zoneRepo, "zone-1", "Vinewood", MichaelFactionId, 8);
            factionService.AddZoneToFaction(MichaelFactionId, zone1.Id);

            var zone2 = CreateZone(zoneRepo, "zone-2", "Sandy Shores", MichaelFactionId, 5);
            factionService.AddZoneToFaction(MichaelFactionId, zone2.Id);

            var zone3 = CreateZone(zoneRepo, "zone-3", "Grove Street", MichaelFactionId, 5);
            factionService.AddZoneToFaction(MichaelFactionId, zone3.Id);

            var victoryConditionService = new VictoryConditionService(zoneService);
            var notificationService = new MockNotificationService();
            var victoryManager = new VictoryManager(
                victoryConditionService,
                factionService,
                notificationService);

            int victoryEventCount = 0;
            victoryManager.OnVictory += (sender, args) => victoryEventCount++;

            victoryManager.Start();

            // Act: Update multiple times - victory should trigger on first check
            for (int i = 0; i < 10; i++)
            {
                victoryManager.Update(1.0f);
            }

            // Assert: Victory was triggered exactly once
            Assert.Equal(1, victoryEventCount);
            Assert.True(victoryManager.IsVictoryAchieved);

            // Only one victory notification
            var victoryNotifications = notificationService.DisplayedNotifications
                .Where(n => n.Title == "Victory!")
                .ToList();
            Assert.Single(victoryNotifications);
        }

        /// <summary>
        /// End-to-end test verifying VictoryManager can be reset and detect victory again.
        /// </summary>
        [Fact]
        public void EndToEnd_VictoryCondition_ResetAllowsNewVictoryDetection()
        {
            // Arrange: Set up game world where Michael owns all zones
            var zoneRepo = new InMemoryZoneRepository();
            var zoneService = new ZoneService(zoneRepo);
            var factionRepo = new InMemoryFactionRepository();
            var factionService = new FactionService(factionRepo);

            SetupFaction(factionRepo, factionService, MichaelFactionId, "Michael's Crew", 10000, 100);
            SetupFaction(factionRepo, factionService, TrevorFactionId, "Trevor's Gang", 5000, 30);

            var zone1 = CreateZone(zoneRepo, "zone-1", "Vinewood", MichaelFactionId, 8);
            factionService.AddZoneToFaction(MichaelFactionId, zone1.Id);

            var zone2 = CreateZone(zoneRepo, "zone-2", "Sandy Shores", MichaelFactionId, 5);
            factionService.AddZoneToFaction(MichaelFactionId, zone2.Id);

            var victoryConditionService = new VictoryConditionService(zoneService);
            var notificationService = new MockNotificationService();
            var victoryManager = new VictoryManager(
                victoryConditionService,
                factionService,
                notificationService);

            int victoryEventCount = 0;
            victoryManager.OnVictory += (sender, args) => victoryEventCount++;

            victoryManager.Start();

            // First victory
            victoryManager.Update(1.0f);
            Assert.Equal(1, victoryEventCount);
            Assert.True(victoryManager.IsVictoryAchieved);

            // Reset (simulating new game or load)
            victoryManager.Reset();
            Assert.False(victoryManager.IsVictoryAchieved);
            Assert.Null(victoryManager.GetWinningFactionId());

            // Game state still shows victory condition met
            Assert.True(victoryConditionService.IsGameOver());

            // Act: Update after reset
            victoryManager.Update(1.0f);

            // Assert: Victory triggered again
            Assert.Equal(2, victoryEventCount);
            Assert.True(victoryManager.IsVictoryAchieved);
        }

        #endregion

        #region Scenario: Zone Attack Notification (AI Attacks Player Zone → Notification → Waypoint)

        /// <summary>
        /// End-to-end test for zone attack notification flow:
        /// 1. Player owns a zone (player faction = Michael)
        /// 2. AI faction (Trevor) decides to attack the player's zone
        /// 3. Zone attack notification service triggers notification
        /// 4. Player can set a waypoint to the attacked zone
        /// </summary>
        [Fact]
        public void EndToEnd_ZoneAttackNotification_AIAttacksPlayerZone_NotificationAndWaypoint()
        {
            // Arrange: Set up game world with all services
            var gameBridge = new MockGameBridge();
            gameBridge.PlayerCharacterModel = "player_zero"; // Michael
            gameBridge.PlayerPosition = new Vector3(500, 500, 0); // Player is away from the attacked zone

            var zoneRepo = new InMemoryZoneRepository();
            var zoneService = new ZoneService(zoneRepo);
            var factionRepo = new InMemoryFactionRepository();
            var factionService = new FactionService(factionRepo);

            // Set up factions
            SetupFaction(factionRepo, factionService, MichaelFactionId, "Michael's Crew", 10000, 50);
            SetupFaction(factionRepo, factionService, TrevorFactionId, "Trevor's Gang", 8000, 60);

            // Create player-owned zone (Michael's zone that will be attacked)
            var playerZone = new Zone("zone-downtown", "Downtown", new Vector3(100, 100, 0), 200f, 8);
            playerZone.OwnerFactionId = MichaelFactionId;
            playerZone.ControlPercentage = 100f;
            zoneRepo.Add(playerZone);
            factionService.AddZoneToFaction(MichaelFactionId, playerZone.Id);

            // Create Trevor's home zone
            var trevorZone = new Zone("zone-sandy", "Sandy Shores", new Vector3(1000, 1000, 0), 200f, 5);
            trevorZone.OwnerFactionId = TrevorFactionId;
            trevorZone.ControlPercentage = 100f;
            zoneRepo.Add(trevorZone);
            factionService.AddZoneToFaction(TrevorFactionId, trevorZone.Id);

            // Set up notification service (mock for testing)
            var notificationService = new MockNotificationService();

            // Set up Zone Attack Notification Service
            var zoneAttackNotificationService = new ZoneAttackNotificationService(notificationService, gameBridge);

            // Set up AI Manager with Trevor's aggressive strategy
            var strategies = new Dictionary<string, IAIStrategy>
            {
                { MichaelFactionId, new MichaelAIStrategy() },
                { TrevorFactionId, new TrevorAIStrategy() }
            };

            var aiManager = new AIManager(factionService, zoneService, strategies);
            aiManager.SetPlayerFactionId(MichaelFactionId); // Player is Michael

            // Track AI decision to attack
            AIDecision? attackDecision = null;
            aiManager.OnAIDecision += (sender, args) =>
            {
                if (args.Decision.DecisionType == AIDecisionType.Attack &&
                    args.Decision.TargetZoneId == playerZone.Id)
                {
                    attackDecision = args.Decision;

                    // When AI decides to attack player's zone, trigger notification
                    zoneAttackNotificationService.NotifyZoneUnderAttack(playerZone, args.FactionId);
                }
            };

            // Verify initial state
            Assert.False(zoneAttackNotificationService.HasActiveZoneAttackNotification);
            Assert.Null(zoneAttackNotificationService.ActiveAttackedZoneId);
            Assert.False(zoneAttackNotificationService.HasWaypointSet);
            Assert.False(gameBridge.HasWaypointSet);

            // Act: AI makes decisions (Trevor should decide to attack Michael's high-value zone)
            aiManager.Start();
            aiManager.ForceDecision();

            // Assert: Notification was triggered
            Assert.NotNull(attackDecision);
            Assert.Equal(AIDecisionType.Attack, attackDecision!.DecisionType);
            Assert.Equal(playerZone.Id, attackDecision.TargetZoneId);

            Assert.True(zoneAttackNotificationService.HasActiveZoneAttackNotification,
                "Should have active zone attack notification");
            Assert.Equal(playerZone.Id, zoneAttackNotificationService.ActiveAttackedZoneId);

            // Verify notification was shown via notification service
            Assert.True(notificationService.DisplayedNotifications.Count > 0,
                "Notification should be displayed");
            var notification = notificationService.DisplayedNotifications[0];
            Assert.Contains("Under Attack", notification.Title);
            Assert.Contains("Downtown", notification.Message);
            Assert.Equal(NotificationPriority.Critical, notification.Priority);

            // Act: Player sets waypoint to the attacked zone
            bool waypointSet = zoneAttackNotificationService.SetWaypointToAttackedZone();

            // Assert: Waypoint was set
            Assert.True(waypointSet, "Waypoint should be set successfully");
            Assert.True(zoneAttackNotificationService.HasWaypointSet);
            Assert.True(gameBridge.HasWaypointSet);
            Assert.Equal(playerZone.Center, gameBridge.WaypointPosition);

            // Act: Player clears the waypoint
            zoneAttackNotificationService.ClearWaypoint();

            // Assert: Waypoint was cleared
            Assert.False(zoneAttackNotificationService.HasWaypointSet);
            Assert.False(gameBridge.HasWaypointSet);
            Assert.Null(gameBridge.WaypointPosition);

            // Act: Clear the notification (battle resolved)
            zoneAttackNotificationService.ClearActiveNotification();

            // Assert: Notification cleared
            Assert.False(zoneAttackNotificationService.HasActiveZoneAttackNotification);
            Assert.Null(zoneAttackNotificationService.ActiveAttackedZoneId);

            // Verify cannot set waypoint after notification cleared
            bool waypointAfterClear = zoneAttackNotificationService.SetWaypointToAttackedZone();
            Assert.False(waypointAfterClear, "Should not be able to set waypoint after notification cleared");
        }

        /// <summary>
        /// Tests that multiple zone attack notifications are handled correctly (latest replaces previous).
        /// </summary>
        [Fact]
        public void EndToEnd_ZoneAttackNotification_MultipleAttacks_LatestTakesPriority()
        {
            // Arrange
            var gameBridge = new MockGameBridge();
            var notificationService = new MockNotificationService();
            var zoneAttackNotificationService = new ZoneAttackNotificationService(notificationService, gameBridge);

            var zone1 = new Zone("zone-1", "Downtown", new Vector3(100, 100, 0), 200f, 8);
            zone1.OwnerFactionId = MichaelFactionId;

            var zone2 = new Zone("zone-2", "Vinewood", new Vector3(300, 300, 0), 200f, 7);
            zone2.OwnerFactionId = MichaelFactionId;

            // Act: First attack notification
            zoneAttackNotificationService.NotifyZoneUnderAttack(zone1, TrevorFactionId);

            // Assert: First notification active
            Assert.Equal("zone-1", zoneAttackNotificationService.ActiveAttackedZoneId);
            Assert.Equal(1, notificationService.DisplayedNotifications.Count);

            // Act: Second attack notification (replaces first)
            zoneAttackNotificationService.NotifyZoneUnderAttack(zone2, FranklinFactionId);

            // Assert: Second notification now active (replaced first)
            Assert.Equal("zone-2", zoneAttackNotificationService.ActiveAttackedZoneId);
            Assert.Equal(2, notificationService.DisplayedNotifications.Count);

            // Verify waypoint goes to latest attacked zone
            zoneAttackNotificationService.SetWaypointToAttackedZone();
            Assert.Equal(zone2.Center, gameBridge.WaypointPosition);
        }

        /// <summary>
        /// Tests that GetActiveAttackedZone returns the correct zone when active.
        /// </summary>
        [Fact]
        public void EndToEnd_ZoneAttackNotification_GetActiveAttackedZone_ReturnsCorrectZone()
        {
            // Arrange
            var gameBridge = new MockGameBridge();
            var notificationService = new MockNotificationService();
            var zoneAttackNotificationService = new ZoneAttackNotificationService(notificationService, gameBridge);

            var zone = new Zone("zone-airport", "Los Santos International", new Vector3(200, 200, 0), 300f, 9);
            zone.OwnerFactionId = MichaelFactionId;

            // Act & Assert: No active zone initially
            Assert.Null(zoneAttackNotificationService.GetActiveAttackedZone());

            // Act: Trigger notification
            zoneAttackNotificationService.NotifyZoneUnderAttack(zone, TrevorFactionId);

            // Assert: Can retrieve the zone
            var activeZone = zoneAttackNotificationService.GetActiveAttackedZone();
            Assert.NotNull(activeZone);
            Assert.Equal("zone-airport", activeZone!.Id);
            Assert.Equal("Los Santos International", activeZone.Name);
            Assert.Equal(new Vector3(200, 200, 0), activeZone.Center);
        }

        /// <summary>
        /// Tests the complete flow from AI attack decision to notification to defending the zone.
        /// </summary>
        [Fact]
        public void EndToEnd_ZoneAttackNotification_CompleteFlow_AttackNotifyDefend()
        {
            // Arrange: Set up a complete scenario with combat resolution
            var gameBridge = new MockGameBridge();
            gameBridge.PlayerCharacterModel = "player_zero"; // Michael
            gameBridge.PlayerPosition = new Vector3(1000, 1000, 0); // Far from zone

            var zoneRepo = new InMemoryZoneRepository();
            var zoneService = new ZoneService(zoneRepo);
            var factionRepo = new InMemoryFactionRepository();
            var factionService = new FactionService(factionRepo);

            SetupFaction(factionRepo, factionService, MichaelFactionId, "Michael's Crew", 10000, 50);
            SetupFaction(factionRepo, factionService, TrevorFactionId, "Trevor's Gang", 8000, 60);

            // Player's zone that will be attacked
            var playerZone = new Zone("zone-player", "Player HQ", new Vector3(100, 100, 0), 200f, 10);
            playerZone.OwnerFactionId = MichaelFactionId;
            playerZone.ControlPercentage = 100f;
            zoneRepo.Add(playerZone);
            factionService.AddZoneToFaction(MichaelFactionId, playerZone.Id);

            // Trevor's zone
            var trevorZone = new Zone("zone-trevor", "Trevor's Base", new Vector3(800, 800, 0), 200f, 5);
            trevorZone.OwnerFactionId = TrevorFactionId;
            zoneRepo.Add(trevorZone);
            factionService.AddZoneToFaction(TrevorFactionId, trevorZone.Id);

            var notificationService = new MockNotificationService();
            var zoneAttackNotificationService = new ZoneAttackNotificationService(notificationService, gameBridge);

            // STEP 1: AI decides to attack (simulated directly)
            // In real game, this would come from AIManager via OnAIDecision event
            zoneAttackNotificationService.NotifyZoneUnderAttack(playerZone, TrevorFactionId);

            // Verify notification is active
            Assert.True(zoneAttackNotificationService.HasActiveZoneAttackNotification);
            Assert.True(notificationService.DisplayedNotifications.Any(n =>
                n.Title.Contains("Under Attack") && n.Message.Contains("Player HQ")));

            // STEP 2: Player sets waypoint and travels to zone
            zoneAttackNotificationService.SetWaypointToAttackedZone();
            Assert.True(gameBridge.HasWaypointSet);
            Assert.Equal(playerZone.Center, gameBridge.WaypointPosition);

            // STEP 3: Player "arrives" at zone (position update)
            gameBridge.PlayerPosition = playerZone.Center;

            // STEP 4: Player successfully defends zone (simulated - zone stays with Michael)
            playerZone.IsContested = false;
            playerZone.ControlPercentage = 100f;
            zoneRepo.Update(playerZone);

            // STEP 5: Clear notification after successful defense
            zoneAttackNotificationService.ClearActiveNotification();
            zoneAttackNotificationService.ClearWaypoint();

            // Final assertions
            Assert.False(zoneAttackNotificationService.HasActiveZoneAttackNotification);
            Assert.False(gameBridge.HasWaypointSet);
            Assert.Equal(MichaelFactionId, zoneService.GetZone(playerZone.Id)!.OwnerFactionId);
        }

        #endregion

        #region Helper Methods

        private (InMemoryZoneRepository, ZoneService, InMemoryFactionRepository, FactionService, CombatResultHandler, ResourceTickService)
            CreateGameWorld()
        {
            var zoneRepo = new InMemoryZoneRepository();
            var zoneService = new ZoneService(zoneRepo);
            var factionRepo = new InMemoryFactionRepository();
            var factionService = new FactionService(factionRepo);
            var combatHandler = new CombatResultHandler(zoneService);
            var resourceModifier = new ZoneTraitResourceModifier();
            var supplyLineService = new SupplyLineService(zoneService);
            var tickService = new ResourceTickService(factionService, zoneService, resourceModifier, supplyLineService, 300);

            return (zoneRepo, zoneService, factionRepo, factionService, combatHandler, tickService);
        }

        private Faction SetupFaction(InMemoryFactionRepository repo, IFactionService service,
            string id, string name, int cash, int troops)
        {
            var faction = new Faction(id, name);
            repo.Add(faction);
            service.InitializeFactionState(id, cash, troops);
            return faction;
        }

        private Zone CreateZone(InMemoryZoneRepository repo, string id, string name,
            string? ownerId, int strategicValue)
        {
            var zone = new Zone(id, name, new Vector3(0, 0, 0), 150f, strategicValue);
            zone.OwnerFactionId = ownerId;
            zone.ControlPercentage = 100f;
            repo.Add(zone);
            return zone;
        }

        #endregion
    }

    /// <summary>
    /// Mock notification service for testing event alerts.
    /// </summary>
    internal class MockNotificationService : INotificationService
    {
        public List<Notification> DisplayedNotifications { get; } = new List<Notification>();

        public int ActiveNotificationCount => DisplayedNotifications.Count;
        public int QueuedNotificationCount => 0;

        public void Show(Notification notification)
        {
            if (notification == null)
                throw new ArgumentNullException(nameof(notification));
            DisplayedNotifications.Add(notification);
        }

        public void ShowInfo(string title, string message, NotificationPriority priority = NotificationPriority.Normal, float durationSeconds = 3.0f)
        {
            DisplayedNotifications.Add(new Notification(title, message, NotificationType.Info, priority, durationSeconds));
        }

        public void ShowSuccess(string title, string message, NotificationPriority priority = NotificationPriority.Normal, float durationSeconds = 3.0f)
        {
            DisplayedNotifications.Add(new Notification(title, message, NotificationType.Success, priority, durationSeconds));
        }

        public void ShowWarning(string title, string message, NotificationPriority priority = NotificationPriority.Normal, float durationSeconds = 3.0f)
        {
            DisplayedNotifications.Add(new Notification(title, message, NotificationType.Warning, priority, durationSeconds));
        }

        public void ShowError(string title, string message, NotificationPriority priority = NotificationPriority.Normal, float durationSeconds = 3.0f)
        {
            DisplayedNotifications.Add(new Notification(title, message, NotificationType.Error, priority, durationSeconds));
        }

        public void Dismiss(Guid id)
        {
            DisplayedNotifications.RemoveAll(n => n.Id == id);
        }

        public void ClearAll()
        {
            DisplayedNotifications.Clear();
        }

        public void ProcessQueue()
        {
            // No-op for testing
        }
    }
}
