using FactionWars.Economy.Models;
using System;
using Xunit;

namespace FactionWars.Tests.Unit.Economy
{
    public class ResourceTypeTests
    {
        #region ResourceType Enum Values

        [Fact]
        public void ResourceType_ShouldHaveCashValue()
        {
            // Cash is the primary currency for purchasing and operations
            var resourceType = ResourceType.Cash;
            Assert.True(Enum.IsDefined(typeof(ResourceType), resourceType));
        }

        [Fact]
        public void ResourceType_ShouldHaveRecruitmentValue()
        {
            // Recruitment points used to hire new troops
            var resourceType = ResourceType.Recruitment;
            Assert.True(Enum.IsDefined(typeof(ResourceType), resourceType));
        }

        [Fact]
        public void ResourceType_ShouldHaveWeaponsValue()
        {
            // Weapons stockpile that enhances military strength
            var resourceType = ResourceType.Weapons;
            Assert.True(Enum.IsDefined(typeof(ResourceType), resourceType));
        }

        [Theory]
        [InlineData(ResourceType.Cash)]
        [InlineData(ResourceType.Recruitment)]
        [InlineData(ResourceType.Weapons)]
        public void ResourceType_AllValues_ShouldBeDefined(ResourceType resourceType)
        {
            // Assert all resource types are valid enum values
            Assert.True(Enum.IsDefined(typeof(ResourceType), resourceType));
        }

        [Fact]
        public void ResourceType_ShouldHaveExactlyThreeValues()
        {
            // Ensure we have exactly the three core resource types
            var values = Enum.GetValues(typeof(ResourceType));
            Assert.Equal(3, values.Length);
        }

        #endregion

        #region ResourceType Integer Values

        [Fact]
        public void ResourceType_Cash_ShouldBeZero()
        {
            // Cash is the first/primary resource
            Assert.Equal(0, (int)ResourceType.Cash);
        }

        [Fact]
        public void ResourceType_Recruitment_ShouldBeOne()
        {
            // Recruitment is the second resource
            Assert.Equal(1, (int)ResourceType.Recruitment);
        }

        [Fact]
        public void ResourceType_Weapons_ShouldBeTwo()
        {
            // Weapons is the third resource
            Assert.Equal(2, (int)ResourceType.Weapons);
        }

        [Fact]
        public void ResourceType_Values_ShouldBeContiguous()
        {
            // Values should be contiguous integers starting from 0
            var values = (int[])Enum.GetValues(typeof(ResourceType));
            Array.Sort(values);

            for (int i = 0; i < values.Length; i++)
            {
                Assert.Equal(i, values[i]);
            }
        }

        #endregion

        #region ResourceType String Representation

        [Theory]
        [InlineData(ResourceType.Cash, "Cash")]
        [InlineData(ResourceType.Recruitment, "Recruitment")]
        [InlineData(ResourceType.Weapons, "Weapons")]
        public void ResourceType_ToString_ShouldReturnCorrectName(ResourceType resourceType, string expectedName)
        {
            // Verify string representation for display purposes
            Assert.Equal(expectedName, resourceType.ToString());
        }

        [Theory]
        [InlineData("Cash", ResourceType.Cash)]
        [InlineData("Recruitment", ResourceType.Recruitment)]
        [InlineData("Weapons", ResourceType.Weapons)]
        public void ResourceType_Parse_ShouldReturnCorrectValue(string name, ResourceType expectedType)
        {
            // Verify parsing from string works correctly
            var parsed = (ResourceType)Enum.Parse(typeof(ResourceType), name);
            Assert.Equal(expectedType, parsed);
        }

        [Fact]
        public void ResourceType_Parse_InvalidValue_ShouldThrow()
        {
            // Invalid resource type names should throw
            Assert.Throws<ArgumentException>(() =>
                Enum.Parse(typeof(ResourceType), "InvalidResource"));
        }

        #endregion

        #region ResourceTypeInfo Tests

        [Fact]
        public void ResourceTypeInfo_ShouldExist()
        {
            // ResourceTypeInfo should provide metadata about resource types
            var info = new ResourceTypeInfo(ResourceType.Cash);
            Assert.NotNull(info);
        }

        [Theory]
        [InlineData(ResourceType.Cash)]
        [InlineData(ResourceType.Recruitment)]
        [InlineData(ResourceType.Weapons)]
        public void ResourceTypeInfo_ShouldProvideDisplayName(ResourceType resourceType)
        {
            // Each resource type should have a display name
            var info = new ResourceTypeInfo(resourceType);
            Assert.False(string.IsNullOrEmpty(info.DisplayName));
        }

        [Theory]
        [InlineData(ResourceType.Cash)]
        [InlineData(ResourceType.Recruitment)]
        [InlineData(ResourceType.Weapons)]
        public void ResourceTypeInfo_ShouldProvideDescription(ResourceType resourceType)
        {
            // Each resource type should have a description
            var info = new ResourceTypeInfo(resourceType);
            Assert.False(string.IsNullOrEmpty(info.Description));
        }

        [Theory]
        [InlineData(ResourceType.Cash, "$")]
        public void ResourceTypeInfo_Cash_ShouldHaveSymbol(ResourceType resourceType, string expectedSymbol)
        {
            // Cash should have a currency symbol
            var info = new ResourceTypeInfo(resourceType);
            Assert.Equal(expectedSymbol, info.Symbol);
        }

        [Theory]
        [InlineData(ResourceType.Cash)]
        [InlineData(ResourceType.Recruitment)]
        [InlineData(ResourceType.Weapons)]
        public void ResourceTypeInfo_ShouldHaveBaseGenerationRate(ResourceType resourceType)
        {
            // Each resource type should have a base generation rate
            var info = new ResourceTypeInfo(resourceType);
            Assert.True(info.BaseGenerationRate >= 0);
        }

        [Theory]
        [InlineData(ResourceType.Cash)]
        [InlineData(ResourceType.Recruitment)]
        [InlineData(ResourceType.Weapons)]
        public void ResourceTypeInfo_ShouldHaveDefaultCap(ResourceType resourceType)
        {
            // Each resource type should have a default storage cap
            var info = new ResourceTypeInfo(resourceType);
            Assert.True(info.DefaultCap > 0);
        }

        [Theory]
        [InlineData(ResourceType.Cash, 100)]
        [InlineData(ResourceType.Recruitment, 10)]
        [InlineData(ResourceType.Weapons, 5)]
        public void ResourceTypeInfo_BaseGenerationRate_ShouldMatchExpected(ResourceType resourceType, int expectedRate)
        {
            // Verify specific base generation rates per resource type
            var info = new ResourceTypeInfo(resourceType);
            Assert.Equal(expectedRate, info.BaseGenerationRate);
        }

        [Theory]
        [InlineData(ResourceType.Cash, 100000)]
        [InlineData(ResourceType.Recruitment, 1000)]
        [InlineData(ResourceType.Weapons, 500)]
        public void ResourceTypeInfo_DefaultCap_ShouldMatchExpected(ResourceType resourceType, int expectedCap)
        {
            // Verify specific default caps per resource type
            var info = new ResourceTypeInfo(resourceType);
            Assert.Equal(expectedCap, info.DefaultCap);
        }

        #endregion

        #region ResourceTypeInfo Factory Method

        [Fact]
        public void ResourceTypeInfo_GetInfo_ShouldReturnInfoForAllTypes()
        {
            // GetInfo should work for all resource types
            foreach (ResourceType type in Enum.GetValues(typeof(ResourceType)))
            {
                var info = ResourceTypeInfo.GetInfo(type);
                Assert.NotNull(info);
                Assert.Equal(type, info.ResourceType);
            }
        }

        [Fact]
        public void ResourceTypeInfo_GetInfo_ShouldReturnCachedInstances()
        {
            // Multiple calls should return same instance (caching)
            var info1 = ResourceTypeInfo.GetInfo(ResourceType.Cash);
            var info2 = ResourceTypeInfo.GetInfo(ResourceType.Cash);
            Assert.Same(info1, info2);
        }

        #endregion
    }
}
