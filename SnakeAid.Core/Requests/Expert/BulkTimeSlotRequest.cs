using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SnakeAid.Core.Requests.Expert
{
    public class BulkTimeSlotRequest
    {
        [Required]
        public DateTime? WeekStartDate { get; set; }

        [Required]
        [MinLength(1, ErrorMessage = "At least one day block is required")]
        public List<DayBlockRequest> Days { get; set; } = new List<DayBlockRequest>();
    }

    public class DayBlockRequest
    {
        [Required]
        public DayOfWeek? DayOfWeek { get; set; }

        [Required]
        [MinLength(1, ErrorMessage = "At least one time block is required")]
        public List<TimeBlockRequest> TimeBlocks { get; set; } = new List<TimeBlockRequest>();
    }

    public class TimeBlockRequest : IValidatableObject
    {
        [Required]
        public TimeSpan? StartTime { get; set; }

        [Required]
        public TimeSpan? EndTime { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (!StartTime.HasValue)
            {
                yield return new ValidationResult("StartTime is required.", new[] { nameof(StartTime) });
                yield break;
            }

            if (!EndTime.HasValue)
            {
                yield return new ValidationResult("EndTime is required.", new[] { nameof(EndTime) });
                yield break;
            }

            var start = StartTime.Value;
            var end = EndTime.Value;
            var startOfDay = TimeSpan.Zero;
            var endOfDay = TimeSpan.FromHours(24);

            if (start < startOfDay || start >= endOfDay)
            {
                yield return new ValidationResult("StartTime must be within [00:00:00, 24:00:00).", new[] { nameof(StartTime) });
            }

            if (end <= startOfDay || end > endOfDay)
            {
                yield return new ValidationResult("EndTime must be within (00:00:00, 24:00:00].", new[] { nameof(EndTime) });
            }

            if (end <= start)
            {
                yield return new ValidationResult("EndTime must be later than StartTime.", new[] { nameof(EndTime) });
            }
        }
    }
}
