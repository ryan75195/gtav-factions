using System.Collections.Generic;

namespace FactionWars.Balance.Models
{
    /// <summary>
    /// Result of validating a BalanceConfiguration.
    /// </summary>
    public class BalanceValidationResult
    {
        /// <summary>
        /// True if the configuration passed all validation checks.
        /// </summary>
        public bool IsValid { get; }

        /// <summary>
        /// List of validation errors. Empty if IsValid is true.
        /// </summary>
        public IReadOnlyList<string> Errors { get; }

        /// <summary>
        /// Creates a new validation result.
        /// </summary>
        /// <param name="isValid">Whether validation passed.</param>
        /// <param name="errors">List of error messages.</param>
        public BalanceValidationResult(bool isValid, IReadOnlyList<string> errors)
        {
            IsValid = isValid;
            Errors = errors ?? new List<string>();
        }
    }
}
