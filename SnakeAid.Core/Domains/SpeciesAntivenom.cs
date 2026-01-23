namespace SnakeAid.Core.Domains
{
    public class SpeciesAntivenom
    {
        public int Id { get; set; }
        public int SnakeSpeciesId { get; set; }
        public int AntivenomId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;

        public SnakeSpecies SnakeSpecies { get; set; }
        public Antivenom Antivenom { get; set; }
    }
}