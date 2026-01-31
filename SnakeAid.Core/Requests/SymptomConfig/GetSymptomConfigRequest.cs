using SnakeAid.Core.Domains;
using SnakeAid.Core.Meta;

namespace SnakeAid.Core.Requests.SymptomConfig
{
    public class GetSymptomConfigRequest : PaginationRequest
    {
        /// <summary>
        /// Tìm kiếm theo AttributeKey
        /// </summary>
        public string? AttributeKey { get; set; }

        /// <summary>
        /// Tìm kiếm theo tên
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// Lọc theo loại UI
        /// </summary>
        public InputType? UIHint { get; set; }

        /// <summary>
        /// Lọc theo category
        /// </summary>
        public SymptomCategory? Category { get; set; }

        /// <summary>
        /// Lọc theo trạng thái
        /// </summary>
        public bool? IsActive { get; set; }

        /// <summary>
        /// Lọc theo VenomTypeId
        /// </summary>
        public int? VenomTypeId { get; set; }
    }
}
