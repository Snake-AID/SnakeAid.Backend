namespace SnakeAid.Core.Domains
{
    public class SpeciesVenom
    {
        public int SnakeSpeciesId { get; set; }
        public int VenomTypeId { get; set; }

        public VenomType VenomType { get; set; }
        public SnakeSpecies SnakeSpecies { get; set; }
    }
}