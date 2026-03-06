using SnakeAid.Core.Meta;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SnakeAid.Core.Requests.Expert
{
    public class ExpertDirectoryQueryRequest : PaginationRequest, IValidatableObject
    {
        public string? Specialization { get; set; }
        public bool? IsOnline { get; set; }
        public string? SortBy { get; set; }
        public string? SortOrder { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (!string.IsNullOrWhiteSpace(SortBy))
            {
                var value = SortBy.Trim().ToLowerInvariant();
                if (value != "isonline" && value != "rating" && value != "consultationfee")
                {
                    yield return new ValidationResult(
                        "SortBy must be one of: isOnline, rating, consultationFee.",
                        new[] { nameof(SortBy) });
                }
            }

            if (!string.IsNullOrWhiteSpace(SortOrder))
            {
                var value = SortOrder.Trim().ToLowerInvariant();
                if (value != "asc" && value != "desc")
                {
                    yield return new ValidationResult(
                        "SortOrder must be either asc or desc.",
                        new[] { nameof(SortOrder) });
                }
            }
        }
    }
}
