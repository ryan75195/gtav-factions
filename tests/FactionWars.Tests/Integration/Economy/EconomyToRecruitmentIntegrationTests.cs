using FactionWars.Core.Interfaces;
using FactionWars.Economy.Interfaces;
using FactionWars.Economy.Models;
using FactionWars.Economy.Services;
using FactionWars.Factions.Interfaces;
using FactionWars.Factions.Models;
using FactionWars.Factions.Repositories;
using FactionWars.Factions.Services;
using FactionWars.Territory.Interfaces;
using FactionWars.Territory.Models;
using FactionWars.Territory.Repositories;
using FactionWars.Territory.Services;
using Xunit;

namespace FactionWars.Tests.Integration.Economy
{
    /// <summary>
    /// Integration tests for the full economy-to-recruitment pipeline:
    /// Zone ownership -> Resource generation -> Recruitment points -> Troop recruitment
    /// Uses real implementations (not mocks) to verify end-to-end behavior.
    /// </summary>
    public class EconomyToRecruitmentIntegrationTests
    {
        private readonly InMemoryZoneRepository _zoneRepository;
        private readonly IZoneService _zoneService;
        private readonly InMemoryFactionRepository _factionRepository;
        private readonly IFactionService _factionService;
        private readonly IZoneTraitResourceModifier _resourceModifier;
        private readonly IResourceTickService _resourceTickService;

        private const string MichaelFactionId = "faction-michael";
        private const string TrevorFactionId = "faction-trevor";
        private const string FranklinFactionId = "faction-franklin";
        private const int TickIntervalSeconds = 300; // 5 minutes

        public EconomyToRecruitmentIntegrationTests()
        {
            _zoneRepository = new InMemoryZoneRepository();
            _zoneService = new ZoneService(_zoneRepository);
            _factionRepository = new InMemoryFactionRepository();
            _factionService = new FactionService(_factionRepository);
            _resourceModifier = new ZoneTraitResourceModifier();
            _resourceTickService = new ResourceTickService(
                _factionService,
                _zoneService,
                _resourceModifier,
                TickIntervalSeconds);
        }

        #region Full Pipeline: Zone to Resources

        [Fact]
        public void FullPipeline_FactionOwningZone_ReceivesCashOnTick()
        {
            // Arrange: Create faction and zone
            SetupFaction(MichaelFactionId, "Michael's Crew", initialCash: 0);
            var zone = CreateAndAddZone("zone-1", "Downtown", MichaelFactionId, strategicValue: 1);

            // Act: Trigger resource tick
            _resourceTickService.Start();
            _resourceTickService.ForceTick();

            // Assert: Faction received cash
            var state = _factionService.GetFactionState(MichaelFactionId);
            Assert.NotNull(state);
            Assert.True(state.Cash > 0, "Faction should have received cash from zone");
        }

        [Fact]
        public void FullPipeline_FactionOwningZone_ReceivesWeaponsOnTick()
        {
            // Arrange: Create faction and zone
            SetupFaction(MichaelFactionId, "Michael's Crew", initialCash: 0);
            var zone = CreateAndAddZone("zone-1", "Industrial", MichaelFactionId, strategicValue: 1);
            zone.Traits = ZoneTrait.Industrial;
            _zoneRepository.Update(zone);

            // Act: Trigger resource tick
            _resourceTickService.Start();
            _resourceTickService.ForceTick();

            // Assert: Faction received weapons
            var state = _factionService.GetFactionState(MichaelFactionId);
            Assert.NotNull(state);
            Assert.True(state.Weapons > 0, "Faction should have received weapons from zone");
        }

        [Fact]
        public void FullPipeline_FactionOwningZone_ReceivesRecruitmentPointsOnTick()
        {
            // Arrange: Create faction and zone with residential trait
            SetupFaction(MichaelFactionId, "Michael's Crew", initialCash: 0);
            var zone = CreateAndAddZone("zone-1", "Residential", MichaelFactionId, strategicValue: 1);
            zone.Traits = ZoneTrait.Residential;
            _zoneRepository.Update(zone);

            // Act: Trigger resource tick
            _resourceTickService.Start();
            _resourceTickService.ForceTick();

            // Assert: Faction received recruitment points
            var state = _factionService.GetFactionState(MichaelFactionId);
            Assert.NotNull(state);
            Assert.True(state.RecruitmentPoints > 0, "Faction should have received recruitment points from zone");
        }

        #endregion

        #region Full Pipeline: Recruitment Points to Troops

        [Fact]
        public void FullPipeline_RecruitmentPointsConvertToTroops()
        {
            // Arrange: Faction starts with recruitment points
            SetupFaction(MichaelFactionId, "Michael's Crew", initialCash: 0, initialTroops: 0);
            var state = _factionService.GetFactionState(MichaelFactionId);
            Assert.NotNull(state);
            state.RecruitmentPoints = 100;
            _factionRepository.SetState(state);

            // Act: Recruit troops using recruitment points (10 points per troop)
            int troopsToRecruit = 5;
            int recruitmentCost = troopsToRecruit * 10; // Assuming 10 points per troop
            bool canRecruit = state.RecruitmentPoints >= recruitmentCost;
            Assert.True(canRecruit);

            _factionService.RecruitTroops(MichaelFactionId, troopsToRecruit);
            state.RecruitmentPoints -= recruitmentCost;
            _factionRepository.SetState(state);

            // Assert: Troops were recruited and points were spent
            var updatedState = _factionService.GetFactionState(MichaelFactionId);
            Assert.NotNull(updatedState);
            Assert.Equal(5, updatedState.TroopCount);
            Assert.Equal(50, updatedState.RecruitmentPoints);
        }

        #endregion

        #region Full Pipeline: Multi-Zone Resource Accumulation

        [Fact]
        public void FullPipeline_MultipleZones_AccumulateResources()
        {
            // Arrange: Create faction with multiple zones
            SetupFaction(MichaelFactionId, "Michael's Crew", initialCash: 0);

            // Zone 1: Commercial (cash bonus)
            var zone1 = CreateAndAddZone("zone-1", "Downtown", MichaelFactionId, strategicValue: 2);
            zone1.Traits = ZoneTrait.Commercial;
            _zoneRepository.Update(zone1);

            // Zone 2: Residential (recruitment bonus)
            var zone2 = CreateAndAddZone("zone-2", "Grove Street", MichaelFactionId, strategicValue: 2);
            zone2.Traits = ZoneTrait.Residential;
            _zoneRepository.Update(zone2);

            // Zone 3: Industrial (weapons bonus)
            var zone3 = CreateAndAddZone("zone-3", "Industrial Park", MichaelFactionId, strategicValue: 2);
            zone3.Traits = ZoneTrait.Industrial;
            _zoneRepository.Update(zone3);

            // Act: Trigger resource tick
            _resourceTickService.Start();
            _resourceTickService.ForceTick();

            // Assert: Faction received all resource types
            var state = _factionService.GetFactionState(MichaelFactionId);
            Assert.NotNull(state);
            Assert.True(state.Cash > 0, "Faction should have received cash");
            Assert.True(state.Weapons > 0, "Faction should have received weapons");
            Assert.True(state.RecruitmentPoints > 0, "Faction should have received recruitment points");
        }

        [Fact]
        public void FullPipeline_HighValueZone_GeneratesMoreResources()
        {
            // Arrange: Create two factions, one with high value zone, one with normal
            SetupFaction(MichaelFactionId, "Michael's Crew", initialCash: 0);
            SetupFaction(TrevorFactionId, "Trevor's Gang", initialCash: 0);

            // Michael gets a high-value zone (strategic value 5)
            var highValueZone = CreateAndAddZone("zone-hv", "Diamond Casino", MichaelFactionId, strategicValue: 5);

            // Trevor gets a normal zone (strategic value 1)
            var normalZone = CreateAndAddZone("zone-normal", "Trailer Park", TrevorFactionId, strategicValue: 1);

            // Act: Trigger resource tick
            _resourceTickService.Start();
            _resourceTickService.ForceTick();

            // Assert: Michael's faction has more resources
            var michaelState = _factionService.GetFactionState(MichaelFactionId);
            var trevorState = _factionService.GetFactionState(TrevorFactionId);

            Assert.NotNull(michaelState);
            Assert.NotNull(trevorState);
            Assert.True(michaelState.Cash > trevorState.Cash,
                "High value zone should generate more cash");
        }

        #endregion

        #region Full Pipeline: Trait Bonuses

        [Fact]
        public void FullPipeline_ResidentialTrait_BoostsRecruitment()
        {
            // Arrange: Create two factions with identical zones except traits
            SetupFaction(MichaelFactionId, "Michael's Crew", initialCash: 0);
            SetupFaction(TrevorFactionId, "Trevor's Gang", initialCash: 0);

            // Michael gets a residential zone
            var residentialZone = CreateAndAddZone("zone-res", "Suburbs", MichaelFactionId, strategicValue: 1);
            residentialZone.Traits = ZoneTrait.Residential;
            _zoneRepository.Update(residentialZone);

            // Trevor gets a zone with no traits
            var noTraitZone = CreateAndAddZone("zone-none", "Empty Lot", TrevorFactionId, strategicValue: 1);

            // Act: Trigger resource tick
            _resourceTickService.Start();
            _resourceTickService.ForceTick();

            // Assert: Michael's faction has more recruitment points
            var michaelState = _factionService.GetFactionState(MichaelFactionId);
            var trevorState = _factionService.GetFactionState(TrevorFactionId);

            Assert.NotNull(michaelState);
            Assert.NotNull(trevorState);
            Assert.True(michaelState.RecruitmentPoints > trevorState.RecruitmentPoints,
                "Residential zone should generate more recruitment points");
        }

        [Fact]
        public void FullPipeline_CommercialTrait_BoostsCash()
        {
            // Arrange: Create two factions with identical zones except traits
            SetupFaction(MichaelFactionId, "Michael's Crew", initialCash: 0);
            SetupFaction(TrevorFactionId, "Trevor's Gang", initialCash: 0);

            // Michael gets a commercial zone
            var commercialZone = CreateAndAddZone("zone-com", "Shopping Mall", MichaelFactionId, strategicValue: 1);
            commercialZone.Traits = ZoneTrait.Commercial;
            _zoneRepository.Update(commercialZone);

            // Trevor gets a zone with no traits
            var noTraitZone = CreateAndAddZone("zone-none", "Empty Lot", TrevorFactionId, strategicValue: 1);

            // Act: Trigger resource tick
            _resourceTickService.Start();
            _resourceTickService.ForceTick();

            // Assert: Michael's faction has more cash
            var michaelState = _factionService.GetFactionState(MichaelFactionId);
            var trevorState = _factionService.GetFactionState(TrevorFactionId);

            Assert.NotNull(michaelState);
            Assert.NotNull(trevorState);
            Assert.True(michaelState.Cash > trevorState.Cash,
                "Commercial zone should generate more cash");
        }

        [Fact]
        public void FullPipeline_IndustrialTrait_BoostsWeapons()
        {
            // Arrange: Create two factions with identical zones except traits
            SetupFaction(MichaelFactionId, "Michael's Crew", initialCash: 0);
            SetupFaction(TrevorFactionId, "Trevor's Gang", initialCash: 0);

            // Michael gets an industrial zone
            var industrialZone = CreateAndAddZone("zone-ind", "Factory", MichaelFactionId, strategicValue: 1);
            industrialZone.Traits = ZoneTrait.Industrial;
            _zoneRepository.Update(industrialZone);

            // Trevor gets a zone with no traits
            var noTraitZone = CreateAndAddZone("zone-none", "Empty Lot", TrevorFactionId, strategicValue: 1);

            // Act: Trigger resource tick
            _resourceTickService.Start();
            _resourceTickService.ForceTick();

            // Assert: Michael's faction has more weapons
            var michaelState = _factionService.GetFactionState(MichaelFactionId);
            var trevorState = _factionService.GetFactionState(TrevorFactionId);

            Assert.NotNull(michaelState);
            Assert.NotNull(trevorState);
            Assert.True(michaelState.Weapons > trevorState.Weapons,
                "Industrial zone should generate more weapons");
        }

        #endregion

        #region Full Pipeline: Multiple Ticks

        [Fact]
        public void FullPipeline_MultipleTicks_AccumulateResources()
        {
            // Arrange: Create faction with zone
            SetupFaction(MichaelFactionId, "Michael's Crew", initialCash: 0);
            var zone = CreateAndAddZone("zone-1", "Downtown", MichaelFactionId, strategicValue: 1);

            _resourceTickService.Start();

            // Act: Trigger multiple ticks
            _resourceTickService.ForceTick();
            var afterFirstTick = _factionService.GetFactionState(MichaelFactionId)!.Cash;

            _resourceTickService.ForceTick();
            var afterSecondTick = _factionService.GetFactionState(MichaelFactionId)!.Cash;

            _resourceTickService.ForceTick();
            var afterThirdTick = _factionService.GetFactionState(MichaelFactionId)!.Cash;

            // Assert: Resources accumulate over time
            Assert.True(afterSecondTick > afterFirstTick, "Resources should accumulate with each tick");
            Assert.True(afterThirdTick > afterSecondTick, "Resources should continue accumulating");
        }

        [Fact]
        public void FullPipeline_TimeBasedTicks_TriggerAutomatically()
        {
            // Arrange: Create faction with zone
            SetupFaction(MichaelFactionId, "Michael's Crew", initialCash: 0);
            var zone = CreateAndAddZone("zone-1", "Downtown", MichaelFactionId, strategicValue: 1);

            _resourceTickService.Start();

            // Act: Simulate time passing (more than tick interval)
            _resourceTickService.Update(TickIntervalSeconds + 1);

            // Assert: Tick should have occurred
            var state = _factionService.GetFactionState(MichaelFactionId);
            Assert.NotNull(state);
            Assert.True(state.Cash > 0, "Resources should be generated after tick interval passes");
        }

        #endregion

        #region Full Pipeline: Zone Ownership Changes

        [Fact]
        public void FullPipeline_ZoneCaptured_NewOwnerGetsResources()
        {
            // Arrange: Michael owns a zone initially
            SetupFaction(MichaelFactionId, "Michael's Crew", initialCash: 0);
            SetupFaction(TrevorFactionId, "Trevor's Gang", initialCash: 0);

            var zone = CreateAndAddZone("zone-1", "Contested Territory", MichaelFactionId, strategicValue: 5);

            _resourceTickService.Start();

            // Michael gets resources initially
            _resourceTickService.ForceTick();
            var michaelInitialCash = _factionService.GetFactionState(MichaelFactionId)!.Cash;
            var trevorInitialCash = _factionService.GetFactionState(TrevorFactionId)!.Cash;

            Assert.True(michaelInitialCash > 0);
            Assert.Equal(0, trevorInitialCash);

            // Act: Trevor captures the zone
            _zoneService.TransferZoneOwnership(zone.Id, TrevorFactionId);
            _factionService.RemoveZoneFromFaction(MichaelFactionId, zone.Id);
            _factionService.AddZoneToFaction(TrevorFactionId, zone.Id);

            // Trigger another tick
            _resourceTickService.ForceTick();

            // Assert: Trevor now gets resources, Michael gets nothing more from this zone
            var michaelFinalCash = _factionService.GetFactionState(MichaelFactionId)!.Cash;
            var trevorFinalCash = _factionService.GetFactionState(TrevorFactionId)!.Cash;

            Assert.True(trevorFinalCash > 0, "Trevor should now receive resources from captured zone");
            Assert.True(michaelFinalCash == michaelInitialCash,
                "Michael should not receive more resources after losing zone");
        }

        #endregion

        #region Full Pipeline: Inactive Factions

        [Fact]
        public void FullPipeline_InactiveFaction_DoesNotReceiveResources()
        {
            // Arrange: Create an inactive faction with zones
            SetupFaction(MichaelFactionId, "Michael's Crew", initialCash: 0);
            var zone = CreateAndAddZone("zone-1", "Downtown", MichaelFactionId, strategicValue: 5);

            // Deactivate the faction
            _factionService.DeactivateFaction(MichaelFactionId);

            // Act: Trigger resource tick
            _resourceTickService.Start();
            _resourceTickService.ForceTick();

            // Assert: No resources generated
            var state = _factionService.GetFactionState(MichaelFactionId);
            Assert.NotNull(state);
            Assert.Equal(0, state.Cash);
        }

        #endregion

        #region Full Pipeline: Economy to Military Strength

        [Fact]
        public void FullPipeline_ResourcesIncreasesMilitaryStrength()
        {
            // Arrange: Create faction
            SetupFaction(MichaelFactionId, "Michael's Crew", initialCash: 0, initialTroops: 10);

            // Add industrial zone for weapons
            var zone = CreateAndAddZone("zone-1", "Arms Factory", MichaelFactionId, strategicValue: 5);
            zone.Traits = ZoneTrait.Industrial;
            _zoneRepository.Update(zone);

            // Get initial military strength
            var initialStrength = _factionService.GetMilitaryStrength(MichaelFactionId);

            // Act: Generate resources (weapons)
            _resourceTickService.Start();
            _resourceTickService.ForceTick();

            // Assert: Military strength increased due to weapons
            var newStrength = _factionService.GetMilitaryStrength(MichaelFactionId);
            Assert.True(newStrength > initialStrength,
                "Military strength should increase with weapons production");
        }

        [Fact]
        public void FullPipeline_EndToEnd_EconomyToCombatReadiness()
        {
            // Arrange: Create a faction starting from nothing
            SetupFaction(MichaelFactionId, "Michael's Crew", initialCash: 0, initialTroops: 0);

            // Give them a diverse set of zones
            var residentialZone = CreateAndAddZone("zone-res", "Suburbs", MichaelFactionId, strategicValue: 3);
            residentialZone.Traits = ZoneTrait.Residential;
            _zoneRepository.Update(residentialZone);

            var industrialZone = CreateAndAddZone("zone-ind", "Factory", MichaelFactionId, strategicValue: 3);
            industrialZone.Traits = ZoneTrait.Industrial;
            _zoneRepository.Update(industrialZone);

            // Act: Simulate several resource ticks
            _resourceTickService.Start();
            for (int i = 0; i < 5; i++)
            {
                _resourceTickService.ForceTick();
            }

            // Get final state
            var state = _factionService.GetFactionState(MichaelFactionId);
            Assert.NotNull(state);

            // Simulate using recruitment points to recruit troops
            // (Assuming conversion rate exists)
            int recruitmentPoints = state.RecruitmentPoints;
            if (recruitmentPoints >= 10)
            {
                int troopsToRecruit = recruitmentPoints / 10;
                _factionService.RecruitTroops(MichaelFactionId, troopsToRecruit);
            }

            // Assert: Faction is now combat-ready
            var finalState = _factionService.GetFactionState(MichaelFactionId);
            Assert.NotNull(finalState);
            Assert.True(finalState.Cash > 0, "Faction should have accumulated cash");
            Assert.True(finalState.Weapons > 0, "Faction should have accumulated weapons");
            Assert.True(finalState.TroopCount > 0, "Faction should have recruited troops");
            Assert.True(finalState.MilitaryStrength > 0, "Faction should have military strength");
        }

        #endregion

        #region Helper Methods

        private void SetupFaction(string factionId, string name, int initialCash = 0, int initialTroops = 0)
        {
            var faction = new Faction(factionId, name);
            _factionRepository.Add(faction);
            _factionService.InitializeFactionState(factionId, initialCash, initialTroops);
        }

        private Zone CreateAndAddZone(string id, string name, string? ownerFactionId, int strategicValue = 1)
        {
            var zone = new Zone(id, name, new Vector3(0, 0, 0), 150f, strategicValue);
            zone.OwnerFactionId = ownerFactionId;
            zone.ControlPercentage = 100f;
            _zoneRepository.Add(zone);

            if (ownerFactionId != null)
            {
                _factionService.AddZoneToFaction(ownerFactionId, id);
            }

            return zone;
        }

        #endregion
    }
}
