using System;
using FactionWars.Tension.Models;
using Xunit;

namespace FactionWars.Tests.Unit.Tension
{
    /// <summary>
    /// Tests for the DiplomaticAction class which represents a diplomatic action
    /// between two factions.
    /// </summary>
    public class DiplomaticActionTests
    {
        private const string FactionMichael = "faction-michael";
        private const string FactionTrevor = "faction-trevor";
        private const string FactionFranklin = "faction-franklin";

        #region Constructor Tests

        [Fact]
        public void Constructor_WithValidParameters_CreatesAction()
        {
            var action = new DiplomaticAction(FactionMichael, FactionTrevor, DiplomaticActionType.Ceasefire);

            Assert.Equal(FactionMichael, action.InitiatorFactionId);
            Assert.Equal(FactionTrevor, action.TargetFactionId);
            Assert.Equal(DiplomaticActionType.Ceasefire, action.ActionType);
        }

        [Fact]
        public void Constructor_DefaultStatus_IsProposed()
        {
            var action = new DiplomaticAction(FactionMichael, FactionTrevor, DiplomaticActionType.Ceasefire);

            Assert.Equal(DiplomaticActionStatus.Proposed, action.Status);
        }

        [Fact]
        public void Constructor_GeneratesUniqueId()
        {
            var action1 = new DiplomaticAction(FactionMichael, FactionTrevor, DiplomaticActionType.Ceasefire);
            var action2 = new DiplomaticAction(FactionMichael, FactionTrevor, DiplomaticActionType.Ceasefire);

            Assert.NotEqual(action1.Id, action2.Id);
            Assert.False(string.IsNullOrEmpty(action1.Id));
        }

        [Fact]
        public void Constructor_SetsCreationTime()
        {
            var before = DateTime.UtcNow;
            var action = new DiplomaticAction(FactionMichael, FactionTrevor, DiplomaticActionType.Ceasefire);
            var after = DateTime.UtcNow;

            Assert.InRange(action.CreatedTime, before, after);
        }

        [Fact]
        public void Constructor_WithNullInitiator_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new DiplomaticAction(null!, FactionTrevor, DiplomaticActionType.Ceasefire));
        }

        [Fact]
        public void Constructor_WithNullTarget_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new DiplomaticAction(FactionMichael, null!, DiplomaticActionType.Ceasefire));
        }

        [Fact]
        public void Constructor_WithEmptyInitiator_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() =>
                new DiplomaticAction("", FactionTrevor, DiplomaticActionType.Ceasefire));
        }

        [Fact]
        public void Constructor_WithEmptyTarget_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() =>
                new DiplomaticAction(FactionMichael, "", DiplomaticActionType.Ceasefire));
        }

        [Fact]
        public void Constructor_WithWhitespaceInitiator_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() =>
                new DiplomaticAction("   ", FactionTrevor, DiplomaticActionType.Ceasefire));
        }

        [Fact]
        public void Constructor_WithWhitespaceTarget_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() =>
                new DiplomaticAction(FactionMichael, "   ", DiplomaticActionType.Ceasefire));
        }

        [Fact]
        public void Constructor_WithSameFactionIds_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() =>
                new DiplomaticAction(FactionMichael, FactionMichael, DiplomaticActionType.Ceasefire));
        }

        #endregion

        #region Status Transition Tests

        [Fact]
        public void Propose_FromProposed_TransitionsToPending()
        {
            var action = new DiplomaticAction(FactionMichael, FactionTrevor, DiplomaticActionType.Ceasefire);

            action.Propose();

            Assert.Equal(DiplomaticActionStatus.Pending, action.Status);
        }

        [Fact]
        public void Propose_SetsProposedTime()
        {
            var action = new DiplomaticAction(FactionMichael, FactionTrevor, DiplomaticActionType.Ceasefire);

            var before = DateTime.UtcNow;
            action.Propose();
            var after = DateTime.UtcNow;

            Assert.NotNull(action.ProposedTime);
            Assert.InRange(action.ProposedTime.Value, before, after);
        }

        [Fact]
        public void Propose_WhenNotProposed_ThrowsInvalidOperationException()
        {
            var action = new DiplomaticAction(FactionMichael, FactionTrevor, DiplomaticActionType.Ceasefire);
            action.Propose();

            Assert.Throws<InvalidOperationException>(() => action.Propose());
        }

        [Fact]
        public void Accept_FromPending_TransitionsToAccepted()
        {
            var action = new DiplomaticAction(FactionMichael, FactionTrevor, DiplomaticActionType.Ceasefire);
            action.Propose();

            action.Accept();

            Assert.Equal(DiplomaticActionStatus.Accepted, action.Status);
        }

        [Fact]
        public void Accept_SetsAcceptedTime()
        {
            var action = new DiplomaticAction(FactionMichael, FactionTrevor, DiplomaticActionType.Ceasefire);
            action.Propose();

            var before = DateTime.UtcNow;
            action.Accept();
            var after = DateTime.UtcNow;

            Assert.NotNull(action.AcceptedTime);
            Assert.InRange(action.AcceptedTime.Value, before, after);
        }

        [Fact]
        public void Accept_WhenNotPending_ThrowsInvalidOperationException()
        {
            var action = new DiplomaticAction(FactionMichael, FactionTrevor, DiplomaticActionType.Ceasefire);

            Assert.Throws<InvalidOperationException>(() => action.Accept());
        }

        [Fact]
        public void Reject_FromPending_TransitionsToRejected()
        {
            var action = new DiplomaticAction(FactionMichael, FactionTrevor, DiplomaticActionType.Ceasefire);
            action.Propose();

            action.Reject("Terms unacceptable");

            Assert.Equal(DiplomaticActionStatus.Rejected, action.Status);
        }

        [Fact]
        public void Reject_SetsRejectionReason()
        {
            var action = new DiplomaticAction(FactionMichael, FactionTrevor, DiplomaticActionType.Ceasefire);
            action.Propose();

            action.Reject("Terms unacceptable");

            Assert.Equal("Terms unacceptable", action.RejectionReason);
        }

        [Fact]
        public void Reject_WhenNotPending_ThrowsInvalidOperationException()
        {
            var action = new DiplomaticAction(FactionMichael, FactionTrevor, DiplomaticActionType.Ceasefire);

            Assert.Throws<InvalidOperationException>(() => action.Reject("Reason"));
        }

        [Fact]
        public void Activate_FromAccepted_TransitionsToActive()
        {
            var action = new DiplomaticAction(FactionMichael, FactionTrevor, DiplomaticActionType.Ceasefire);
            action.Propose();
            action.Accept();

            action.Activate();

            Assert.Equal(DiplomaticActionStatus.Active, action.Status);
        }

        [Fact]
        public void Activate_SetsActivatedTime()
        {
            var action = new DiplomaticAction(FactionMichael, FactionTrevor, DiplomaticActionType.Ceasefire);
            action.Propose();
            action.Accept();

            var before = DateTime.UtcNow;
            action.Activate();
            var after = DateTime.UtcNow;

            Assert.NotNull(action.ActivatedTime);
            Assert.InRange(action.ActivatedTime.Value, before, after);
        }

        [Fact]
        public void Activate_WhenNotAccepted_ThrowsInvalidOperationException()
        {
            var action = new DiplomaticAction(FactionMichael, FactionTrevor, DiplomaticActionType.Ceasefire);
            action.Propose();

            Assert.Throws<InvalidOperationException>(() => action.Activate());
        }

        [Fact]
        public void Expire_FromActive_TransitionsToExpired()
        {
            var action = new DiplomaticAction(FactionMichael, FactionTrevor, DiplomaticActionType.Ceasefire);
            action.Propose();
            action.Accept();
            action.Activate();

            action.Expire();

            Assert.Equal(DiplomaticActionStatus.Expired, action.Status);
        }

        [Fact]
        public void Expire_WhenNotActive_ThrowsInvalidOperationException()
        {
            var action = new DiplomaticAction(FactionMichael, FactionTrevor, DiplomaticActionType.Ceasefire);
            action.Propose();
            action.Accept();

            Assert.Throws<InvalidOperationException>(() => action.Expire());
        }

        [Fact]
        public void Break_FromActive_TransitionsToBroken()
        {
            var action = new DiplomaticAction(FactionMichael, FactionTrevor, DiplomaticActionType.Ceasefire);
            action.Propose();
            action.Accept();
            action.Activate();

            action.Break(FactionMichael, "Attacked enemy territory");

            Assert.Equal(DiplomaticActionStatus.Broken, action.Status);
        }

        [Fact]
        public void Break_SetsViolatorFactionId()
        {
            var action = new DiplomaticAction(FactionMichael, FactionTrevor, DiplomaticActionType.Ceasefire);
            action.Propose();
            action.Accept();
            action.Activate();

            action.Break(FactionMichael, "Attacked enemy territory");

            Assert.Equal(FactionMichael, action.ViolatorFactionId);
        }

        [Fact]
        public void Break_SetsViolationReason()
        {
            var action = new DiplomaticAction(FactionMichael, FactionTrevor, DiplomaticActionType.Ceasefire);
            action.Propose();
            action.Accept();
            action.Activate();

            action.Break(FactionMichael, "Attacked enemy territory");

            Assert.Equal("Attacked enemy territory", action.ViolationReason);
        }

        [Fact]
        public void Break_WhenNotActive_ThrowsInvalidOperationException()
        {
            var action = new DiplomaticAction(FactionMichael, FactionTrevor, DiplomaticActionType.Ceasefire);
            action.Propose();
            action.Accept();

            Assert.Throws<InvalidOperationException>(() => action.Break(FactionMichael, "Reason"));
        }

        [Fact]
        public void Break_WithUnrelatedFaction_ThrowsArgumentException()
        {
            var action = new DiplomaticAction(FactionMichael, FactionTrevor, DiplomaticActionType.Ceasefire);
            action.Propose();
            action.Accept();
            action.Activate();

            Assert.Throws<ArgumentException>(() => action.Break(FactionFranklin, "Reason"));
        }

        [Fact]
        public void Cancel_FromProposed_TransitionsToCancelled()
        {
            var action = new DiplomaticAction(FactionMichael, FactionTrevor, DiplomaticActionType.Ceasefire);

            action.Cancel();

            Assert.Equal(DiplomaticActionStatus.Cancelled, action.Status);
        }

        [Fact]
        public void Cancel_FromPending_TransitionsToCancelled()
        {
            var action = new DiplomaticAction(FactionMichael, FactionTrevor, DiplomaticActionType.Ceasefire);
            action.Propose();

            action.Cancel();

            Assert.Equal(DiplomaticActionStatus.Cancelled, action.Status);
        }

        [Fact]
        public void Cancel_WhenActive_ThrowsInvalidOperationException()
        {
            var action = new DiplomaticAction(FactionMichael, FactionTrevor, DiplomaticActionType.Ceasefire);
            action.Propose();
            action.Accept();
            action.Activate();

            Assert.Throws<InvalidOperationException>(() => action.Cancel());
        }

        #endregion

        #region State Query Tests

        [Theory]
        [InlineData(DiplomaticActionStatus.Proposed, false)]
        [InlineData(DiplomaticActionStatus.Pending, false)]
        [InlineData(DiplomaticActionStatus.Accepted, false)]
        [InlineData(DiplomaticActionStatus.Rejected, true)]
        [InlineData(DiplomaticActionStatus.Expired, true)]
        [InlineData(DiplomaticActionStatus.Broken, true)]
        [InlineData(DiplomaticActionStatus.Cancelled, true)]
        public void IsTerminal_ReturnsExpectedValue(DiplomaticActionStatus status, bool expected)
        {
            var action = CreateActionInStatus(status);
            Assert.Equal(expected, action.IsTerminal);
        }

        [Theory]
        [InlineData(DiplomaticActionStatus.Proposed, false)]
        [InlineData(DiplomaticActionStatus.Pending, true)]
        [InlineData(DiplomaticActionStatus.Accepted, false)]
        [InlineData(DiplomaticActionStatus.Rejected, false)]
        public void IsPendingResponse_ReturnsExpectedValue(DiplomaticActionStatus status, bool expected)
        {
            var action = CreateActionInStatus(status);
            Assert.Equal(expected, action.IsPendingResponse);
        }

        [Fact]
        public void IsActive_WhenActive_ReturnsTrue()
        {
            var action = new DiplomaticAction(FactionMichael, FactionTrevor, DiplomaticActionType.Ceasefire);
            action.Propose();
            action.Accept();
            action.Activate();

            Assert.True(action.IsActive);
        }

        [Fact]
        public void IsActive_WhenNotActive_ReturnsFalse()
        {
            var action = new DiplomaticAction(FactionMichael, FactionTrevor, DiplomaticActionType.Ceasefire);
            action.Propose();
            action.Accept();

            Assert.False(action.IsActive);
        }

        [Fact]
        public void WasBroken_WhenBroken_ReturnsTrue()
        {
            var action = new DiplomaticAction(FactionMichael, FactionTrevor, DiplomaticActionType.Ceasefire);
            action.Propose();
            action.Accept();
            action.Activate();
            action.Break(FactionMichael, "Reason");

            Assert.True(action.WasBroken);
        }

        [Fact]
        public void WasBroken_WhenNotBroken_ReturnsFalse()
        {
            var action = new DiplomaticAction(FactionMichael, FactionTrevor, DiplomaticActionType.Ceasefire);
            action.Propose();
            action.Accept();
            action.Activate();

            Assert.False(action.WasBroken);
        }

        #endregion

        #region Faction Query Tests

        [Fact]
        public void ContainsFaction_WithInitiator_ReturnsTrue()
        {
            var action = new DiplomaticAction(FactionMichael, FactionTrevor, DiplomaticActionType.Ceasefire);

            Assert.True(action.ContainsFaction(FactionMichael));
        }

        [Fact]
        public void ContainsFaction_WithTarget_ReturnsTrue()
        {
            var action = new DiplomaticAction(FactionMichael, FactionTrevor, DiplomaticActionType.Ceasefire);

            Assert.True(action.ContainsFaction(FactionTrevor));
        }

        [Fact]
        public void ContainsFaction_WithOtherFaction_ReturnsFalse()
        {
            var action = new DiplomaticAction(FactionMichael, FactionTrevor, DiplomaticActionType.Ceasefire);

            Assert.False(action.ContainsFaction(FactionFranklin));
        }

        [Fact]
        public void ContainsFaction_WithNull_ReturnsFalse()
        {
            var action = new DiplomaticAction(FactionMichael, FactionTrevor, DiplomaticActionType.Ceasefire);

            Assert.False(action.ContainsFaction(null!));
        }

        [Fact]
        public void InvolvesBothFactions_WithBothInOrder_ReturnsTrue()
        {
            var action = new DiplomaticAction(FactionMichael, FactionTrevor, DiplomaticActionType.Ceasefire);

            Assert.True(action.InvolvesBothFactions(FactionMichael, FactionTrevor));
        }

        [Fact]
        public void InvolvesBothFactions_WithBothReversed_ReturnsTrue()
        {
            var action = new DiplomaticAction(FactionMichael, FactionTrevor, DiplomaticActionType.Ceasefire);

            Assert.True(action.InvolvesBothFactions(FactionTrevor, FactionMichael));
        }

        [Fact]
        public void InvolvesBothFactions_WithOnlyOne_ReturnsFalse()
        {
            var action = new DiplomaticAction(FactionMichael, FactionTrevor, DiplomaticActionType.Ceasefire);

            Assert.False(action.InvolvesBothFactions(FactionMichael, FactionFranklin));
        }

        [Fact]
        public void GetOtherFaction_WithInitiator_ReturnsTarget()
        {
            var action = new DiplomaticAction(FactionMichael, FactionTrevor, DiplomaticActionType.Ceasefire);

            Assert.Equal(FactionTrevor, action.GetOtherFaction(FactionMichael));
        }

        [Fact]
        public void GetOtherFaction_WithTarget_ReturnsInitiator()
        {
            var action = new DiplomaticAction(FactionMichael, FactionTrevor, DiplomaticActionType.Ceasefire);

            Assert.Equal(FactionMichael, action.GetOtherFaction(FactionTrevor));
        }

        [Fact]
        public void GetOtherFaction_WithOtherFaction_ReturnsNull()
        {
            var action = new DiplomaticAction(FactionMichael, FactionTrevor, DiplomaticActionType.Ceasefire);

            Assert.Null(action.GetOtherFaction(FactionFranklin));
        }

        #endregion

        #region Duration Tests

        [Theory]
        [InlineData(DiplomaticActionType.Ceasefire, 300)]
        [InlineData(DiplomaticActionType.NonAggressionPact, 600)]
        [InlineData(DiplomaticActionType.TradeAgreement, 900)]
        [InlineData(DiplomaticActionType.MutualDefense, 1200)]
        [InlineData(DiplomaticActionType.Alliance, 0)] // Permanent
        [InlineData(DiplomaticActionType.DeclarationOfWar, 0)] // Until peace
        [InlineData(DiplomaticActionType.PeaceTreaty, 1800)]
        [InlineData(DiplomaticActionType.TerritorialConcession, 0)] // Instant
        public void DefaultDurationSeconds_ReturnsExpectedValue(DiplomaticActionType actionType, int expectedDuration)
        {
            var action = new DiplomaticAction(FactionMichael, FactionTrevor, actionType);

            Assert.Equal(expectedDuration, action.DefaultDurationSeconds);
        }

        [Fact]
        public void RemainingDurationSeconds_WhenNotActive_ReturnsNull()
        {
            var action = new DiplomaticAction(FactionMichael, FactionTrevor, DiplomaticActionType.Ceasefire);

            Assert.Null(action.RemainingDurationSeconds);
        }

        [Fact]
        public void RemainingDurationSeconds_WhenActiveWithDuration_ReturnsPositiveValue()
        {
            var action = new DiplomaticAction(FactionMichael, FactionTrevor, DiplomaticActionType.Ceasefire);
            action.Propose();
            action.Accept();
            action.Activate();

            Assert.NotNull(action.RemainingDurationSeconds);
            Assert.True(action.RemainingDurationSeconds > 0);
        }

        [Fact]
        public void RemainingDurationSeconds_ForPermanentAction_ReturnsNull()
        {
            var action = new DiplomaticAction(FactionMichael, FactionTrevor, DiplomaticActionType.Alliance);
            action.Propose();
            action.Accept();
            action.Activate();

            Assert.Null(action.RemainingDurationSeconds);
        }

        #endregion

        #region Tension Impact Tests

        [Theory]
        [InlineData(DiplomaticActionType.Ceasefire, -20)]
        [InlineData(DiplomaticActionType.NonAggressionPact, -15)]
        [InlineData(DiplomaticActionType.TradeAgreement, -10)]
        [InlineData(DiplomaticActionType.MutualDefense, -25)]
        [InlineData(DiplomaticActionType.Alliance, -40)]
        [InlineData(DiplomaticActionType.DeclarationOfWar, 50)]
        [InlineData(DiplomaticActionType.PeaceTreaty, -35)]
        [InlineData(DiplomaticActionType.TerritorialConcession, -15)]
        public void TensionImpact_ReturnsExpectedValue(DiplomaticActionType actionType, int expectedImpact)
        {
            var action = new DiplomaticAction(FactionMichael, FactionTrevor, actionType);

            Assert.Equal(expectedImpact, action.TensionImpact);
        }

        [Fact]
        public void ViolationTensionPenalty_ReturnsPositiveValue()
        {
            var action = new DiplomaticAction(FactionMichael, FactionTrevor, DiplomaticActionType.Ceasefire);

            Assert.True(action.ViolationTensionPenalty > 0);
        }

        [Theory]
        [InlineData(DiplomaticActionType.Ceasefire, 30)]
        [InlineData(DiplomaticActionType.NonAggressionPact, 25)]
        [InlineData(DiplomaticActionType.MutualDefense, 40)]
        [InlineData(DiplomaticActionType.Alliance, 50)]
        public void ViolationTensionPenalty_ScalesWithActionImportance(DiplomaticActionType actionType, int expectedPenalty)
        {
            var action = new DiplomaticAction(FactionMichael, FactionTrevor, actionType);

            Assert.Equal(expectedPenalty, action.ViolationTensionPenalty);
        }

        #endregion

        #region Warfare State Impact Tests

        [Theory]
        [InlineData(DiplomaticActionType.Ceasefire, WarfareState.ColdWar)]
        [InlineData(DiplomaticActionType.PeaceTreaty, WarfareState.Peace)]
        [InlineData(DiplomaticActionType.DeclarationOfWar, WarfareState.OpenWarfare)]
        public void TargetWarfareState_ReturnsExpectedState(DiplomaticActionType actionType, WarfareState expectedState)
        {
            var action = new DiplomaticAction(FactionMichael, FactionTrevor, actionType);

            Assert.Equal(expectedState, action.TargetWarfareState);
        }

        [Theory]
        [InlineData(DiplomaticActionType.TradeAgreement)]
        [InlineData(DiplomaticActionType.NonAggressionPact)]
        public void TargetWarfareState_ForNonWarfareActions_ReturnsNull(DiplomaticActionType actionType)
        {
            var action = new DiplomaticAction(FactionMichael, FactionTrevor, actionType);

            Assert.Null(action.TargetWarfareState);
        }

        #endregion

        #region Requirements Tests

        [Fact]
        public void RequiresMutualAgreement_ForCeasefire_ReturnsTrue()
        {
            var action = new DiplomaticAction(FactionMichael, FactionTrevor, DiplomaticActionType.Ceasefire);

            Assert.True(action.RequiresMutualAgreement);
        }

        [Fact]
        public void RequiresMutualAgreement_ForDeclarationOfWar_ReturnsFalse()
        {
            var action = new DiplomaticAction(FactionMichael, FactionTrevor, DiplomaticActionType.DeclarationOfWar);

            Assert.False(action.RequiresMutualAgreement);
        }

        [Theory]
        [InlineData(DiplomaticActionType.Ceasefire, true)]
        [InlineData(DiplomaticActionType.NonAggressionPact, true)]
        [InlineData(DiplomaticActionType.TradeAgreement, true)]
        [InlineData(DiplomaticActionType.MutualDefense, true)]
        [InlineData(DiplomaticActionType.Alliance, true)]
        [InlineData(DiplomaticActionType.DeclarationOfWar, false)]
        [InlineData(DiplomaticActionType.PeaceTreaty, true)]
        [InlineData(DiplomaticActionType.TerritorialConcession, true)]
        public void RequiresMutualAgreement_ReturnsExpectedValue(DiplomaticActionType actionType, bool expected)
        {
            var action = new DiplomaticAction(FactionMichael, FactionTrevor, actionType);

            Assert.Equal(expected, action.RequiresMutualAgreement);
        }

        #endregion

        #region ToString Tests

        [Fact]
        public void ToString_ContainsActionType()
        {
            var action = new DiplomaticAction(FactionMichael, FactionTrevor, DiplomaticActionType.Ceasefire);

            var result = action.ToString();

            Assert.Contains("Ceasefire", result);
        }

        [Fact]
        public void ToString_ContainsFactionIds()
        {
            var action = new DiplomaticAction(FactionMichael, FactionTrevor, DiplomaticActionType.Ceasefire);

            var result = action.ToString();

            Assert.Contains(FactionMichael, result);
            Assert.Contains(FactionTrevor, result);
        }

        [Fact]
        public void ToString_ContainsStatus()
        {
            var action = new DiplomaticAction(FactionMichael, FactionTrevor, DiplomaticActionType.Ceasefire);
            action.Propose();

            var result = action.ToString();

            Assert.Contains("Pending", result);
        }

        #endregion

        #region Helper Methods

        private DiplomaticAction CreateActionInStatus(DiplomaticActionStatus status)
        {
            var action = new DiplomaticAction(FactionMichael, FactionTrevor, DiplomaticActionType.Ceasefire);

            switch (status)
            {
                case DiplomaticActionStatus.Proposed:
                    // Default state
                    break;
                case DiplomaticActionStatus.Pending:
                    action.Propose();
                    break;
                case DiplomaticActionStatus.Accepted:
                    action.Propose();
                    action.Accept();
                    break;
                case DiplomaticActionStatus.Rejected:
                    action.Propose();
                    action.Reject("Reason");
                    break;
                case DiplomaticActionStatus.Active:
                    action.Propose();
                    action.Accept();
                    action.Activate();
                    break;
                case DiplomaticActionStatus.Expired:
                    action.Propose();
                    action.Accept();
                    action.Activate();
                    action.Expire();
                    break;
                case DiplomaticActionStatus.Broken:
                    action.Propose();
                    action.Accept();
                    action.Activate();
                    action.Break(FactionMichael, "Reason");
                    break;
                case DiplomaticActionStatus.Cancelled:
                    action.Cancel();
                    break;
            }

            return action;
        }

        #endregion
    }
}
