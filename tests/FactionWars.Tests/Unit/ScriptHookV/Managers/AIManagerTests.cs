using System;
using System.Collections.Generic;
using System.Linq;
using FactionWars.AI.Interfaces;
using FactionWars.AI.Models;
using FactionWars.Factions.Interfaces;
using FactionWars.Factions.Models;
using FactionWars.ScriptHookV.Managers;
using FactionWars.Territory.Interfaces;
using FactionWars.Territory.Models;
using Moq;
using Xunit;

namespace FactionWars.Tests.Unit.ScriptHookV.Managers
{
    public class AIManagerTests
    {
        private readonly Mock<IFactionService> _factionServiceMock;
        private readonly Mock<IZoneService> _zoneServiceMock;
        private readonly Mock<IAIStrategy> _michaelStrategyMock;
        private readonly Mock<IAIStrategy> _trevorStrategyMock;
        private readonly Mock<IAIStrategy> _franklinStrategyMock;
        private readonly AIManager _aiManager;

        public AIManagerTests()
        {
            _factionServiceMock = new Mock<IFactionService>();
            _zoneServiceMock = new Mock<IZoneService>();
            _michaelStrategyMock = new Mock<IAIStrategy>();
            _trevorStrategyMock = new Mock<IAIStrategy>();
            _franklinStrategyMock = new Mock<IAIStrategy>();

            _michaelStrategyMock.Setup(s => s.FactionType).Returns(FactionType.Michael);
            _trevorStrategyMock.Setup(s => s.FactionType).Returns(FactionType.Trevor);
            _franklinStrategyMock.Setup(s => s.FactionType).Returns(FactionType.Franklin);

            var strategies = new Dictionary<string, IAIStrategy>
            {
                { "michael", _michaelStrategyMock.Object },
                { "trevor", _trevorStrategyMock.Object },
                { "franklin", _franklinStrategyMock.Object }
            };

            _aiManager = new AIManager(
                _factionServiceMock.Object,
                _zoneServiceMock.Object,
                strategies);
        }

        #region Constructor Tests

        [Fact]
        public void Constructor_WithNullFactionService_ThrowsArgumentNullException()
        {
            var strategies = new Dictionary<string, IAIStrategy>();

            Assert.Throws<ArgumentNullException>(() => new AIManager(
                null!,
                _zoneServiceMock.Object,
                strategies));
        }

        [Fact]
        public void Constructor_WithNullZoneService_ThrowsArgumentNullException()
        {
            var strategies = new Dictionary<string, IAIStrategy>();

            Assert.Throws<ArgumentNullException>(() => new AIManager(
                _factionServiceMock.Object,
                null!,
                strategies));
        }

        [Fact]
        public void Constructor_WithNullStrategies_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new AIManager(
                _factionServiceMock.Object,
                _zoneServiceMock.Object,
                null!));
        }

        [Fact]
        public void Constructor_WithValidParameters_Succeeds()
        {
            var strategies = new Dictionary<string, IAIStrategy>();

            var manager = new AIManager(
                _factionServiceMock.Object,
                _zoneServiceMock.Object,
                strategies);

            Assert.NotNull(manager);
            Assert.False(manager.IsRunning);
        }

        #endregion

        #region Start/Stop Tests

        [Fact]
        public void Start_SetsIsRunningTrue()
        {
            _aiManager.Start();

            Assert.True(_aiManager.IsRunning);
        }

        [Fact]
        public void Stop_SetsIsRunningFalse()
        {
            _aiManager.Start();
            _aiManager.Stop();

            Assert.False(_aiManager.IsRunning);
        }

        #endregion

        #region SetPlayerFactionId Tests

        [Fact]
        public void SetPlayerFactionId_SetsPlayerId()
        {
            _aiManager.SetPlayerFactionId("michael");

            Assert.Equal("michael", _aiManager.GetPlayerFactionId());
        }

        [Fact]
        public void SetPlayerFactionId_WithNull_ClearsPlayerId()
        {
            _aiManager.SetPlayerFactionId("michael");
            _aiManager.SetPlayerFactionId(null);

            Assert.Null(_aiManager.GetPlayerFactionId());
        }

        #endregion

        #region Update Tests

        [Fact]
        public void Update_WhenNotRunning_DoesNotMakeDecisions()
        {
            _aiManager.Update(1.0f);

            _michaelStrategyMock.Verify(s => s.MakeDecisions(It.IsAny<AIContext>()), Times.Never);
        }

        [Fact]
        public void Update_WhenRunning_AccumulatesTimeUntilInterval()
        {
            SetupFactionData();
            _aiManager.Start();

            // Update with less than decision interval
            _aiManager.Update(0.5f);

            // No decisions yet because interval not reached
            _michaelStrategyMock.Verify(s => s.MakeDecisions(It.IsAny<AIContext>()), Times.Never);
        }

        [Fact]
        public void Update_AfterDecisionInterval_MakesDecisionsForAIFactions()
        {
            SetupFactionData();
            _aiManager.Start();
            _aiManager.SetPlayerFactionId("michael"); // Michael is player

            // Setup strategy to return no decisions
            _trevorStrategyMock.Setup(s => s.MakeDecisions(It.IsAny<AIContext>()))
                .Returns(new List<AIDecision>());
            _franklinStrategyMock.Setup(s => s.MakeDecisions(It.IsAny<AIContext>()))
                .Returns(new List<AIDecision>());

            // Update with enough time to trigger decisions (default interval is 30 seconds)
            _aiManager.Update(30.0f);

            // Trevor and Franklin are AI, Michael is player - should NOT call Michael's strategy
            _michaelStrategyMock.Verify(s => s.MakeDecisions(It.IsAny<AIContext>()), Times.Never);
            _trevorStrategyMock.Verify(s => s.MakeDecisions(It.IsAny<AIContext>()), Times.Once);
            _franklinStrategyMock.Verify(s => s.MakeDecisions(It.IsAny<AIContext>()), Times.Once);
        }

        [Fact]
        public void Update_ExcludesPlayerFactionFromAIDecisions()
        {
            SetupFactionData();
            _aiManager.Start();
            _aiManager.SetPlayerFactionId("trevor"); // Trevor is player

            _michaelStrategyMock.Setup(s => s.MakeDecisions(It.IsAny<AIContext>()))
                .Returns(new List<AIDecision>());
            _franklinStrategyMock.Setup(s => s.MakeDecisions(It.IsAny<AIContext>()))
                .Returns(new List<AIDecision>());

            _aiManager.Update(30.0f);

            // Michael and Franklin are AI, Trevor is player
            _michaelStrategyMock.Verify(s => s.MakeDecisions(It.IsAny<AIContext>()), Times.Once);
            _trevorStrategyMock.Verify(s => s.MakeDecisions(It.IsAny<AIContext>()), Times.Never);
            _franklinStrategyMock.Verify(s => s.MakeDecisions(It.IsAny<AIContext>()), Times.Once);
        }

        [Fact]
        public void Update_BuildsCorrectAIContext()
        {
            SetupFactionData();
            _aiManager.Start();
            _aiManager.SetPlayerFactionId("michael");

            AIContext? capturedContext = null;
            _trevorStrategyMock.Setup(s => s.MakeDecisions(It.IsAny<AIContext>()))
                .Callback<AIContext>(ctx => capturedContext = ctx)
                .Returns(new List<AIDecision>());
            _franklinStrategyMock.Setup(s => s.MakeDecisions(It.IsAny<AIContext>()))
                .Returns(new List<AIDecision>());

            _aiManager.Update(30.0f);

            Assert.NotNull(capturedContext);
            Assert.Equal("trevor", capturedContext!.Faction.Id);
            Assert.NotNull(capturedContext.FactionState);
            Assert.NotEmpty(capturedContext.AllZones);
            Assert.NotEmpty(capturedContext.EnemyFactions);
        }

        #endregion

        #region DecisionInterval Tests

        [Fact]
        public void DecisionIntervalSeconds_DefaultValue_Is30()
        {
            Assert.Equal(30.0f, _aiManager.DecisionIntervalSeconds);
        }

        [Fact]
        public void SetDecisionInterval_UpdatesInterval()
        {
            _aiManager.SetDecisionInterval(10.0f);

            Assert.Equal(10.0f, _aiManager.DecisionIntervalSeconds);
        }

        [Fact]
        public void SetDecisionInterval_WithNegativeValue_ClampsToMinimum()
        {
            _aiManager.SetDecisionInterval(-5.0f);

            Assert.True(_aiManager.DecisionIntervalSeconds >= 1.0f);
        }

        #endregion

        #region OnAIDecision Event Tests

        [Fact]
        public void Update_WhenDecisionMade_RaisesOnAIDecisionEvent()
        {
            SetupFactionData();
            _aiManager.Start();
            _aiManager.SetPlayerFactionId("michael");

            var attackDecision = new AIDecision(AIDecisionType.Attack, "zone1", 0.8f, 10);
            _trevorStrategyMock.Setup(s => s.MakeDecisions(It.IsAny<AIContext>()))
                .Returns(new List<AIDecision> { attackDecision });
            _franklinStrategyMock.Setup(s => s.MakeDecisions(It.IsAny<AIContext>()))
                .Returns(new List<AIDecision>());

            AIDecisionEventArgs? receivedArgs = null;
            _aiManager.OnAIDecision += (sender, args) => receivedArgs = args;

            _aiManager.Update(30.0f);

            Assert.NotNull(receivedArgs);
            Assert.Equal("trevor", receivedArgs!.FactionId);
            Assert.Equal(attackDecision, receivedArgs.Decision);
        }

        #endregion

        #region ForceDecision Tests

        [Fact]
        public void ForceDecision_WhenNotRunning_MakesDecisionsImmediately()
        {
            SetupFactionData();
            _aiManager.SetPlayerFactionId("michael");

            _trevorStrategyMock.Setup(s => s.MakeDecisions(It.IsAny<AIContext>()))
                .Returns(new List<AIDecision>());
            _franklinStrategyMock.Setup(s => s.MakeDecisions(It.IsAny<AIContext>()))
                .Returns(new List<AIDecision>());

            _aiManager.ForceDecision();

            _trevorStrategyMock.Verify(s => s.MakeDecisions(It.IsAny<AIContext>()), Times.Once);
            _franklinStrategyMock.Verify(s => s.MakeDecisions(It.IsAny<AIContext>()), Times.Once);
        }

        [Fact]
        public void ForceDecision_ForSpecificFaction_OnlyMakesDecisionForThatFaction()
        {
            SetupFactionData();
            _aiManager.SetPlayerFactionId("michael");

            _trevorStrategyMock.Setup(s => s.MakeDecisions(It.IsAny<AIContext>()))
                .Returns(new List<AIDecision>());

            _aiManager.ForceDecision("trevor");

            _trevorStrategyMock.Verify(s => s.MakeDecisions(It.IsAny<AIContext>()), Times.Once);
            _franklinStrategyMock.Verify(s => s.MakeDecisions(It.IsAny<AIContext>()), Times.Never);
        }

        [Fact]
        public void ForceDecision_ForPlayerFaction_DoesNotMakeDecision()
        {
            SetupFactionData();
            _aiManager.SetPlayerFactionId("trevor");

            _aiManager.ForceDecision("trevor");

            _trevorStrategyMock.Verify(s => s.MakeDecisions(It.IsAny<AIContext>()), Times.Never);
        }

        #endregion

        #region GetLastDecisions Tests

        [Fact]
        public void GetLastDecisions_BeforeAnyDecisions_ReturnsEmptyList()
        {
            var decisions = _aiManager.GetLastDecisions("trevor");

            Assert.Empty(decisions);
        }

        [Fact]
        public void GetLastDecisions_AfterDecisions_ReturnsLastDecisions()
        {
            SetupFactionData();
            _aiManager.Start();
            _aiManager.SetPlayerFactionId("michael");

            var attackDecision = new AIDecision(AIDecisionType.Attack, "zone1", 0.8f, 10);
            _trevorStrategyMock.Setup(s => s.MakeDecisions(It.IsAny<AIContext>()))
                .Returns(new List<AIDecision> { attackDecision });
            _franklinStrategyMock.Setup(s => s.MakeDecisions(It.IsAny<AIContext>()))
                .Returns(new List<AIDecision>());

            _aiManager.Update(30.0f);

            var decisions = _aiManager.GetLastDecisions("trevor");

            Assert.Single(decisions);
            Assert.Equal(attackDecision, decisions[0]);
        }

        #endregion

        #region Helper Methods

        private void SetupFactionData()
        {
            var michaelFaction = new Faction("michael", "Michael's Crew", "Michael De Santa");
            var trevorFaction = new Faction("trevor", "Trevor's Gang", "Trevor Philips");
            var franklinFaction = new Faction("franklin", "Franklin's Hustle", "Franklin Clinton");

            var michaelState = new FactionState("michael", 10000, 50);
            var trevorState = new FactionState("trevor", 8000, 60);
            var franklinState = new FactionState("franklin", 5000, 30);

            var zones = new List<Zone>
            {
                new Zone("zone1", "Downtown", new FactionWars.Core.Interfaces.Vector3(0, 0, 0), 100f, 10) { OwnerFactionId = "michael", Traits = ZoneTrait.None },
                new Zone("zone2", "Industrial", new FactionWars.Core.Interfaces.Vector3(100, 0, 0), 100f, 8) { OwnerFactionId = "trevor", Traits = ZoneTrait.Industrial },
                new Zone("zone3", "Residential", new FactionWars.Core.Interfaces.Vector3(200, 0, 0), 100f, 6) { OwnerFactionId = "franklin", Traits = ZoneTrait.Residential }
            };

            _factionServiceMock.Setup(f => f.GetAllFactions())
                .Returns(new[] { michaelFaction, trevorFaction, franklinFaction });
            _factionServiceMock.Setup(f => f.GetActiveFactions())
                .Returns(new[] { michaelFaction, trevorFaction, franklinFaction });
            _factionServiceMock.Setup(f => f.GetFaction("michael"))
                .Returns(michaelFaction);
            _factionServiceMock.Setup(f => f.GetFaction("trevor"))
                .Returns(trevorFaction);
            _factionServiceMock.Setup(f => f.GetFaction("franklin"))
                .Returns(franklinFaction);
            _factionServiceMock.Setup(f => f.GetFactionState("michael"))
                .Returns(michaelState);
            _factionServiceMock.Setup(f => f.GetFactionState("trevor"))
                .Returns(trevorState);
            _factionServiceMock.Setup(f => f.GetFactionState("franklin"))
                .Returns(franklinState);

            _zoneServiceMock.Setup(z => z.GetAllZones())
                .Returns(zones);
            _zoneServiceMock.Setup(z => z.GetZonesByOwner("michael"))
                .Returns(zones.Where(z => z.OwnerFactionId == "michael"));
            _zoneServiceMock.Setup(z => z.GetZonesByOwner("trevor"))
                .Returns(zones.Where(z => z.OwnerFactionId == "trevor"));
            _zoneServiceMock.Setup(z => z.GetZonesByOwner("franklin"))
                .Returns(zones.Where(z => z.OwnerFactionId == "franklin"));
        }

        #endregion
    }
}
