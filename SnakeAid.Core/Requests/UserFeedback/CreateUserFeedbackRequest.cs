using System;
using System.ComponentModel.DataAnnotations;
using SnakeAid.Core.Domains;

namespace SnakeAid.Core.Requests.UserFeedback
{
    public class CreateUserFeedbackRequest
    {
        /// <summary>
        /// ID của user nhận đánh giá
        /// </summary>
        [Required]
        public Guid TargetUserId { get; set; }

        /// <summary>
        /// ID của request tương ứng (SnakeCatchingRequest, SnakebiteIncident, etc.)
        /// </summary>
        [Required]
        public Guid ReferenceId { get; set; }

        /// <summary>
        /// Loại feedback (Emergency=0, Catching=1, Consultation=2)
        /// </summary>
        [Required]
        public FeedbackType Type { get; set; }

        /// <summary>
        /// Đánh giá từ 1-5 sao
        /// </summary>
        [Required]
        [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5")]
        public int Rating { get; set; }

        /// <summary>
        /// Nhận xét (tối đa 2000 ký tự)
        /// </summary>
        [MaxLength(2000, ErrorMessage = "Comments cannot exceed 2000 characters")]
        public string? Comments { get; set; }

        /// <summary>
        /// Role của user nhận đánh giá (để cập nhật rating trong profile tương ứng)
        /// </summary>
        [Required]
        public AccountRole TargetUserRole { get; set; }
    }
}
