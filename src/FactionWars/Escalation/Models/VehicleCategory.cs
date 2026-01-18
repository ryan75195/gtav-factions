namespace FactionWars.Escalation.Models
{
    /// <summary>
    /// Represents the category of a vehicle.
    /// Each category contains different vehicles that fit a similar role or type.
    /// </summary>
    public enum VehicleCategory
    {
        /// <summary>
        /// Compact cars - small, nimble city vehicles.
        /// </summary>
        Compact = 0,

        /// <summary>
        /// Sedans - standard four-door vehicles.
        /// </summary>
        Sedan = 1,

        /// <summary>
        /// SUVs - sport utility vehicles with more capacity.
        /// </summary>
        SUV = 2,

        /// <summary>
        /// Coupes - two-door sporty vehicles.
        /// </summary>
        Coupe = 3,

        /// <summary>
        /// Muscle cars - powerful American-style vehicles.
        /// </summary>
        Muscle = 4,

        /// <summary>
        /// Sports cars - high-performance vehicles.
        /// </summary>
        Sports = 5,

        /// <summary>
        /// Motorcycles - two-wheeled vehicles.
        /// </summary>
        Motorcycle = 6,

        /// <summary>
        /// Vans - utility and cargo vehicles.
        /// </summary>
        Van = 7,

        /// <summary>
        /// Armored vehicles - bulletproof and reinforced vehicles.
        /// </summary>
        Armored = 8,

        /// <summary>
        /// Military vehicles - tanks and military-grade transport.
        /// </summary>
        Military = 9
    }
}
