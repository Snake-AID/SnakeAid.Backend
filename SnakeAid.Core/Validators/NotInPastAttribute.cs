using System;
using System.ComponentModel.DataAnnotations;

namespace SnakeAid.Core.Validators
{
    /// <summary>
    /// Validates that a DateTime is not in the past (with optional tolerance in minutes)
    /// </summary>
    public class NotInPastAttribute : ValidationAttribute
    {
        /// <summary>
        /// Tolerance in minutes. DateTime within this tolerance from now are considered valid.
        /// Default is 5 minutes to account for clock differences.
        /// </summary>
        public int ToleranceMinutes { get; set; } = 5;

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value == null)
            {
                return ValidationResult.Success; // Null values should be handled by [Required]
            }

            if (value is not DateTime dateTime)
            {
                return new ValidationResult($"{validationContext.DisplayName} must be a valid DateTime.");
            }

            var minDateTime = DateTime.UtcNow.AddMinutes(-ToleranceMinutes);
            
            if (dateTime < minDateTime)
            {
                return new ValidationResult($"{validationContext.DisplayName} cannot be more than {ToleranceMinutes} minutes in the past.");
            }

            return ValidationResult.Success;
        }
    }
}
