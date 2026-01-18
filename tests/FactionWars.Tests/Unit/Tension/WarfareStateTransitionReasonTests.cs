using FactionWars.Tension.Models;
using Xunit;

namespace FactionWars.Tests.Unit.Tension
{
    /// <summary>
    /// Tests for the WarfareStateTransitionReason enum which describes why a warfare state changed.
    /// </summary>
    public class WarfareStateTransitionReasonTests
    {
        [Fact]
        public void WarfareStateTransitionReason_HasTensionThresholdReached()
        {
            // Arrange & Act
            var reason = WarfareStateTransitionReason.TensionThresholdReached;

            // Assert
            Assert.Equal(0, (int)reason);
        }

        [Fact]
        public void WarfareStateTransitionReason_HasTensionDecay()
        {
            // Arrange & Act
            var reason = WarfareStateTransitionReason.TensionDecay;

            // Assert
            Assert.Equal(1, (int)reason);
        }

        [Fact]
        public void WarfareStateTransitionReason_HasDiplomaticAction()
        {
            // Arrange & Act
            var reason = WarfareStateTransitionReason.DiplomaticAction;

            // Assert
            Assert.Equal(2, (int)reason);
        }

        [Fact]
        public void WarfareStateTransitionReason_HasMajorIncident()
        {
            // Arrange & Act
            var reason = WarfareStateTransitionReason.MajorIncident;

            // Assert
            Assert.Equal(3, (int)reason);
        }

        [Fact]
        public void WarfareStateTransitionReason_HasDeclarationOfWar()
        {
            // Arrange & Act
            var reason = WarfareStateTransitionReason.DeclarationOfWar;

            // Assert
            Assert.Equal(4, (int)reason);
        }

        [Fact]
        public void WarfareStateTransitionReason_HasPeaceTreaty()
        {
            // Arrange & Act
            var reason = WarfareStateTransitionReason.PeaceTreaty;

            // Assert
            Assert.Equal(5, (int)reason);
        }

        [Fact]
        public void WarfareStateTransitionReason_HasCeasefire()
        {
            // Arrange & Act
            var reason = WarfareStateTransitionReason.Ceasefire;

            // Assert
            Assert.Equal(6, (int)reason);
        }

        [Fact]
        public void WarfareStateTransitionReason_HasForcedByEvent()
        {
            // Arrange & Act
            var reason = WarfareStateTransitionReason.ForcedByEvent;

            // Assert
            Assert.Equal(7, (int)reason);
        }

        [Fact]
        public void WarfareStateTransitionReason_HasExactlyEightReasons()
        {
            // Arrange & Act
            var values = System.Enum.GetValues(typeof(WarfareStateTransitionReason));

            // Assert
            Assert.Equal(8, values.Length);
        }

        [Theory]
        [InlineData(WarfareStateTransitionReason.TensionThresholdReached, "TensionThresholdReached")]
        [InlineData(WarfareStateTransitionReason.TensionDecay, "TensionDecay")]
        [InlineData(WarfareStateTransitionReason.DiplomaticAction, "DiplomaticAction")]
        [InlineData(WarfareStateTransitionReason.MajorIncident, "MajorIncident")]
        [InlineData(WarfareStateTransitionReason.DeclarationOfWar, "DeclarationOfWar")]
        [InlineData(WarfareStateTransitionReason.PeaceTreaty, "PeaceTreaty")]
        [InlineData(WarfareStateTransitionReason.Ceasefire, "Ceasefire")]
        [InlineData(WarfareStateTransitionReason.ForcedByEvent, "ForcedByEvent")]
        public void WarfareStateTransitionReason_HasCorrectNames(WarfareStateTransitionReason reason, string expectedName)
        {
            // Assert
            Assert.Equal(expectedName, reason.ToString());
        }

        [Theory]
        [InlineData(WarfareStateTransitionReason.TensionThresholdReached, true)]
        [InlineData(WarfareStateTransitionReason.MajorIncident, true)]
        [InlineData(WarfareStateTransitionReason.DeclarationOfWar, true)]
        [InlineData(WarfareStateTransitionReason.TensionDecay, false)]
        [InlineData(WarfareStateTransitionReason.DiplomaticAction, false)]
        [InlineData(WarfareStateTransitionReason.PeaceTreaty, false)]
        [InlineData(WarfareStateTransitionReason.Ceasefire, false)]
        [InlineData(WarfareStateTransitionReason.ForcedByEvent, false)]
        public void WarfareStateTransitionReason_IdentifyEscalationReasons(WarfareStateTransitionReason reason, bool isTypicallyEscalation)
        {
            // These reasons typically indicate escalation
            bool result = reason == WarfareStateTransitionReason.TensionThresholdReached ||
                          reason == WarfareStateTransitionReason.MajorIncident ||
                          reason == WarfareStateTransitionReason.DeclarationOfWar;

            Assert.Equal(isTypicallyEscalation, result);
        }

        [Theory]
        [InlineData(WarfareStateTransitionReason.TensionDecay, true)]
        [InlineData(WarfareStateTransitionReason.PeaceTreaty, true)]
        [InlineData(WarfareStateTransitionReason.Ceasefire, true)]
        [InlineData(WarfareStateTransitionReason.TensionThresholdReached, false)]
        [InlineData(WarfareStateTransitionReason.MajorIncident, false)]
        [InlineData(WarfareStateTransitionReason.DeclarationOfWar, false)]
        [InlineData(WarfareStateTransitionReason.DiplomaticAction, false)]
        [InlineData(WarfareStateTransitionReason.ForcedByEvent, false)]
        public void WarfareStateTransitionReason_IdentifyDeescalationReasons(WarfareStateTransitionReason reason, bool isTypicallyDeescalation)
        {
            // These reasons typically indicate deescalation
            bool result = reason == WarfareStateTransitionReason.TensionDecay ||
                          reason == WarfareStateTransitionReason.PeaceTreaty ||
                          reason == WarfareStateTransitionReason.Ceasefire;

            Assert.Equal(isTypicallyDeescalation, result);
        }
    }
}
