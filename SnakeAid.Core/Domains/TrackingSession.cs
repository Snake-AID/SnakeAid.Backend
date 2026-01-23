using System.ComponentModel.DataAnnotations;
using NetTopologySuite.Geometries;

namespace SnakeAid.Core.Domains
{
    public class TrackingSession
    {
        [Key]
        public Guid Id { get; set; }

        // Bắt buộc phải có để link tới SOSMission hoặc CatchingRequest
        public Guid SessionId { get; set; }
        public SessionType SessionType { get; set; }

        public bool IsActive { get; set; }

        // Tọa độ thực tế dùng NetTopologySuite
        public Point? MemberLocation { get; set; }
        public Point? RescuerLocation { get; set; }

        // Timestamps để Flutter biết dữ liệu có bị "cũ" (stale) không
        public DateTime? MemberLastUpdate { get; set; }
        public DateTime? RescuerLastUpdate { get; set; }

        // Các thông số hiển thị trực tiếp cho User
        public double? DistanceMeters { get; set; }
        public int? EtaMinutes { get; set; }
    }

    public enum SessionType
    {
        SOS = 0,
        Catching = 1
    }
}