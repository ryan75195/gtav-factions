using System.Collections.Generic;
using FactionWars.Economy.Models;
using FactionWars.Territory.Models;

namespace FactionWars.Economy.Interfaces
{
    public interface IResourceGenerationCalculator
    {
        int CalculateGeneration(Zone zone, ResourceType resourceType);
        Dictionary<ResourceType, int> CalculateAllGeneration(Zone zone);
    }
}
