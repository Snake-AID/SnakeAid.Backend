using System.ComponentModel.DataAnnotations;
using SnakeAid.Core.Domains;

namespace SnakeAid.Core.Requests.Lesson
{
    public class CreateLessonRequest
    {
        [Required(ErrorMessage = "Title is required")]
        [MaxLength(255, ErrorMessage = "Title must not exceed 255 characters")]
        public string Title { get; set; }

        [Required(ErrorMessage = "Content is required")]
        public string Content { get; set; }

        [Required(ErrorMessage = "Category is required")]
        [EnumDataType(typeof(Category), ErrorMessage = "Category is invalid")]
        public Category Category { get; set; }

        public bool IsPublished { get; set; } = true;
    }
}
