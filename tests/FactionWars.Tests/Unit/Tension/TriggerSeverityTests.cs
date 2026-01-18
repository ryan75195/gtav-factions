using Xunit;
using FactionWars.Tension.Models;

namespace FactionWars.Tests.Unit.Tension
{
    /// <summary>
    /// Tests for the TriggerSeverity enum which modifies the impact
    /// of tension escalation triggers.
    /// </summary>
    public class TriggerSeverityTests
    {
        #region Enum Value Tests

        [Fact]
        public void Minor_HasCorrectValue()
        {
            Assert.Equal(0, (int)TriggerSeverity.Minor);
        }

        [Fact]
        public void Normal_HasCorrectValue()
        {
            Assert.Equal(1, (int)TriggerSeverity.Normal);
        }

        [Fact]
        public void Major_HasCorrectValue()
        {
            Assert.Equal(2, (int)TriggerSeverity.Major);
        }

        [Fact]
        public void Critical_HasCorrectValue()
        {
            Assert.Equal(3, (int)TriggerSeverity.Critical);
        }

        #endregion

        #region Enum Completeness

        [Fact]
        public void TriggerSeverity_HasExpectedNumberOfValues()
        {
            var values = System.Enum.GetValues(typeof(TriggerSeverity));
            Assert.Equal(4, values.Length);
        }

        #endregion
    }
}
