using FactionWars.AI.Controllers;
using FactionWars.AI.Events;
using FactionWars.AI.Interfaces;
using FactionWars.AI.Models;
using FactionWars.Combat.Interfaces;
using FactionWars.Combat.Models;
using FactionWars.Configuration;
using FactionWars.Core.Interfaces;
using FactionWars.Core.Models;
using FactionWars.Factions.Interfaces;
using FactionWars.Factions.Models;
using FactionWars.Territory.Interfaces;
using FactionWars.Territory.Models;
using Moq;
using System.Collections.Generic;
using Xunit;

namespace FactionWars.Tests.Unit.AI
{
    public class AIControllerTests
    {
        private readonly Mock<IFactionService> _factionServiceMock;
        private readonly Mock<IZoneService> _zoneServiceMock;
        private readonly Mock<IBattleSimulationService> _battleSimulationServiceMock;
        private readonly Mock<IZoneDefenderAllocationService> _allocationServiceMock;
        private readonly Mock<IGameBridge> _gameBridgeMock;
        private readonly Mock<IZoneBattleManager> _zoneBattleManagerMock;
        private readonly Dictionary<string, IAIStrategy> _strategies;

        public AIControllerTests()
        {
            _factionServiceMock = new Mock<IFactionService>();
            _zoneServiceMock = new Mock<IZoneService>();
            _battleSimulationServiceMock = new Mock<IBattleSimulationService>();
            _allocationServiceMock = new Mock<IZoneDefenderAllocationService>();
            _gameBridgeMock = new Mock<IGameBridge>();
            _zoneBattleManagerMock = new Mock<IZoneBattleManager>();
            _strategies = new Dictionary<string, IAIStrategy>();
        }

        [Fact]
        public void Constructor_ShouldInitializeWithIsRunningFalse()
        {
            var controller = CreateController();
            Assert.False(controller.IsRunning);
        }

        [Fact]
        public void Start_ShouldSetIsRunningTrue()
        {
            var controller = CreateController();
            controller.Start();
            Assert.True(controller.IsRunning);
        }

        [Fact]
        public void Stop_ShouldSetIsRunningFalse()
        {
            var controller = CreateController();
            controller.Start();
            controller.Stop();
            Assert.False(controller.IsRunning);
        }

        [Fact]
        public void SetPlayerFactionId_ShouldStoreValue()
        {
            var controller = CreateController();
            controller.SetPlayerFactionId("michael");
            Assert.Equal("michael", controller.PlayerFactionId);
        }

        [Fact]
        public void SetPlayerZone_ShouldStoreValue()
        {
            var controller = CreateController();
            controller.SetPlayerZone("vinewood");
            Assert.Equal("vinewood", controller.PlayerZoneId);
        }

        [Fact]
        public void Update_After90Seconds_ShouldTriggerRecruitment()
        {
            // Arrange
            var faction = new Faction("trevor", "Trevor", color: new FactionColor(255, 150, 0));
            var factionState = new FactionState("trevor", 1000, 5);

            _factionServiceMock.Setup(f => f.GetActiveFactions())
                .Returns(new[] { faction });
            _factionServiceMock.Setup(f => f.GetFactionState("trevor"))
                .Returns(factionState);

            var controller = CreateController();
            controller.Start();

            // Act - simulate 90 seconds
            controller.Update(90f);

            // Assert - should have recruited (1000 cash / 200 per troop = 5, capped at 5)
            _factionServiceMock.Verify(f => f.RecruitTroops("trevor", 5), Times.Once);
            _factionServiceMock.Verify(f => f.SpendCash("trevor", 1000), Times.Once);
        }

        [Fact]
        public void Update_WhenNotRunning_ShouldNotProcess()
        {
            var controller = CreateController();
            // Don't call Start()

            controller.Update(100f);

            _factionServiceMock.Verify(f => f.GetActiveFactions(), Times.Never);
        }

        [Fact]
        public void Update_ShouldSkipPlayerFaction()
        {
            var playerFaction = new Faction("michael", "Michael", color: new FactionColor(0, 100, 255));
            var aiFaction = new Faction("trevor", "Trevor", color: new FactionColor(255, 150, 0));

            _factionServiceMock.Setup(f => f.GetActiveFactions())
                .Returns(new[] { playerFaction, aiFaction });
            _factionServiceMock.Setup(f => f.GetFactionState("trevor"))
                .Returns(new FactionState("trevor", 1000, 5));

            var controller = CreateController();
            controller.SetPlayerFactionId("michael");
            controller.Start();

            controller.Update(90f);

            // Should recruit for trevor but not michael
            _factionServiceMock.Verify(f => f.RecruitTroops("trevor", It.IsAny<int>()), Times.Once);
            _factionServiceMock.Verify(f => f.RecruitTroops("michael", It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public void ExecuteAttack_WhenBattleAlreadyExistsInZone_ShouldSkipWithoutException()
        {
            // Arrange
            var attackerFaction = new Faction("trevor", "Trevor", color: new FactionColor(255, 150, 0));
            var defenderFaction = new Faction("michael", "Michael", color: new FactionColor(0, 100, 255));
            var targetZone = new Zone("vinewood", "Vinewood", new FactionWars.Core.Interfaces.Vector3(100f, 100f, 30f), 100f, 5);
            targetZone.OwnerFactionId = "michael";

            var attackerOwnedZone = new Zone("davis", "Davis", new FactionWars.Core.Interfaces.Vector3(200f, 200f, 30f), 100f, 3);
            attackerOwnedZone.OwnerFactionId = "trevor";
            attackerOwnedZone.AdjacentZoneIds.Add("vinewood");

            // Setup faction service
            _factionServiceMock.Setup(f => f.GetActiveFactions())
                .Returns(new[] { attackerFaction, defenderFaction });
            _factionServiceMock.Setup(f => f.GetFaction("trevor"))
                .Returns(attackerFaction);
            _factionServiceMock.Setup(f => f.GetFactionState("trevor"))
                .Returns(new FactionState("trevor", 10000, 20)); // Enough cash and troops
            _factionServiceMock.Setup(f => f.GetAllFactions())
                .Returns(new[] { attackerFaction, defenderFaction });

            // Setup zone service
            _zoneServiceMock.Setup(z => z.GetAllZones())
                .Returns(new[] { targetZone, attackerOwnedZone });
            _zoneServiceMock.Setup(z => z.GetZonesByOwner("trevor"))
                .Returns(new[] { attackerOwnedZone });
            _zoneServiceMock.Setup(z => z.GetZone("vinewood"))
                .Returns(targetZone);

            // Setup strategy that will attack vinewood
            var strategyMock = new Mock<IAIStrategy>();
            strategyMock.Setup(s => s.MakeDecisions(It.IsAny<FactionWars.AI.Models.AIContext>()))
                .Returns(new List<FactionWars.AI.Models.AIDecision>
                {
                    new FactionWars.AI.Models.AIDecision(
                        FactionWars.AI.Models.AIDecisionType.Attack,
                        "vinewood",
                        0.8f,
                        10)
                });
            _strategies["trevor"] = strategyMock.Object;

            // KEY: A battle already exists in the target zone
            var existingBattle = new FactionWars.Combat.Models.ZoneBattle(
                attackerFactionId: "franklin",
                defenderFactionId: "michael",
                zoneId: "vinewood",
                attackerTroops: new Dictionary<FactionWars.Core.Models.DefenderTier, int>
                {
                    { FactionWars.Core.Models.DefenderTier.Basic, 5 }
                },
                defenderTroops: new Dictionary<FactionWars.Core.Models.DefenderTier, int>
                {
                    { FactionWars.Core.Models.DefenderTier.Basic, 3 }
                },
                playerFactionId: null);

            _zoneBattleManagerMock.Setup(m => m.GetBattleForZone("vinewood"))
                .Returns(existingBattle);

            // Simulate real ZoneBattleManager behavior: throws if battle already exists
            _zoneBattleManagerMock.Setup(m => m.StartBattle(
                    "vinewood",
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<Dictionary<FactionWars.Core.Models.DefenderTier, int>>(),
                    It.IsAny<Dictionary<FactionWars.Core.Models.DefenderTier, int>>()))
                .Throws(new System.InvalidOperationException("A battle already exists in zone 'vinewood'."));

            var controller = CreateController();
            controller.SetPlayerFactionId("michael"); // Player is michael, so trevor is AI
            controller.Start();

            // Act & Assert - should NOT throw, should skip the attack gracefully
            var exception = Record.Exception(() => controller.Update(90f));
            Assert.Null(exception);

            // StartBattle should NOT be called since a battle already exists
            _zoneBattleManagerMock.Verify(
                m => m.StartBattle(
                    "vinewood",
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<Dictionary<FactionWars.Core.Models.DefenderTier, int>>(),
                    It.IsAny<Dictionary<FactionWars.Core.Models.DefenderTier, int>>()),
                Times.Never);
        }

        [Fact]
        public void Update_After90Seconds_ShouldUseRecruitmentService()
        {
            // Arrange
            var recruitmentServiceMock = new Mock<IAIRecruitmentService>();
            var faction = new Faction("trevor", "Trevor", color: new FactionColor(255, 150, 0));

            _factionServiceMock.Setup(f => f.GetActiveFactions())
                .Returns(new[] { faction });

            var controller = CreateControllerWithRecruitmentService(recruitmentServiceMock.Object);
            controller.Start();

            // Act - simulate 90 seconds (recruitment interval)
            controller.Update(90f);

            // Assert - should call recruitment service, not internal hardcoded logic
            recruitmentServiceMock.Verify(r => r.TryAutoRecruit("trevor", It.IsAny<int>()), Times.Once);
        }

        [Fact]
        public void Update_ShouldSkipPlayerFactionWhenUsingRecruitmentService()
        {
            // Arrange
            var recruitmentServiceMock = new Mock<IAIRecruitmentService>();
            var playerFaction = new Faction("michael", "Michael", color: new FactionColor(0, 100, 255));
            var aiFaction = new Faction("trevor", "Trevor", color: new FactionColor(255, 150, 0));

            _factionServiceMock.Setup(f => f.GetActiveFactions())
                .Returns(new[] { playerFaction, aiFaction });

            var controller = CreateControllerWithRecruitmentService(recruitmentServiceMock.Object);
            controller.SetPlayerFactionId("michael");
            controller.Start();

            // Act
            controller.Update(90f);

            // Assert - should recruit for trevor but not michael
            recruitmentServiceMock.Verify(r => r.TryAutoRecruit("trevor", It.IsAny<int>()), Times.Once);
            recruitmentServiceMock.Verify(r => r.TryAutoRecruit("michael", It.IsAny<int>()), Times.Never);
        }

        private AIController CreateController()
        {
            return CreateController(new AIConfig());
        }

        private AIController CreateController(AIConfig config)
        {
            return new AIController(new AIControllerDependencies
            {
                FactionService = _factionServiceMock.Object,
                ZoneService = _zoneServiceMock.Object,
                BattleSimulationService = _battleSimulationServiceMock.Object,
                AllocationService = _allocationServiceMock.Object,
                GameBridge = _gameBridgeMock.Object,
                Strategies = _strategies,
                ZoneBattleManager = _zoneBattleManagerMock.Object,
                AIConfig = config
            });
        }

        private AIController CreateControllerWithRecruitmentService(IAIRecruitmentService recruitmentService)
        {
            return new AIController(
                _factionServiceMock.Object,
                _zoneServiceMock.Object,
                _battleSimulationServiceMock.Object,
                _allocationServiceMock.Object,
                _gameBridgeMock.Object,
                _strategies,
                _zoneBattleManagerMock.Object,
                recruitmentService);
        }

        #region ExecuteDefendDecision Tests

        [Fact]
        public void ExecuteDefendDecision_WithOneZone_Deploys80PercentOfReserves()
        {
            // Arrange
            var faction = new Faction("michael", "Michael", color: new FactionColor(0, 100, 255));
            var factionState = new FactionState("michael", 100000, 0);
            factionState.AddZone("richman"); // Only 1 zone
            factionState.AddReserveTroops(FactionWars.Core.Models.DefenderTier.Basic, 100);

            var zone = new Zone("richman", "Richman", new FactionWars.Core.Interfaces.Vector3(-1600f, 200f, 30f), 250f, 6);
            zone.OwnerFactionId = "michael";

            _factionServiceMock.Setup(f => f.GetActiveFactions())
                .Returns(new[] { faction });
            _factionServiceMock.Setup(f => f.GetFaction("michael"))
                .Returns(faction);
            _factionServiceMock.Setup(f => f.GetFactionState("michael"))
                .Returns(factionState);
            _factionServiceMock.Setup(f => f.GetAllFactions())
                .Returns(new[] { faction });

            _zoneServiceMock.Setup(z => z.GetAllZones())
                .Returns(new[] { zone });
            _zoneServiceMock.Setup(z => z.GetZonesByOwner("michael"))
                .Returns(new[] { zone });

            // Setup strategy that returns a Defend decision
            var strategyMock = new Mock<IAIStrategy>();
            strategyMock.Setup(s => s.MakeDecisions(It.IsAny<FactionWars.AI.Models.AIContext>()))
                .Returns(new List<FactionWars.AI.Models.AIDecision>
                {
                    new FactionWars.AI.Models.AIDecision(
                        FactionWars.AI.Models.AIDecisionType.Defend,
                        "richman",
                        0.8f,
                        0) // Troops field not used for defend
                });
            _strategies["michael"] = strategyMock.Object;

            // Setup allocation service to accept troops
            _allocationServiceMock.Setup(a => a.AllocateTroops(
                    factionState,
                    "richman",
                    FactionWars.Core.Models.DefenderTier.Basic,
                    80)) // 80% of 100 = 80
                .Returns(true);

            var controller = CreateController();
            controller.Start();

            // Act - trigger decision cycle (90 seconds)
            controller.Update(90f);

            // Assert - should allocate 80% of reserves (80 Basic troops)
            _allocationServiceMock.Verify(a => a.AllocateTroops(
                factionState,
                "richman",
                FactionWars.Core.Models.DefenderTier.Basic,
                80), Times.Once);
        }

        [Fact]
        public void ExecuteDefendDecision_WithTwoZones_Deploys50PercentOfReserves()
        {
            // Arrange
            var faction = new Faction("michael", "Michael", color: new FactionColor(0, 100, 255));
            var factionState = new FactionState("michael", 100000, 0);
            factionState.AddZone("richman");
            factionState.AddZone("vinewood"); // 2 zones
            factionState.AddReserveTroops(FactionWars.Core.Models.DefenderTier.Basic, 100);

            var richman = new Zone("richman", "Richman", new FactionWars.Core.Interfaces.Vector3(-1600f, 200f, 30f), 250f, 6);
            richman.OwnerFactionId = "michael";
            var vinewood = new Zone("vinewood", "Vinewood", new FactionWars.Core.Interfaces.Vector3(320f, 180f, 70f), 200f, 7);
            vinewood.OwnerFactionId = "michael";

            _factionServiceMock.Setup(f => f.GetActiveFactions())
                .Returns(new[] { faction });
            _factionServiceMock.Setup(f => f.GetFaction("michael"))
                .Returns(faction);
            _factionServiceMock.Setup(f => f.GetFactionState("michael"))
                .Returns(factionState);
            _factionServiceMock.Setup(f => f.GetAllFactions())
                .Returns(new[] { faction });

            _zoneServiceMock.Setup(z => z.GetAllZones())
                .Returns(new[] { richman, vinewood });
            _zoneServiceMock.Setup(z => z.GetZonesByOwner("michael"))
                .Returns(new[] { richman, vinewood });

            var strategyMock = new Mock<IAIStrategy>();
            strategyMock.Setup(s => s.MakeDecisions(It.IsAny<FactionWars.AI.Models.AIContext>()))
                .Returns(new List<FactionWars.AI.Models.AIDecision>
                {
                    new FactionWars.AI.Models.AIDecision(
                        FactionWars.AI.Models.AIDecisionType.Defend,
                        "richman",
                        0.8f,
                        0)
                });
            _strategies["michael"] = strategyMock.Object;

            _allocationServiceMock.Setup(a => a.AllocateTroops(
                    factionState,
                    "richman",
                    FactionWars.Core.Models.DefenderTier.Basic,
                    50)) // 50% of 100 = 50
                .Returns(true);

            var controller = CreateController();
            controller.Start();

            // Act
            controller.Update(90f);

            // Assert - should allocate 50% of reserves
            _allocationServiceMock.Verify(a => a.AllocateTroops(
                factionState,
                "richman",
                FactionWars.Core.Models.DefenderTier.Basic,
                50), Times.Once);
        }

        [Fact]
        public void ExecuteDefendDecision_WithThreeOrMoreZones_Deploys30PercentOfReserves()
        {
            // Arrange
            var faction = new Faction("michael", "Michael", color: new FactionColor(0, 100, 255));
            var factionState = new FactionState("michael", 100000, 0);
            factionState.AddZone("richman");
            factionState.AddZone("vinewood");
            factionState.AddZone("rockford"); // 3 zones
            factionState.AddReserveTroops(FactionWars.Core.Models.DefenderTier.Basic, 100);

            var richman = new Zone("richman", "Richman", new FactionWars.Core.Interfaces.Vector3(-1600f, 200f, 30f), 250f, 6);
            richman.OwnerFactionId = "michael";

            _factionServiceMock.Setup(f => f.GetActiveFactions())
                .Returns(new[] { faction });
            _factionServiceMock.Setup(f => f.GetFaction("michael"))
                .Returns(faction);
            _factionServiceMock.Setup(f => f.GetFactionState("michael"))
                .Returns(factionState);
            _factionServiceMock.Setup(f => f.GetAllFactions())
                .Returns(new[] { faction });

            _zoneServiceMock.Setup(z => z.GetAllZones())
                .Returns(new[] { richman });
            _zoneServiceMock.Setup(z => z.GetZonesByOwner("michael"))
                .Returns(new[] { richman });

            var strategyMock = new Mock<IAIStrategy>();
            strategyMock.Setup(s => s.MakeDecisions(It.IsAny<FactionWars.AI.Models.AIContext>()))
                .Returns(new List<FactionWars.AI.Models.AIDecision>
                {
                    new FactionWars.AI.Models.AIDecision(
                        FactionWars.AI.Models.AIDecisionType.Defend,
                        "richman",
                        0.8f,
                        0)
                });
            _strategies["michael"] = strategyMock.Object;

            _allocationServiceMock.Setup(a => a.AllocateTroops(
                    factionState,
                    "richman",
                    FactionWars.Core.Models.DefenderTier.Basic,
                    30)) // 30% of 100 = 30
                .Returns(true);

            var controller = CreateController();
            controller.Start();

            // Act
            controller.Update(90f);

            // Assert - should allocate 30% of reserves
            _allocationServiceMock.Verify(a => a.AllocateTroops(
                factionState,
                "richman",
                FactionWars.Core.Models.DefenderTier.Basic,
                30), Times.Once);
        }

        [Fact]
        public void ExecuteDefendDecision_DeploysAllTiersProportionally()
        {
            // Arrange
            var faction = new Faction("michael", "Michael", color: new FactionColor(0, 100, 255));
            var factionState = new FactionState("michael", 100000, 0);
            factionState.AddZone("richman");
            factionState.AddZone("vinewood"); // 2 zones = 50%
            factionState.AddReserveTroops(FactionWars.Core.Models.DefenderTier.Basic, 100);
            factionState.AddReserveTroops(FactionWars.Core.Models.DefenderTier.Medium, 50);
            factionState.AddReserveTroops(FactionWars.Core.Models.DefenderTier.Heavy, 20);

            var richman = new Zone("richman", "Richman", new FactionWars.Core.Interfaces.Vector3(-1600f, 200f, 30f), 250f, 6);
            richman.OwnerFactionId = "michael";

            _factionServiceMock.Setup(f => f.GetActiveFactions())
                .Returns(new[] { faction });
            _factionServiceMock.Setup(f => f.GetFaction("michael"))
                .Returns(faction);
            _factionServiceMock.Setup(f => f.GetFactionState("michael"))
                .Returns(factionState);
            _factionServiceMock.Setup(f => f.GetAllFactions())
                .Returns(new[] { faction });

            _zoneServiceMock.Setup(z => z.GetAllZones())
                .Returns(new[] { richman });
            _zoneServiceMock.Setup(z => z.GetZonesByOwner("michael"))
                .Returns(new[] { richman });

            var strategyMock = new Mock<IAIStrategy>();
            strategyMock.Setup(s => s.MakeDecisions(It.IsAny<FactionWars.AI.Models.AIContext>()))
                .Returns(new List<FactionWars.AI.Models.AIDecision>
                {
                    new FactionWars.AI.Models.AIDecision(
                        FactionWars.AI.Models.AIDecisionType.Defend,
                        "richman",
                        0.8f,
                        0)
                });
            _strategies["michael"] = strategyMock.Object;

            // Setup allocation service to accept all tiers
            _allocationServiceMock.Setup(a => a.AllocateTroops(
                    factionState, "richman", It.IsAny<FactionWars.Core.Models.DefenderTier>(), It.IsAny<int>()))
                .Returns(true);

            var controller = CreateController();
            controller.Start();

            // Act
            controller.Update(90f);

            // Assert - should allocate 50% of each tier
            _allocationServiceMock.Verify(a => a.AllocateTroops(
                factionState, "richman", FactionWars.Core.Models.DefenderTier.Basic, 50), Times.Once);
            _allocationServiceMock.Verify(a => a.AllocateTroops(
                factionState, "richman", FactionWars.Core.Models.DefenderTier.Medium, 25), Times.Once);
            _allocationServiceMock.Verify(a => a.AllocateTroops(
                factionState, "richman", FactionWars.Core.Models.DefenderTier.Heavy, 10), Times.Once);
        }

        [Fact]
        public void ExecuteDefendDecision_WithNoReserves_DoesNotAllocate()
        {
            // Arrange
            var faction = new Faction("michael", "Michael", color: new FactionColor(0, 100, 255));
            var factionState = new FactionState("michael", 100000, 0);
            factionState.AddZone("richman");
            // No reserves added

            var richman = new Zone("richman", "Richman", new FactionWars.Core.Interfaces.Vector3(-1600f, 200f, 30f), 250f, 6);
            richman.OwnerFactionId = "michael";

            _factionServiceMock.Setup(f => f.GetActiveFactions())
                .Returns(new[] { faction });
            _factionServiceMock.Setup(f => f.GetFaction("michael"))
                .Returns(faction);
            _factionServiceMock.Setup(f => f.GetFactionState("michael"))
                .Returns(factionState);
            _factionServiceMock.Setup(f => f.GetAllFactions())
                .Returns(new[] { faction });

            _zoneServiceMock.Setup(z => z.GetAllZones())
                .Returns(new[] { richman });
            _zoneServiceMock.Setup(z => z.GetZonesByOwner("michael"))
                .Returns(new[] { richman });

            var strategyMock = new Mock<IAIStrategy>();
            strategyMock.Setup(s => s.MakeDecisions(It.IsAny<FactionWars.AI.Models.AIContext>()))
                .Returns(new List<FactionWars.AI.Models.AIDecision>
                {
                    new FactionWars.AI.Models.AIDecision(
                        FactionWars.AI.Models.AIDecisionType.Defend,
                        "richman",
                        0.8f,
                        0)
                });
            _strategies["michael"] = strategyMock.Object;

            var controller = CreateController();
            controller.Start();

            // Act
            controller.Update(90f);

            // Assert - should NOT allocate any troops
            _allocationServiceMock.Verify(a => a.AllocateTroops(
                It.IsAny<FactionState>(),
                It.IsAny<string>(),
                It.IsAny<FactionWars.Core.Models.DefenderTier>(),
                It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public void ExecuteDefendDecision_ActiveBattle_RepeatedReinforcementDecaysDeploymentAmount()
        {
            var factionState = SetupDefendScenario(zoneCount: 2, reserveBasic: 100);
            var battle = CreateBattleForZone("richman", "michael");
            _zoneBattleManagerMock.Setup(b => b.GetBattleForZone("richman")).Returns(battle);
            _allocationServiceMock.Setup(a => a.AllocateTroops(
                    factionState, "richman", DefenderTier.Basic, It.IsAny<int>()))
                .Returns(true);

            var controller = CreateController(new AIConfig
            {
                ReinforcementDeploymentDecayMultiplier = 0.5f
            });
            controller.Start();

            controller.Update(90f);
            controller.Update(90f);

            _allocationServiceMock.Verify(a => a.AllocateTroops(
                factionState, "richman", DefenderTier.Basic, 50), Times.Once);
            _allocationServiceMock.Verify(a => a.AllocateTroops(
                factionState, "richman", DefenderTier.Basic, 25), Times.Once);
        }

        [Fact]
        public void ExecuteDefendDecision_BattleEnded_ResetsReinforcementDecay()
        {
            var factionState = SetupDefendScenario(zoneCount: 2, reserveBasic: 100);
            var battle = CreateBattleForZone("richman", "michael");
            _zoneBattleManagerMock.Setup(b => b.GetBattleForZone("richman")).Returns(battle);
            _allocationServiceMock.Setup(a => a.AllocateTroops(
                    factionState, "richman", DefenderTier.Basic, It.IsAny<int>()))
                .Returns(true);

            var controller = CreateController(new AIConfig
            {
                ReinforcementDeploymentDecayMultiplier = 0.5f
            });
            controller.Start();

            controller.Update(90f);
            _zoneBattleManagerMock.Raise(b => b.BattleEnded += null, battle, BattleOutcome.DefendersWon);
            controller.Update(90f);

            _allocationServiceMock.Verify(a => a.AllocateTroops(
                factionState, "richman", DefenderTier.Basic, 50), Times.Exactly(2));
        }

        [Fact]
        public void ExecuteDefendDecision_NoActiveBattle_DoesNotApplyReinforcementDecay()
        {
            var factionState = SetupDefendScenario(zoneCount: 2, reserveBasic: 100);
            _zoneBattleManagerMock.Setup(b => b.GetBattleForZone("richman")).Returns((ZoneBattle?)null);
            _allocationServiceMock.Setup(a => a.AllocateTroops(
                    factionState, "richman", DefenderTier.Basic, It.IsAny<int>()))
                .Returns(true);

            var controller = CreateController(new AIConfig
            {
                ReinforcementDeploymentDecayMultiplier = 0.5f
            });
            controller.Start();

            controller.Update(90f);
            controller.Update(90f);

            _allocationServiceMock.Verify(a => a.AllocateTroops(
                factionState, "richman", DefenderTier.Basic, 50), Times.Exactly(2));
        }

        private FactionState SetupDefendScenario(int zoneCount, int reserveBasic)
        {
            var faction = new Faction("michael", "Michael", color: new FactionColor(0, 100, 255));
            var factionState = new FactionState("michael", 100000, 0);
            for (int i = 0; i < zoneCount; i++)
            {
                factionState.AddZone("zone-" + i);
            }
            factionState.AddReserveTroops(DefenderTier.Basic, reserveBasic);

            var richman = new Zone("richman", "Richman", new Vector3(-1600f, 200f, 30f), 250f, 6)
            {
                OwnerFactionId = "michael"
            };

            _factionServiceMock.Setup(f => f.GetActiveFactions()).Returns(new[] { faction });
            _factionServiceMock.Setup(f => f.GetFaction("michael")).Returns(faction);
            _factionServiceMock.Setup(f => f.GetFactionState("michael")).Returns(factionState);
            _factionServiceMock.Setup(f => f.GetAllFactions()).Returns(new[] { faction });
            _zoneServiceMock.Setup(z => z.GetAllZones()).Returns(new[] { richman });
            _zoneServiceMock.Setup(z => z.GetZonesByOwner("michael")).Returns(new[] { richman });

            var strategyMock = new Mock<IAIStrategy>();
            strategyMock.Setup(s => s.MakeDecisions(It.IsAny<AIContext>()))
                .Returns(new List<AIDecision>
                {
                    new AIDecision(AIDecisionType.Defend, "richman", 0.8f, 0)
                });
            _strategies["michael"] = strategyMock.Object;
            return factionState;
        }

        private static ZoneBattle CreateBattleForZone(string zoneId, string defenderFactionId)
            => new ZoneBattle(
                "trevor",
                defenderFactionId,
                zoneId,
                new Dictionary<DefenderTier, int> { { DefenderTier.Basic, 10 } },
                new Dictionary<DefenderTier, int> { { DefenderTier.Basic, 10 } });

        #endregion

        #region Attack Cost Removal Tests

        [Fact]
        public void ExecuteAttack_ShouldNotSpendCash_AttacksAreFreeOnceRecruited()
        {
            // Arrange: Faction with troops but minimal cash should be able to attack
            // This test verifies the removal of deployment cost - troops are free to deploy once recruited
            var attackerFaction = new Faction("trevor", "Trevor", color: new FactionColor(255, 150, 0));
            var defenderFaction = new Faction("michael", "Michael", color: new FactionColor(0, 100, 255));

            // Trevor has 50 troops but only $50 cash - under old system this would block attack
            var factionState = new FactionState("trevor", 50, 50);

            var targetZone = new Zone("vinewood", "Vinewood", new FactionWars.Core.Interfaces.Vector3(100f, 100f, 30f), 100f, 5);
            targetZone.OwnerFactionId = "michael";

            var attackerOwnedZone = new Zone("davis", "Davis", new FactionWars.Core.Interfaces.Vector3(200f, 200f, 30f), 100f, 3);
            attackerOwnedZone.OwnerFactionId = "trevor";
            attackerOwnedZone.AdjacentZoneIds.Add("vinewood");

            _factionServiceMock.Setup(f => f.GetActiveFactions())
                .Returns(new[] { attackerFaction, defenderFaction });
            _factionServiceMock.Setup(f => f.GetFaction("trevor"))
                .Returns(attackerFaction);
            _factionServiceMock.Setup(f => f.GetFactionState("trevor"))
                .Returns(factionState);
            _factionServiceMock.Setup(f => f.GetAllFactions())
                .Returns(new[] { attackerFaction, defenderFaction });

            _zoneServiceMock.Setup(z => z.GetAllZones())
                .Returns(new[] { targetZone, attackerOwnedZone });
            _zoneServiceMock.Setup(z => z.GetZonesByOwner("trevor"))
                .Returns(new[] { attackerOwnedZone });
            _zoneServiceMock.Setup(z => z.GetZone("vinewood"))
                .Returns(targetZone);

            // Setup strategy that attacks with 20 troops
            var strategyMock = new Mock<IAIStrategy>();
            strategyMock.Setup(s => s.MakeDecisions(It.IsAny<FactionWars.AI.Models.AIContext>()))
                .Returns(new List<FactionWars.AI.Models.AIDecision>
                {
                    new FactionWars.AI.Models.AIDecision(
                        FactionWars.AI.Models.AIDecisionType.Attack,
                        "vinewood",
                        0.8f,
                        20) // 20 troops committed
                });
            _strategies["trevor"] = strategyMock.Object;

            // Setup battle manager to accept the battle
            var battle = new FactionWars.Combat.Models.ZoneBattle(
                attackerFactionId: "trevor",
                defenderFactionId: "michael",
                zoneId: "vinewood",
                attackerTroops: new Dictionary<FactionWars.Core.Models.DefenderTier, int>
                {
                    { FactionWars.Core.Models.DefenderTier.Basic, 20 }
                },
                defenderTroops: new Dictionary<FactionWars.Core.Models.DefenderTier, int>
                {
                    { FactionWars.Core.Models.DefenderTier.Basic, 5 }
                },
                playerFactionId: null);

            _zoneBattleManagerMock.Setup(m => m.GetBattleForZone("vinewood")).Returns((FactionWars.Combat.Models.ZoneBattle?)null);
            _zoneBattleManagerMock.Setup(m => m.StartBattle(
                    "vinewood",
                    "trevor",
                    "michael",
                    It.IsAny<Dictionary<FactionWars.Core.Models.DefenderTier, int>>(),
                    It.IsAny<Dictionary<FactionWars.Core.Models.DefenderTier, int>>()))
                .Returns(battle);

            var controller = CreateController();
            controller.SetPlayerFactionId("michael"); // Michael is player, Trevor is AI
            controller.Start();

            // Act - trigger decision cycle
            controller.Update(90f);

            // Assert: SpendCash should NEVER be called during attack execution
            // Attacks are free once troops are recruited
            _factionServiceMock.Verify(f => f.SpendCash("trevor", It.IsAny<int>()), Times.Never,
                "Attacks should NOT cost cash - troops are free to deploy once recruited");
        }

        [Fact]
        public void ExecuteAttack_WithLowCash_ShouldStillExecute()
        {
            // Arrange: Faction with troops but zero cash should still be able to attack
            var attackerFaction = new Faction("trevor", "Trevor", color: new FactionColor(255, 150, 0));
            var defenderFaction = new Faction("michael", "Michael", color: new FactionColor(0, 100, 255));

            // Trevor has 100 troops but $0 cash
            var factionState = new FactionState("trevor", 0, 100);

            var targetZone = new Zone("vinewood", "Vinewood", new FactionWars.Core.Interfaces.Vector3(100f, 100f, 30f), 100f, 5);
            targetZone.OwnerFactionId = "michael";

            var attackerOwnedZone = new Zone("davis", "Davis", new FactionWars.Core.Interfaces.Vector3(200f, 200f, 30f), 100f, 3);
            attackerOwnedZone.OwnerFactionId = "trevor";
            attackerOwnedZone.AdjacentZoneIds.Add("vinewood");

            _factionServiceMock.Setup(f => f.GetActiveFactions())
                .Returns(new[] { attackerFaction, defenderFaction });
            _factionServiceMock.Setup(f => f.GetFaction("trevor"))
                .Returns(attackerFaction);
            _factionServiceMock.Setup(f => f.GetFactionState("trevor"))
                .Returns(factionState);
            _factionServiceMock.Setup(f => f.GetAllFactions())
                .Returns(new[] { attackerFaction, defenderFaction });

            _zoneServiceMock.Setup(z => z.GetAllZones())
                .Returns(new[] { targetZone, attackerOwnedZone });
            _zoneServiceMock.Setup(z => z.GetZonesByOwner("trevor"))
                .Returns(new[] { attackerOwnedZone });
            _zoneServiceMock.Setup(z => z.GetZone("vinewood"))
                .Returns(targetZone);

            var strategyMock = new Mock<IAIStrategy>();
            strategyMock.Setup(s => s.MakeDecisions(It.IsAny<FactionWars.AI.Models.AIContext>()))
                .Returns(new List<FactionWars.AI.Models.AIDecision>
                {
                    new FactionWars.AI.Models.AIDecision(
                        FactionWars.AI.Models.AIDecisionType.Attack,
                        "vinewood",
                        0.8f,
                        50)
                });
            _strategies["trevor"] = strategyMock.Object;

            var battle = new FactionWars.Combat.Models.ZoneBattle(
                attackerFactionId: "trevor",
                defenderFactionId: "michael",
                zoneId: "vinewood",
                attackerTroops: new Dictionary<FactionWars.Core.Models.DefenderTier, int>
                {
                    { FactionWars.Core.Models.DefenderTier.Basic, 50 }
                },
                defenderTroops: new Dictionary<FactionWars.Core.Models.DefenderTier, int>(),
                playerFactionId: null);

            _zoneBattleManagerMock.Setup(m => m.GetBattleForZone("vinewood")).Returns((FactionWars.Combat.Models.ZoneBattle?)null);
            _zoneBattleManagerMock.Setup(m => m.StartBattle(
                    "vinewood",
                    "trevor",
                    "michael",
                    It.IsAny<Dictionary<FactionWars.Core.Models.DefenderTier, int>>(),
                    It.IsAny<Dictionary<FactionWars.Core.Models.DefenderTier, int>>()))
                .Returns(battle);

            var controller = CreateController();
            controller.SetPlayerFactionId("michael");
            controller.Start();

            // Act
            controller.Update(90f);

            // Assert: Battle should start even with $0 cash
            _zoneBattleManagerMock.Verify(m => m.StartBattle(
                "vinewood",
                "trevor",
                "michael",
                It.IsAny<Dictionary<FactionWars.Core.Models.DefenderTier, int>>(),
                It.IsAny<Dictionary<FactionWars.Core.Models.DefenderTier, int>>()), Times.Once,
                "Attack should execute with $0 cash - deployment is free");
        }

        #endregion

        #region OnTroopsRecruited Event Tests

        [Fact]
        public void RunRecruitment_WhenTroopsRecruited_RaisesEvent()
        {
            // Arrange
            var faction = new Faction("trevor", "Trevor", color: new FactionColor(255, 150, 0));
            var factionState = new FactionState("trevor", 1000, 5);

            _factionServiceMock.Setup(f => f.GetActiveFactions())
                .Returns(new[] { faction });
            _factionServiceMock.Setup(f => f.GetFactionState("trevor"))
                .Returns(factionState);

            // Wire SpendCash to actually mutate the state so cashAfter differs from cashBefore
            _factionServiceMock.Setup(f => f.SpendCash("trevor", It.IsAny<int>()))
                .Callback<string, int>((_, amount) => factionState.SpendCash(amount))
                .Returns(true);

            var controller = CreateController();
            controller.SetPlayerFactionId("michael");
            controller.Start();

            TroopsRecruitedEventArgs? captured = null;
            controller.OnTroopsRecruited += (_, args) => captured = args;

            // Act
            controller.Update(90f);

            // Assert
            Assert.NotNull(captured);
            Assert.Equal("trevor", captured!.FactionId);
            Assert.True(captured.TroopsRecruited > 0);
            Assert.True(captured.Cost > 0);
            Assert.Equal(captured.CashBefore - captured.Cost, captured.CashAfter);
        }

        [Fact]
        public void RunRecruitment_WhenNoTroopsRecruited_DoesNotRaiseEvent()
        {
            // Arrange: faction with $0 cash -> no recruitment possible
            var faction = new Faction("trevor", "Trevor", color: new FactionColor(255, 150, 0));
            var factionState = new FactionState("trevor", 0, 5); // $0 cash

            _factionServiceMock.Setup(f => f.GetActiveFactions())
                .Returns(new[] { faction });
            _factionServiceMock.Setup(f => f.GetFactionState("trevor"))
                .Returns(factionState);

            var controller = CreateController();
            controller.SetPlayerFactionId("michael");
            controller.Start();

            bool raised = false;
            controller.OnTroopsRecruited += (_, _) => raised = true;

            // Act
            controller.Update(90f);

            // Assert
            Assert.False(raised);
        }

        [Fact]
        public void RunRecruitment_WithRecruitmentService_RaisesEventWithServiceResults()
        {
            // Arrange: faction with cash, mock recruitment service returns 4 troops at cost 800.
            var faction = new Faction("trevor", "Trevor", color: new FactionColor(255, 150, 0));
            var factionState = new FactionState("trevor", 1000, 5);

            _factionServiceMock.Setup(f => f.GetActiveFactions())
                .Returns(new[] { faction });
            _factionServiceMock.Setup(f => f.GetFactionState("trevor"))
                .Returns(factionState);

            var recruitmentServiceMock = new Mock<IAIRecruitmentService>();
            recruitmentServiceMock.Setup(r => r.TryAutoRecruit("trevor", It.IsAny<int>()))
                .Callback<string, int>((_, _) => factionState.SpendCash(800))
                .Returns(4);

            var controller = CreateControllerWithRecruitmentService(recruitmentServiceMock.Object);
            controller.SetPlayerFactionId("michael");
            controller.Start();

            TroopsRecruitedEventArgs? captured = null;
            controller.OnTroopsRecruited += (_, args) => captured = args;

            // Act: trigger one recruitment cycle
            controller.Update(90f);

            // Assert
            Assert.NotNull(captured);
            Assert.Equal("trevor", captured!.FactionId);
            Assert.Equal(4, captured.TroopsRecruited);
            Assert.Equal(800, captured.Cost);
            Assert.Equal(1000, captured.CashBefore);
            Assert.Equal(200, captured.CashAfter);
        }

        #endregion
    }
}
