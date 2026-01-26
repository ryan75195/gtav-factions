using System;
using System.Collections.Generic;
using FactionWars.AI.Interfaces;
using FactionWars.AI.Models;

namespace FactionWars.AI.Services
{
    /// <summary>
    /// Service for classifying vehicle threats and determining RPG response requirements.
    /// Used by AI troop purchasing to respond appropriately to vehicle threats with Elite/RPG units.
    /// </summary>
    public class VehicleThreatService : IVehicleThreatService
    {
        private readonly Dictionary<string, VehicleThreatLevel> _vehicleThreatLevels;

        /// <summary>
        /// Creates a new VehicleThreatService with default vehicle classifications.
        /// </summary>
        public VehicleThreatService()
        {
            _vehicleThreatLevels = new Dictionary<string, VehicleThreatLevel>(StringComparer.OrdinalIgnoreCase)
            {
                // None threat - civilian vehicles, motorcycles
                { "bati", VehicleThreatLevel.None },

                // Light threat - armed technicals, sports cars
                { "technical", VehicleThreatLevel.Light },
                { "zentorno", VehicleThreatLevel.Light },

                // Heavy threat - armored vehicles, helicopters, tanks
                { "insurgent", VehicleThreatLevel.Heavy },
                { "apc", VehicleThreatLevel.Heavy },
                { "buzzard", VehicleThreatLevel.Heavy },
                { "khanjali", VehicleThreatLevel.Heavy }
            };
        }

        /// <inheritdoc/>
        public VehicleThreatLevel GetThreatLevel(string vehicleModelName)
        {
            if (string.IsNullOrEmpty(vehicleModelName))
            {
                return VehicleThreatLevel.None;
            }

            if (_vehicleThreatLevels.TryGetValue(vehicleModelName, out var threatLevel))
            {
                return threatLevel;
            }

            // Unknown vehicles default to no threat
            return VehicleThreatLevel.None;
        }

        /// <inheritdoc/>
        public int GetRequiredRpgCount(VehicleThreatLevel threatLevel)
        {
            switch (threatLevel)
            {
                case VehicleThreatLevel.None:
                    return 0;
                case VehicleThreatLevel.Light:
                    return 1;
                case VehicleThreatLevel.Heavy:
                    return 2;
                default:
                    return 0;
            }
        }
    }
}
