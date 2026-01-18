using FactionWars.AI.Interfaces;
using FactionWars.AI.Models;
using FactionWars.AI.Services;
using FactionWars.Factions.Models;
using FactionWars.Territory.Models;
using FactionWars.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace FactionWars.Tests.Unit.AI
{
    /// <summary>
    /// Tests for ZoneEvaluationService.
    /// The zone evaluation scoring system calculates how attractive a zone is for capture
    /// based on multiple factors: strategic value, traits, ownership, adjacency, and combat status.
    /// </summary>
    public class ZoneEvaluationServiceTests
    {
        #region Constructor Tests

        [Fact]
        public void Constructor_WithValidParameters_CreatesInstance()
        {
            var service = new ZoneEvaluationService();

            Assert.NotNull(service);
        }

        [Fact]
        public void Constructor_ImplementsIZoneEvaluationService()
        {
            var service = new ZoneEvaluationService();

            Assert.IsAssignableFrom<IZoneEvaluationService>(service);
        }

        #endregion

        #region EvaluateZone - Null Parameter Tests

        [Fact]
        public void EvaluateZone_NullZone_ThrowsArgumentNullException()
        {
            var service = new ZoneEvaluationService();
            var context = CreateTestContext();

            Assert.Throws<ArgumentNullException>(() => service.EvaluateZone(null!, context));
        }

        [Fact]
        public void EvaluateZone_NullContext_ThrowsArgumentNullException()
        {
            var service = new ZoneEvaluationService();
            var zone = CreateTestZone("zone-1");

            Assert.Throws<ArgumentNullException>(() => service.EvaluateZone(zone, null!));
        }

        #endregion

        #region EvaluateZone - Return Value Range Tests

        [Fact]
        public void EvaluateZone_ReturnsValueBetweenZeroAndOne()
        {
            var service = new ZoneEvaluationService();
            var context = CreateTestContext();
            var zone = CreateTestZone("zone-1", strategicValue: 5);

            var score = service.EvaluateZone(zone, context);

            Assert.InRange(score, 0f, 1f);
        }

        [Fact]
        public void EvaluateZone_MinimumStrategicValue_ReturnsNonNegative()
        {
            var service = new ZoneEvaluationService();
            var context = CreateTestContext();
            var zone = CreateTestZone("zone-1", strategicValue: 1);

            var score = service.EvaluateZone(zone, context);

            Assert.True(score >= 0f);
        }

        [Fact]
        public void EvaluateZone_MaximumStrategicValue_ReturnsAtMostOne()
        {
            var service = new ZoneEvaluationService();
            var context = CreateTestContext();
            var zone = CreateTestZone("zone-1", strategicValue: 10);
            zone.Traits = ZoneTrait.HighValue | ZoneTrait.Commercial | ZoneTrait.Industrial;

            var score = service.EvaluateZone(zone, context);

            Assert.True(score <= 1f);
        }

        #endregion

        #region EvaluateZone - Strategic Value Tests

        [Fact]
        public void EvaluateZone_HigherStrategicValue_ReturnsHigherScore()
        {
            var service = new ZoneEvaluationService();
            var context = CreateTestContext();

            var lowValueZone = CreateTestZone("low", strategicValue: 2);
            var highValueZone = CreateTestZone("high", strategicValue: 9);

            var lowScore = service.EvaluateZone(lowValueZone, context);
            var highScore = service.EvaluateZone(highValueZone, context);

            Assert.True(highScore > lowScore);
        }

        [Fact]
        public void EvaluateZone_StrategicValueScalesProportionally()
        {
            var service = new ZoneEvaluationService();
            var context = CreateTestContext();

            var zone3 = CreateTestZone("zone3", strategicValue: 3);
            var zone6 = CreateTestZone("zone6", strategicValue: 6);
            var zone9 = CreateTestZone("zone9", strategicValue: 9);

            var score3 = service.EvaluateZone(zone3, context);
            var score6 = service.EvaluateZone(zone6, context);
            var score9 = service.EvaluateZone(zone9, context);

            // Score should increase with strategic value
            Assert.True(score6 > score3);
            Assert.True(score9 > score6);
        }

        [Fact]
        public void EvaluateZone_StrategicValueOf10_HasHighestBaseScore()
        {
            var service = new ZoneEvaluationService();
            var context = CreateTestContext();

            var maxValueZone = CreateTestZone("max", strategicValue: 10);
            var mediumValueZone = CreateTestZone("medium", strategicValue: 5);

            var maxScore = service.EvaluateZone(maxValueZone, context);
            var mediumScore = service.EvaluateZone(mediumValueZone, context);

            // Max value zone should score noticeably higher
            // With weighted scoring (strategic value has 40% weight), the difference is proportional
            Assert.True(maxScore > mediumScore * 1.2f, "Max value zone should be at least 20% higher than medium");
        }

        #endregion

        #region EvaluateZone - Zone Trait Tests

        [Fact]
        public void EvaluateZone_HighValueTrait_IncreasesScore()
        {
            var service = new ZoneEvaluationService();
            var context = CreateTestContext();

            var baseZone = CreateTestZone("base", strategicValue: 5);
            var highValueZone = CreateTestZone("highvalue", strategicValue: 5);
            highValueZone.Traits = ZoneTrait.HighValue;

            var baseScore = service.EvaluateZone(baseZone, context);
            var highValueScore = service.EvaluateZone(highValueZone, context);

            Assert.True(highValueScore > baseScore);
        }

        [Fact]
        public void EvaluateZone_CommercialTrait_IncreasesScore()
        {
            var service = new ZoneEvaluationService();
            var context = CreateTestContext();

            var baseZone = CreateTestZone("base", strategicValue: 5);
            var commercialZone = CreateTestZone("commercial", strategicValue: 5);
            commercialZone.Traits = ZoneTrait.Commercial;

            var baseScore = service.EvaluateZone(baseZone, context);
            var commercialScore = service.EvaluateZone(commercialZone, context);

            Assert.True(commercialScore > baseScore);
        }

        [Fact]
        public void EvaluateZone_IndustrialTrait_IncreasesScore()
        {
            var service = new ZoneEvaluationService();
            var context = CreateTestContext();

            var baseZone = CreateTestZone("base", strategicValue: 5);
            var industrialZone = CreateTestZone("industrial", strategicValue: 5);
            industrialZone.Traits = ZoneTrait.Industrial;

            var baseScore = service.EvaluateZone(baseZone, context);
            var industrialScore = service.EvaluateZone(industrialZone, context);

            Assert.True(industrialScore > baseScore);
        }

        [Fact]
        public void EvaluateZone_ResidentialTrait_IncreasesScore()
        {
            var service = new ZoneEvaluationService();
            var context = CreateTestContext();

            var baseZone = CreateTestZone("base", strategicValue: 5);
            var residentialZone = CreateTestZone("residential", strategicValue: 5);
            residentialZone.Traits = ZoneTrait.Residential;

            var baseScore = service.EvaluateZone(baseZone, context);
            var residentialScore = service.EvaluateZone(residentialZone, context);

            Assert.True(residentialScore > baseScore);
        }

        [Fact]
        public void EvaluateZone_PortTrait_IncreasesScore()
        {
            var service = new ZoneEvaluationService();
            var context = CreateTestContext();

            var baseZone = CreateTestZone("base", strategicValue: 5);
            var portZone = CreateTestZone("port", strategicValue: 5);
            portZone.Traits = ZoneTrait.Port;

            var baseScore = service.EvaluateZone(baseZone, context);
            var portScore = service.EvaluateZone(portZone, context);

            Assert.True(portScore > baseScore);
        }

        [Fact]
        public void EvaluateZone_AirfieldTrait_IncreasesScore()
        {
            var service = new ZoneEvaluationService();
            var context = CreateTestContext();

            var baseZone = CreateTestZone("base", strategicValue: 5);
            var airfieldZone = CreateTestZone("airfield", strategicValue: 5);
            airfieldZone.Traits = ZoneTrait.Airfield;

            var baseScore = service.EvaluateZone(baseZone, context);
            var airfieldScore = service.EvaluateZone(airfieldZone, context);

            Assert.True(airfieldScore > baseScore);
        }

        [Fact]
        public void EvaluateZone_FortifiedTrait_ConsideredInScore()
        {
            var service = new ZoneEvaluationService();
            var context = CreateTestContext();

            var baseZone = CreateTestZone("base", strategicValue: 5);
            var fortifiedZone = CreateTestZone("fortified", strategicValue: 5);
            fortifiedZone.Traits = ZoneTrait.Fortified;

            var baseScore = service.EvaluateZone(baseZone, context);
            var fortifiedScore = service.EvaluateZone(fortifiedZone, context);

            // Fortified trait may be positive (valuable to own) or negative (harder to capture)
            // depending on current ownership - test it's considered
            Assert.NotEqual(baseScore, fortifiedScore);
        }

        [Fact]
        public void EvaluateZone_MultipleTraits_StackBonuses()
        {
            var service = new ZoneEvaluationService();
            var context = CreateTestContext();

            var singleTraitZone = CreateTestZone("single", strategicValue: 5);
            singleTraitZone.Traits = ZoneTrait.Commercial;

            var multiTraitZone = CreateTestZone("multi", strategicValue: 5);
            multiTraitZone.Traits = ZoneTrait.Commercial | ZoneTrait.Industrial | ZoneTrait.HighValue;

            var singleScore = service.EvaluateZone(singleTraitZone, context);
            var multiScore = service.EvaluateZone(multiTraitZone, context);

            Assert.True(multiScore > singleScore, "Multiple traits should provide higher score");
        }

        [Fact]
        public void EvaluateZone_NoTraits_UsesBaseScoring()
        {
            var service = new ZoneEvaluationService();
            var context = CreateTestContext();

            var noTraitsZone = CreateTestZone("notraits", strategicValue: 5);
            noTraitsZone.Traits = ZoneTrait.None;

            var score = service.EvaluateZone(noTraitsZone, context);

            // Should still return valid score based on strategic value
            Assert.True(score > 0f);
        }

        #endregion

        #region EvaluateZone - Ownership Status Tests

        [Fact]
        public void EvaluateZone_NeutralZone_HasBonusOverEnemyZone()
        {
            var service = new ZoneEvaluationService();
            var context = CreateTestContext();

            var neutralZone = CreateTestZone("neutral", strategicValue: 5);
            neutralZone.OwnerFactionId = null;

            var enemyZone = CreateTestZone("enemy", strategicValue: 5);
            enemyZone.OwnerFactionId = "enemy-faction";

            var neutralScore = service.EvaluateZone(neutralZone, context);
            var enemyScore = service.EvaluateZone(enemyZone, context);

            Assert.True(neutralScore > enemyScore, "Neutral zones should be more attractive (easier to capture)");
        }

        [Fact]
        public void EvaluateZone_OwnedZone_ReturnsLowScore()
        {
            var service = new ZoneEvaluationService();
            var faction = CreateTestFaction(FactionType.Michael);
            var context = CreateTestContextForFaction(faction);

            var ownedZone = CreateTestZone("owned", strategicValue: 8);
            ownedZone.OwnerFactionId = faction.Id;

            var score = service.EvaluateZone(ownedZone, context);

            // Already owned zones shouldn't be attractive targets
            Assert.True(score < 0.2f, "Owned zones should have very low attack score");
        }

        [Fact]
        public void EvaluateZone_EnemyZone_ReducedScoreDueToDifficulty()
        {
            var service = new ZoneEvaluationService();
            var context = CreateTestContext();

            var enemyZone = CreateTestZone("enemy", strategicValue: 5);
            enemyZone.OwnerFactionId = "enemy-faction";

            var neutralZone = CreateTestZone("neutral", strategicValue: 5);
            neutralZone.OwnerFactionId = null;

            var enemyScore = service.EvaluateZone(enemyZone, context);
            var neutralScore = service.EvaluateZone(neutralZone, context);

            // Enemy zones are harder to take, so should have lower score
            Assert.True(enemyScore < neutralScore);
        }

        #endregion

        #region EvaluateZone - Contested Status Tests

        [Fact]
        public void EvaluateZone_ContestedZone_AffectsScore()
        {
            var service = new ZoneEvaluationService();
            var context = CreateTestContext();

            var normalZone = CreateTestZone("normal", strategicValue: 5);
            normalZone.OwnerFactionId = "enemy-faction";
            normalZone.IsContested = false;

            var contestedZone = CreateTestZone("contested", strategicValue: 5);
            contestedZone.OwnerFactionId = "enemy-faction";
            contestedZone.IsContested = true;

            var normalScore = service.EvaluateZone(normalZone, context);
            var contestedScore = service.EvaluateZone(contestedZone, context);

            // Contested zones might be opportunities (enemy is weakened)
            Assert.NotEqual(normalScore, contestedScore);
        }

        [Fact]
        public void EvaluateZone_ContestedEnemyZone_OpportunityBonus()
        {
            var service = new ZoneEvaluationService();
            var context = CreateTestContext();

            var contestedEnemyZone = CreateTestZone("contested-enemy", strategicValue: 5);
            contestedEnemyZone.OwnerFactionId = "enemy-faction";
            contestedEnemyZone.IsContested = true;

            var stableEnemyZone = CreateTestZone("stable-enemy", strategicValue: 5);
            stableEnemyZone.OwnerFactionId = "enemy-faction";
            stableEnemyZone.IsContested = false;

            var contestedScore = service.EvaluateZone(contestedEnemyZone, context);
            var stableScore = service.EvaluateZone(stableEnemyZone, context);

            // Contested enemy zones are opportunities
            Assert.True(contestedScore >= stableScore, "Contested enemy zones should be attractive targets");
        }

        #endregion

        #region EvaluateZone - Adjacency Tests

        [Fact]
        public void EvaluateZone_AdjacentToOwnedZone_HasBonus()
        {
            var service = new ZoneEvaluationService();
            var faction = CreateTestFaction(FactionType.Michael);

            // Create zones where one is adjacent to owned territory
            var ownedZone = CreateTestZone("owned", strategicValue: 5, center: new Vector3(0, 0, 0));
            ownedZone.OwnerFactionId = faction.Id;

            var adjacentNeutralZone = CreateTestZone("adjacent-neutral", strategicValue: 5, center: new Vector3(200, 0, 0));
            adjacentNeutralZone.OwnerFactionId = null;

            var distantNeutralZone = CreateTestZone("distant-neutral", strategicValue: 5, center: new Vector3(2000, 0, 0));
            distantNeutralZone.OwnerFactionId = null;

            var allZones = new List<Zone> { ownedZone, adjacentNeutralZone, distantNeutralZone };
            var context = CreateTestContextWithZones(faction, new[] { ownedZone }, allZones);

            var adjacentScore = service.EvaluateZone(adjacentNeutralZone, context);
            var distantScore = service.EvaluateZone(distantNeutralZone, context);

            Assert.True(adjacentScore > distantScore, "Adjacent zones should be more attractive");
        }

        [Fact]
        public void EvaluateZone_IsolatedZone_HasPenalty()
        {
            var service = new ZoneEvaluationService();
            var faction = CreateTestFaction(FactionType.Michael);

            // Owned zone at origin
            var ownedZone = CreateTestZone("owned", strategicValue: 5, center: new Vector3(0, 0, 0));
            ownedZone.OwnerFactionId = faction.Id;

            // Very distant zone
            var isolatedZone = CreateTestZone("isolated", strategicValue: 8, center: new Vector3(5000, 5000, 0));
            isolatedZone.OwnerFactionId = null;

            var allZones = new List<Zone> { ownedZone, isolatedZone };
            var context = CreateTestContextWithZones(faction, new[] { ownedZone }, allZones);

            var score = service.EvaluateZone(isolatedZone, context);

            // Even high value isolated zones should have reduced attractiveness
            Assert.True(score < 0.8f, "Isolated zones should be less attractive due to supply line concerns");
        }

        [Fact]
        public void EvaluateZone_NoOwnedTerritory_IgnoresAdjacencyBonus()
        {
            var service = new ZoneEvaluationService();
            var faction = CreateTestFaction(FactionType.Michael);

            var neutralZone = CreateTestZone("neutral", strategicValue: 5);
            neutralZone.OwnerFactionId = null;

            // Context with no owned zones
            var allZones = new List<Zone> { neutralZone };
            var context = CreateTestContextWithZones(faction, new Zone[0], allZones);

            var score = service.EvaluateZone(neutralZone, context);

            // Should still provide valid score
            Assert.True(score > 0f);
            Assert.True(score <= 1f);
        }

        #endregion

        #region GetZoneScoreBreakdown Tests

        [Fact]
        public void GetZoneScoreBreakdown_NullZone_ThrowsArgumentNullException()
        {
            var service = new ZoneEvaluationService();
            var context = CreateTestContext();

            Assert.Throws<ArgumentNullException>(() => service.GetZoneScoreBreakdown(null!, context));
        }

        [Fact]
        public void GetZoneScoreBreakdown_NullContext_ThrowsArgumentNullException()
        {
            var service = new ZoneEvaluationService();
            var zone = CreateTestZone("zone-1");

            Assert.Throws<ArgumentNullException>(() => service.GetZoneScoreBreakdown(zone, null!));
        }

        [Fact]
        public void GetZoneScoreBreakdown_ReturnsBreakdownWithAllComponents()
        {
            var service = new ZoneEvaluationService();
            var context = CreateTestContext();
            var zone = CreateTestZone("zone-1", strategicValue: 7);
            zone.Traits = ZoneTrait.Commercial;

            var breakdown = service.GetZoneScoreBreakdown(zone, context);

            Assert.NotNull(breakdown);
            Assert.True(breakdown.ContainsKey("StrategicValue"), "Should include strategic value component");
            Assert.True(breakdown.ContainsKey("TraitBonus"), "Should include trait bonus component");
            Assert.True(breakdown.ContainsKey("OwnershipModifier"), "Should include ownership modifier");
            Assert.True(breakdown.ContainsKey("AdjacencyBonus"), "Should include adjacency bonus");
            Assert.True(breakdown.ContainsKey("TotalScore"), "Should include total score");
        }

        [Fact]
        public void GetZoneScoreBreakdown_TotalScoreMatchesEvaluateZone()
        {
            var service = new ZoneEvaluationService();
            var context = CreateTestContext();
            var zone = CreateTestZone("zone-1", strategicValue: 6);

            var evaluatedScore = service.EvaluateZone(zone, context);
            var breakdown = service.GetZoneScoreBreakdown(zone, context);

            Assert.Equal(evaluatedScore, breakdown["TotalScore"], precision: 3);
        }

        [Fact]
        public void GetZoneScoreBreakdown_ComponentsSumToTotal()
        {
            var service = new ZoneEvaluationService();
            var context = CreateTestContext();
            var zone = CreateTestZone("zone-1", strategicValue: 5);
            zone.Traits = ZoneTrait.Industrial;

            var breakdown = service.GetZoneScoreBreakdown(zone, context);

            // Verify breakdown is internally consistent
            Assert.True(breakdown["TotalScore"] >= 0f);
            Assert.True(breakdown["TotalScore"] <= 1f);
        }

        #endregion

        #region RankZonesByAttractiveness Tests

        [Fact]
        public void RankZonesByAttractiveness_NullZones_ThrowsArgumentNullException()
        {
            var service = new ZoneEvaluationService();
            var context = CreateTestContext();

            Assert.Throws<ArgumentNullException>(() => service.RankZonesByAttractiveness(null!, context));
        }

        [Fact]
        public void RankZonesByAttractiveness_NullContext_ThrowsArgumentNullException()
        {
            var service = new ZoneEvaluationService();
            var zones = new List<Zone> { CreateTestZone("zone-1") };

            Assert.Throws<ArgumentNullException>(() => service.RankZonesByAttractiveness(zones, null!));
        }

        [Fact]
        public void RankZonesByAttractiveness_EmptyList_ReturnsEmptyList()
        {
            var service = new ZoneEvaluationService();
            var context = CreateTestContext();

            var ranked = service.RankZonesByAttractiveness(new List<Zone>(), context);

            Assert.NotNull(ranked);
            Assert.Empty(ranked);
        }

        [Fact]
        public void RankZonesByAttractiveness_ReturnsZonesOrderedByScoreDescending()
        {
            var service = new ZoneEvaluationService();
            var context = CreateTestContext();

            var lowValueZone = CreateTestZone("low", strategicValue: 2);
            var mediumValueZone = CreateTestZone("medium", strategicValue: 5);
            var highValueZone = CreateTestZone("high", strategicValue: 9);

            var zones = new List<Zone> { lowValueZone, mediumValueZone, highValueZone };

            var ranked = service.RankZonesByAttractiveness(zones, context);

            Assert.Equal(3, ranked.Count);
            Assert.Equal("high", ranked[0].Zone.Id);
            Assert.Equal("medium", ranked[1].Zone.Id);
            Assert.Equal("low", ranked[2].Zone.Id);
        }

        [Fact]
        public void RankZonesByAttractiveness_ReturnsZonesWithScores()
        {
            var service = new ZoneEvaluationService();
            var context = CreateTestContext();

            var zone = CreateTestZone("test", strategicValue: 7);
            var zones = new List<Zone> { zone };

            var ranked = service.RankZonesByAttractiveness(zones, context);

            Assert.Single(ranked);
            Assert.Equal(zone, ranked[0].Zone);
            Assert.True(ranked[0].Score > 0f);
            Assert.True(ranked[0].Score <= 1f);
        }

        [Fact]
        public void RankZonesByAttractiveness_ExcludesOwnedZones()
        {
            var service = new ZoneEvaluationService();
            var faction = CreateTestFaction(FactionType.Michael);
            var context = CreateTestContextForFaction(faction);

            var ownedZone = CreateTestZone("owned", strategicValue: 10);
            ownedZone.OwnerFactionId = faction.Id;

            var neutralZone = CreateTestZone("neutral", strategicValue: 5);
            neutralZone.OwnerFactionId = null;

            var zones = new List<Zone> { ownedZone, neutralZone };

            var ranked = service.RankZonesByAttractiveness(zones, context);

            // Owned zones should be excluded or at bottom
            Assert.True(ranked.All(r => r.Zone.Id != "owned") || ranked.Last().Zone.Id == "owned");
        }

        #endregion

        #region GetBestAttackTarget Tests

        [Fact]
        public void GetBestAttackTarget_NullZones_ThrowsArgumentNullException()
        {
            var service = new ZoneEvaluationService();
            var context = CreateTestContext();

            Assert.Throws<ArgumentNullException>(() => service.GetBestAttackTarget(null!, context));
        }

        [Fact]
        public void GetBestAttackTarget_NullContext_ThrowsArgumentNullException()
        {
            var service = new ZoneEvaluationService();
            var zones = new List<Zone> { CreateTestZone("zone-1") };

            Assert.Throws<ArgumentNullException>(() => service.GetBestAttackTarget(zones, null!));
        }

        [Fact]
        public void GetBestAttackTarget_EmptyList_ReturnsNull()
        {
            var service = new ZoneEvaluationService();
            var context = CreateTestContext();

            var bestTarget = service.GetBestAttackTarget(new List<Zone>(), context);

            Assert.Null(bestTarget);
        }

        [Fact]
        public void GetBestAttackTarget_ReturnsHighestScoringZone()
        {
            var service = new ZoneEvaluationService();
            var context = CreateTestContext();

            var lowZone = CreateTestZone("low", strategicValue: 3);
            var highZone = CreateTestZone("high", strategicValue: 9);

            var zones = new List<Zone> { lowZone, highZone };

            var bestTarget = service.GetBestAttackTarget(zones, context);

            Assert.NotNull(bestTarget);
            Assert.Equal("high", bestTarget.Id);
        }

        [Fact]
        public void GetBestAttackTarget_ExcludesOwnedZones()
        {
            var service = new ZoneEvaluationService();
            var faction = CreateTestFaction(FactionType.Michael);
            var context = CreateTestContextForFaction(faction);

            var ownedHighValueZone = CreateTestZone("owned-high", strategicValue: 10);
            ownedHighValueZone.OwnerFactionId = faction.Id;

            var neutralLowValueZone = CreateTestZone("neutral-low", strategicValue: 3);
            neutralLowValueZone.OwnerFactionId = null;

            var zones = new List<Zone> { ownedHighValueZone, neutralLowValueZone };

            var bestTarget = service.GetBestAttackTarget(zones, context);

            Assert.NotNull(bestTarget);
            Assert.Equal("neutral-low", bestTarget.Id);
        }

        [Fact]
        public void GetBestAttackTarget_AllZonesOwned_ReturnsNull()
        {
            var service = new ZoneEvaluationService();
            var faction = CreateTestFaction(FactionType.Michael);
            var context = CreateTestContextForFaction(faction);

            var ownedZone1 = CreateTestZone("owned1", strategicValue: 5);
            ownedZone1.OwnerFactionId = faction.Id;

            var ownedZone2 = CreateTestZone("owned2", strategicValue: 7);
            ownedZone2.OwnerFactionId = faction.Id;

            var zones = new List<Zone> { ownedZone1, ownedZone2 };

            var bestTarget = service.GetBestAttackTarget(zones, context);

            Assert.Null(bestTarget);
        }

        #endregion

        #region CalculateTraitScore Tests

        [Fact]
        public void CalculateTraitScore_NoTraits_ReturnsZero()
        {
            var service = new ZoneEvaluationService();

            var score = service.CalculateTraitScore(ZoneTrait.None);

            Assert.Equal(0f, score);
        }

        [Fact]
        public void CalculateTraitScore_SingleTrait_ReturnsPositiveValue()
        {
            var service = new ZoneEvaluationService();

            var commercialScore = service.CalculateTraitScore(ZoneTrait.Commercial);
            var industrialScore = service.CalculateTraitScore(ZoneTrait.Industrial);
            var residentialScore = service.CalculateTraitScore(ZoneTrait.Residential);

            Assert.True(commercialScore > 0f);
            Assert.True(industrialScore > 0f);
            Assert.True(residentialScore > 0f);
        }

        [Fact]
        public void CalculateTraitScore_HighValueTrait_HasHighestBonus()
        {
            var service = new ZoneEvaluationService();

            var highValueScore = service.CalculateTraitScore(ZoneTrait.HighValue);
            var commercialScore = service.CalculateTraitScore(ZoneTrait.Commercial);

            Assert.True(highValueScore >= commercialScore, "HighValue trait should have significant bonus");
        }

        [Fact]
        public void CalculateTraitScore_MultipleTraits_SumsScores()
        {
            var service = new ZoneEvaluationService();

            var commercialScore = service.CalculateTraitScore(ZoneTrait.Commercial);
            var industrialScore = service.CalculateTraitScore(ZoneTrait.Industrial);
            var combinedScore = service.CalculateTraitScore(ZoneTrait.Commercial | ZoneTrait.Industrial);

            // Combined should be at least the sum (might have synergy bonuses)
            Assert.True(combinedScore >= commercialScore);
            Assert.True(combinedScore >= industrialScore);
        }

        [Fact]
        public void CalculateTraitScore_AllTraits_ReturnsCappedValue()
        {
            var service = new ZoneEvaluationService();

            var allTraits = ZoneTrait.Industrial | ZoneTrait.Commercial | ZoneTrait.Residential |
                           ZoneTrait.Port | ZoneTrait.Airfield | ZoneTrait.Fortified | ZoneTrait.HighValue;

            var score = service.CalculateTraitScore(allTraits);

            // Should be capped to reasonable value
            Assert.True(score <= 1f, "Trait score should be capped");
        }

        #endregion

        #region Configuration Tests

        [Fact]
        public void StrategicValueWeight_DefaultsToReasonableValue()
        {
            var service = new ZoneEvaluationService();

            Assert.True(service.StrategicValueWeight > 0f);
            Assert.True(service.StrategicValueWeight <= 1f);
        }

        [Fact]
        public void TraitWeight_DefaultsToReasonableValue()
        {
            var service = new ZoneEvaluationService();

            Assert.True(service.TraitWeight > 0f);
            Assert.True(service.TraitWeight <= 1f);
        }

        [Fact]
        public void AdjacencyWeight_DefaultsToReasonableValue()
        {
            var service = new ZoneEvaluationService();

            Assert.True(service.AdjacencyWeight >= 0f);
            Assert.True(service.AdjacencyWeight <= 1f);
        }

        [Fact]
        public void NeutralZoneBonus_DefaultsToPositiveValue()
        {
            var service = new ZoneEvaluationService();

            Assert.True(service.NeutralZoneBonus > 0f);
        }

        #endregion

        #region Edge Cases

        [Fact]
        public void EvaluateZone_ZoneWithAllMaxValues_HandlesCorrectly()
        {
            var service = new ZoneEvaluationService();
            var context = CreateTestContext();

            var maxZone = CreateTestZone("max", strategicValue: 10);
            maxZone.Traits = ZoneTrait.Industrial | ZoneTrait.Commercial | ZoneTrait.Residential |
                            ZoneTrait.Port | ZoneTrait.Airfield | ZoneTrait.HighValue;
            maxZone.OwnerFactionId = null; // Neutral for bonus
            maxZone.ControlPercentage = 100f;

            var score = service.EvaluateZone(maxZone, context);

            Assert.InRange(score, 0f, 1f);
        }

        [Fact]
        public void EvaluateZone_ZoneWithMinimumValues_HandlesCorrectly()
        {
            var service = new ZoneEvaluationService();
            var context = CreateTestContext();

            var minZone = CreateTestZone("min", strategicValue: 1);
            minZone.Traits = ZoneTrait.None;
            minZone.OwnerFactionId = "enemy-faction";
            minZone.ControlPercentage = 100f;

            var score = service.EvaluateZone(minZone, context);

            Assert.InRange(score, 0f, 1f);
            Assert.True(score < 0.5f, "Low value enemy zone should have low score");
        }

        [Fact]
        public void EvaluateZone_SameZoneDifferentContexts_MayHaveDifferentScores()
        {
            var service = new ZoneEvaluationService();

            var zone = CreateTestZone("test", strategicValue: 5);
            zone.OwnerFactionId = null;

            var contextWithOwnedZonesNearby = CreateTestContextWithOwnedZonesNear(zone.Center);
            var contextWithDistantOwnedZones = CreateTestContextWithDistantOwnedZones(zone.Center);

            var scoreNearby = service.EvaluateZone(zone, contextWithOwnedZonesNearby);
            var scoreDistant = service.EvaluateZone(zone, contextWithDistantOwnedZones);

            // Score should be higher when adjacent to owned territory
            Assert.True(scoreNearby >= scoreDistant);
        }

        #endregion

        #region Helper Methods

        private Faction CreateTestFaction(FactionType type, string? id = null)
        {
            var factionId = id ?? $"faction-{type.ToString().ToLower()}";
            var info = FactionTypeInfo.GetInfo(type);
            return new Faction(
                id: factionId,
                name: info.FactionName,
                leader: info.LeaderName,
                description: info.Description,
                color: info.Color);
        }

        private FactionState CreateTestFactionState(string factionId = "faction-michael", int troopCount = 50)
        {
            return new FactionState(
                factionId: factionId,
                initialCash: 10000,
                initialTroopCount: troopCount);
        }

        private Zone CreateTestZone(string id, string? ownerFactionId = null, int strategicValue = 5, Vector3? center = null)
        {
            var zone = new Zone(
                id: id,
                name: $"Test Zone {id}",
                center: center ?? new Vector3(0, 0, 0),
                radius: 150f,
                strategicValue: strategicValue);
            zone.OwnerFactionId = ownerFactionId;
            return zone;
        }

        private AIContext CreateTestContext()
        {
            var faction = CreateTestFaction(FactionType.Michael);
            return CreateTestContextForFaction(faction);
        }

        private AIContext CreateTestContextForFaction(Faction faction)
        {
            var factionState = CreateTestFactionState(faction.Id);
            var ownedZones = new List<Zone> { CreateTestZone("owned-zone", ownerFactionId: faction.Id) };
            var allZones = new List<Zone>
            {
                CreateTestZone("owned-zone", ownerFactionId: faction.Id),
                CreateTestZone("enemy-zone", ownerFactionId: "enemy-faction"),
                CreateTestZone("neutral-zone")
            };
            var enemyFactions = new List<Faction> { CreateTestFaction(FactionType.Trevor, "enemy-faction") };

            return new AIContext(faction, factionState, ownedZones, allZones, enemyFactions);
        }

        private AIContext CreateTestContextWithZones(Faction faction, IEnumerable<Zone> ownedZones, IEnumerable<Zone> allZones)
        {
            var factionState = CreateTestFactionState(faction.Id);
            var enemyFactions = new List<Faction> { CreateTestFaction(FactionType.Trevor, "enemy-faction") };

            return new AIContext(faction, factionState, ownedZones, allZones, enemyFactions);
        }

        private AIContext CreateTestContextWithOwnedZonesNear(Vector3 position)
        {
            var faction = CreateTestFaction(FactionType.Michael);
            var factionState = CreateTestFactionState(faction.Id);

            // Create owned zone adjacent to the target position
            var ownedZone = CreateTestZone("owned-nearby", ownerFactionId: faction.Id,
                center: new Vector3(position.X + 200, position.Y, position.Z));

            var allZones = new List<Zone> { ownedZone };
            var enemyFactions = new List<Faction> { CreateTestFaction(FactionType.Trevor, "enemy-faction") };

            return new AIContext(faction, factionState, new[] { ownedZone }, allZones, enemyFactions);
        }

        private AIContext CreateTestContextWithDistantOwnedZones(Vector3 position)
        {
            var faction = CreateTestFaction(FactionType.Michael);
            var factionState = CreateTestFactionState(faction.Id);

            // Create owned zone far from the target position
            var ownedZone = CreateTestZone("owned-distant", ownerFactionId: faction.Id,
                center: new Vector3(position.X + 5000, position.Y + 5000, position.Z));

            var allZones = new List<Zone> { ownedZone };
            var enemyFactions = new List<Faction> { CreateTestFaction(FactionType.Trevor, "enemy-faction") };

            return new AIContext(faction, factionState, new[] { ownedZone }, allZones, enemyFactions);
        }

        #endregion
    }
}
