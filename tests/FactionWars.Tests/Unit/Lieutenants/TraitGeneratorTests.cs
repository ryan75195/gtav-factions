using System;
using System.Collections.Generic;
using System.Linq;
using FactionWars.Lieutenants.Interfaces;
using FactionWars.Lieutenants.Models;
using FactionWars.Lieutenants.Services;
using Moq;
using Xunit;

namespace FactionWars.Tests.Unit.Lieutenants
{
    public class TraitGeneratorTests
    {
        #region Construction

        [Fact]
        public void Constructor_WithNullRandomProvider_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new TraitGenerator(null!));
        }

        [Fact]
        public void Constructor_WithValidRandomProvider_CreatesInstance()
        {
            // Arrange
            var mockRandom = new Mock<IRandomProvider>();

            // Act
            var generator = new TraitGenerator(mockRandom.Object);

            // Assert
            Assert.NotNull(generator);
        }

        #endregion

        #region GenerateTraits - Basic Functionality

        [Fact]
        public void GenerateTraits_WithZeroCount_ReturnsEmptyCollection()
        {
            // Arrange
            var mockRandom = new Mock<IRandomProvider>();
            var generator = new TraitGenerator(mockRandom.Object);

            // Act
            var traits = generator.GenerateTraits(0);

            // Assert
            Assert.Empty(traits);
        }

        [Fact]
        public void GenerateTraits_WithNegativeCount_ReturnsEmptyCollection()
        {
            // Arrange
            var mockRandom = new Mock<IRandomProvider>();
            var generator = new TraitGenerator(mockRandom.Object);

            // Act
            var traits = generator.GenerateTraits(-1);

            // Assert
            Assert.Empty(traits);
        }

        [Fact]
        public void GenerateTraits_WithPositiveCount_ReturnsRequestedNumberOfTraits()
        {
            // Arrange
            var mockRandom = new Mock<IRandomProvider>();
            mockRandom.Setup(r => r.Next(It.IsAny<int>())).Returns(0);
            mockRandom.Setup(r => r.NextDouble()).Returns(0.5);
            var generator = new TraitGenerator(mockRandom.Object);

            // Act
            var traits = generator.GenerateTraits(3);

            // Assert
            Assert.Equal(3, traits.Count);
        }

        [Fact]
        public void GenerateTraits_ReturnsDistinctTraits()
        {
            // Arrange
            var mockRandom = new Mock<IRandomProvider>();
            int callCount = 0;
            mockRandom.Setup(r => r.Next(It.IsAny<int>())).Returns(() => callCount++);
            mockRandom.Setup(r => r.NextDouble()).Returns(0.5);
            var generator = new TraitGenerator(mockRandom.Object);

            // Act
            var traits = generator.GenerateTraits(5);

            // Assert
            Assert.Equal(5, traits.Distinct().Count());
        }

        [Fact]
        public void GenerateTraits_WithCountExceedingTotalTraits_ReturnsMaxPossibleTraits()
        {
            // Arrange
            var mockRandom = new Mock<IRandomProvider>();
            int callCount = 0;
            mockRandom.Setup(r => r.Next(It.IsAny<int>())).Returns(() => callCount++ % 12);
            mockRandom.Setup(r => r.NextDouble()).Returns(0.5);
            var generator = new TraitGenerator(mockRandom.Object);
            var totalTraits = Enum.GetValues(typeof(LieutenantTrait)).Length;

            // Act
            var traits = generator.GenerateTraits(totalTraits + 5);

            // Assert
            Assert.True(traits.Count <= totalTraits);
        }

        #endregion

        #region Mutually Exclusive Traits

        [Fact]
        public void GenerateTraits_NeverContainsBothLoyalAndAmbitious()
        {
            // Arrange
            var mockRandom = new Mock<IRandomProvider>();
            int callCount = 0;
            // Setup to always try to select Loyal (index 6) then Ambitious (index 7)
            mockRandom.Setup(r => r.Next(It.IsAny<int>())).Returns(() =>
            {
                var index = callCount++;
                return index % 2 == 0 ? 6 : 7; // Alternate between Loyal and Ambitious indices
            });
            mockRandom.Setup(r => r.NextDouble()).Returns(0.5);
            var generator = new TraitGenerator(mockRandom.Object);

            // Act
            var traits = generator.GenerateTraits(5);

            // Assert
            bool hasLoyal = traits.Contains(LieutenantTrait.Loyal);
            bool hasAmbitious = traits.Contains(LieutenantTrait.Ambitious);
            Assert.False(hasLoyal && hasAmbitious, "Should not have both Loyal and Ambitious traits");
        }

        [Fact]
        public void GenerateTraits_NeverContainsBothAggressiveAndDefensive()
        {
            // Arrange
            var mockRandom = new Mock<IRandomProvider>();
            int callCount = 0;
            // Setup to always try to select Aggressive (index 0) then Defensive (index 1)
            mockRandom.Setup(r => r.Next(It.IsAny<int>())).Returns(() =>
            {
                var index = callCount++;
                return index % 2 == 0 ? 0 : 1; // Alternate between Aggressive and Defensive indices
            });
            mockRandom.Setup(r => r.NextDouble()).Returns(0.5);
            var generator = new TraitGenerator(mockRandom.Object);

            // Act
            var traits = generator.GenerateTraits(5);

            // Assert
            bool hasAggressive = traits.Contains(LieutenantTrait.Aggressive);
            bool hasDefensive = traits.Contains(LieutenantTrait.Defensive);
            Assert.False(hasAggressive && hasDefensive, "Should not have both Aggressive and Defensive traits");
        }

        #endregion

        #region Weighted Trait Generation

        [Fact]
        public void GenerateTraitsWithWeights_WithNullWeights_ThrowsArgumentNullException()
        {
            // Arrange
            var mockRandom = new Mock<IRandomProvider>();
            var generator = new TraitGenerator(mockRandom.Object);

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => generator.GenerateTraitsWithWeights(3, null!));
        }

        [Fact]
        public void GenerateTraitsWithWeights_WithEmptyWeights_UsesDefaultWeights()
        {
            // Arrange
            var mockRandom = new Mock<IRandomProvider>();
            mockRandom.Setup(r => r.Next(It.IsAny<int>())).Returns(0);
            mockRandom.Setup(r => r.NextDouble()).Returns(0.5);
            var generator = new TraitGenerator(mockRandom.Object);

            // Act
            var traits = generator.GenerateTraitsWithWeights(3, new Dictionary<LieutenantTrait, double>());

            // Assert
            Assert.Equal(3, traits.Count);
        }

        [Fact]
        public void GenerateTraitsWithWeights_HighWeightedTraitsAreMoreLikely()
        {
            // Arrange
            var mockRandom = new Mock<IRandomProvider>();
            int doubleCallCount = 0;
            // Return low values to always select highest weighted trait
            mockRandom.Setup(r => r.NextDouble()).Returns(() => doubleCallCount++ * 0.01);
            var generator = new TraitGenerator(mockRandom.Object);
            var weights = new Dictionary<LieutenantTrait, double>
            {
                { LieutenantTrait.Veteran, 10.0 },
                { LieutenantTrait.Aggressive, 1.0 },
                { LieutenantTrait.Defensive, 1.0 }
            };

            // Act
            var traits = generator.GenerateTraitsWithWeights(1, weights);

            // Assert
            Assert.Contains(LieutenantTrait.Veteran, traits);
        }

        [Fact]
        public void GenerateTraitsWithWeights_ZeroWeightedTraitsAreNeverSelected()
        {
            // Arrange
            var mockRandom = new Mock<IRandomProvider>();
            mockRandom.Setup(r => r.NextDouble()).Returns(0.5);
            var generator = new TraitGenerator(mockRandom.Object);
            var weights = new Dictionary<LieutenantTrait, double>
            {
                { LieutenantTrait.Veteran, 0.0 },
                { LieutenantTrait.Aggressive, 1.0 },
                { LieutenantTrait.Defensive, 1.0 }
            };

            // Act - Generate multiple traits to test
            var traits = generator.GenerateTraitsWithWeights(2, weights);

            // Assert
            Assert.DoesNotContain(LieutenantTrait.Veteran, traits);
        }

        #endregion

        #region Faction-Specific Trait Preferences

        [Fact]
        public void GenerateTraitsForFaction_WithNullFactionId_ThrowsArgumentNullException()
        {
            // Arrange
            var mockRandom = new Mock<IRandomProvider>();
            var generator = new TraitGenerator(mockRandom.Object);

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => generator.GenerateTraitsForFaction(3, null!));
        }

        [Fact]
        public void GenerateTraitsForFaction_WithEmptyFactionId_ThrowsArgumentException()
        {
            // Arrange
            var mockRandom = new Mock<IRandomProvider>();
            var generator = new TraitGenerator(mockRandom.Object);

            // Act & Assert
            Assert.Throws<ArgumentException>(() => generator.GenerateTraitsForFaction(3, ""));
        }

        [Fact]
        public void GenerateTraitsForFaction_MichaelFaction_FavorsCunningAndResourceful()
        {
            // Arrange
            var mockRandom = new Mock<IRandomProvider>();
            int doubleCallCount = 0;
            // Low values select highest weighted traits
            mockRandom.Setup(r => r.NextDouble()).Returns(() => doubleCallCount++ * 0.01);
            var generator = new TraitGenerator(mockRandom.Object);

            // Act
            var traits = generator.GenerateTraitsForFaction(2, "michael");

            // Assert
            // Michael's faction should favor calculated traits
            Assert.True(
                traits.Contains(LieutenantTrait.Cunning) ||
                traits.Contains(LieutenantTrait.Resourceful) ||
                traits.Contains(LieutenantTrait.Connected),
                "Michael's faction should favor strategic traits");
        }

        [Fact]
        public void GenerateTraitsForFaction_TrevorFaction_FavorsAggressiveAndRuthless()
        {
            // Arrange
            var mockRandom = new Mock<IRandomProvider>();
            int doubleCallCount = 0;
            mockRandom.Setup(r => r.NextDouble()).Returns(() => doubleCallCount++ * 0.01);
            var generator = new TraitGenerator(mockRandom.Object);

            // Act
            var traits = generator.GenerateTraitsForFaction(2, "trevor");

            // Assert
            Assert.True(
                traits.Contains(LieutenantTrait.Aggressive) ||
                traits.Contains(LieutenantTrait.Ruthless) ||
                traits.Contains(LieutenantTrait.Intimidating),
                "Trevor's faction should favor combat traits");
        }

        [Fact]
        public void GenerateTraitsForFaction_FranklinFaction_FavorsCharismaticAndLoyal()
        {
            // Arrange
            var mockRandom = new Mock<IRandomProvider>();
            int doubleCallCount = 0;
            mockRandom.Setup(r => r.NextDouble()).Returns(() => doubleCallCount++ * 0.01);
            var generator = new TraitGenerator(mockRandom.Object);

            // Act
            var traits = generator.GenerateTraitsForFaction(2, "franklin");

            // Assert
            Assert.True(
                traits.Contains(LieutenantTrait.Charismatic) ||
                traits.Contains(LieutenantTrait.Loyal) ||
                traits.Contains(LieutenantTrait.Veteran),
                "Franklin's faction should favor loyalty traits");
        }

        [Fact]
        public void GenerateTraitsForFaction_UnknownFaction_UsesDefaultWeights()
        {
            // Arrange
            var mockRandom = new Mock<IRandomProvider>();
            mockRandom.Setup(r => r.Next(It.IsAny<int>())).Returns(0);
            mockRandom.Setup(r => r.NextDouble()).Returns(0.5);
            var generator = new TraitGenerator(mockRandom.Object);

            // Act
            var traits = generator.GenerateTraitsForFaction(3, "unknown_faction");

            // Assert
            Assert.Equal(3, traits.Count);
        }

        [Fact]
        public void GenerateTraitsForFaction_IsCaseInsensitive()
        {
            // Arrange
            var mockRandom = new Mock<IRandomProvider>();
            int doubleCallCount = 0;
            mockRandom.Setup(r => r.NextDouble()).Returns(() => doubleCallCount++ * 0.01);
            var generator = new TraitGenerator(mockRandom.Object);

            // Act
            var traitsLower = generator.GenerateTraitsForFaction(2, "michael");
            doubleCallCount = 0;
            var traitsUpper = generator.GenerateTraitsForFaction(2, "MICHAEL");

            // Assert
            Assert.Equal(traitsLower, traitsUpper);
        }

        #endregion

        #region GetMutuallyExclusiveTraits

        [Fact]
        public void GetMutuallyExclusiveTraits_ForLoyal_ReturnsAmbitious()
        {
            // Arrange
            var mockRandom = new Mock<IRandomProvider>();
            var generator = new TraitGenerator(mockRandom.Object);

            // Act
            var exclusives = generator.GetMutuallyExclusiveTraits(LieutenantTrait.Loyal);

            // Assert
            Assert.Contains(LieutenantTrait.Ambitious, exclusives);
        }

        [Fact]
        public void GetMutuallyExclusiveTraits_ForAmbitious_ReturnsLoyal()
        {
            // Arrange
            var mockRandom = new Mock<IRandomProvider>();
            var generator = new TraitGenerator(mockRandom.Object);

            // Act
            var exclusives = generator.GetMutuallyExclusiveTraits(LieutenantTrait.Ambitious);

            // Assert
            Assert.Contains(LieutenantTrait.Loyal, exclusives);
        }

        [Fact]
        public void GetMutuallyExclusiveTraits_ForAggressive_ReturnsDefensive()
        {
            // Arrange
            var mockRandom = new Mock<IRandomProvider>();
            var generator = new TraitGenerator(mockRandom.Object);

            // Act
            var exclusives = generator.GetMutuallyExclusiveTraits(LieutenantTrait.Aggressive);

            // Assert
            Assert.Contains(LieutenantTrait.Defensive, exclusives);
        }

        [Fact]
        public void GetMutuallyExclusiveTraits_ForDefensive_ReturnsAggressive()
        {
            // Arrange
            var mockRandom = new Mock<IRandomProvider>();
            var generator = new TraitGenerator(mockRandom.Object);

            // Act
            var exclusives = generator.GetMutuallyExclusiveTraits(LieutenantTrait.Defensive);

            // Assert
            Assert.Contains(LieutenantTrait.Aggressive, exclusives);
        }

        [Fact]
        public void GetMutuallyExclusiveTraits_ForTraitWithNoExclusives_ReturnsEmpty()
        {
            // Arrange
            var mockRandom = new Mock<IRandomProvider>();
            var generator = new TraitGenerator(mockRandom.Object);

            // Act
            var exclusives = generator.GetMutuallyExclusiveTraits(LieutenantTrait.Veteran);

            // Assert
            Assert.Empty(exclusives);
        }

        #endregion

        #region GetDefaultTraitWeights

        [Fact]
        public void GetDefaultTraitWeights_ReturnsWeightsForAllTraits()
        {
            // Arrange
            var mockRandom = new Mock<IRandomProvider>();
            var generator = new TraitGenerator(mockRandom.Object);
            var allTraits = Enum.GetValues(typeof(LieutenantTrait)).Cast<LieutenantTrait>().ToList();

            // Act
            var weights = generator.GetDefaultTraitWeights();

            // Assert
            foreach (var trait in allTraits)
            {
                Assert.True(weights.ContainsKey(trait), $"Missing weight for trait: {trait}");
            }
        }

        [Fact]
        public void GetDefaultTraitWeights_AllWeightsArePositive()
        {
            // Arrange
            var mockRandom = new Mock<IRandomProvider>();
            var generator = new TraitGenerator(mockRandom.Object);

            // Act
            var weights = generator.GetDefaultTraitWeights();

            // Assert
            foreach (var weight in weights.Values)
            {
                Assert.True(weight > 0, "All default weights should be positive");
            }
        }

        #endregion

        #region GetFactionTraitWeights

        [Fact]
        public void GetFactionTraitWeights_WithNullFactionId_ThrowsArgumentNullException()
        {
            // Arrange
            var mockRandom = new Mock<IRandomProvider>();
            var generator = new TraitGenerator(mockRandom.Object);

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => generator.GetFactionTraitWeights(null!));
        }

        [Fact]
        public void GetFactionTraitWeights_MichaelFaction_HasHigherCunningWeight()
        {
            // Arrange
            var mockRandom = new Mock<IRandomProvider>();
            var generator = new TraitGenerator(mockRandom.Object);

            // Act
            var michaelWeights = generator.GetFactionTraitWeights("michael");
            var defaultWeights = generator.GetDefaultTraitWeights();

            // Assert
            Assert.True(michaelWeights[LieutenantTrait.Cunning] > defaultWeights[LieutenantTrait.Cunning],
                "Michael's faction should have higher cunning weight");
        }

        [Fact]
        public void GetFactionTraitWeights_TrevorFaction_HasHigherAggressiveWeight()
        {
            // Arrange
            var mockRandom = new Mock<IRandomProvider>();
            var generator = new TraitGenerator(mockRandom.Object);

            // Act
            var trevorWeights = generator.GetFactionTraitWeights("trevor");
            var defaultWeights = generator.GetDefaultTraitWeights();

            // Assert
            Assert.True(trevorWeights[LieutenantTrait.Aggressive] > defaultWeights[LieutenantTrait.Aggressive],
                "Trevor's faction should have higher aggressive weight");
        }

        [Fact]
        public void GetFactionTraitWeights_FranklinFaction_HasHigherLoyalWeight()
        {
            // Arrange
            var mockRandom = new Mock<IRandomProvider>();
            var generator = new TraitGenerator(mockRandom.Object);

            // Act
            var franklinWeights = generator.GetFactionTraitWeights("franklin");
            var defaultWeights = generator.GetDefaultTraitWeights();

            // Assert
            Assert.True(franklinWeights[LieutenantTrait.Loyal] > defaultWeights[LieutenantTrait.Loyal],
                "Franklin's faction should have higher loyal weight");
        }

        #endregion
    }
}
