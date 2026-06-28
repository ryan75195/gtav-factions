using FactionWars.ScriptHookV.Logging;
using Xunit;

namespace FactionWars.Tests.Unit.ScriptHookV.Logging
{
    public class LogFlushPolicyTests
    {
        [Theory]
        [InlineData("ERROR")]
        [InlineData("WARN")]
        public void RequiresImmediateFlush_TrueForCriticalLevels(string level)
        {
            Assert.True(LogFlushPolicy.RequiresImmediateFlush(level));
        }

        [Theory]
        [InlineData("INFO")]
        [InlineData("DEBUG")]
        [InlineData("AI")]
        [InlineData("COMBAT")]
        [InlineData("SPAWN")]
        [InlineData("ZONE")]
        public void RequiresImmediateFlush_FalseForChattyLevels(string level)
        {
            Assert.False(LogFlushPolicy.RequiresImmediateFlush(level));
        }

        [Fact]
        public void RequiresImmediateFlush_IsCaseInsensitive()
        {
            Assert.True(LogFlushPolicy.RequiresImmediateFlush("error"));
            Assert.True(LogFlushPolicy.RequiresImmediateFlush("warn"));
        }
    }
}
