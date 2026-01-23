namespace SnakeAid.Core.Domains
{
    public class AnswerOption
    {
        public int Id { get; set; }
        public string OptionText { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; }
        public int QuestionId { get; set; }

        public FilterQuestion Question { get; set; }
        public ICollection<SpeciesFilterMapping> SpeciesFilterMappings { get; set; }
    }
}