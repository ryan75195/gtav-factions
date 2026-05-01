using FactionWars.Core.Utils;
using FactionWars.Persistence.Models;
using Xunit;

namespace FactionWars.Tests.Unit.Persistence
{
    public class SaveFingerprintTests
    {
        private static SaveFingerprint Make(long playTime = 12340, int money = 50000, int missions = 23, int clockMinutes = 854)
            => new SaveFingerprint
            {
                TotalPlayTimeSeconds = playTime,
                Money = money,
                CompletedMissionCount = missions,
                InGameClockMinutes = clockMinutes,
            };

        [Fact]
        public void ExactMatch_AllFieldsEqual_ReturnsTrue()
        {
            var a = Make();
            var b = Make();
            Assert.True(a.ExactMatch(b));
        }

        [Fact]
        public void ExactMatch_TotalPlayTimeDiffers_ReturnsFalse()
        {
            var a = Make(playTime: 12340);
            var b = Make(playTime: 12341);
            Assert.False(a.ExactMatch(b));
        }

        [Fact]
        public void ExactMatch_MoneyDiffers_ReturnsFalse()
        {
            var a = Make(money: 50000);
            var b = Make(money: 50001);
            Assert.False(a.ExactMatch(b));
        }

        [Fact]
        public void ExactMatch_MissionCountDiffers_ReturnsFalse()
        {
            var a = Make(missions: 23);
            var b = Make(missions: 24);
            Assert.False(a.ExactMatch(b));
        }

        [Fact]
        public void ExactMatch_ClockDiffers_ReturnsFalse()
        {
            var a = Make(clockMinutes: 854);
            var b = Make(clockMinutes: 855);
            Assert.False(a.ExactMatch(b));
        }

        [Fact]
        public void PrimaryMatch_OnlyTotalPlayTime_OtherFieldsDiffer_ReturnsTrue()
        {
            var a = Make(playTime: 12340, money: 50000, missions: 23, clockMinutes: 854);
            var b = Make(playTime: 12340, money: 99999, missions: 99, clockMinutes: 0);
            Assert.True(a.PrimaryMatch(b));
        }

        [Fact]
        public void PrimaryMatch_TotalPlayTimeDiffers_ReturnsFalse()
        {
            var a = Make(playTime: 12340);
            var b = Make(playTime: 12341);
            Assert.False(a.PrimaryMatch(b));
        }

        [Fact]
        public void Capture_FromBridge_BuildsFingerprintFromCurrentState()
        {
            var bridge = new MockGameBridge
            {
                TotalPlayTimeSeconds = 12340,
                CompletedMissionCount = 23,
                InGameClockMinutes = 854,
            };
            bridge.SetPlayerMoney(50000);

            var fp = SaveFingerprint.Capture(bridge);

            Assert.Equal(12340L, fp.TotalPlayTimeSeconds);
            Assert.Equal(50000, fp.Money);
            Assert.Equal(23, fp.CompletedMissionCount);
            Assert.Equal(854, fp.InGameClockMinutes);
        }
    }
}
