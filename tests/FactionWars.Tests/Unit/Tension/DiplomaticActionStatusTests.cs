using FactionWars.Tension.Models;
using Xunit;

namespace FactionWars.Tests.Unit.Tension
{
    /// <summary>
    /// Tests for the DiplomaticActionStatus enum which defines the possible states
    /// of a diplomatic action.
    /// </summary>
    public class DiplomaticActionStatusTests
    {
        [Fact]
        public void DiplomaticActionStatus_HasProposedOption()
        {
            var status = DiplomaticActionStatus.Proposed;
            Assert.Equal(0, (int)status);
        }

        [Fact]
        public void DiplomaticActionStatus_HasPendingOption()
        {
            var status = DiplomaticActionStatus.Pending;
            Assert.Equal(1, (int)status);
        }

        [Fact]
        public void DiplomaticActionStatus_HasAcceptedOption()
        {
            var status = DiplomaticActionStatus.Accepted;
            Assert.Equal(2, (int)status);
        }

        [Fact]
        public void DiplomaticActionStatus_HasRejectedOption()
        {
            var status = DiplomaticActionStatus.Rejected;
            Assert.Equal(3, (int)status);
        }

        [Fact]
        public void DiplomaticActionStatus_HasActiveOption()
        {
            var status = DiplomaticActionStatus.Active;
            Assert.Equal(4, (int)status);
        }

        [Fact]
        public void DiplomaticActionStatus_HasExpiredOption()
        {
            var status = DiplomaticActionStatus.Expired;
            Assert.Equal(5, (int)status);
        }

        [Fact]
        public void DiplomaticActionStatus_HasBrokenOption()
        {
            var status = DiplomaticActionStatus.Broken;
            Assert.Equal(6, (int)status);
        }

        [Fact]
        public void DiplomaticActionStatus_HasCancelledOption()
        {
            var status = DiplomaticActionStatus.Cancelled;
            Assert.Equal(7, (int)status);
        }

        [Theory]
        [InlineData(DiplomaticActionStatus.Proposed)]
        [InlineData(DiplomaticActionStatus.Pending)]
        [InlineData(DiplomaticActionStatus.Accepted)]
        [InlineData(DiplomaticActionStatus.Rejected)]
        [InlineData(DiplomaticActionStatus.Active)]
        [InlineData(DiplomaticActionStatus.Expired)]
        [InlineData(DiplomaticActionStatus.Broken)]
        [InlineData(DiplomaticActionStatus.Cancelled)]
        public void DiplomaticActionStatus_AllValues_CanBeParsedFromString(DiplomaticActionStatus status)
        {
            var name = status.ToString();
            Assert.True(System.Enum.TryParse<DiplomaticActionStatus>(name, out var parsed));
            Assert.Equal(status, parsed);
        }
    }
}
