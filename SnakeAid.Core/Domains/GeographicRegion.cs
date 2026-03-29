using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NetTopologySuite.Geometries;

namespace SnakeAid.Core.Domains
{
    /// <summary>
    /// Khu vực địa lý (Đông Nam Bộ, Tây Nam Bộ, Miền Trung, v.v.)
    /// Sử dụng PostGIS Polygon để định nghĩa ranh giới
    /// </summary>
    public class GeographicRegion : BaseEntity
    {
        [Key]
        public int Id { get; set; }

        /// Tên khu vực (VD: "Đông Nam Bộ", "Tây Nam Bộ", "Duyên hải Nam Trung Bộ")
        [Required]
        [MaxLength(200)]
        public string Name { get; set; }

        /// Mã khu vực (VD: "DNB", "TNB", "DHNTB") - dùng cho API
        [Required]
        [MaxLength(50)]
        public string Code { get; set; }

        [MaxLength(1000)]
        public string? Description { get; set; }

        /// Ranh giới khu vực dạng Polygon (PostGIS)
        /// SRID 4326 (WGS84 - latitude/longitude)
        [Required]
        [Column(TypeName = "geography(Polygon, 4326)")]
        public Polygon Boundary { get; set; }

        /// Thứ tự hiển thị (từ Bắc xuống Nam)
        [Required]
        public int DisplayOrder { get; set; }

        /// Trạng thái hoạt động
        [Required]
        public bool IsActive { get; set; } = true;

        // Navigation properties
        public ICollection<RegionSnakeMapping> RegionSnakeMappings { get; set; } = new List<RegionSnakeMapping>();
    }
}
