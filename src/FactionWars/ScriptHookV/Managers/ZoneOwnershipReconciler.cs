using System;
using FactionWars.ScriptHookV.Logging;

namespace FactionWars.ScriptHookV.Managers
{
    /// <summary>
    /// Centralises despawn-on-ownership-change so that when a zone changes hands the losing
    /// side's combatants are removed — even when the change happens while the player is standing
    /// inside the zone (captured/neutralised mid-fight), in which case no zone-exit event fires.
    /// This is what prevents orphaned hostile garrisons (the "blue/green peds that stay hostile
    /// after capture") and stale friendly defenders in lost territory.
    /// </summary>
    public class ZoneOwnershipReconciler
    {
        private readonly Action<string> _despawnFriendlyForZone;
        private readonly Action<string> _despawnEnemyForZone;
        private readonly Func<string?> _getPlayerFactionId;

        public ZoneOwnershipReconciler(
            Action<string> despawnFriendlyForZone,
            Action<string> despawnEnemyForZone,
            Func<string?> getPlayerFactionId)
        {
            _despawnFriendlyForZone = despawnFriendlyForZone ?? throw new ArgumentNullException(nameof(despawnFriendlyForZone));
            _despawnEnemyForZone = despawnEnemyForZone ?? throw new ArgumentNullException(nameof(despawnEnemyForZone));
            _getPlayerFactionId = getPlayerFactionId ?? throw new ArgumentNullException(nameof(getPlayerFactionId));
        }

        public void OnOwnershipChanged(string zoneId, string? previousOwner, string? newOwner)
        {
            if (string.IsNullOrEmpty(zoneId)) return;

            var playerFaction = _getPlayerFactionId();

            // The zone left the player: friendly defenders there are now stale.
            if (previousOwner == playerFaction && newOwner != playerFaction)
            {
                FileLogger.Combat($"ZoneOwnershipReconciler: {zoneId} left player ({previousOwner}->{newOwner ?? "NONE"}) — despawning friendly defenders");
                _despawnFriendlyForZone(zoneId);
            }

            // The zone left a rival faction (player captured or neutralised it, possibly while
            // inside so no zone-exit fired): that faction's garrison is now stale.
            if (!string.IsNullOrEmpty(previousOwner) && previousOwner != playerFaction && previousOwner != newOwner)
            {
                FileLogger.Combat($"ZoneOwnershipReconciler: {zoneId} left rival {previousOwner} (->{newOwner ?? "NONE"}) — despawning enemy garrison");
                _despawnEnemyForZone(zoneId);
            }
        }
    }
}
