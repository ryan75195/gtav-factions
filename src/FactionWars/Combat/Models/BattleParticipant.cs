using System;
using System.Collections.Generic;
using FactionWars.Core.Models;

namespace FactionWars.Combat.Models
{
    /// <summary>
    /// A single side in a <see cref="ZoneBattle"/> — either an AI faction
    /// with tier-keyed troops, or the player whose alive count comes from
    /// a squad-service callback. Construct via <see cref="ForAi"/> or
    /// <see cref="ForPlayer"/>; do not use the constructor directly.
    /// </summary>
    public class BattleParticipant
    {
        private readonly Func<int>? _playerAliveCountCallback;

        public string FactionId { get; }
        public BattleRole Role { get; }
        public bool IsPlayer { get; }

        /// <summary>
        /// Tier-keyed troop counts for AI participants. Always empty for
        /// player participants — their alive count comes from the
        /// squad callback. Mutable during a battle.
        /// </summary>
        public Dictionary<DefenderTier, int> Troops { get; }

        private BattleParticipant(
            string factionId,
            BattleRole role,
            bool isPlayer,
            Dictionary<DefenderTier, int> troops,
            Func<int>? playerAliveCountCallback)
        {
            FactionId = factionId;
            Role = role;
            IsPlayer = isPlayer;
            Troops = troops;
            _playerAliveCountCallback = playerAliveCountCallback;
        }

        public static BattleParticipant ForAi(string factionId, BattleRole role, Dictionary<DefenderTier, int> troops)
        {
            if (factionId == null) throw new ArgumentNullException(nameof(factionId));
            if (troops == null) throw new ArgumentNullException(nameof(troops));
            return new BattleParticipant(factionId, role, isPlayer: false, new Dictionary<DefenderTier, int>(troops), null);
        }

        public static BattleParticipant ForPlayer(string factionId, BattleRole role, Func<int> aliveCountCallback)
        {
            if (factionId == null) throw new ArgumentNullException(nameof(factionId));
            if (aliveCountCallback == null) throw new ArgumentNullException(nameof(aliveCountCallback));
            return new BattleParticipant(factionId, role, isPlayer: true, new Dictionary<DefenderTier, int>(), aliveCountCallback);
        }

        /// <summary>
        /// Total surviving members on this side. AI: sum of <see cref="Troops"/>.
        /// Player: result of the squad callback.
        /// </summary>
        public int AliveCount
        {
            get
            {
                if (IsPlayer) return _playerAliveCountCallback!();
                int total = 0;
                foreach (var kvp in Troops) total += kvp.Value;
                return total;
            }
        }

        /// <summary>
        /// Decrements one troop of the given tier. No-op for player participants.
        /// </summary>
        /// <returns>True if a troop was removed.</returns>
        public bool RemoveTroop(DefenderTier tier)
        {
            if (IsPlayer) return false;
            if (Troops.TryGetValue(tier, out int count) && count > 0)
            {
                Troops[tier] = count - 1;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Adds <paramref name="count"/> troops of the given tier. No-op for
        /// player participants and for non-positive counts.
        /// </summary>
        public void AddTroops(DefenderTier tier, int count)
        {
            if (IsPlayer || count <= 0) return;
            if (Troops.ContainsKey(tier)) Troops[tier] += count;
            else Troops[tier] = count;
        }
    }
}
