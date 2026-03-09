using SnakeAid.Core.Domains;

namespace SnakeAid.Core.Responses.SymptomConfig
{
    public class GroupedSymptomConfigResponse
    {
        public string GroupName { get; set; } = null!;

        /// Technical key for programmatic access (e.g., "BITE_LOCATION", "CORE_SIGNS")
        public string AttributeKey { get; set; } = null!;

        /// User-friendly question text (e.g., "Vị trí vết cắn", "Dấu hiệu toàn thân")
        /// Display this as the question title
        public string AttributeLabel { get; set; } = null!;

        /// Display order for sorting questions
        public int DisplayOrder { get; set; }

        /// List of options/answers for this question
        public List<SymptomOptionResponse> Options { get; set; } = new List<SymptomOptionResponse>();
    }

    /// Individual symptom option within a question group
    public class SymptomOptionResponse
    {
        /// Unique ID of this symptom option
        public int Id { get; set; }

        /// Display name of the option (e.g., "Sụp mí mắt", "Đầu/Cổ")
        public string Name { get; set; } = null!;

        /// Optional detailed description
        public string? Description { get; set; }

        /// If true, show alert popup immediately when user selects this option
        public bool IsCritical { get; set; }

        /// Alert message to show in popup (e.g., "Gọi 115 ngay!")
        /// Only relevant if IsCritical = true
        public string? AlertMessage { get; set; }

        /// Category determines how scores are calculated
        /// Core = Max score only, Modifier = Sum all scores
        public SymptomCategory Category { get; set; }

        /// Human-readable category name
        public string CategoryDisplay { get; set; } = null!;

        /// Time-based scoring rules
        /// Used to calculate severity based on time elapsed since bite
        public List<TimeScorePoint> TimeScoreList { get; set; } = new List<TimeScorePoint>();

        /// Optional: Associated venom type ID
        public int? VenomTypeId { get; set; }

        /// Optional: Associated venom type information
        public VenomTypeInfo? VenomType { get; set; }

        /// Whether this option is currently active
        public bool IsActive { get; set; }
    }
}
