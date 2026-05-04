using System;
using System.Collections.Generic;
using FactionWars.Core.Interfaces;
using FactionWars.Territory.Models;
using Newtonsoft.Json.Linq;

namespace FactionWars.ScriptHookV.Data
{
    public partial class ZoneDataLoader
    {
        private static Zone CreateZoneFromDto(ZoneDto dto)
        {
            var center = new Vector3(dto.CenterX, dto.CenterY, dto.CenterZ);
            var zone = new Zone(dto.Id, dto.Name, center, dto.Radius, dto.StrategicValue);
            zone.Traits = ParseTraits(dto.TraitsRaw);
            return zone;
        }

        private static ZoneTrait ParseTraits(object? traitsRaw)
        {
            if (traitsRaw == null)
                return ZoneTrait.None;

            ZoneTrait result = ZoneTrait.None;

            // Handle JArray (from JSON array)
            if (traitsRaw is JArray jArray)
            {
                foreach (var item in jArray)
                {
                    var traitStr = item.ToString().Trim();
                    if (Enum.TryParse<ZoneTrait>(traitStr, true, out var trait))
                    {
                        result |= trait;
                    }
                }
                return result;
            }

            // Handle string (comma-separated)
            var traitsString = traitsRaw.ToString();
            if (string.IsNullOrWhiteSpace(traitsString))
                return ZoneTrait.None;

            var parts = traitsString.Split(new[] { ',', '|' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var part in parts)
            {
                if (Enum.TryParse<ZoneTrait>(part.Trim(), true, out var trait))
                {
                    result |= trait;
                }
            }
            return result;
        }

        /// <summary>
        /// Creates the default 31 zones for Los Santos and Blaine County.
        /// Based on actual GTA V map locations and coordinates.
        /// </summary>
        private static IEnumerable<Zone> CreateDefaultZones()
        {
            // Los Santos - Urban Core
            yield return CreateZone("downtown", "Downtown", -250f, -850f, 200f, 8, ZoneTrait.Commercial | ZoneTrait.HighValue);
            yield return CreateZone("vinewood", "Vinewood", 300f, 150f, 250f, 7, ZoneTrait.Commercial | ZoneTrait.Residential);
            yield return CreateZone("rockford_hills", "Rockford Hills", -550f, 100f, 200f, 6, ZoneTrait.Residential | ZoneTrait.HighValue);
            yield return CreateZone("little_seoul", "Little Seoul", -750f, -1000f, 180f, 5, ZoneTrait.Commercial);
            yield return CreateZone("pillbox_hill", "Pillbox Hill", -100f, -1100f, 150f, 6, ZoneTrait.Commercial);
            yield return CreateZone("mission_row", "Mission Row", 400f, -1000f, 150f, 5, ZoneTrait.Industrial);
            yield return CreateZone("strawberry", "Strawberry", 200f, -1400f, 150f, 4, ZoneTrait.Residential);
            yield return CreateZone("davis", "Davis", 100f, -1700f, 200f, 4, ZoneTrait.Residential);
            yield return CreateZone("rancho", "Rancho", 400f, -1700f, 180f, 3, ZoneTrait.Residential);
            yield return CreateZone("la_puerta", "La Puerta", -1000f, -1500f, 200f, 5, ZoneTrait.Residential);
            yield return CreateZone("vespucci", "Vespucci", -1100f, -1200f, 200f, 5, ZoneTrait.Commercial | ZoneTrait.Residential);
            yield return CreateZone("del_perro", "Del Perro", -1700f, -800f, 250f, 6, ZoneTrait.Commercial);
            yield return CreateZone("morningwood", "Morningwood", -1300f, -600f, 180f, 4, ZoneTrait.Residential);

            // Los Santos - Port & Industrial
            yield return CreateZone("port_of_los_santos", "Port of Los Santos", 200f, -2900f, 400f, 8, ZoneTrait.Port | ZoneTrait.Industrial);
            yield return CreateZone("elysian_island", "Elysian Island", -300f, -2700f, 300f, 6, ZoneTrait.Port | ZoneTrait.Industrial);
            yield return CreateZone("cypress_flats", "Cypress Flats", 700f, -2300f, 200f, 4, ZoneTrait.Industrial);
            yield return CreateZone("terminal", "Terminal", -400f, -2200f, 180f, 4, ZoneTrait.Industrial);

            // Los Santos - Airport
            yield return CreateZone("lsia", "Los Santos International", -1200f, -2500f, 500f, 9, ZoneTrait.Airfield | ZoneTrait.HighValue);

            // Blaine County - Sandy Shores Area
            yield return CreateZone("sandy_shores", "Sandy Shores", 1700f, 3700f, 350f, 5, ZoneTrait.Residential);
            yield return CreateZone("harmony", "Harmony", 600f, 2800f, 200f, 3, ZoneTrait.Residential);
            yield return CreateZone("grapeseed", "Grapeseed", 1700f, 4800f, 250f, 4, ZoneTrait.Residential);
            yield return CreateZone("alamo_sea", "Alamo Sea", 600f, 4000f, 300f, 3, ZoneTrait.None);

            // Blaine County - Mount Chiliad Area
            yield return CreateZone("paleto_bay", "Paleto Bay", -200f, 6300f, 350f, 6, ZoneTrait.Port | ZoneTrait.Residential);
            yield return CreateZone("paleto_forest", "Paleto Forest", -700f, 5500f, 400f, 2, ZoneTrait.None);
            yield return CreateZone("chiliad_wilderness", "Chiliad Wilderness", 500f, 5500f, 400f, 2, ZoneTrait.Fortified);

            // Blaine County - Desert
            yield return CreateZone("grand_senora_desert", "Grand Senora Desert", 2500f, 3000f, 400f, 3, ZoneTrait.None);
            yield return CreateZone("trevor_airfield", "Trevor's Airfield", 1750f, 3250f, 250f, 7, ZoneTrait.Airfield);

            // Northern Suburbs
            yield return CreateZone("vinewood_hills", "Vinewood Hills", 600f, 600f, 300f, 5, ZoneTrait.Residential | ZoneTrait.Fortified);
            yield return CreateZone("richman", "Richman", -1600f, 200f, 250f, 6, ZoneTrait.Residential | ZoneTrait.HighValue);
            yield return CreateZone("mirror_park", "Mirror Park", 1100f, -700f, 200f, 4, ZoneTrait.Residential);
            yield return CreateZone("east_los_santos", "East Los Santos", 1200f, -1700f, 300f, 4, ZoneTrait.Industrial | ZoneTrait.Residential);
        }

        private static Zone CreateZone(string id, string name, float x, float y, float radius, int strategicValue, ZoneTrait traits)
        {
            // Z coordinate for zones - ground level approximation
            const float groundLevel = 30f;
            var center = new Vector3(x, y, groundLevel);
            var zone = new Zone(id, name, center, radius, strategicValue);
            zone.Traits = traits;
            return zone;
        }

        /// <summary>
        /// Sets up adjacency relationships between zones.
        /// Uses hardcoded adjacencies for default zones, proximity-based for custom zones.
        /// </summary>
        /// <param name="zoneRepository">The zone repository containing loaded zones.</param>
    }
}
