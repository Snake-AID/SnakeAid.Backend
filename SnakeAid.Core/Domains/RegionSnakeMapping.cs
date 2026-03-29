using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SnakeAid.Core.Domains
{
    /// <summary>
    /// Mapping giữa loài rắn và khu vực địa lý
    /// Tương tự FilterSnakeMapping nhưng dùng cho location-based filtering
    /// </summary>
    public class RegionSnakeMapping : BaseEntity
    {
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// ID khu vực địa lý
        /// </summary>
        [Required]
        [ForeignKey(nameof(GeographicRegion))]
        public int GeographicRegionId { get; set; }

        /// <summary>
        /// ID loài rắn
        /// </summary>
        [Required]
        [ForeignKey(nameof(SnakeSpecies))]
        public int SnakeSpeciesId { get; set; }

        /// <summary>
        /// Mức độ phổ biến của loài rắn trong khu vực này
        /// </summary>
        [Required]
        public CommonLevel CommonLevel { get; set; } = CommonLevel.Common;

        /// <summary>
        /// Độ ưu tiên hiển thị (loài phổ biến hơn = số cao hơn)
        /// Dùng để sắp xếp kết quả khi filter theo location
        /// </summary>
        [Required]
        public int Priority { get; set; } = 0;

        /// <summary>
        /// Ghi chú về phân bố (VD: "Thường gặp ở vùng ven sông", "Chỉ xuất hiện mùa mưa")
        /// </summary>
        [MaxLength(500)]
        public string? DistributionNotes { get; set; }

        /// <summary>
        /// Trạng thái hoạt động
        /// </summary>
        [Required]
        public bool IsActive { get; set; } = true;

        // Navigation properties
        public GeographicRegion GeographicRegion { get; set; }
        public SnakeSpecies SnakeSpecies { get; set; }
    }

    /// <summary>
    /// Mức độ phổ biến của loài rắn trong khu vực
    /// </summary>
    public enum CommonLevel
    {
        /// <summary>Rất hiếm gặp</summary>
        Rare = 1,
        
        /// <summary>Ít gặp</summary>
        Uncommon = 2,
        
        /// <summary>Phổ biến</summary>
        Common = 3,
        
        /// <summary>Rất phổ biến</summary>
        VeryCommon = 4,
        
        /// <summary>Cực kỳ phổ biến (loài đặc trưng của vùng)</summary>
        Abundant = 5
    }
}
