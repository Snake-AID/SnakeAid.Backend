namespace SnakeAid.Core.Domains
{
    public class SnakeSpeciesName : BaseEntity
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int SnakeSpeciesId { get; set; }
        public SnakeSpecies SnakeSpecies { get; set; }

    }
}