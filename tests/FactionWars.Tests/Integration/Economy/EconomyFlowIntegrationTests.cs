using FactionWars.Core.Interfaces;
using FactionWars.Core.Models;
using FactionWars.Core.Services;
using FactionWars.Economy.Interfaces;
using FactionWars.Economy.Services;
using FactionWars.Factions.Interfaces;
using FactionWars.Factions.Models;
using FactionWars.Factions.Repositories;
using FactionWars.Factions.Services;
using FactionWars.ScriptHookV.Managers;
using FactionWars.Territory.Interfaces;
using FactionWars.Territory.Models;
using FactionWars.Territory.Repositories;
using FactionWars.Territory.Services;
using Moq;
using Xunit;

namespace FactionWars.Tests.Integration.Economy
{
    /// <summary>
    /// Integration tests for the complete economy flow:
    /// Zone ownership → Resource generation → Player GTA V cash → Troop purchasing → Reserve pool
    ///
    /// Tests the full end-to-end economy cycle using real service implementations
    /// with only the IGameBridge mocked (as it represents actual GTA V interactions).
    /// </summary>
    public class EconomyFlowIntegrationTests
    {
        private readonly Mock<IGameBridge> _mockGameBridge;
        private readonly InMemoryZoneRepository _zoneRepository;
        private readonly IZoneService _zoneService;
        private readonly InMemoryFactionRepository _factionRepository;
        private readonly IFactionService _factionService;
        private readonly IZoneTraitResourceModifier _resourceModifier;
        private readonly ISupplyLineService _supplyLineService;
        private readonly IResourceTickService _resourceTickService;
        private readonly IDefenderTierService _defenderTierService;
        private readonly ITroopPurchaseService _troopPurchaseService;
        private readonly EconomyManager _economyManager;

        private const string MichaelFactionId = "faction-michael";
        private const string TrevorFactionId = "faction-trevor";
        private const string FranklinFactionId = "faction-franklin";
        private const int TickIntervalSeconds = 300;

        private int _playerMoney = 0;

        public EconomyFlowIntegrationTests()
        {
            // Setup mock GameBridge with simulated player money
            _mockGameBridge = new Mock<IGameBridge>();
            _mockGameBridge.Setup(g => g.GetPlayerMoney()).Returns(() => _playerMoney);
            _mockGameBridge.Setup(g => g.AddPlayerMoney(It.IsAny<int>()))
                .Callback<int>(amount => _playerMoney += amount);

            // Real implementations
            _zoneRepository = new InMemoryZoneRepository();
            _zoneService = new ZoneService(_zoneRepository);
            _factionRepository = new InMemoryFactionRepository();
            _factionService = new FactionService(_factionRepository);
            _resourceModifier = new ZoneTraitResourceModifier();
            _supplyLineService = new SupplyLineService(_zoneService);
            _resourceTickService = new ResourceTickService(
                _factionService,
                _zoneService,
                _resourceModifier,
                _supplyLineService,
                TickIntervalSeconds);
            _defenderTierService = new DefenderTierService();
            _troopPurchaseService = new TroopPurchaseService(
                _mockGameBridge.Object,
                _defenderTierService,
                _factionService);
            _economyManager = new EconomyManager(_resourceTickService, _mockGameBridge.Object);
        }

        #region Full Economy Cycle Tests

        [Fact]
        public void FullCycle_PlayerReceivesIncomeAndPurchasesTroops()
        {
            // Arrange: Setup player faction with zone
            SetupFaction(MichaelFactionId, "Michael's Crew", initialCash: 0);
            var zone = CreateAndAddZone("zone-1", "Downtown", MichaelFactionId, strategicValue: 5);
            zone.Traits = ZoneTrait.Commercial; // +50% cash
            _zoneRepository.Update(zone);

            _playerMoney = 0;
            _economyManager.SetPlayerFactionId(MichaelFactionId);
            _economyManager.Start();

            // Act: Trigger multiple resource ticks to accumulate money
            for (int i = 0; i < 3; i++)
            {
                _economyManager.ForceTick();
            }

            // Assert: Player has received GTA V cash
            Assert.True(_playerMoney > 0, "Player should have received GTA V cash from zone income");

            // Act: Purchase troops with earned money
            int initialMoney = _playerMoney;
            var purchaseResult = _troopPurchaseService.PurchaseTroops(MichaelFactionId, DefenderTier.Basic, 2);

            // Assert: Purchase succeeded and money was deducted
            Assert.True(purchaseResult.Success, "Troop purchase should succeed with earned income");
            Assert.True(_playerMoney < initialMoney, "Money should be deducted after purchase");

            // Assert: Troops were added to reserve
            var factionState = _factionService.GetFactionState(MichaelFactionId);
            Assert.NotNull(factionState);
            Assert.True(factionState.GetReserveTroops(DefenderTier.Basic) >= 2,
                "Purchased troops should be in reserve pool");
        }

        [Fact]
        public void FullCycle_EarningsAcrossTicks_AccumulateCorrectly()
        {
            // Arrange
            SetupFaction(MichaelFactionId, "Michael's Crew", initialCash: 0);
            var zone = CreateAndAddZone("zone-1", "Commerce", MichaelFactionId, strategicValue: 2);

            _playerMoney = 1000; // Start with some money
            _economyManager.SetPlayerFactionId(MichaelFactionId);
            _economyManager.Start();

            int moneyAfterFirstTick, moneyAfterSecondTick, moneyAfterThirdTick;

            // Act: Trigger multiple ticks
            _economyManager.ForceTick();
            moneyAfterFirstTick = _playerMoney;

            _economyManager.ForceTick();
            moneyAfterSecondTick = _playerMoney;

            _economyManager.ForceTick();
            moneyAfterThirdTick = _playerMoney;

            // Assert: Money accumulates consistently
            int firstIncrement = moneyAfterFirstTick - 1000;
            int secondIncrement = moneyAfterSecondTick - moneyAfterFirstTick;
            int thirdIncrement = moneyAfterThirdTick - moneyAfterSecondTick;

            Assert.True(firstIncrement > 0, "First tick should generate income");
            Assert.Equal(firstIncrement, secondIncrement); // Consistent income each tick
            Assert.Equal(secondIncrement, thirdIncrement);
        }

        [Fact]
        public void FullCycle_NonPlayerFactionIncome_DoesNotAffectPlayerMoney()
        {
            // Arrange: Setup two factions, player is Michael
            SetupFaction(MichaelFactionId, "Michael's Crew", initialCash: 0);
            SetupFaction(TrevorFactionId, "Trevor's Gang", initialCash: 0);

            // Trevor owns a high-value zone
            var trevorZone = CreateAndAddZone("zone-trevor", "Sandy Shores", TrevorFactionId, strategicValue: 5);

            _playerMoney = 5000;
            _economyManager.SetPlayerFactionId(MichaelFactionId);
            _economyManager.Start();

            int initialPlayerMoney = _playerMoney;

            // Act: Trigger tick - Trevor generates income but player shouldn't get it
            _economyManager.ForceTick();

            // Assert: Player money unchanged (Michael has no zones)
            Assert.Equal(initialPlayerMoney, _playerMoney);

            // Assert: But Trevor's faction did accumulate resources
            var trevorState = _factionService.GetFactionState(TrevorFactionId);
            Assert.NotNull(trevorState);
            Assert.True(trevorState.Cash > 0, "Trevor's faction should have accumulated cash");
        }

        #endregion

        #region Troop Purchase Integration Tests

        [Fact]
        public void TroopPurchase_WithEarnedIncome_DeductsFromGTAVMoney()
        {
            // Arrange
            SetupFaction(MichaelFactionId, "Michael's Crew", initialCash: 0);
            var zone = CreateAndAddZone("zone-1", "Bank District", MichaelFactionId, strategicValue: 10);
            zone.Traits = ZoneTrait.Commercial;
            _zoneRepository.Update(zone);

            _playerMoney = 0;
            _economyManager.SetPlayerFactionId(MichaelFactionId);
            _economyManager.Start();

            // Generate income
            for (int i = 0; i < 10; i++)
            {
                _economyManager.ForceTick();
            }

            int earnedMoney = _playerMoney;
            Assert.True(earnedMoney > 0, "Should have earned some money");

            // Act: Purchase medium tier troops
            int mediumCost = _defenderTierService.GetCost(DefenderTier.Medium);
            int affordableCount = earnedMoney / mediumCost;
            Assert.True(affordableCount > 0, "Should be able to afford at least one troop");

            var result = _troopPurchaseService.PurchaseTroops(
                MichaelFactionId,
                DefenderTier.Medium,
                affordableCount);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(affordableCount, result.TroopsPurchased);
            Assert.Equal(earnedMoney - (affordableCount * mediumCost), _playerMoney);
        }

        [Fact]
        public void TroopPurchase_AllTiers_CostsAndAddsCorrectly()
        {
            // Arrange
            SetupFaction(MichaelFactionId, "Michael's Crew", initialCash: 0);
            _playerMoney = 10000; // Start with plenty of money

            // Act & Assert: Purchase each tier
            int basicCost = _defenderTierService.GetCost(DefenderTier.Basic);
            var basicResult = _troopPurchaseService.PurchaseTroops(MichaelFactionId, DefenderTier.Basic, 5);
            Assert.True(basicResult.Success);
            Assert.Equal(5 * basicCost, basicResult.TotalCost);

            int mediumCost = _defenderTierService.GetCost(DefenderTier.Medium);
            var mediumResult = _troopPurchaseService.PurchaseTroops(MichaelFactionId, DefenderTier.Medium, 3);
            Assert.True(mediumResult.Success);
            Assert.Equal(3 * mediumCost, mediumResult.TotalCost);

            int heavyCost = _defenderTierService.GetCost(DefenderTier.Heavy);
            var heavyResult = _troopPurchaseService.PurchaseTroops(MichaelFactionId, DefenderTier.Heavy, 2);
            Assert.True(heavyResult.Success);
            Assert.Equal(2 * heavyCost, heavyResult.TotalCost);

            // Verify all troops in reserve
            var state = _factionService.GetFactionState(MichaelFactionId);
            Assert.NotNull(state);
            Assert.Equal(5, state.GetReserveTroops(DefenderTier.Basic));
            Assert.Equal(3, state.GetReserveTroops(DefenderTier.Medium));
            Assert.Equal(2, state.GetReserveTroops(DefenderTier.Heavy));

            // Verify total money deducted
            int totalSpent = (5 * basicCost) + (3 * mediumCost) + (2 * heavyCost);
            Assert.Equal(10000 - totalSpent, _playerMoney);
        }

        [Fact]
        public void TroopPurchase_InsufficientFunds_FailsGracefully()
        {
            // Arrange
            SetupFaction(MichaelFactionId, "Michael's Crew", initialCash: 0);
            _playerMoney = 100; // Very little money

            // Act: Try to buy heavy troops (cost $1000 each)
            var result = _troopPurchaseService.PurchaseTroops(MichaelFactionId, DefenderTier.Heavy, 1);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(0, result.TroopsPurchased);
            Assert.Equal(100, _playerMoney); // Money unchanged

            var state = _factionService.GetFactionState(MichaelFactionId);
            Assert.NotNull(state);
            Assert.Equal(0, state.GetReserveTroops(DefenderTier.Heavy));
        }

        #endregion

        #region Income and Zone Traits Integration Tests

        [Fact]
        public void Income_CommercialZone_GeneratesMoreCash()
        {
            // Arrange: Two factions, one with commercial zone, one without
            SetupFaction(MichaelFactionId, "Michael's Crew", initialCash: 0);
            SetupFaction(TrevorFactionId, "Trevor's Gang", initialCash: 0);

            var commercialZone = CreateAndAddZone("zone-com", "Mall", MichaelFactionId, strategicValue: 1);
            commercialZone.Traits = ZoneTrait.Commercial;
            _zoneRepository.Update(commercialZone);

            var normalZone = CreateAndAddZone("zone-normal", "Desert", TrevorFactionId, strategicValue: 1);

            _playerMoney = 0;
            _economyManager.SetPlayerFactionId(MichaelFactionId);
            _economyManager.Start();

            // Act
            _economyManager.ForceTick();

            // Assert: Michael (commercial) earned more cash than Trevor (normal)
            var michaelState = _factionService.GetFactionState(MichaelFactionId);
            var trevorState = _factionService.GetFactionState(TrevorFactionId);

            Assert.NotNull(michaelState);
            Assert.NotNull(trevorState);
            Assert.True(michaelState.Cash > trevorState.Cash,
                "Commercial zone should generate more cash");

            // Player money should match Michael's cash
            Assert.Equal(michaelState.Cash, _playerMoney);
        }

        [Fact]
        public void Income_MultipleZones_CumulativeEffect()
        {
            // Arrange
            SetupFaction(MichaelFactionId, "Michael's Crew", initialCash: 0);

            // Add 5 zones with varying strategic values
            for (int i = 1; i <= 5; i++)
            {
                CreateAndAddZone($"zone-{i}", $"Territory {i}", MichaelFactionId, strategicValue: i);
            }

            _playerMoney = 0;
            _economyManager.SetPlayerFactionId(MichaelFactionId);
            _economyManager.Start();

            // Act
            _economyManager.ForceTick();

            // Assert: Should have substantial income from all zones combined
            // Total strategic value: 1+2+3+4+5 = 15, base cash per value = 100, so 1500 expected
            Assert.True(_playerMoney >= 1500, $"Expected at least $1500, got ${_playerMoney}");
        }

        #endregion

        #region Character Switch Integration Tests

        [Fact]
        public void CharacterSwitch_ChangesPlayerFaction_AffectsIncomeRecipient()
        {
            // Arrange: All three factions with zones
            SetupFaction(MichaelFactionId, "Michael's Crew", initialCash: 0);
            SetupFaction(TrevorFactionId, "Trevor's Gang", initialCash: 0);
            SetupFaction(FranklinFactionId, "Franklin's Family", initialCash: 0);

            CreateAndAddZone("zone-m", "Vinewood", MichaelFactionId, strategicValue: 3);
            CreateAndAddZone("zone-t", "Sandy Shores", TrevorFactionId, strategicValue: 3);
            CreateAndAddZone("zone-f", "Grove Street", FranklinFactionId, strategicValue: 3);

            _playerMoney = 0;
            _economyManager.Start();

            // Act: Start as Michael
            _economyManager.SetPlayerFactionId(MichaelFactionId);
            _economyManager.ForceTick();
            int moneyAsMichael = _playerMoney;

            // Switch to Trevor
            _playerMoney = 0; // Simulate fresh money counter
            _economyManager.SetPlayerFactionId(TrevorFactionId);
            _economyManager.ForceTick();
            int moneyAsTrevor = _playerMoney;

            // Switch to Franklin
            _playerMoney = 0;
            _economyManager.SetPlayerFactionId(FranklinFactionId);
            _economyManager.ForceTick();
            int moneyAsFranklin = _playerMoney;

            // Assert: Each character received their faction's income
            Assert.True(moneyAsMichael > 0, "Michael should receive income from Vinewood");
            Assert.True(moneyAsTrevor > 0, "Trevor should receive income from Sandy Shores");
            Assert.True(moneyAsFranklin > 0, "Franklin should receive income from Grove Street");

            // All equal since same strategic value
            Assert.Equal(moneyAsMichael, moneyAsTrevor);
            Assert.Equal(moneyAsTrevor, moneyAsFranklin);
        }

        #endregion

        #region Reserve Pool Integration Tests

        [Fact]
        public void ReservePool_PurchasedTroops_AddedToCorrectTier()
        {
            // Arrange
            SetupFaction(MichaelFactionId, "Michael's Crew", initialCash: 0);
            _playerMoney = 5000;

            // Act: Purchase troops of different tiers
            _troopPurchaseService.PurchaseTroops(MichaelFactionId, DefenderTier.Basic, 3);
            _troopPurchaseService.PurchaseTroops(MichaelFactionId, DefenderTier.Medium, 2);
            _troopPurchaseService.PurchaseTroops(MichaelFactionId, DefenderTier.Heavy, 1);

            // Assert: Each tier has correct count in reserve
            var state = _factionService.GetFactionState(MichaelFactionId);
            Assert.NotNull(state);
            Assert.Equal(3, state.GetReserveTroops(DefenderTier.Basic));
            Assert.Equal(2, state.GetReserveTroops(DefenderTier.Medium));
            Assert.Equal(1, state.GetReserveTroops(DefenderTier.Heavy));
            Assert.Equal(6, state.TotalReserveTroops);
        }

        #endregion

        #region Verification: Income Adds to GTA V Money (Task 5.7)

        [Fact]
        public void Verify_IncomeAddsToPlayerGTAVMoney_WhenResourceTickOccurs()
        {
            // Arrange: Player faction with zone that generates income
            SetupFaction(MichaelFactionId, "Michael's Crew", initialCash: 0);
            var zone = CreateAndAddZone("zone-1", "Downtown", MichaelFactionId, strategicValue: 5);

            // Initial player money is 0
            _playerMoney = 0;
            Assert.Equal(0, _playerMoney);

            // Set player faction and start economy
            _economyManager.SetPlayerFactionId(MichaelFactionId);
            _economyManager.Start();

            // Act: Trigger a resource tick
            _economyManager.ForceTick();

            // Assert: Player's GTA V money increased from zone income
            Assert.True(_playerMoney > 0,
                "Player's GTA V money should increase after resource tick from owned zone");

            // Verify the amount matches the faction's cash (which is based on zone strategic value)
            var factionState = _factionService.GetFactionState(MichaelFactionId);
            Assert.NotNull(factionState);
            // Player's GTA V money should equal faction cash generated
            Assert.Equal(factionState.Cash, _playerMoney);
        }

        [Fact]
        public void Verify_GameBridgeAddPlayerMoneyIsCalled_WhenPlayerFactionGeneratesIncome()
        {
            // Arrange: Verify the GameBridge.AddPlayerMoney callback was invoked
            int addPlayerMoneyCalls = 0;
            int totalMoneyAdded = 0;

            var customGameBridge = new Mock<IGameBridge>();
            customGameBridge.Setup(g => g.GetPlayerMoney()).Returns(() => _playerMoney);
            customGameBridge.Setup(g => g.AddPlayerMoney(It.IsAny<int>()))
                .Callback<int>(amount =>
                {
                    addPlayerMoneyCalls++;
                    totalMoneyAdded += amount;
                    _playerMoney += amount;
                });

            var customEconomyManager = new EconomyManager(_resourceTickService, customGameBridge.Object);

            SetupFaction(MichaelFactionId, "Michael's Crew", initialCash: 0);
            CreateAndAddZone("zone-1", "Commerce", MichaelFactionId, strategicValue: 3);

            _playerMoney = 0;
            customEconomyManager.SetPlayerFactionId(MichaelFactionId);
            customEconomyManager.Start();

            // Act
            customEconomyManager.ForceTick();

            // Assert: AddPlayerMoney was called exactly once with the income amount
            Assert.Equal(1, addPlayerMoneyCalls);
            Assert.True(totalMoneyAdded > 0, "Should have added positive income amount");
            Assert.Equal(totalMoneyAdded, _playerMoney);

            // Verify mock was called
            customGameBridge.Verify(g => g.AddPlayerMoney(It.Is<int>(amt => amt > 0)), Times.Once);
        }

        [Fact]
        public void Verify_MultipleZones_AccumulateIncomeToPlayerMoney()
        {
            // Arrange: Player owns multiple zones with different values
            SetupFaction(MichaelFactionId, "Michael's Crew", initialCash: 0);

            CreateAndAddZone("zone-1", "Downtown", MichaelFactionId, strategicValue: 5);
            CreateAndAddZone("zone-2", "Industrial", MichaelFactionId, strategicValue: 3);
            CreateAndAddZone("zone-3", "Suburbs", MichaelFactionId, strategicValue: 2);
            // Total strategic value: 5 + 3 + 2 = 10

            _playerMoney = 0;
            _economyManager.SetPlayerFactionId(MichaelFactionId);
            _economyManager.Start();

            // Act
            _economyManager.ForceTick();

            // Assert: Player money reflects cumulative income from all zones
            var factionState = _factionService.GetFactionState(MichaelFactionId);
            Assert.NotNull(factionState);
            Assert.True(factionState.Cash >= 1000,
                $"Expected at least $1000 from 10 strategic value, got ${factionState.Cash}");
            // Player GTA V money should match total faction income
            Assert.Equal(factionState.Cash, _playerMoney);
        }

        #endregion

        #region Edge Cases

        [Fact]
        public void Economy_ZeroIncome_NoMoneyAdded()
        {
            // Arrange: Faction with no zones
            SetupFaction(MichaelFactionId, "Michael's Crew", initialCash: 0);

            _playerMoney = 1000;
            _economyManager.SetPlayerFactionId(MichaelFactionId);
            _economyManager.Start();

            // Act
            _economyManager.ForceTick();

            // Assert: Money unchanged
            Assert.Equal(1000, _playerMoney);
        }

        [Fact]
        public void Economy_InactiveFaction_NoIncomeGenerated()
        {
            // Arrange
            SetupFaction(MichaelFactionId, "Michael's Crew", initialCash: 0);
            CreateAndAddZone("zone-1", "Downtown", MichaelFactionId, strategicValue: 5);

            _factionService.DeactivateFaction(MichaelFactionId);

            _playerMoney = 0;
            _economyManager.SetPlayerFactionId(MichaelFactionId);
            _economyManager.Start();

            // Act
            _economyManager.ForceTick();

            // Assert
            Assert.Equal(0, _playerMoney);
        }

        [Fact]
        public void TroopPurchase_CanAfford_AccuratelyReflectsPlayerMoney()
        {
            // Arrange
            SetupFaction(MichaelFactionId, "Michael's Crew", initialCash: 0);
            _playerMoney = 500;

            int basicCost = _defenderTierService.GetCost(DefenderTier.Basic); // $200
            int mediumCost = _defenderTierService.GetCost(DefenderTier.Medium); // $500
            int heavyCost = _defenderTierService.GetCost(DefenderTier.Heavy); // $1000

            // Act & Assert
            Assert.True(_troopPurchaseService.CanAfford(DefenderTier.Basic, 2)); // $400
            Assert.False(_troopPurchaseService.CanAfford(DefenderTier.Basic, 3)); // $600
            Assert.True(_troopPurchaseService.CanAfford(DefenderTier.Medium, 1)); // $500
            Assert.False(_troopPurchaseService.CanAfford(DefenderTier.Heavy, 1)); // $1000
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
