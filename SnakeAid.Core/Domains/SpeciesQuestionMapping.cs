namespace SnakeAid.Core.Domains
{
    public class SpeciesQuestionMapping
    {
        public int Id { get; set; }
        public int FilterQuestionId { get; set; }
        public int SnakeSpeciesId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;

        public FilterQuestion FilterQuestion { get; set; }
        public SnakeSpecies SnakeSpecies { get; set; }
    }
}