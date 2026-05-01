using FactionWars.Core.Utils;
using FactionWars.Persistence;
using FactionWars.Persistence.Models;
using FactionWars.ScriptHookV.Persistence;
using Moq;
using Xunit;

namespace FactionWars.Tests.Unit.ScriptHookV.Persistence
{
    public class LoadDetectorTests
    {
        private readonly MockGameBridge _bridge = new MockGameBridge();
        private readonly Mock<ISidecarStore> _store = new Mock<ISidecarStore>();
        private readonly LoadDetector _sut;

        private Sidecar? _hydrated;
        private bool _hydrateCalled;
        private bool _newGameCalled;

        public LoadDetectorTests()
        {
            _sut = new LoadDetector(
                _bridge,
                _store.Object,
                onHydrate: s => { _hydrateCalled = true; _hydrated = s; },
                onNewGame: () => _newGameCalled = true);
        }

        private void SetupClosestMatch(long expectedPlayTime, Sidecar sidecar)
        {
            _store
                .Setup(s => s.TryFindClosestByPlayTime(It.IsAny<long>(), It.IsAny<long>(), out sidecar))
                .Returns(true);
        }

        private void SetupClosestMiss()
        {
            Sidecar? notFound = null;
            _store
                .Setup(s => s.TryFindClosestByPlayTime(It.IsAny<long>(), It.IsAny<long>(), out notFound!))
                .Returns(false);
        }

        [Fact]
        public void FirstTick_MatchingSidecar_Hydrates()
        {
            _bridge.TotalPlayTimeSeconds = 12340;
            var matched = new Sidecar { Fingerprint = new SaveFingerprint { TotalPlayTimeSeconds = 12340 } };
            SetupClosestMatch(12340, matched);

            _sut.Tick();

            Assert.True(_hydrateCalled);
            Assert.Equal(12340L, _hydrated!.Fingerprint.TotalPlayTimeSeconds);
            Assert.False(_newGameCalled);
        }

        [Fact]
        public void FirstTick_NoMatch_TriggersNewGame()
        {
            _bridge.TotalPlayTimeSeconds = 999;
            SetupClosestMiss();

            _sut.Tick();

            Assert.False(_hydrateCalled);
            Assert.True(_newGameCalled);
        }

        [Fact]
        public void SubsequentTicks_PlayTimeAdvancesNormally_DoesNothing()
        {
            _bridge.TotalPlayTimeSeconds = 1000;
            SetupClosestMiss();
            _sut.Tick();
            _hydrateCalled = false;
            _newGameCalled = false;

            _bridge.TotalPlayTimeSeconds = 1010;
            _sut.Tick();
            _bridge.TotalPlayTimeSeconds = 1020;
            _sut.Tick();

            Assert.False(_hydrateCalled);
            Assert.False(_newGameCalled);
        }

        [Fact]
        public void PlayTimeGoesBackwards_MatchingSidecar_Hydrates()
        {
            _bridge.TotalPlayTimeSeconds = 50000;
            SetupClosestMiss();
            _sut.Tick();
            _hydrateCalled = false;
            _newGameCalled = false;

            _bridge.TotalPlayTimeSeconds = 46473;
            var matched = new Sidecar { Fingerprint = new SaveFingerprint { TotalPlayTimeSeconds = 46444 } };
            SetupClosestMatch(46473, matched);

            _sut.Tick();

            Assert.True(_hydrateCalled);
            Assert.Equal(46444L, _hydrated!.Fingerprint.TotalPlayTimeSeconds);
            Assert.False(_newGameCalled);
        }

        [Fact]
        public void PlayTimeGoesBackwards_NoMatch_TriggersNewGame()
        {
            _bridge.TotalPlayTimeSeconds = 50000;
            SetupClosestMiss();
            _sut.Tick();
            _hydrateCalled = false;
            _newGameCalled = false;

            _bridge.TotalPlayTimeSeconds = 100;
            _sut.Tick();

            Assert.False(_hydrateCalled);
            Assert.True(_newGameCalled);
        }

        [Fact]
        public void PlayTimeUnchanged_DoesNothing()
        {
            _bridge.TotalPlayTimeSeconds = 5000;
            SetupClosestMiss();
            _sut.Tick();
            _hydrateCalled = false;
            _newGameCalled = false;

            _sut.Tick();
            _sut.Tick();

            Assert.False(_hydrateCalled);
            Assert.False(_newGameCalled);
        }

        [Fact]
        public void UsesClosestByPlayTime_ForPostLoadDrift()
        {
            _bridge.TotalPlayTimeSeconds = 46473;
            var sidecar = new Sidecar { Fingerprint = new SaveFingerprint { TotalPlayTimeSeconds = 46444 } };
            SetupClosestMatch(46473, sidecar);

            _sut.Tick();

            _store.Verify(s => s.TryFindClosestByPlayTime(46473L, It.Is<long>(w => w >= 30), out It.Ref<Sidecar>.IsAny), Times.AtLeastOnce);
        }
    }
}
