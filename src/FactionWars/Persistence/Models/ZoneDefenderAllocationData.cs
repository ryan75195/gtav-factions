using FactionWars.Core.Models;
using FactionWars.Persistence.Converters;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace FactionWars.Persistence.Models
{
    /// <summary>
    /// Data transfer object for serializing ZoneDefenderAllocation data.
    /// </summary>
    public class ZoneDefenderAllocationData
    {
        public string FactionId { get; set; } = string.Empty;
        public string ZoneId { get; set; } = string.Empty;

        [JsonConverter(typeof(LegacyRoleDictionaryConverter))]
        public Dictionary<DefenderRole, int> Troops { get; set; } = new Dictionary<DefenderRole, int>();

        /// <summary>
        /// Creates a ZoneDefenderAllocationData from a ZoneDefenderAllocation model.
        /// </summary>
        public static ZoneDefenderAllocationData FromAllocation(ZoneDefenderAllocation allocation)
        {
            return new ZoneDefenderAllocationData
            {
                FactionId = allocation.FactionId,
                ZoneId = allocation.ZoneId,
                Troops = allocation.GetTroopsCopy()
            };
        }

        /// <summary>
        /// Converts this data object to a ZoneDefenderAllocation model.
        /// </summary>
        public ZoneDefenderAllocation ToAllocation()
        {
            var allocation = new ZoneDefenderAllocation(FactionId, ZoneId);
            foreach (var kvp in Troops)
            {
                if (kvp.Value > 0)
                {
                    allocation.AddTroops(kvp.Key, kvp.Value);
                }
            }
            return allocation;
        }
    }
}
