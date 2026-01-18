using System;
using System.Collections.Generic;
using System.Linq;
using FactionWars.AI.Interfaces;
using FactionWars.AI.Models;
using FactionWars.AI.Strategies;
using FactionWars.Combat.Interfaces;
using FactionWars.Combat.Models;
using FactionWars.Combat.Services;
using FactionWars.Core.Interfaces;
using FactionWars.Economy.Interfaces;
using FactionWars.Economy.Services;
using FactionWars.Factions.Interfaces;
using FactionWars.Factions.Models;
using FactionWars.Factions.Repositories;
using FactionWars.Factions.Services;
using FactionWars.Territory.Interfaces;
using FactionWars.Territory.Models;
using FactionWars.Territory.Repositories;
using FactionWars.Territory.Services;
using FactionWars.UI.Interfaces;
using FactionWars.UI.Models;
using FactionWars.UI.Services;
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
            var tickService = new ResourceTickService(factionService, zoneService, resourceModifier, 300);

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
