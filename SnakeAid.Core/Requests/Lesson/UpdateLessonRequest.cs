using System.ComponentModel.DataAnnotations;
using SnakeAid.Core.Domains;

namespace SnakeAid.Core.Requests.Lesson
{
    public class UpdateLessonRequest
    {
        [MaxLength(255, ErrorMessage = "Title must not exceed 255 characters")]
        public string? Title { get; set; }

        public string? Content { get; set; }

        [EnumDataType(typeof(Category), ErrorMessage = "Category is invalid")]
        public Category? Category { get; set; }

        public bool? IsPublished { get; set; }
    }
}
