using FactionWars.Lieutenants.Models;
using Xunit;

namespace FactionWars.Tests.Unit.Lieutenants
{
    public class LieutenantTraitTests
    {
        #region Enum Values

        [Fact]
        public void LieutenantTrait_ShouldHaveAggressiveTrait()
        {
            // Assert
            Assert.True(System.Enum.IsDefined(typeof(LieutenantTrait), LieutenantTrait.Aggressive));
        }

        [Fact]
        public void LieutenantTrait_ShouldHaveDefensiveTrait()
        {
            // Assert
            Assert.True(System.Enum.IsDefined(typeof(LieutenantTrait), LieutenantTrait.Defensive));
        }

        [Fact]
        public void LieutenantTrait_ShouldHaveCunningTrait()
        {
            // Assert
            Assert.True(System.Enum.IsDefined(typeof(LieutenantTrait), LieutenantTrait.Cunning));
        }

        [Fact]
        public void LieutenantTrait_ShouldHaveCharimaticTrait()
        {
            // Assert
            Assert.True(System.Enum.IsDefined(typeof(LieutenantTrait), LieutenantTrait.Charismatic));
        }

        [Fact]
        public void LieutenantTrait_ShouldHaveResourcefulTrait()
        {
            // Assert
            Assert.True(System.Enum.IsDefined(typeof(LieutenantTrait), LieutenantTrait.Resourceful));
        }

        [Fact]
        public void LieutenantTrait_ShouldHaveRuthlessTrait()
        {
            // Assert
            Assert.True(System.Enum.IsDefined(typeof(LieutenantTrait), LieutenantTrait.Ruthless));
        }

        [Fact]
        public void LieutenantTrait_ShouldHaveLoyalTrait()
        {
            // Assert
            Assert.True(System.Enum.IsDefined(typeof(LieutenantTrait), LieutenantTrait.Loyal));
        }

        [Fact]
        public void LieutenantTrait_ShouldHaveAmbitiousTrait()
        {
            // Assert
            Assert.True(System.Enum.IsDefined(typeof(LieutenantTrait), LieutenantTrait.Ambitious));
        }

        [Fact]
        public void LieutenantTrait_ShouldHaveVeteranTrait()
        {
            // Assert
            Assert.True(System.Enum.IsDefined(typeof(LieutenantTrait), LieutenantTrait.Veteran));
        }

        [Fact]
        public void LieutenantTrait_ShouldHaveConnectedTrait()
        {
            // Assert
            Assert.True(System.Enum.IsDefined(typeof(LieutenantTrait), LieutenantTrait.Connected));
        }

        [Fact]
        public void LieutenantTrait_ShouldHaveIntimidatingTrait()
        {
            // Assert
            Assert.True(System.Enum.IsDefined(typeof(LieutenantTrait), LieutenantTrait.Intimidating));
        }

        [Fact]
        public void LieutenantTrait_ShouldHaveCorruptTrait()
        {
            // Assert
            Assert.True(System.Enum.IsDefined(typeof(LieutenantTrait), LieutenantTrait.Corrupt));
        }

        #endregion
    }
}
