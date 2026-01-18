using System.Collections.Generic;

namespace FactionWars.Persistence.Models
{
    /// <summary>
    /// Represents the result of validating a save file.
    /// Contains validation status and any errors found.
    /// </summary>
    public class SaveFileValidationResult
    {
        private readonly List<string> _errors;

        /// <summary>
        /// Gets whether the save file is valid.
        /// </summary>
        public bool IsValid => _errors.Count == 0;

        /// <summary>
        /// Gets the list of validation errors found.
        /// </summary>
        public IReadOnlyList<string> Errors => _errors.AsReadOnly();

        /// <summary>
        /// Creates a new validation result.
        /// </summary>
        public SaveFileValidationResult()
        {
            _errors = new List<string>();
        }

        /// <summary>
        /// Adds an error to the validation result.
        /// </summary>
        /// <param name="error">The error message to add.</param>
        public void AddError(string error)
        {
            if (!string.IsNullOrEmpty(error))
            {
                _errors.Add(error);
            }
        }

        /// <summary>
        /// Creates a valid result with no errors.
        /// </summary>
        public static SaveFileValidationResult Valid()
        {
            return new SaveFileValidationResult();
        }

        /// <summary>
        /// Creates an invalid result with a single error.
        /// </summary>
        /// <param name="error">The error message.</param>
        public static SaveFileValidationResult Invalid(string error)
        {
            var result = new SaveFileValidationResult();
            result.AddError(error);
            return result;
        }
    }
}
