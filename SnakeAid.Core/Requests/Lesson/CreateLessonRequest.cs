using System.ComponentModel.DataAnnotations;

namespace SnakeAid.Core.Requests.Lesson
{
    public class CreateLessonRequest
    {
        [Required(ErrorMessage = "Title is required")]
        [MaxLength(255, ErrorMessage = "Title must not exceed 255 characters")]
        public string Title { get; set; }

        [Required(ErrorMessage = "Content is required")]
        public string Content { get; set; }
    }
}
