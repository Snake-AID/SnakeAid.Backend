using SnakeAid.Core.Domains;

namespace SnakeAid.Core.Responses.SymptomConfig
{
    public class SymptomConfigResponse
    {
        public int Id { get; set; }

        // --- NHÓM LOGIC ---
        public string GroupName { get; set; } = null!;
        public string AttributeKey { get; set; } = null!;
        public string AttributeLabel { get; set; } = null!;
        public int DisplayOrder { get; set; }

        // --- CHI TIẾT LỰA CHỌN (OPTION) ---
        public string Name { get; set; } = null!;
        public string? Description { get; set; }

        // --- LOGIC CẢNH BÁO (ALERT/POPUP) ---
        public bool IsCritical { get; set; }
        public string? AlertMessage { get; set; }

        // --- LOGIC TÍNH ĐIỂM ---
        public SymptomCategory Category { get; set; }
        public string CategoryDisplay { get; set; } = null!; // Computed: "Core" or "Modifier"
        public List<TimeScorePoint> TimeScoreList { get; set; } = new List<TimeScorePoint>();
        public int? VenomTypeId { get; set; }
        public VenomTypeInfo? VenomType { get; set; }

        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class VenomTypeInfo
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
    }
}
