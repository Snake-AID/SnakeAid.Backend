using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SnakeAid.Core.Domains;

namespace SnakeAid.Repository.Data.Configurations
{
    public class SpeciesVenomConfiguration : IEntityTypeConfiguration<SpeciesVenom>
    {
        public void Configure(EntityTypeBuilder<SpeciesVenom> builder)
        {
            builder.ToTable("SpeciesVenoms");

            // Composite primary key
            builder.HasKey(sv => new { sv.SnakeSpeciesId, sv.VenomTypeId });

            // Relationship: SpeciesVenom -> SnakeSpecies
            builder.HasOne(sv => sv.SnakeSpecies)
                .WithMany(s => s.SpeciesVenoms)
                .HasForeignKey(sv => sv.SnakeSpeciesId)
                .OnDelete(DeleteBehavior.Cascade);

            // Relationship: SpeciesVenom -> VenomType
            builder.HasOne(sv => sv.VenomType)
                .WithMany(v => v.SpeciesVenoms)
                .HasForeignKey(sv => sv.VenomTypeId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
