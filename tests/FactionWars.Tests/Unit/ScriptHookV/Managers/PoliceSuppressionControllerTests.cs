using FactionWars.Combat.Interfaces;
using FactionWars.Core.Utils;
using FactionWars.ScriptHookV.Managers;
using Moq;
using Xunit;

namespace FactionWars.Tests.Unit.ScriptHookV.Managers
{
    public class PoliceSuppressionControllerTests
    {
        private readonly MockGameBridge _bridge = new MockGameBridge();
        private readonly Mock<IZoneBattleManager> _battleManager = new Mock<IZoneBattleManager>();

        [Fact]
        public void Update_WhenPlayerBattleStarts_EnablesSuppressionAndClearsWantedLevel()
        {
            _bridge.WantedLevel = 3;
            _battleManager.Setup(b => b.IsPlayerInBattle()).Returns(true);

            var sut = new PoliceSuppressionController(_bridge, _battleManager.Object);
            sut.Update();

            Assert.True(_bridge.PoliceSuppressionEnabled);
            Assert.Equal(0, _bridge.WantedLevel);
            Assert.Equal(1, _bridge.SetPoliceSuppressionCallCount);
            Assert.True(_bridge.ClearWantedLevelCallCount >= 1);
        }

        [Fact]
        public void Update_WhileSuppressionAlreadyEnabled_ClearsWantedLevelWithoutTogglingAgain()
        {
            _battleManager.Setup(b => b.IsPlayerInBattle()).Returns(true);

            var sut = new PoliceSuppressionController(_bridge, _battleManager.Object);
            sut.Update();
            _bridge.WantedLevel = 2;
            sut.Update();

            Assert.True(_bridge.PoliceSuppressionEnabled);
            Assert.Equal(0, _bridge.WantedLevel);
            Assert.Equal(1, _bridge.SetPoliceSuppressionCallCount);
        }

        [Fact]
        public void Update_WhenPlayerBattleEnds_DisablesSuppression()
        {
            var inBattle = true;
            _battleManager.Setup(b => b.IsPlayerInBattle()).Returns(() => inBattle);

            var sut = new PoliceSuppressionController(_bridge, _battleManager.Object);
            sut.Update();
            inBattle = false;
            sut.Update();

            Assert.False(_bridge.PoliceSuppressionEnabled);
            Assert.Equal(2, _bridge.SetPoliceSuppressionCallCount);
        }

        [Fact]
        public void Dispose_WhenSuppressionEnabled_DisablesSuppression()
        {
            _battleManager.Setup(b => b.IsPlayerInBattle()).Returns(true);

            var sut = new PoliceSuppressionController(_bridge, _battleManager.Object);
            sut.Update();
            sut.Dispose();

            Assert.False(_bridge.PoliceSuppressionEnabled);
            Assert.Equal(2, _bridge.SetPoliceSuppressionCallCount);
        }
    }
}
