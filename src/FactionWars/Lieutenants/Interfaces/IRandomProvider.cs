namespace FactionWars.Lieutenants.Interfaces
{
    /// <summary>
    /// Abstraction over random number generation for testability.
    /// </summary>
    public interface IRandomProvider
    {
        /// <summary>
        /// Returns a non-negative random integer less than the specified maximum.
        /// </summary>
        /// <param name="maxValue">The exclusive upper bound.</param>
        /// <returns>A non-negative random integer.</returns>
        int Next(int maxValue);

        /// <summary>
        /// Returns a random floating-point number between 0.0 and 1.0.
        /// </summary>
        /// <returns>A double-precision floating point number greater than or equal to 0.0, and less than 1.0.</returns>
        double NextDouble();
    }
}
