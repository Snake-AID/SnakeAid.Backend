namespace SnakeAid.Core.Domains
{
    public class RescuerRequest
    {
        public Guid Id { get; set; }
        public Guid IncidentId { get; set; }  // FK to SnakebiteIncident
        public Guid RescuerId { get; set; }   // FK to RescuerProfile
        public RescueRequestStatus Status { get; set; } = RescueRequestStatus.Pending;
        public DateTime RequestSentAt { get; set; } = DateTime.UtcNow;
        public DateTime? ResponseAt { get; set; }  // Khi rescuer accept/reject
        public DateTime? ExpiredAt { get; set; }   // Auto-expire after X minutes
        public string? RejectionReason { get; set; }


        // Navigation properties
        public SnakebiteIncident Incident { get; set; }
        public RescueRequestSession Session { get; set; }
        public RescuerProfile Rescuer { get; set; }
    }

    public enum RescueRequestStatus
    {
        Pending = 0,   // đang chờ rescuer phản hồi
        Accepted = 1, // rescuer đồng ý giúp
        Taken = 2, // bị lụm bởi rescuer khác
        Cancelled = 3, // bởi user
        Expired = 4, // 1.ko phải hồi (quét lại), 2.chủ động hủy (sẽ force set offline rescuer)
    }
}