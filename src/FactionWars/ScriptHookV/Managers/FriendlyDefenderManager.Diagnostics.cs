using System.Collections.Generic;
using FactionWars.ScriptHookV.Logging;

namespace FactionWars.ScriptHookV.Managers
{
    // TEMPORARY diagnostic instrumentation for the bug:
    // "friendly (player-coloured) defenders become hostile to the player".
    // High-signal, low-noise: logs ONLY when something actually goes wrong —
    //   (a) the global FRIENDLY_DEFENDERS<->PLAYER relationship value changes,
    //   (b) a tracked friendly defender's relationship group changes away from the
    //       group it was spawned into (reassignment — the suspected mechanism), or
    //   (c) a tracked friendly defender starts fighting the player.
    // No periodic "all healthy" heartbeat, so a clean session produces no [ALLEGIANCE] lines.
    // Grep the log for [ALLEGIANCE]. Delete this file (and its call from Update()) once fixed.
    public partial class FriendlyDefenderManager
    {
        private int _lastFriendlyToPlayerRel = int.MinValue;
        private readonly HashSet<int> _defendersFightingPlayer = new HashSet<int>();
        // Relationship group hash each defender was first seen with (its correct baseline).
        private readonly Dictionary<int, int> _defenderBaselineGroupHash = new Dictionary<int, int>();

        private void LogRelationshipDiagnostics(int currentGameTime)
        {
            // (a) Global FRIENDLY_DEFENDERS->PLAYER relationship changes.
            int rel = _gameBridge.GetGroupRelationship("FRIENDLY_DEFENDERS", "PLAYER");
            if (rel != _lastFriendlyToPlayerRel)
            {
                int relReverse = _gameBridge.GetGroupRelationship("PLAYER", "FRIENDLY_DEFENDERS");
                FileLogger.AI(
                    $"[ALLEGIANCE] FRIENDLY_DEFENDERS->PLAYER changed {RelName(_lastFriendlyToPlayerRel)} -> {RelName(rel)} " +
                    $"(PLAYER->FRIENDLY_DEFENDERS={RelName(relReverse)}, playerGroupHash={_gameBridge.GetPlayerRelationshipGroupHash()})");
                _lastFriendlyToPlayerRel = rel;
            }

            // (b) + (c) Per-ped audit of tracked friendly defenders.
            foreach (var kvp in _spawnedPedTierByZone)
            {
                string zoneId = kvp.Key;
                foreach (var pedHandle in kvp.Value.Keys)
                {
                    if (_corpseDeathTimes.ContainsKey(pedHandle))
                        continue;
                    if (!_gameBridge.DoesPedExist(pedHandle) || !_gameBridge.IsPedAlive(pedHandle))
                        continue;

                    AuditDefender(zoneId, pedHandle, rel);
                }
            }
        }

        private void AuditDefender(string zoneId, int pedHandle, int globalRel)
        {
            int groupHash = _gameBridge.GetPedRelationshipGroupHash(pedHandle);

            // (b) Relationship-group reassignment: record the baseline on first sight,
            // then flag any later change. A friendly defender that keeps its blue blip but
            // is moved into a different group is exactly the "blue but hostile" mechanism.
            if (!_defenderBaselineGroupHash.TryGetValue(pedHandle, out var baseline))
            {
                _defenderBaselineGroupHash[pedHandle] = groupHash;
            }
            else if (groupHash != baseline)
            {
                _defenderBaselineGroupHash[pedHandle] = groupHash;
                FileLogger.AI(
                    $"[ALLEGIANCE] *** defender {pedHandle} (zone {zoneId}) RELATIONSHIP GROUP CHANGED " +
                    $"{baseline} -> {groupHash} (playerGroupHash={_gameBridge.GetPlayerRelationshipGroupHash()}, " +
                    $"FRIENDLY_DEFENDERS->PLAYER={RelName(globalRel)})");
            }

            // (c) Actively fighting the player — log once on the transition.
            bool fighting = _gameBridge.IsPedInCombatWithPlayer(pedHandle);
            bool was = _defendersFightingPlayer.Contains(pedHandle);
            if (fighting && !was)
            {
                _defendersFightingPlayer.Add(pedHandle);
                FileLogger.AI(
                    $"[ALLEGIANCE] *** defender {pedHandle} (zone {zoneId}) STARTED FIGHTING PLAYER. " +
                    $"pedGroupHash={groupHash} playerGroupHash={_gameBridge.GetPlayerRelationshipGroupHash()} " +
                    $"FRIENDLY_DEFENDERS->PLAYER={RelName(globalRel)}");
            }
            else if (!fighting && was)
            {
                _defendersFightingPlayer.Remove(pedHandle);
            }
        }

        private static string RelName(int rel) => rel switch
        {
            int.MinValue => "<unread>",
            -1 => "<error>",
            0 => "Companion(0)",
            1 => "Respect(1)",
            2 => "Like(2)",
            3 => "Neutral(3)",
            4 => "Dislike(4)",
            5 => "Hate(5)",
            255 => "Pedestrians(255)",
            _ => $"?({rel})"
        };
    }
}
