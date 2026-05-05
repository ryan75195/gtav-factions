using System;
using FactionWars.Core.Interfaces;
using FactionWars.Persistence.Models;
using FactionWars.ScriptHookV.Persistence;
using Moq;
using Xunit;

namespace FactionWars.Tests.Unit.ScriptHookV.Persistence
{
    public class NativeSaveSidecarWriterTests
    {
        [Fact]
        public void Enqueue_DoesNotReadNativeBridge()
        {
            var bridge = new Mock<IGameBridge>(MockBehavior.Strict);
            var manager = new Mock<IGameStateManager>(MockBehavior.Strict);
            var writer = new NativeSaveSidecarWriter(bridge.Object, manager.Object);

            writer.Enqueue(new SaveEvent(@"C:\saves\SGTA50015", DateTime.UtcNow));

            bridge.VerifyNoOtherCalls();
            manager.VerifyNoOtherCalls();
        }

        [Fact]
        public void ProcessPending_ReadsBridgeAndWritesSidecarOnCallingThread()
        {
            var bridge = new Mock<IGameBridge>(MockBehavior.Strict);
            var manager = new Mock<IGameStateManager>(MockBehavior.Strict);
            bridge.Setup(x => x.GetTotalPlayTimeSeconds()).Returns(1234);
            bridge.Setup(x => x.GetPlayerMoney()).Returns(5000);
            bridge.Setup(x => x.GetCompletedMissionCount()).Returns(12);
            bridge.Setup(x => x.GetInGameClockMinutes()).Returns(503);
            bridge.Setup(x => x.GetPlayerPosition()).Returns(new Vector3(1, 2, 3));
            bridge.Setup(x => x.GetPlayerHeading()).Returns(45);

            SaveFingerprint? capturedFingerprint = null;
            PlayerPosition? capturedPosition = null;
            string? capturedFilename = null;
            manager
                .Setup(x => x.WriteCurrentSidecar(It.IsAny<SaveFingerprint>(), It.IsAny<PlayerPosition>(), It.IsAny<string>()))
                .Callback<SaveFingerprint, PlayerPosition, string>((fp, pos, filename) =>
                {
                    capturedFingerprint = fp;
                    capturedPosition = pos;
                    capturedFilename = filename;
                });

            var writer = new NativeSaveSidecarWriter(bridge.Object, manager.Object);
            writer.Enqueue(new SaveEvent(@"C:\saves\SGTA50015", DateTime.UtcNow));

            writer.ProcessPending();

            Assert.NotNull(capturedFingerprint);
            Assert.Equal(1234, capturedFingerprint!.TotalPlayTimeSeconds);
            Assert.Equal(5000, capturedFingerprint.Money);
            Assert.Equal(12, capturedFingerprint.CompletedMissionCount);
            Assert.Equal(503, capturedFingerprint.InGameClockMinutes);
            Assert.NotNull(capturedPosition);
            Assert.Equal(1, capturedPosition!.X);
            Assert.Equal(2, capturedPosition.Y);
            Assert.Equal(3, capturedPosition.Z);
            Assert.Equal(45, capturedPosition.Heading);
            Assert.Equal("SGTA50015", capturedFilename);
        }

        [Fact]
        public void ProcessPending_WhenPlayTimeReadFails_SkipsSidecarWrite()
        {
            var bridge = new Mock<IGameBridge>(MockBehavior.Strict);
            var manager = new Mock<IGameStateManager>(MockBehavior.Strict);
            bridge.Setup(x => x.GetTotalPlayTimeSeconds()).Returns((long?)null);

            var writer = new NativeSaveSidecarWriter(bridge.Object, manager.Object);
            writer.Enqueue(new SaveEvent(@"C:\saves\SGTA50015", DateTime.UtcNow));

            writer.ProcessPending();

            manager.Verify(x => x.WriteCurrentSidecar(
                It.IsAny<SaveFingerprint>(),
                It.IsAny<PlayerPosition>(),
                It.IsAny<string>()), Times.Never);
        }
    }
}
