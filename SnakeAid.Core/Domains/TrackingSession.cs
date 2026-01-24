using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NetTopologySuite.Geometries;

namespace SnakeAid.Core.Domains
{
    public class TrackingSession : BaseEntity
    {
        [Key]
        public Guid Id { get; set; }

        // Link tới SOSMission hoặc CatchingRequest
        [Required]
        public Guid SessionId { get; set; }

        [Required]
        public SessionType SessionType { get; set; }

        [Required]
        public bool IsActive { get; set; } = true;

        // Tọa độ thực tế dùng NetTopologySuite với PostGIS
        [Column(TypeName = "geometry(Point, 4326)")]
        public Point? MemberLocation { get; set; }

        [Column(TypeName = "geometry(Point, 4326)")]
        public Point? RescuerLocation { get; set; }

        // Timestamps để kiểm tra dữ liệu stale
        public DateTime? MemberLastUpdate { get; set; }

        public DateTime? RescuerLastUpdate { get; set; }

        // Các thông số hiển thị
        [Range(0.0, double.MaxValue)]
        public double? DistanceMeters { get; set; }

        [Range(0, int.MaxValue)]
        public int? EtaMinutes { get; set; }
    }

    public enum SessionType
    {
        SOS = 0,
        Catching = 1
    }
}