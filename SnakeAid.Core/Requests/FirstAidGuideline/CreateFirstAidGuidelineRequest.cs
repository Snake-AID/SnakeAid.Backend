using System.ComponentModel.DataAnnotations;
using SnakeAid.Core.Domains;

namespace SnakeAid.Core.Requests.FirstAidGuideline
{
    public class CreateFirstAidGuidelineRequest
    {
        /// <summary>
        /// Tên hướng dẫn sơ cứu
        /// </summary>
        [Required(ErrorMessage = "Name is required")]
        [MaxLength(255, ErrorMessage = "Name must not exceed 255 characters")]
        public string Name { get; set; }

        /// <summary>
        /// Nội dung chi tiết (JSON format)
        /// </summary>
        [Required(ErrorMessage = "Content is required")]
        public object Content { get; set; }

        /// <summary>
        /// Loại hướng dẫn: General (0) hoặc SpeciesSpecific (1)
        /// </summary>
        [Required(ErrorMessage = "Type is required")]
        public GuidelineType Type { get; set; }

        /// <summary>
        /// Tóm tắt ngắn
        /// </summary>
        [MaxLength(500, ErrorMessage = "Summary must not exceed 500 characters")]
        public string? Summary { get; set; }
    }
}
