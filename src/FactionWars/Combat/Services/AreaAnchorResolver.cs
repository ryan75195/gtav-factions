using FactionWars.Combat.Interfaces;
using FactionWars.Combat.Models;
using FactionWars.Core.Interfaces;

namespace FactionWars.Combat.Services
{
    public class AreaAnchorResolver : IAreaAnchorResolver
    {
        public AreaAnchor Resolve(Vector3? zoneCenter, float zoneRadius, Vector3 playerPosition, float defaultRadius)
        {
            if (zoneCenter.HasValue)
            {
                return new AreaAnchor(zoneCenter.Value, zoneRadius);
            }

            return new AreaAnchor(playerPosition, defaultRadius);
        }
    }
}
