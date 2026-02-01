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

        // --- NHÓM LOGIC ---
        [Required]
        [MaxLength(100)]
        public string GroupName { get; set; }      // "GENERAL", "LOCAL", "CRITICAL" -> Để Flutter chia màn hình/section

        [Required]
        [MaxLength(100)]
        public string AttributeKey { get; set; }   // "BITE_LOCATION", "CORE_SIGNS" -> Để lập trình viên xử lý Logic

        [Required]
        [MaxLength(500)]
        public string AttributeLabel { get; set; } // "Vị trí vết cắn", "Dấu hiệu toàn thân" -> Tiêu đề hiển thị trên UI

        [Required]
        public int DisplayOrder { get; set; }      // Thứ tự sắp xếp các câu hỏi

        // --- CHI TIẾT LỰA CHỌN (OPTION) ---
        [Required]
        [MaxLength(300)]
        public string Name { get; set; }           // "Sụp mí mắt", "Đầu/Cổ"

        [MaxLength(1000)]
        public string? Description { get; set; }   // Giải thích thêm cho user (nếu cần)

        // --- LOGIC CẢNH BÁO (ALERT/POPUP) ---
        public bool IsCritical { get; set; } = false; // Nếu true, Flutter sẽ hiện Popup ngay khi người dùng vừa Tick chọn

        [MaxLength(1000)]
        public string? AlertMessage { get; set; }  // Nội dung tin nhắn trong Popup (Ví dụ: "Gọi 115 ngay!")

        // --- LOGIC TÍNH ĐIỂM ---
        [Required]
        public SymptomCategory Category { get; set; } // Core (Max) hay Modifier (Sum)

        [Column(TypeName = "jsonb")]
        public string? TimeScoresJson { get; set; }

        public int? VenomTypeId { get; set; }      // Gợi ý loại độc tố (1: Thần kinh, 2: Máu...)

        [Required]
        public bool IsActive { get; set; } = true;

        [NotMapped]
        public List<TimeScorePoint> TimeScoreList =>
            string.IsNullOrEmpty(TimeScoresJson)
                ? new List<TimeScorePoint>()
                : JsonSerializer.Deserialize<List<TimeScorePoint>>(TimeScoresJson) ?? new List<TimeScorePoint>();
    }

    public enum SymptomCategory
    {
        Core = 1,      // Lấy điểm cao nhất (Max)
        Modifier = 2   // Cộng dồn điểm (Sum)
    }

    public class TimeScorePoint
    {
        public int MinMinutes { get; set; } // Phút bắt đầu
        public int MaxMinutes { get; set; } // Phút kết thúc
        public int Score { get; set; }      // Điểm số tương ứng
    }

}