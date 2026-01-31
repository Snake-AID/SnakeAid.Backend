using SnakeAid.Core.Domains;
using SnakeAid.Core.Meta;

namespace SnakeAid.Core.Requests.FirstAidGuideline
{
    public class GetFirstAidGuidelineRequest : PaginationRequest
    {
        /// <summary>
        /// Tìm kiếm theo tên
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// Lọc theo loại hướng dẫn
        /// </summary>
        public GuidelineType? Type { get; set; }
    }
}
