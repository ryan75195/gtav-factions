using System.Collections.Generic;

namespace FactionWars.ScriptHookV.Managers
{
    /// <summary>
    /// Grace-state handling for player-owned zones: when the last defender dies while the
    /// player is present and alive, the loss is held open instead of transferring immediately.
    /// See <see cref="FriendlyDefenderManager.HandleTerritoryLost"/> for where grace is entered.
    /// </summary>
    public partial class FriendlyDefenderManager
    {
        // Zones whose last defender died while the player was holding them: ownership transfer is
        // deferred until the player dies, leaves undefended, or (saved) a defender is redeployed.
        private readonly HashSet<string> _undefendedGraceZones = new HashSet<string>();

        /// <summary>
        /// Transfers a zone to neutral and raises TerritoryLost. The single path that actually
        /// finalizes a loss, whether immediate or after grace expires.
        /// </summary>
        private void FinalizeZoneLoss(string zoneId)
        {
            _undefendedGraceZones.Remove(zoneId);
            _zoneService.TransferZoneOwnership(zoneId, null);
            TerritoryLost?.Invoke(this, new TerritoryLostEventArgs(zoneId));
        }

        private bool PlayerIsInZoneAndAlive(string zoneId)
            => !_gameBridge.IsPlayerDead() && PlayerCurrentZoneId() == zoneId;

        private string? PlayerCurrentZoneId()
            => _zoneService.GetZoneAtPosition(_gameBridge.GetPlayerPosition())?.Id;

        /// <summary>
        /// Resolves each grace zone every tick: saved if a defender is present again, lost if the
        /// player died or left the zone undefended, or dropped if ownership changed elsewhere.
        /// </summary>
        private void MonitorGraceZones()
        {
            if (_undefendedGraceZones.Count == 0) return;

            foreach (var zoneId in new List<string>(_undefendedGraceZones))
            {
                // Ownership changed via another path (battle/reclaim): nothing to hold.
                if (_zoneService.GetZone(zoneId)?.OwnerFactionId != _playerFactionId)
                {
                    _undefendedGraceZones.Remove(zoneId);
                    continue;
                }

                // Saved: a defender exists again (player redeployed).
                if (GetSpawnedDefenderCount(zoneId) > 0)
                {
                    _undefendedGraceZones.Remove(zoneId);
                    var name = _zoneService.GetZone(zoneId)?.Name ?? zoneId;
                    _gameBridge.ShowNotification($"~g~{name} secured.");
                    continue;
                }

                // Lost: player died, or left the zone while it is still undefended.
                if (_gameBridge.IsPlayerDead() || PlayerCurrentZoneId() != zoneId)
                {
                    FinalizeZoneLoss(zoneId);
                }
            }
        }
    }
}
