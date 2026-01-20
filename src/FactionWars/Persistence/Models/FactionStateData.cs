using FactionWars.Core.Models;
using FactionWars.Factions.Models;
using System.Collections.Generic;

namespace FactionWars.Persistence.Models
{
    /// <summary>
    /// Data transfer object for serializing FactionState data.
    /// </summary>
    public class FactionStateData
    {
        public string FactionId { get; set; } = string.Empty;
        public int Cash { get; set; }
        public int RecruitmentPoints { get; set; }
        public int Weapons { get; set; }
        public int TroopCount { get; set; }
        public List<string> OwnedZoneIds { get; set; } = new List<string>();
        public Dictionary<DefenderTier, int> ReservePool { get; set; } = new Dictionary<DefenderTier, int>();

        /// <summary>
        /// Creates a FactionStateData from a FactionState model.
        /// </summary>
        public static FactionStateData FromFactionState(FactionState state)
        {
            var data = new FactionStateData
            {
                FactionId = state.FactionId,
                Cash = state.Cash,
                RecruitmentPoints = state.RecruitmentPoints,
                Weapons = state.Weapons,
                TroopCount = state.TroopCount,
                OwnedZoneIds = new List<string>(state.OwnedZoneIds),
                ReservePool = state.GetReservePoolCopy()
            };
            return data;
        }

        /// <summary>
        /// Converts this data object to a FactionState model.
        /// </summary>
        public FactionState ToFactionState()
        {
            var state = new FactionState(FactionId, Cash, TroopCount);
            state.RecruitmentPoints = RecruitmentPoints;
            state.Weapons = Weapons;
            foreach (var zoneId in OwnedZoneIds)
            {
                state.AddZone(zoneId);
            }
            foreach (var kvp in ReservePool)
            {
                state.AddReserveTroops(kvp.Key, kvp.Value);
            }
            return state;
        }
    }
}
