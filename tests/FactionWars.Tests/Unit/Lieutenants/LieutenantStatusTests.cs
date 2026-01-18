using FactionWars.Lieutenants.Models;
using Xunit;

namespace FactionWars.Tests.Unit.Lieutenants
{
    public class LieutenantStatusTests
    {
        #region Enum Values

        [Fact]
        public void LieutenantStatus_ShouldHaveActiveStatus()
        {
            // Assert
            Assert.True(System.Enum.IsDefined(typeof(LieutenantStatus), LieutenantStatus.Active));
        }

        [Fact]
        public void LieutenantStatus_ShouldHaveCapturedStatus()
        {
            // Assert
            Assert.True(System.Enum.IsDefined(typeof(LieutenantStatus), LieutenantStatus.Captured));
        }

        [Fact]
        public void LieutenantStatus_ShouldHaveDeceasedStatus()
        {
            // Assert
            Assert.True(System.Enum.IsDefined(typeof(LieutenantStatus), LieutenantStatus.Deceased));
        }

        [Fact]
        public void LieutenantStatus_ShouldHaveRecoveringStatus()
        {
            // Assert
            Assert.True(System.Enum.IsDefined(typeof(LieutenantStatus), LieutenantStatus.Recovering));
        }

        #endregion
    }
}
