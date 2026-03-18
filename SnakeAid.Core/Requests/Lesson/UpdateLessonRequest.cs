using System.ComponentModel.DataAnnotations;

namespace SnakeAid.Core.Requests.Lesson
{
    public class UpdateLessonRequest
    {
        [MaxLength(255, ErrorMessage = "Title must not exceed 255 characters")]
        public string? Title { get; set; }

        public string? Content { get; set; }
    }
}
