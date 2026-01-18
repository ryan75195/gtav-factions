using System;

namespace FactionWars.Territory.Models
{
    /// <summary>
    /// Represents special characteristics of a zone that affect resource generation and combat.
    /// Flags enum allows combining multiple traits on a single zone.
    /// </summary>
    [Flags]
    public enum ZoneTrait
    {
        /// <summary>
        /// No special traits. Base resource generation and combat modifiers.
        /// </summary>
        None = 0,

        /// <summary>
        /// Industrial zones boost weapons production and provide manufacturing capabilities.
        /// </summary>
        Industrial = 1 << 0,

        /// <summary>
        /// Commercial zones generate increased cash income from businesses.
        /// </summary>
        Commercial = 1 << 1,

        /// <summary>
        /// Residential zones provide recruitment bonuses from local population.
        /// </summary>
        Residential = 1 << 2,

        /// <summary>
        /// Port zones enable supply line access and weapons smuggling.
        /// </summary>
        Port = 1 << 3,

        /// <summary>
        /// Airfield zones provide rapid deployment and reinforcement speed bonuses.
        /// </summary>
        Airfield = 1 << 4,

        /// <summary>
        /// Fortified zones provide significant defensive bonuses in combat.
        /// </summary>
        Fortified = 1 << 5,

        /// <summary>
        /// High-value zones multiply all resource generation rates.
        /// </summary>
        HighValue = 1 << 6
    }
}
