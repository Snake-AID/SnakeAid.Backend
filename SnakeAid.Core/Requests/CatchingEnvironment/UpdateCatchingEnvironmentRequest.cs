using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SnakeAid.Core.Requests.CatchingEnvironment
{
    public class UpdateCatchingEnvironmentRequest
    {
        /// <summary>
        /// Tên môi trường bắt rắn
        /// </summary>
        [MaxLength(255, ErrorMessage = "Name must not exceed 255 characters")]
        public string? Name { get; set; }

        /// <summary>
        /// Mô tả môi trường bắt rắn
        /// </summary>
        [MaxLength(1000, ErrorMessage = "Description must not exceed 1000 characters")]
        public string? Description { get; set; }

        /// <summary>
        /// Giá dịch vụ
        /// </summary>
        [Range(0, double.MaxValue, ErrorMessage = "Price must be greater than or equal to 0")]
        public decimal? Price { get; set; }

        /// <summary>
        /// Loại tiền tệ (VND, USD, etc.)
        /// </summary>
        [StringLength(5, ErrorMessage = "Currency must not exceed 5 characters")]
        public string? Currency { get; set; }
    }
}
