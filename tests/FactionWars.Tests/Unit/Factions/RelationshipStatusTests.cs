using FactionWars.Factions.Models;
using Xunit;

namespace FactionWars.Tests.Unit.Factions
{
    public class RelationshipStatusTests
    {
        [Fact]
        public void RelationshipStatus_ContainsAllExpectedValues()
        {
            Assert.Equal(5, System.Enum.GetValues(typeof(RelationshipStatus)).Length);
        }

        [Fact]
        public void RelationshipStatus_War_Exists()
        {
            Assert.True(System.Enum.IsDefined(typeof(RelationshipStatus), "War"));
        }

        [Fact]
        public void RelationshipStatus_Hostile_Exists()
        {
            Assert.True(System.Enum.IsDefined(typeof(RelationshipStatus), "Hostile"));
        }

        [Fact]
        public void RelationshipStatus_Neutral_Exists()
        {
            Assert.True(System.Enum.IsDefined(typeof(RelationshipStatus), "Neutral"));
        }

        [Fact]
        public void RelationshipStatus_Friendly_Exists()
        {
            Assert.True(System.Enum.IsDefined(typeof(RelationshipStatus), "Friendly"));
        }

        [Fact]
        public void RelationshipStatus_Allied_Exists()
        {
            Assert.True(System.Enum.IsDefined(typeof(RelationshipStatus), "Allied"));
        }

        [Fact]
        public void RelationshipStatus_OrderedCorrectly()
        {
            Assert.True((int)RelationshipStatus.War < (int)RelationshipStatus.Hostile);
            Assert.True((int)RelationshipStatus.Hostile < (int)RelationshipStatus.Neutral);
            Assert.True((int)RelationshipStatus.Neutral < (int)RelationshipStatus.Friendly);
            Assert.True((int)RelationshipStatus.Friendly < (int)RelationshipStatus.Allied);
        }
    }
}
