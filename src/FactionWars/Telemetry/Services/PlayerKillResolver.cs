using System;
using FactionWars.Combat.Events;
using FactionWars.Telemetry.Models;

namespace FactionWars.Telemetry.Services
{
    /// <summary>
    /// Resolves whether an AttackerKilled event was caused by the player ped.
    /// Pure: no I/O, no side effects.
    /// </summary>
    public static class PlayerKillResolver
    {
        public static PlayerEventRow? Resolve(AttackerKilledEventArgs args, int playerPedHandle,
            DateTime timestamp, long playTimeSeconds)
        {
            if (args == null) throw new ArgumentNullException(nameof(args));
            if (args.KillerPedHandle <= 0) return null;
            if (args.KillerPedHandle != playerPedHandle) return null;

            return new PlayerEventRow(
                timestamp,
                playTimeSeconds,
                PlayerEventType.Kill,
                zoneId: args.ZoneId,
                targetFaction: args.FactionId,
                targetTier: args.Tier,
                details: string.Empty);
        }
    }
}
