using System.Collections.Generic;
using FactionWars.Core.Interfaces;
using FactionWars.Core.Utils;
using FactionWars.ScriptHookV.Combat;
using Xunit;

namespace FactionWars.Tests.Unit.ScriptHookV.Combat
{
    public class EnemyTargetCollectorTests
    {
        [Fact]
        public void Collect_ReturnsOnlyHandlesWithinRadius()
        {
            var bridge = new MockGameBridge();
            int near = bridge.CreatePed("e1", new Vector3(5f, 0f, 0f));
            int far = bridge.CreatePed("e2", new Vector3(500f, 0f, 0f));
            var collector = new EnemyTargetCollector(bridge);

            var result = collector.Collect(new List<int> { near, far }, new Vector3(0f, 0f, 0f), 50f);

            Assert.Single(result);
            Assert.Equal(near, result[0].Handle);
            Assert.Equal(new Vector3(5f, 0f, 0f), result[0].Position);
        }

        [Fact]
        public void Collect_EmptyInput_ReturnsEmpty()
        {
            var collector = new EnemyTargetCollector(new MockGameBridge());
            Assert.Empty(collector.Collect(new List<int>(), new Vector3(0f, 0f, 0f), 50f));
        }

        [Fact]
        public void Collect_NullInput_ReturnsEmpty()
        {
            var collector = new EnemyTargetCollector(new MockGameBridge());
            Assert.Empty(collector.Collect(null!, new Vector3(0f, 0f, 0f), 50f));
        }
    }
}
