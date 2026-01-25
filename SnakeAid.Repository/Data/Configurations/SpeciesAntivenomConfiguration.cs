using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SnakeAid.Core.Domains;

namespace SnakeAid.Repository.Data.Configurations
{
    public class SpeciesAntivenomConfiguration : IEntityTypeConfiguration<SpeciesAntivenom>
    {
        public void Configure(EntityTypeBuilder<SpeciesAntivenom> builder)
        {
            builder.ToTable("SpeciesAntivenoms");

            // Relationship: SpeciesAntivenom -> SnakeSpecies
            builder.HasOne(sa => sa.SnakeSpecies)
                .WithMany(s => s.SpeciesAntivenoms)
                .HasForeignKey(sa => sa.SnakeSpeciesId)
                .OnDelete(DeleteBehavior.Cascade);

            // Relationship: SpeciesAntivenom -> Antivenom
            builder.HasOne(sa => sa.Antivenom)
                .WithMany(a => a.SpeciesAntivenoms)
                .HasForeignKey(sa => sa.AntivenomId)
                .OnDelete(DeleteBehavior.Cascade);

            // Unique constraint
            builder.HasIndex(sa => new { sa.SnakeSpeciesId, sa.AntivenomId })
                .IsUnique()
                .HasDatabaseName("IX_SpeciesAntivenoms_SnakeSpeciesId_AntivenomId");
        }
    }
}
