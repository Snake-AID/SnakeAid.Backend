using System;
using System.ComponentModel.DataAnnotations;

namespace SnakeAid.Core.Validators
{
    /// <summary>
    /// Validates that a string can be parsed as a valid coordinate value
    /// </summary>
    public class CoordinateAttribute : ValidationAttribute
    {
        public CoordinateType Type { get; set; }

        public CoordinateAttribute(CoordinateType type)
        {
            Type = type;
        }

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
            {
                return new ValidationResult($"{validationContext.DisplayName} is required.");
            }

            var stringValue = value.ToString()!;

            if (!double.TryParse(stringValue, out double coordinate))
            {
                return new ValidationResult($"{validationContext.DisplayName} must be a valid number.");
            }

            return Type switch
            {
                CoordinateType.Latitude when coordinate < -90 || coordinate > 90 
                    => new ValidationResult($"{validationContext.DisplayName} must be between -90 and 90."),
                CoordinateType.Longitude when coordinate < -180 || coordinate > 180 
                    => new ValidationResult($"{validationContext.DisplayName} must be between -180 and 180."),
                _ => ValidationResult.Success
            };
        }
    }

    public enum CoordinateType
    {
        Latitude,
        Longitude
    }
}
