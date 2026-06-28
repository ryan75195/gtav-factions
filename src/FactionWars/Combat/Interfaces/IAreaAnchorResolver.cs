using FactionWars.Combat.Models;
using FactionWars.Core.Interfaces;

namespace FactionWars.Combat.Interfaces
{
    /// <summary>
    /// Resolves the squad's operating anchor: the current zone when inside one,
    /// otherwise the player's position with a default loose radius.
    /// </summary>
    public interface IAreaAnchorResolver
    {
        AreaAnchor Resolve(Vector3? zoneCenter, float zoneRadius, Vector3 playerPosition, float defaultRadius);
    }
}
