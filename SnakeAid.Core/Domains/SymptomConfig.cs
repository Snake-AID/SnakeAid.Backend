using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace SnakeAid.Core.Domains
{
    public class SymptomConfig : BaseEntity
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string AttributeKey { get; set; }   // "BITE_LOCATION", "AGE_GROUP", "SYMPTOMS"

        [Required]
        [MaxLength(500)]
        public string AttributeLabel { get; set; } // Tiêu đề hiển thị

        [Required]
        public InputType UIHint { get; set; }      // Cách hiển thị trên UI

        [Required]
        [Range(1, 999)]
        public int DisplayOrder { get; set; }      // Thứ tự xuất hiện

        // --- Option details ---
        [Required]
        [MaxLength(300)]
        public string Name { get; set; }           // Tên option cụ thể

        [MaxLength(1000)]
        public string? Description { get; set; }   // Mô tả chi tiết

        [Required]
        public bool IsActive { get; set; } = true;

        [Required]
        public SymptomCategory Category { get; set; } // Core (Max) hay Modifier (Sum)

        [Column(TypeName = "jsonb")]
        public string? TimeScoresJson { get; set; }

        // Relations
        [ForeignKey(nameof(VenomType))]
        public int? VenomTypeId { get; set; }

        // Navigation properties
        public VenomType? VenomType { get; set; }

        // Helper property
        [NotMapped]
        public List<TimeScorePoint> TimeScoreList =>
            string.IsNullOrEmpty(TimeScoresJson) 
                ? new List<TimeScorePoint>() 
                : JsonSerializer.Deserialize<List<TimeScorePoint>>(TimeScoresJson) ?? new List<TimeScorePoint>();
    }

    public enum InputType
    {
        SingleChoice = 1,
        MultiChoice = 2,
        Boolean = 3,
        Numeric = 4,      // Thêm cho nhập số (tuổi, cân nặng...)
        Text = 5          // Thêm cho nhập text tự do
    }

    public enum SymptomCategory
    {
        Core = 1,      // Lấy điểm cao nhất trong các lựa chọn (Max)
        Modifier = 2   // Cộng dồn điểm của các lựa chọn (Sum)
    }

    public class TimeScorePoint
    {
        public int MinMinutes { get; set; } // Phút bắt đầu
        public int MaxMinutes { get; set; } // Phút kết thúc
        public int Score { get; set; }      // Điểm số tương ứng
    }
}