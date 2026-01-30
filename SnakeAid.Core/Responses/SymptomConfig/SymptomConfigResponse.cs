using SnakeAid.Core.Domains;

namespace SnakeAid.Core.Responses.SymptomConfig
{
    public class SymptomConfigResponse
    {
        public int Id { get; set; }
        public string AttributeKey { get; set; }
        public string AttributeLabel { get; set; }
        public InputType UIHint { get; set; }
        public string UIHintDisplay { get; set; } // Friendly display name
        public int DisplayOrder { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }
        public bool IsActive { get; set; }
        public SymptomCategory Category { get; set; }
        public string CategoryDisplay { get; set; } // Friendly display name
        public List<TimeScorePoint> TimeScoreList { get; set; } = new List<TimeScorePoint>();
        public int? VenomTypeId { get; set; }
        public VenomTypeInfo? VenomType { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class VenomTypeInfo
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }
}
