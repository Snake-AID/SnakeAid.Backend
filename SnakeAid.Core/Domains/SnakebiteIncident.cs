using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;
using NetTopologySuite.Geometries;

namespace SnakeAid.Core.Domains
{
    public class SnakebiteIncident : BaseEntity, IHasReportMedia
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [ForeignKey(nameof(User))]
        public Guid UserId { get; set; }  // FK to MemberProfile

        [Required]
        [Column(TypeName = "geometry(Point, 4326)")]
        public Point LocationCoordinates { get; set; }

        [Column(TypeName = "jsonb")]
        public ICollection<ReportSymptom>? SymptomsReport { get; set; } = new List<ReportSymptom>();

        [Required]
        public SnakebiteIncidentStatus Status { get; set; } = SnakebiteIncidentStatus.Pending;

        [Timestamp]
        public uint Version { get; set; }

        // --- Operator fields ---
        [ForeignKey(nameof(HandlingOperator))]
        public Guid? HandlingOperatorId { get; set; }  // Operator đang xử lý incident này

        [MaxLength(1000)]
        public string? OperatorNotes { get; set; }  // Ghi chú của operator

        public DateTime? DispatchedAt { get; set; }   // Khi operator đã điều phối rescuer

        public DateTime? ConfirmedAt { get; set; }    // Khi xác nhận thật (không phải báo động giả)

        // Assigned rescuer info
        public DateTime? AssignedAt { get; set; }

        [ForeignKey(nameof(AssignedRescuer))]
        public Guid? AssignedRescuerId { get; set; }  // FK to assigned rescuer

        [MaxLength(500)]
        public string? CancellationReason { get; set; }

        public int? SeverityLevel { get; set; } = 0;  // Tính toán dựa trên symptoms + time

        public DateTime? IncidentOccurredAt { get; set; }  // Khi nào bị cắn

        // Snake Identification
        [ForeignKey(nameof(IdentifiedSnakeSpecies))]
        public int? IdentifiedSnakeSpeciesId { get; set; }  // Loài rắn đã được xác định

        public SnakeIdentificationMethod IdentificationMethod { get; set; } = SnakeIdentificationMethod.None;

        [ForeignKey(nameof(AIRecognitionResult))]
        public Guid? AIRecognitionResultId { get; set; }  // Nếu xác định bằng AI

        [Column(TypeName = "jsonb")]
        public FilterAnswerData? FilterAnswers { get; set; }  // Nếu xác định bằng filter questions

        public DateTime? IdentifiedAt { get; set; }  // Thời điểm xác định được loài rắn

        // Navigation properties
        public MemberProfile User { get; set; }
        public SnakeSpecies? IdentifiedSnakeSpecies { get; set; }
        public SnakeAIRecognitionResult? AIRecognitionResult { get; set; }
        public RescuerProfile? AssignedRescuer { get; set; }
        public Account? HandlingOperator { get; set; }
        public ICollection<RescuerRequest> DispatchRequests { get; set; } = new List<RescuerRequest>();
        public ICollection<RescueMission> Missions { get; set; } = new List<RescueMission>();
        public ICollection<ReportMedia> Media { get; set; } = new List<ReportMedia>();
    }

    public enum SnakebiteIncidentStatus
    {
        Pending = 0,              // Chờ Operator nhận
        OperatorContacting = 1,   // Operator đang gọi xác nhận
        Verified = 2,            // Xác nhận thật, chờ điều phối
        Assigned = 3,             // Rescuer đã acknowledge, đang chuẩn bị
        FalseAlarm = 4,           // Báo động giả
        Finished = 5,
        Cancelled = 6,
        NoRescuerFound = 7,
        Paid = 8,
        Disputed = 9,
        Completed = 10
    }

    public enum SnakeIdentificationMethod
    {
        None = 0,              // Chưa xác định
        AIDetection = 1,       // Xác định bằng AI từ ảnh
        FilterQuestions = 2,   // Xác định bằng trả lời câu hỏi filter
        ManualByRescuer = 3,   // Rescuer xác định trực tiếp
        ExpertVerified = 4     // Chuyên gia xác nhận
    }

    /// <summary>
    /// Lưu thông tin câu trả lời filter questions của user
    /// </summary>
    public class FilterAnswerData
    {
        public List<FilterAnswer> Answers { get; set; } = new();
        public List<int> MatchedSnakeSpeciesIds { get; set; } = new();  // Danh sách rắn khớp
        public int? SelectedSnakeSpeciesId { get; set; }  // Rắn mà user chọn cuối cùng (nếu có nhiều kết quả)
    }

    public class FilterAnswer
    {
        [Required]
        public int QuestionId { get; set; }

        [Required]
        public string QuestionText { get; set; } = string.Empty;

        [Required]
        public int SelectedOptionId { get; set; }

        [Required]
        public string SelectedOptionText { get; set; } = string.Empty;
    }

    public class ReportSymptom
    {
        public int SymptomId { get; set; }
        public string SymptomName { get; set; } = string.Empty;

        public string SymptomDescription { get; set; } = string.Empty;
    }
}