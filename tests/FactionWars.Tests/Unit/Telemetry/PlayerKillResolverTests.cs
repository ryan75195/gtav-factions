using System;
using FactionWars.Combat.Events;
using FactionWars.Core.Models;
using FactionWars.Telemetry.Models;
using FactionWars.Telemetry.Services;
using Xunit;

namespace FactionWars.Tests.Unit.Telemetry
{
    public class PlayerKillResolverTests
    {
        [Fact]
        public void Resolve_KillerIsPlayer_ReturnsPlayerEvent()
        {
            var args = new AttackerKilledEventArgs("morningwood", "trevor", DefenderTier.Heavy,
                pedHandle: 42, killerPedHandle: 99);

            var result = PlayerKillResolver.Resolve(args, playerPedHandle: 99,
                timestamp: new DateTime(2026, 1, 1), playTimeSeconds: 100);

            Assert.NotNull(result);
            Assert.Equal(PlayerEventType.Kill, result!.Type);
            Assert.Equal("morningwood", result.ZoneId);
            Assert.Equal("trevor", result.TargetFaction);
            Assert.Equal(DefenderTier.Heavy, result.TargetTier);
        }

        [Fact]
        public void Resolve_KillerIsNotPlayer_ReturnsNull()
        {
            var args = new AttackerKilledEventArgs("morningwood", "trevor", DefenderTier.Basic,
                pedHandle: 42, killerPedHandle: 50);

            var result = PlayerKillResolver.Resolve(args, playerPedHandle: 99,
                timestamp: new DateTime(2026, 1, 1), playTimeSeconds: 100);

            Assert.Null(result);
        }

        [Fact]
        public void Resolve_KillerUnknown_ReturnsNull()
        {
            var args = new AttackerKilledEventArgs("morningwood", "trevor", DefenderTier.Basic,
                pedHandle: 42, killerPedHandle: 0);

            var result = PlayerKillResolver.Resolve(args, playerPedHandle: 99,
                timestamp: new DateTime(2026, 1, 1), playTimeSeconds: 100);

            Assert.Null(result);
        }

        [Fact]
        public void Resolve_NullArgs_Throws()
        {
            Assert.Throws<ArgumentNullException>(() =>
                PlayerKillResolver.Resolve(null!, 99, DateTime.Now, 0));
        }
    }
}
