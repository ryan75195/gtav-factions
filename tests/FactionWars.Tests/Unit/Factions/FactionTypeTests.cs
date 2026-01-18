using FactionWars.Factions.Models;
using Xunit;

namespace FactionWars.Tests.Unit.Factions
{
    public class FactionTypeTests
    {
        #region Enum Values

        [Fact]
        public void FactionType_ShouldHaveMichaelType()
        {
            // Act & Assert
            Assert.True(System.Enum.IsDefined(typeof(FactionType), FactionType.Michael));
        }

        [Fact]
        public void FactionType_ShouldHaveTrevorType()
        {
            // Act & Assert
            Assert.True(System.Enum.IsDefined(typeof(FactionType), FactionType.Trevor));
        }

        [Fact]
        public void FactionType_ShouldHaveFranklinType()
        {
            // Act & Assert
            Assert.True(System.Enum.IsDefined(typeof(FactionType), FactionType.Franklin));
        }

        [Fact]
        public void FactionType_ShouldHaveExactlyThreeValues()
        {
            // Arrange
            var values = System.Enum.GetValues(typeof(FactionType));

            // Assert
            Assert.Equal(3, values.Length);
        }

        #endregion

        #region FactionTypeInfo - Michael

        [Fact]
        public void FactionTypeInfo_Michael_ShouldHaveCorrectLeaderName()
        {
            // Act
            var info = FactionTypeInfo.GetInfo(FactionType.Michael);

            // Assert
            Assert.Equal("Michael De Santa", info.LeaderName);
        }

        [Fact]
        public void FactionTypeInfo_Michael_ShouldHaveCorrectFactionName()
        {
            // Act
            var info = FactionTypeInfo.GetInfo(FactionType.Michael);

            // Assert
            Assert.Equal("De Santa Family", info.FactionName);
        }

        [Fact]
        public void FactionTypeInfo_Michael_ShouldHaveBlueColor()
        {
            // Act
            var info = FactionTypeInfo.GetInfo(FactionType.Michael);

            // Assert - Michael should be blue (sophisticated, professional)
            Assert.Equal(new FactionColor(0, 100, 255), info.Color);
        }

        [Fact]
        public void FactionTypeInfo_Michael_ShouldHaveIncomeBonus()
        {
            // Act
            var info = FactionTypeInfo.GetInfo(FactionType.Michael);

            // Assert - Michael focuses on money (calculated, high-value heists)
            Assert.Equal(1.25f, info.IncomeBonus);
        }

        [Fact]
        public void FactionTypeInfo_Michael_ShouldHaveNeutralCombatBonus()
        {
            // Act
            var info = FactionTypeInfo.GetInfo(FactionType.Michael);

            // Assert - Michael isn't combat-focused
            Assert.Equal(1.0f, info.CombatBonus);
        }

        [Fact]
        public void FactionTypeInfo_Michael_ShouldHaveDefenseBonus()
        {
            // Act
            var info = FactionTypeInfo.GetInfo(FactionType.Michael);

            // Assert - Michael is defensive/strategic
            Assert.Equal(1.15f, info.DefenseBonus);
        }

        [Fact]
        public void FactionTypeInfo_Michael_ShouldHaveCorrectDescription()
        {
            // Act
            var info = FactionTypeInfo.GetInfo(FactionType.Michael);

            // Assert
            Assert.Contains("calculated", info.Description.ToLower());
        }

        #endregion

        #region FactionTypeInfo - Trevor

        [Fact]
        public void FactionTypeInfo_Trevor_ShouldHaveCorrectLeaderName()
        {
            // Act
            var info = FactionTypeInfo.GetInfo(FactionType.Trevor);

            // Assert
            Assert.Equal("Trevor Philips", info.LeaderName);
        }

        [Fact]
        public void FactionTypeInfo_Trevor_ShouldHaveCorrectFactionName()
        {
            // Act
            var info = FactionTypeInfo.GetInfo(FactionType.Trevor);

            // Assert
            Assert.Equal("Trevor Philips Industries", info.FactionName);
        }

        [Fact]
        public void FactionTypeInfo_Trevor_ShouldHaveOrangeColor()
        {
            // Act
            var info = FactionTypeInfo.GetInfo(FactionType.Trevor);

            // Assert - Trevor should be orange (chaotic, aggressive)
            Assert.Equal(new FactionColor(255, 128, 0), info.Color);
        }

        [Fact]
        public void FactionTypeInfo_Trevor_ShouldHaveLowerIncomeBonus()
        {
            // Act
            var info = FactionTypeInfo.GetInfo(FactionType.Trevor);

            // Assert - Trevor is chaotic, not focused on economics
            Assert.Equal(0.9f, info.IncomeBonus);
        }

        [Fact]
        public void FactionTypeInfo_Trevor_ShouldHaveHighCombatBonus()
        {
            // Act
            var info = FactionTypeInfo.GetInfo(FactionType.Trevor);

            // Assert - Trevor is extremely aggressive and combat-focused
            Assert.Equal(1.35f, info.CombatBonus);
        }

        [Fact]
        public void FactionTypeInfo_Trevor_ShouldHaveLowerDefenseBonus()
        {
            // Act
            var info = FactionTypeInfo.GetInfo(FactionType.Trevor);

            // Assert - Trevor is offense-focused, weaker defense
            Assert.Equal(0.85f, info.DefenseBonus);
        }

        [Fact]
        public void FactionTypeInfo_Trevor_ShouldHaveCorrectDescription()
        {
            // Act
            var info = FactionTypeInfo.GetInfo(FactionType.Trevor);

            // Assert
            Assert.Contains("aggressive", info.Description.ToLower());
        }

        #endregion

        #region FactionTypeInfo - Franklin

        [Fact]
        public void FactionTypeInfo_Franklin_ShouldHaveCorrectLeaderName()
        {
            // Act
            var info = FactionTypeInfo.GetInfo(FactionType.Franklin);

            // Assert
            Assert.Equal("Franklin Clinton", info.LeaderName);
        }

        [Fact]
        public void FactionTypeInfo_Franklin_ShouldHaveCorrectFactionName()
        {
            // Act
            var info = FactionTypeInfo.GetInfo(FactionType.Franklin);

            // Assert
            Assert.Equal("Clinton Organization", info.FactionName);
        }

        [Fact]
        public void FactionTypeInfo_Franklin_ShouldHaveGreenColor()
        {
            // Act
            var info = FactionTypeInfo.GetInfo(FactionType.Franklin);

            // Assert - Franklin should be green (his canonical color, also growth/balance)
            Assert.Equal(new FactionColor(0, 200, 100), info.Color);
        }

        [Fact]
        public void FactionTypeInfo_Franklin_ShouldHaveBalancedIncomeBonus()
        {
            // Act
            var info = FactionTypeInfo.GetInfo(FactionType.Franklin);

            // Assert - Franklin is balanced/opportunistic
            Assert.Equal(1.1f, info.IncomeBonus);
        }

        [Fact]
        public void FactionTypeInfo_Franklin_ShouldHaveBalancedCombatBonus()
        {
            // Act
            var info = FactionTypeInfo.GetInfo(FactionType.Franklin);

            // Assert - Franklin is adaptable
            Assert.Equal(1.1f, info.CombatBonus);
        }

        [Fact]
        public void FactionTypeInfo_Franklin_ShouldHaveBalancedDefenseBonus()
        {
            // Act
            var info = FactionTypeInfo.GetInfo(FactionType.Franklin);

            // Assert - Franklin is well-rounded
            Assert.Equal(1.1f, info.DefenseBonus);
        }

        [Fact]
        public void FactionTypeInfo_Franklin_ShouldHaveMobilityBonus()
        {
            // Act
            var info = FactionTypeInfo.GetInfo(FactionType.Franklin);

            // Assert - Franklin is known for driving and mobility
            Assert.Equal(1.25f, info.MobilityBonus);
        }

        [Fact]
        public void FactionTypeInfo_Franklin_ShouldHaveCorrectDescription()
        {
            // Act
            var info = FactionTypeInfo.GetInfo(FactionType.Franklin);

            // Assert
            Assert.Contains("opportunistic", info.Description.ToLower());
        }

        #endregion

        #region FactionTypeInfo - Mobility (All Types)

        [Fact]
        public void FactionTypeInfo_Michael_ShouldHaveNeutralMobility()
        {
            // Act
            var info = FactionTypeInfo.GetInfo(FactionType.Michael);

            // Assert
            Assert.Equal(1.0f, info.MobilityBonus);
        }

        [Fact]
        public void FactionTypeInfo_Trevor_ShouldHaveNeutralMobility()
        {
            // Act
            var info = FactionTypeInfo.GetInfo(FactionType.Trevor);

            // Assert
            Assert.Equal(1.0f, info.MobilityBonus);
        }

        #endregion

        #region FactionTypeInfo - Recruitment (All Types)

        [Fact]
        public void FactionTypeInfo_Michael_ShouldHaveNeutralRecruitment()
        {
            // Act
            var info = FactionTypeInfo.GetInfo(FactionType.Michael);

            // Assert - Michael is professional but not charismatic
            Assert.Equal(1.0f, info.RecruitmentBonus);
        }

        [Fact]
        public void FactionTypeInfo_Trevor_ShouldHaveHigherRecruitment()
        {
            // Act
            var info = FactionTypeInfo.GetInfo(FactionType.Trevor);

            // Assert - Trevor attracts violent/chaotic followers
            Assert.Equal(1.2f, info.RecruitmentBonus);
        }

        [Fact]
        public void FactionTypeInfo_Franklin_ShouldHaveBalancedRecruitment()
        {
            // Act
            var info = FactionTypeInfo.GetInfo(FactionType.Franklin);

            // Assert
            Assert.Equal(1.05f, info.RecruitmentBonus);
        }

        #endregion

        #region FactionTypeInfo - Static Method Validation

        [Fact]
        public void FactionTypeInfo_GetInfo_ShouldReturnSameInstance()
        {
            // Act
            var info1 = FactionTypeInfo.GetInfo(FactionType.Michael);
            var info2 = FactionTypeInfo.GetInfo(FactionType.Michael);

            // Assert - Should return cached instance
            Assert.Same(info1, info2);
        }

        [Fact]
        public void FactionTypeInfo_AllTypes_ShouldHaveInfo()
        {
            // Arrange
            var allTypes = System.Enum.GetValues(typeof(FactionType));

            // Act & Assert - Every faction type should have info
            foreach (FactionType type in allTypes)
            {
                var info = FactionTypeInfo.GetInfo(type);
                Assert.NotNull(info);
                Assert.NotEmpty(info.LeaderName);
                Assert.NotEmpty(info.FactionName);
                Assert.NotEmpty(info.Description);
            }
        }

        #endregion
    }
}
