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

        [Fact]
        public void NoLoadingTransition_DoesNothing()
        {
            _sut.Tick(isLoading: false);
            _sut.Tick(isLoading: false);

            Assert.False(_hydrateCalled);
            Assert.False(_newGameCalled);
        }

        [Fact]
        public void LoadingFalseToTrue_DoesNothing()
        {
            _sut.Tick(isLoading: false);
            _sut.Tick(isLoading: true);

            Assert.False(_hydrateCalled);
            Assert.False(_newGameCalled);
        }

        [Fact]
        public void LoadingTrueToFalse_FingerprintChanged_MatchingSidecar_Hydrates()
        {
            _bridge.TotalPlayTimeSeconds = 0;
            _sut.Tick(isLoading: false);
            _bridge.TotalPlayTimeSeconds = 12340;
            _sut.Tick(isLoading: true);
            _bridge.SetPlayerMoney(50000);
            _bridge.CompletedMissionCount = 23;
            _bridge.InGameClockMinutes = 854;

            var matchedSidecar = new Sidecar
            {
                Fingerprint = new SaveFingerprint
                {
                    TotalPlayTimeSeconds = 12340,
                    Money = 50000,
                    CompletedMissionCount = 23,
                    InGameClockMinutes = 854,
                },
            };
            _store.Setup(s => s.TryFindByFingerprint(It.IsAny<SaveFingerprint>(), out matchedSidecar)).Returns(true);

            _sut.Tick(isLoading: false);

            Assert.True(_hydrateCalled);
            Assert.Equal(12340L, _hydrated!.Fingerprint.TotalPlayTimeSeconds);
            Assert.False(_newGameCalled);
        }

        [Fact]
        public void LoadingTrueToFalse_FingerprintChanged_NoMatch_TriggersNewGame()
        {
            _sut.Tick(isLoading: false);
            _bridge.TotalPlayTimeSeconds = 999;
            _sut.Tick(isLoading: true);

            Sidecar? notFound = null;
            _store.Setup(s => s.TryFindByFingerprint(It.IsAny<SaveFingerprint>(), out notFound!)).Returns(false);

            _sut.Tick(isLoading: false);

            Assert.False(_hydrateCalled);
            Assert.True(_newGameCalled);
        }

        [Fact]
        public void LoadingTrueToFalse_FingerprintUnchanged_DoesNothing()
        {
            _bridge.TotalPlayTimeSeconds = 5000;
            _sut.Tick(isLoading: false);
            _sut.Tick(isLoading: true);
            _sut.Tick(isLoading: false);

            Assert.False(_hydrateCalled);
            Assert.False(_newGameCalled);
        }
    }
}
