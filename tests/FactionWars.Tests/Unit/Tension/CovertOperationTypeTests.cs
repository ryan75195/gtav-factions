using FactionWars.Tension.Models;
using Xunit;

namespace FactionWars.Tests.Unit.Tension
{
    /// <summary>
    /// Tests for the CovertOperationType enum.
    /// </summary>
    public class CovertOperationTypeTests
    {
        [Fact]
        public void CovertOperationType_Sabotage_HasExpectedValue()
        {
            Assert.Equal(0, (int)CovertOperationType.Sabotage);
        }

        [Fact]
        public void CovertOperationType_Assassination_HasExpectedValue()
        {
            Assert.Equal(1, (int)CovertOperationType.Assassination);
        }

        [Fact]
        public void CovertOperationType_Bribery_HasExpectedValue()
        {
            Assert.Equal(2, (int)CovertOperationType.Bribery);
        }

        [Fact]
        public void CovertOperationType_HasExactlyThreeValues()
        {
            var values = System.Enum.GetValues(typeof(CovertOperationType));
            Assert.Equal(3, values.Length);
        }

        [Theory]
        [InlineData(CovertOperationType.Sabotage)]
        [InlineData(CovertOperationType.Assassination)]
        [InlineData(CovertOperationType.Bribery)]
        public void CovertOperationType_AllValues_HaveUniqueIntegerValues(CovertOperationType operationType)
        {
            var values = System.Enum.GetValues(typeof(CovertOperationType));
            int count = 0;
            foreach (CovertOperationType value in values)
            {
                if ((int)value == (int)operationType)
                    count++;
            }
            Assert.Equal(1, count);
        }
    }
}
