using System.ComponentModel.DataAnnotations;
using SnakeAid.Core.Domains;

namespace SnakeAid.Core.Requests.SymptomConfig
{
    public class UpdateSymptomConfigRequest
    {
        /// <summary>
        /// Khóa thuộc tính (VD: "BITE_LOCATION", "AGE_GROUP", "SYMPTOMS")
        /// </summary>
        [MaxLength(100, ErrorMessage = "AttributeKey must not exceed 100 characters")]
        public string? AttributeKey { get; set; }

        /// <summary>
        /// Nhãn hiển thị của thuộc tính
        /// </summary>
        [MaxLength(500, ErrorMessage = "AttributeLabel must not exceed 500 characters")]
        public string? AttributeLabel { get; set; }

        /// <summary>
        /// Loại input trên UI
        /// </summary>
        public InputType? UIHint { get; set; }

        /// <summary>
        /// Thứ tự hiển thị (1-999)
        /// </summary>
        [Range(1, 999, ErrorMessage = "DisplayOrder must be between 1 and 999")]
        public int? DisplayOrder { get; set; }

        /// <summary>
        /// Tên của option cụ thể
        /// </summary>
        [MaxLength(300, ErrorMessage = "Name must not exceed 300 characters")]
        public string? Name { get; set; }

        /// <summary>
        /// Mô tả chi tiết
        /// </summary>
        [MaxLength(1000, ErrorMessage = "Description must not exceed 1000 characters")]
        public string? Description { get; set; }

        /// <summary>
        /// Trạng thái kích hoạt
        /// </summary>
        public bool? IsActive { get; set; }

        /// <summary>
        /// Danh mục triệu chứng
        /// </summary>
        public SymptomCategory? Category { get; set; }

        /// <summary>
        /// Danh sách điểm theo thời gian
        /// </summary>
        public List<TimeScorePoint>? TimeScoreList { get; set; }

        /// <summary>
        /// ID loại nọc độc (optional)
        /// </summary>
        public int? VenomTypeId { get; set; }
    }
}
