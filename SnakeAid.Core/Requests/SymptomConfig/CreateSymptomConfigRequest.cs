using System.ComponentModel.DataAnnotations;
using SnakeAid.Core.Domains;

namespace SnakeAid.Core.Requests.SymptomConfig
{
    public class CreateSymptomConfigRequest
    {
        /// <summary>
        /// Khóa thuộc tính (VD: "BITE_LOCATION", "AGE_GROUP", "SYMPTOMS")
        /// </summary>
        [Required(ErrorMessage = "AttributeKey is required")]
        [MaxLength(100, ErrorMessage = "AttributeKey must not exceed 100 characters")]
        public string AttributeKey { get; set; }

        /// <summary>
        /// Nhãn hiển thị của thuộc tính
        /// </summary>
        [Required(ErrorMessage = "AttributeLabel is required")]
        [MaxLength(500, ErrorMessage = "AttributeLabel must not exceed 500 characters")]
        public string AttributeLabel { get; set; }

        /// <summary>
        /// Loại input trên UI: SingleChoice (1), MultiChoice (2), Boolean (3), Numeric (4), Text (5)
        /// </summary>
        [Required(ErrorMessage = "UIHint is required")]
        public InputType UIHint { get; set; }

        /// <summary>
        /// Thứ tự hiển thị (1-999)
        /// </summary>
        [Required(ErrorMessage = "DisplayOrder is required")]
        [Range(1, 999, ErrorMessage = "DisplayOrder must be between 1 and 999")]
        public int DisplayOrder { get; set; }

        /// <summary>
        /// Tên của option cụ thể
        /// </summary>
        [Required(ErrorMessage = "Name is required")]
        [MaxLength(300, ErrorMessage = "Name must not exceed 300 characters")]
        public string Name { get; set; }

        /// <summary>
        /// Mô tả chi tiết
        /// </summary>
        [MaxLength(1000, ErrorMessage = "Description must not exceed 1000 characters")]
        public string? Description { get; set; }

        /// <summary>
        /// Trạng thái kích hoạt
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Danh mục triệu chứng: Core (1) - Max điểm, Modifier (2) - Cộng dồn
        /// </summary>
        [Required(ErrorMessage = "Category is required")]
        public SymptomCategory Category { get; set; }

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
