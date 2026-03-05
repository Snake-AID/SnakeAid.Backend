using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SnakeAid.Core.Requests.Expert
{
    public class BulkTimeSlotRequest
    {
        [Required]
        public DateTime WeekStartDate { get; set; }

        [Required]
        [MinLength(1, ErrorMessage = "At least one day block is required")]
        public List<DayBlockRequest> Days { get; set; } = new List<DayBlockRequest>();
    }

    public class DayBlockRequest
    {
        [Required]
        public DayOfWeek DayOfWeek { get; set; }

        [Required]
        public List<TimeBlockRequest> TimeBlocks { get; set; } = new List<TimeBlockRequest>();
    }

    public class TimeBlockRequest : IValidatableObject
    {
        [Required]
        public TimeSpan StartTime { get; set; }

        [Required]
        public TimeSpan EndTime { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (EndTime <= StartTime)
            {
                yield return new ValidationResult("EndTime must be later than StartTime.", new[] { nameof(EndTime) });
            }
        }
    }
}
