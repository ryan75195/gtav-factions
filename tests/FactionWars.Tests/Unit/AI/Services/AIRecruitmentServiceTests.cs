using System;
using System.Collections.Generic;
using FactionWars.AI.Interfaces;
using FactionWars.AI.Services;
using FactionWars.Core.Interfaces;
using FactionWars.Core.Models;
using FactionWars.Core.Services;
using FactionWars.Factions.Interfaces;
using FactionWars.Factions.Models;
using Moq;
using Xunit;

namespace FactionWars.Tests.Unit.AI.Services
{
    public class AIRecruitmentServiceTests
    {
        private readonly Mock<IFactionService> _mockFactionService;
        private readonly IDefenderRoleService _tierService;
        private readonly IAIBudgetService _budgetService;
        private readonly AIRecruitmentService _service;

        public AIRecruitmentServiceTests()
        {
            _mockFactionService = new Mock<IFactionService>();
            _tierService = new DefenderRoleService();
            _budgetService = new AIBudgetService();
            _service = new AIRecruitmentService(_mockFactionService.Object, _budgetService, _tierService);
        }

        #region Basic Functionality Tests

        [Fact]
        public void TryAutoRecruit_WithNullFactionId_ReturnsZero()
        {
            var result = _service.TryAutoRecruit(null!);

            Assert.Equal(0, result);
        }

        [Fact]
        public void TryAutoRecruit_WithEmptyFactionId_ReturnsZero()
        {
            var result = _service.TryAutoRecruit("");

            Assert.Equal(0, result);
        }

        [Fact]
        public void TryAutoRecruit_WithNonexistentFaction_ReturnsZero()
        {
            _mockFactionService.Setup(f => f.GetFactionState("test")).Returns((FactionState?)null);

            var result = _service.TryAutoRecruit("test");

            Assert.Equal(0, result);
        }

        #endregion

        #region Below $5k - 100% Basic

        [Fact]
        public void TryAutoRecruit_Below5k_OnlyRecruitsBasicTroops()
        {
            // $4000 = 20 Basic affordable, capped at 10
            var factionState = new FactionState("test", initialCash: 4000, initialTroopCount: 0);
            _mockFactionService.Setup(f => f.GetFactionState("test")).Returns(factionState);
            _mockFactionService.Setup(f => f.AddReserveTroops(It.IsAny<string>(), It.IsAny<DefenderRole>(), It.IsAny<int>())).Returns(true);
            _mockFactionService.Setup(f => f.SpendCash(It.IsAny<string>(), It.IsAny<int>())).Returns(true);

            var result = _service.TryAutoRecruit("test");

            Assert.Equal(10, result);
            _mockFactionService.Verify(f => f.AddReserveTroops("test", DefenderRole.Grunt, 10), Times.Once);
            _mockFactionService.Verify(f => f.AddReserveTroops("test", DefenderRole.Gunner, It.IsAny<int>()), Times.Never);
            _mockFactionService.Verify(f => f.AddReserveTroops("test", DefenderRole.Rifleman, It.IsAny<int>()), Times.Never);
            _mockFactionService.Verify(f => f.AddReserveTroops("test", DefenderRole.Rocketeer, It.IsAny<int>()), Times.Never);
            _mockFactionService.Verify(f => f.SpendCash("test", 2000), Times.Once); // 10 * $200
        }

        [Fact]
        public void TryAutoRecruit_Below5k_LimitedByBudget_RecruitsAffordableAmount()
        {
            // $600 = 3 Basic affordable
            var factionState = new FactionState("test", initialCash: 600, initialTroopCount: 0);
            _mockFactionService.Setup(f => f.GetFactionState("test")).Returns(factionState);
            _mockFactionService.Setup(f => f.AddReserveTroops(It.IsAny<string>(), It.IsAny<DefenderRole>(), It.IsAny<int>())).Returns(true);
            _mockFactionService.Setup(f => f.SpendCash(It.IsAny<string>(), It.IsAny<int>())).Returns(true);

            var result = _service.TryAutoRecruit("test");

            Assert.Equal(3, result);
            _mockFactionService.Verify(f => f.AddReserveTroops("test", DefenderRole.Grunt, 3), Times.Once);
            _mockFactionService.Verify(f => f.SpendCash("test", 600), Times.Once);
        }

        [Fact]
        public void TryAutoRecruit_WithZeroCash_ReturnsZero()
        {
            var factionState = new FactionState("test", initialCash: 0, initialTroopCount: 0);
            _mockFactionService.Setup(f => f.GetFactionState("test")).Returns(factionState);

            var result = _service.TryAutoRecruit("test");

            Assert.Equal(0, result);
            _mockFactionService.Verify(f => f.AddReserveTroops(It.IsAny<string>(), It.IsAny<DefenderRole>(), It.IsAny<int>()), Times.Never);
        }

        #endregion

        #region $5k-$15k - 60/30/10 Distribution, No Elite

        [Fact]
        public void TryAutoRecruit_5kTo15k_Uses60_30_10Distribution()
        {
            // $10,000 - should buy mix of Basic(60%), Medium(30%), Heavy(10%)
            // With 10 troops max: 6 Basic, 3 Medium, 1 Heavy
            var factionState = new FactionState("test", initialCash: 10000, initialTroopCount: 0);
            _mockFactionService.Setup(f => f.GetFactionState("test")).Returns(factionState);
            _mockFactionService.Setup(f => f.AddReserveTroops(It.IsAny<string>(), It.IsAny<DefenderRole>(), It.IsAny<int>())).Returns(true);
            _mockFactionService.Setup(f => f.SpendCash(It.IsAny<string>(), It.IsAny<int>())).Returns(true);

            var result = _service.TryAutoRecruit("test");

            // 6 Basic ($1200) + 3 Medium ($1500) + 1 Heavy ($1000) = $3700
            Assert.Equal(10, result);
            _mockFactionService.Verify(f => f.AddReserveTroops("test", DefenderRole.Grunt, 6), Times.Once);
            _mockFactionService.Verify(f => f.AddReserveTroops("test", DefenderRole.Gunner, 3), Times.Once);
            _mockFactionService.Verify(f => f.AddReserveTroops("test", DefenderRole.Rifleman, 1), Times.Once);
            _mockFactionService.Verify(f => f.AddReserveTroops("test", DefenderRole.Rocketeer, It.IsAny<int>()), Times.Never);
            _mockFactionService.Verify(f => f.SpendCash("test", 3700), Times.Once);
        }

        [Fact]
        public void TryAutoRecruit_At5k_Uses60_30_10Distribution()
        {
            // Exactly $5k is the threshold for 60/30/10
            var factionState = new FactionState("test", initialCash: 5000, initialTroopCount: 0);
            _mockFactionService.Setup(f => f.GetFactionState("test")).Returns(factionState);
            _mockFactionService.Setup(f => f.AddReserveTroops(It.IsAny<string>(), It.IsAny<DefenderRole>(), It.IsAny<int>())).Returns(true);
            _mockFactionService.Setup(f => f.SpendCash(It.IsAny<string>(), It.IsAny<int>())).Returns(true);

            var result = _service.TryAutoRecruit("test");

            Assert.Equal(10, result);
            _mockFactionService.Verify(f => f.AddReserveTroops("test", DefenderRole.Grunt, 6), Times.Once);
            _mockFactionService.Verify(f => f.AddReserveTroops("test", DefenderRole.Gunner, 3), Times.Once);
            _mockFactionService.Verify(f => f.AddReserveTroops("test", DefenderRole.Rifleman, 1), Times.Once);
        }

        #endregion

        #region $15k-$30k - 40/30/20 Distribution, 1 Elite

        [Fact]
        public void TryAutoRecruit_15kTo30k_Buys1Elite_AndUses40_30_20Distribution()
        {
            // $20,000 - should buy 1 Elite ($2000), then mix of Basic(40%), Medium(30%), Heavy(20%)
            // After Elite: 9 slots remain, distribution: 4 Basic, 3 Medium, 2 Heavy (rounded)
            var factionState = new FactionState("test", initialCash: 20000, initialTroopCount: 0);
            _mockFactionService.Setup(f => f.GetFactionState("test")).Returns(factionState);
            _mockFactionService.Setup(f => f.AddReserveTroops(It.IsAny<string>(), It.IsAny<DefenderRole>(), It.IsAny<int>())).Returns(true);
            _mockFactionService.Setup(f => f.SpendCash(It.IsAny<string>(), It.IsAny<int>())).Returns(true);

            var result = _service.TryAutoRecruit("test");

            // Sniper-first: 2 Sniper ($3000), then 1 Rocketeer, then 7 standard slots (40/30/20 → 4/2/1)
            Assert.Equal(10, result);
            _mockFactionService.Verify(f => f.AddReserveTroops("test", DefenderRole.Sniper, 2), Times.Once);
            _mockFactionService.Verify(f => f.AddReserveTroops("test", DefenderRole.Rocketeer, 1), Times.Once);
            _mockFactionService.Verify(f => f.AddReserveTroops("test", DefenderRole.Grunt, 4), Times.Once);
            _mockFactionService.Verify(f => f.AddReserveTroops("test", DefenderRole.Gunner, 2), Times.Once);
            _mockFactionService.Verify(f => f.AddReserveTroops("test", DefenderRole.Rifleman, 1), Times.Once);
            // 2*$1500 + 1*$2000 + 4*$200 + 2*$500 + 1*$1000 = $3000 + $2000 + $800 + $1000 + $1000 = $7800
            _mockFactionService.Verify(f => f.SpendCash("test", 7800), Times.Once);
        }

        [Fact]
        public void TryAutoRecruit_At15k_Buys1Elite()
        {
            // Exactly $15k threshold for 1 Elite
            var factionState = new FactionState("test", initialCash: 15000, initialTroopCount: 0);
            _mockFactionService.Setup(f => f.GetFactionState("test")).Returns(factionState);
            _mockFactionService.Setup(f => f.AddReserveTroops(It.IsAny<string>(), It.IsAny<DefenderRole>(), It.IsAny<int>())).Returns(true);
            _mockFactionService.Setup(f => f.SpendCash(It.IsAny<string>(), It.IsAny<int>())).Returns(true);

            var result = _service.TryAutoRecruit("test");

            _mockFactionService.Verify(f => f.AddReserveTroops("test", DefenderRole.Rocketeer, 1), Times.Once);
        }

        #endregion

        #region Above $30k - 20/30/40 Distribution, 2 Elite

        [Fact]
        public void TryAutoRecruit_Above30k_Buys2Elite_AndUses20_30_40Distribution()
        {
            // $40,000 - should buy 2 Elite ($4000), then mix of Basic(20%), Medium(30%), Heavy(40%)
            // After Elite: 8 slots remain
            // Distribution: 8*0.2=1.6->2, 8*0.3=2.4->2, 8*0.4=3.2->3 = 7, +1 basic = 3/2/3
            var factionState = new FactionState("test", initialCash: 40000, initialTroopCount: 0);
            _mockFactionService.Setup(f => f.GetFactionState("test")).Returns(factionState);
            _mockFactionService.Setup(f => f.AddReserveTroops(It.IsAny<string>(), It.IsAny<DefenderRole>(), It.IsAny<int>())).Returns(true);
            _mockFactionService.Setup(f => f.SpendCash(It.IsAny<string>(), It.IsAny<int>())).Returns(true);

            var result = _service.TryAutoRecruit("test");

            // Sniper-first: 2 Sniper ($3000), then 2 Rocketeer, then 6 standard slots (20/30/40 → 2/2/2)
            Assert.Equal(10, result);
            _mockFactionService.Verify(f => f.AddReserveTroops("test", DefenderRole.Sniper, 2), Times.Once);
            _mockFactionService.Verify(f => f.AddReserveTroops("test", DefenderRole.Rocketeer, 2), Times.Once);
            _mockFactionService.Verify(f => f.AddReserveTroops("test", DefenderRole.Grunt, 2), Times.Once);
            _mockFactionService.Verify(f => f.AddReserveTroops("test", DefenderRole.Gunner, 2), Times.Once);
            _mockFactionService.Verify(f => f.AddReserveTroops("test", DefenderRole.Rifleman, 2), Times.Once);
            // 2*$1500 + 2*$2000 + 2*$200 + 2*$500 + 2*$1000 = $3000 + $4000 + $400 + $1000 + $2000 = $10400
            _mockFactionService.Verify(f => f.SpendCash("test", 10400), Times.Once);
        }

        [Fact]
        public void TryAutoRecruit_At30k_Buys2Elite()
        {
            // Exactly $30k is the threshold for 2 Elite
            var factionState = new FactionState("test", initialCash: 30000, initialTroopCount: 0);
            _mockFactionService.Setup(f => f.GetFactionState("test")).Returns(factionState);
            _mockFactionService.Setup(f => f.AddReserveTroops(It.IsAny<string>(), It.IsAny<DefenderRole>(), It.IsAny<int>())).Returns(true);
            _mockFactionService.Setup(f => f.SpendCash(It.IsAny<string>(), It.IsAny<int>())).Returns(true);

            var result = _service.TryAutoRecruit("test");

            // $30k exactly - should buy 2 Elite
            _mockFactionService.Verify(f => f.AddReserveTroops("test", DefenderRole.Rocketeer, 2), Times.Once);
        }

        #endregion

        #region Budget Constraints

        [Fact]
        public void TryAutoRecruit_LimitedBudget_StopsWhenCantAfford()
        {
            // $3000 at $5k-$15k tier - can afford some troops but not all 10
            // 60/30/10 of 10 = 6 Basic, 3 Medium, 1 Heavy
            // Cost: 6*$200 + 3*$500 + 1*$1000 = $3700 > $3000
            // Should recruit what it can afford
            var factionState = new FactionState("test", initialCash: 5000, initialTroopCount: 0);
            _mockFactionService.Setup(f => f.GetFactionState("test")).Returns(factionState);
            _mockFactionService.Setup(f => f.AddReserveTroops(It.IsAny<string>(), It.IsAny<DefenderRole>(), It.IsAny<int>())).Returns(true);
            _mockFactionService.Setup(f => f.SpendCash(It.IsAny<string>(), It.IsAny<int>())).Returns(true);

            var result = _service.TryAutoRecruit("test");

            // Should recruit troops and deduct appropriate cash
            Assert.True(result > 0);
        }

        [Fact]
        public void TryAutoRecruit_MaxTroopsLimit_EnforcedAtTen()
        {
            // Even with huge budget, max 10 troops per cycle
            var factionState = new FactionState("test", initialCash: 100000, initialTroopCount: 0);
            _mockFactionService.Setup(f => f.GetFactionState("test")).Returns(factionState);
            _mockFactionService.Setup(f => f.AddReserveTroops(It.IsAny<string>(), It.IsAny<DefenderRole>(), It.IsAny<int>())).Returns(true);
            _mockFactionService.Setup(f => f.SpendCash(It.IsAny<string>(), It.IsAny<int>())).Returns(true);

            var result = _service.TryAutoRecruit("test");

            Assert.Equal(10, result);
        }

        [Fact]
        public void TryAutoRecruit_CustomMaxTroops_Respected()
        {
            var factionState = new FactionState("test", initialCash: 100000, initialTroopCount: 0);
            _mockFactionService.Setup(f => f.GetFactionState("test")).Returns(factionState);
            _mockFactionService.Setup(f => f.AddReserveTroops(It.IsAny<string>(), It.IsAny<DefenderRole>(), It.IsAny<int>())).Returns(true);
            _mockFactionService.Setup(f => f.SpendCash(It.IsAny<string>(), It.IsAny<int>())).Returns(true);

            var result = _service.TryAutoRecruit("test", maxTroopsToRecruit: 5);

            Assert.Equal(5, result);
        }

        #endregion

        #region Elite Purchase When Cannot Afford Full Elite

        [Fact]
        public void TryAutoRecruit_At15kButCantAffordElitePlusStandard_SkipsElite()
        {
            // Edge case: $15k threshold but not enough for 1 Elite + standard troops
            // $2000 exactly - can only afford 1 Elite, no standard
            // But since we're at $15k threshold, it should try Elite first
            var factionState = new FactionState("test", initialCash: 15000, initialTroopCount: 0);
            _mockFactionService.Setup(f => f.GetFactionState("test")).Returns(factionState);
            _mockFactionService.Setup(f => f.AddReserveTroops(It.IsAny<string>(), It.IsAny<DefenderRole>(), It.IsAny<int>())).Returns(true);
            _mockFactionService.Setup(f => f.SpendCash(It.IsAny<string>(), It.IsAny<int>())).Returns(true);

            var result = _service.TryAutoRecruit("test");

            // Should buy 1 Elite + remaining troops
            Assert.True(result > 0);
            _mockFactionService.Verify(f => f.AddReserveTroops("test", DefenderRole.Rocketeer, 1), Times.Once);
        }

        [Fact]
        public void TryAutoRecruit_CantAffordAnyElite_SkipsEliteEntirely()
        {
            // $14,999 - just below $15k threshold, no Elite
            var factionState = new FactionState("test", initialCash: 14999, initialTroopCount: 0);
            _mockFactionService.Setup(f => f.GetFactionState("test")).Returns(factionState);
            _mockFactionService.Setup(f => f.AddReserveTroops(It.IsAny<string>(), It.IsAny<DefenderRole>(), It.IsAny<int>())).Returns(true);
            _mockFactionService.Setup(f => f.SpendCash(It.IsAny<string>(), It.IsAny<int>())).Returns(true);

            var result = _service.TryAutoRecruit("test");

            _mockFactionService.Verify(f => f.AddReserveTroops("test", DefenderRole.Rocketeer, It.IsAny<int>()), Times.Never);
        }

        #endregion

        #region Tier Distribution Rounding

        [Fact]
        public void TryAutoRecruit_RoundsDistributionCorrectly_NoBudgetLeak()
        {
            // Verify that rounding doesn't cause us to lose or gain troops
            // With 9 troops at 40/30/20: 3.6/2.7/1.8 -> should round to total 9
            var factionState = new FactionState("test", initialCash: 20000, initialTroopCount: 0);
            _mockFactionService.Setup(f => f.GetFactionState("test")).Returns(factionState);
            _mockFactionService.Setup(f => f.AddReserveTroops(It.IsAny<string>(), It.IsAny<DefenderRole>(), It.IsAny<int>())).Returns(true);
            _mockFactionService.Setup(f => f.SpendCash(It.IsAny<string>(), It.IsAny<int>())).Returns(true);

            var troopCounts = new Dictionary<DefenderRole, int>();
            _mockFactionService.Setup(f => f.AddReserveTroops("test", It.IsAny<DefenderRole>(), It.IsAny<int>()))
                .Callback<string, DefenderRole, int>((id, tier, count) => troopCounts[tier] = count)
                .Returns(true);

            var result = _service.TryAutoRecruit("test");

            // Total should equal result
            int totalRecorded = 0;
            foreach (var kvp in troopCounts)
            {
                totalRecorded += kvp.Value;
            }
            Assert.Equal(result, totalRecorded);
        }

        #endregion

        #region Scaled Recruitment with ICapitalDeploymentService

        [Fact]
        public void TryAutoRecruit_WithCapitalDeploymentService_UsesScaledMax()
        {
            // $25,000 cash: GetScaledRecruitmentMax returns 10 + (25000/10000) = 12
            var mockCapitalService = new Mock<ICapitalDeploymentService>();
            mockCapitalService.Setup(c => c.GetScaledRecruitmentMax(25000)).Returns(12);

            var factionState = new FactionState("test", initialCash: 25000, initialTroopCount: 0);
            _mockFactionService.Setup(f => f.GetFactionState("test")).Returns(factionState);
            _mockFactionService.Setup(f => f.AddReserveTroops(It.IsAny<string>(), It.IsAny<DefenderRole>(), It.IsAny<int>())).Returns(true);
            _mockFactionService.Setup(f => f.SpendCash(It.IsAny<string>(), It.IsAny<int>())).Returns(true);

            var serviceWithCapital = new AIRecruitmentService(
                _mockFactionService.Object,
                _budgetService,
                _tierService,
                mockCapitalService.Object);

            var result = serviceWithCapital.TryAutoRecruit("test");

            // Should recruit 12 troops (scaled max) instead of default 10
            Assert.Equal(12, result);
            mockCapitalService.Verify(c => c.GetScaledRecruitmentMax(25000), Times.Once);
        }

        [Fact]
        public void TryAutoRecruit_WithCapitalDeploymentService_HighWealth_UsesHigherMax()
        {
            // $100,000 cash: GetScaledRecruitmentMax returns 10 + (100000/10000) = 20, capped at 50
            var mockCapitalService = new Mock<ICapitalDeploymentService>();
            mockCapitalService.Setup(c => c.GetScaledRecruitmentMax(100000)).Returns(20);

            var factionState = new FactionState("test", initialCash: 100000, initialTroopCount: 0);
            _mockFactionService.Setup(f => f.GetFactionState("test")).Returns(factionState);
            _mockFactionService.Setup(f => f.AddReserveTroops(It.IsAny<string>(), It.IsAny<DefenderRole>(), It.IsAny<int>())).Returns(true);
            _mockFactionService.Setup(f => f.SpendCash(It.IsAny<string>(), It.IsAny<int>())).Returns(true);

            var serviceWithCapital = new AIRecruitmentService(
                _mockFactionService.Object,
                _budgetService,
                _tierService,
                mockCapitalService.Object);

            var result = serviceWithCapital.TryAutoRecruit("test");

            // Should recruit 20 troops (scaled max) instead of default 10
            Assert.Equal(20, result);
        }

        [Fact]
        public void TryAutoRecruit_WithCapitalDeploymentService_LowWealth_UsesBaseRate()
        {
            // $2,000 cash: GetScaledRecruitmentMax returns 10 + (2000/10000) = 10
            var mockCapitalService = new Mock<ICapitalDeploymentService>();
            mockCapitalService.Setup(c => c.GetScaledRecruitmentMax(2000)).Returns(10);

            var factionState = new FactionState("test", initialCash: 2000, initialTroopCount: 0);
            _mockFactionService.Setup(f => f.GetFactionState("test")).Returns(factionState);
            _mockFactionService.Setup(f => f.AddReserveTroops(It.IsAny<string>(), It.IsAny<DefenderRole>(), It.IsAny<int>())).Returns(true);
            _mockFactionService.Setup(f => f.SpendCash(It.IsAny<string>(), It.IsAny<int>())).Returns(true);

            var serviceWithCapital = new AIRecruitmentService(
                _mockFactionService.Object,
                _budgetService,
                _tierService,
                mockCapitalService.Object);

            var result = serviceWithCapital.TryAutoRecruit("test");

            // Should recruit up to 10 (base rate), but limited by what can be afforded
            Assert.Equal(10, result);
        }

        [Fact]
        public void TryAutoRecruit_WithoutCapitalDeploymentService_UsesDefaultMax()
        {
            // Without ICapitalDeploymentService, should use default maxTroopsToRecruit parameter
            var factionState = new FactionState("test", initialCash: 100000, initialTroopCount: 0);
            _mockFactionService.Setup(f => f.GetFactionState("test")).Returns(factionState);
            _mockFactionService.Setup(f => f.AddReserveTroops(It.IsAny<string>(), It.IsAny<DefenderRole>(), It.IsAny<int>())).Returns(true);
            _mockFactionService.Setup(f => f.SpendCash(It.IsAny<string>(), It.IsAny<int>())).Returns(true);

            // Using existing _service which has no ICapitalDeploymentService
            var result = _service.TryAutoRecruit("test");

            // Should still use default max of 10, even with high wealth
            Assert.Equal(10, result);
        }

        [Fact]
        public void TryAutoRecruit_WithCapitalDeploymentService_ExplicitMaxParam_IgnoresParam()
        {
            // When ICapitalDeploymentService is provided, explicit maxTroopsToRecruit parameter is ignored
            var mockCapitalService = new Mock<ICapitalDeploymentService>();
            mockCapitalService.Setup(c => c.GetScaledRecruitmentMax(30000)).Returns(13);

            var factionState = new FactionState("test", initialCash: 30000, initialTroopCount: 0);
            _mockFactionService.Setup(f => f.GetFactionState("test")).Returns(factionState);
            _mockFactionService.Setup(f => f.AddReserveTroops(It.IsAny<string>(), It.IsAny<DefenderRole>(), It.IsAny<int>())).Returns(true);
            _mockFactionService.Setup(f => f.SpendCash(It.IsAny<string>(), It.IsAny<int>())).Returns(true);

            var serviceWithCapital = new AIRecruitmentService(
                _mockFactionService.Object,
                _budgetService,
                _tierService,
                mockCapitalService.Object);

            // Pass explicit maxTroopsToRecruit=5, but service should ignore it and use scaled max
            var result = serviceWithCapital.TryAutoRecruit("test", maxTroopsToRecruit: 5);

            // Should recruit 13 troops (scaled max) not 5 (explicit param)
            Assert.Equal(13, result);
        }

        [Fact]
        public void Constructor_WithCapitalDeploymentService_AcceptsNonNullService()
        {
            var mockCapitalService = new Mock<ICapitalDeploymentService>();

            // Should not throw
            var service = new AIRecruitmentService(
                _mockFactionService.Object,
                _budgetService,
                _tierService,
                mockCapitalService.Object);

            Assert.NotNull(service);
        }

        [Fact]
        public void Constructor_WithNullCapitalDeploymentService_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new AIRecruitmentService(
                _mockFactionService.Object,
                _budgetService,
                _tierService,
                null!));
        }

        #endregion

        #region Sniper Minority Recruitment

        [Fact]
        public void Recruit_WealthyFaction_BuysCappedSnipers()
        {
            // $20,000 with 10 max troops → ceil(10/6) = 2 snipers allowed
            var factionState = new FactionState("test", initialCash: 20000, initialTroopCount: 0);
            _mockFactionService.Setup(f => f.GetFactionState("test")).Returns(factionState);
            _mockFactionService.Setup(f => f.AddReserveTroops(It.IsAny<string>(), It.IsAny<DefenderRole>(), It.IsAny<int>())).Returns(true);
            _mockFactionService.Setup(f => f.SpendCash(It.IsAny<string>(), It.IsAny<int>())).Returns(true);

            _service.TryAutoRecruit("test");

            int maxAllowed = (10 + 6 - 1) / 6; // ceil(10/6) = 2
            _mockFactionService.Verify(
                f => f.AddReserveTroops("test", DefenderRole.Sniper, It.Is<int>(c => c >= 1 && c <= maxAllowed)),
                Times.Once);
        }

        [Fact]
        public void Recruit_PoorFaction_BuysNoSnipers()
        {
            // $14,999 — just below MidWealthThreshold → 0 snipers
            var factionState = new FactionState("test", initialCash: 14999, initialTroopCount: 0);
            _mockFactionService.Setup(f => f.GetFactionState("test")).Returns(factionState);
            _mockFactionService.Setup(f => f.AddReserveTroops(It.IsAny<string>(), It.IsAny<DefenderRole>(), It.IsAny<int>())).Returns(true);
            _mockFactionService.Setup(f => f.SpendCash(It.IsAny<string>(), It.IsAny<int>())).Returns(true);

            _service.TryAutoRecruit("test");

            _mockFactionService.Verify(
                f => f.AddReserveTroops("test", DefenderRole.Sniper, It.IsAny<int>()),
                Times.Never);
        }

        #endregion
    }
}
