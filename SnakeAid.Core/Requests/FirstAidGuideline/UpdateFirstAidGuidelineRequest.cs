using System.ComponentModel.DataAnnotations;
using SnakeAid.Core.Domains;

namespace SnakeAid.Core.Requests.FirstAidGuideline
{
    public class UpdateFirstAidGuidelineRequest
    {
        /// <summary>
        /// Tên hướng dẫn sơ cứu
        /// </summary>
        [MaxLength(255, ErrorMessage = "Name must not exceed 255 characters")]
        public string? Name { get; set; }

        /// <summary>
        /// Nội dung chi tiết (JSON format)
        /// </summary>
        public string? Content { get; set; }

        /// <summary>
        /// Loại hướng dẫn: General (0) hoặc SpeciesSpecific (1)
        /// </summary>
        public GuidelineType? Type { get; set; }

        /// <summary>
        /// Tóm tắt ngắn
        /// </summary>
        [MaxLength(500, ErrorMessage = "Summary must not exceed 500 characters")]
        public string? Summary { get; set; }
    }
}
