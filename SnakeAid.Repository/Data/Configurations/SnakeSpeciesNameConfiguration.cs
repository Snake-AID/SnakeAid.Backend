using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SnakeAid.Core.Domains;

namespace SnakeAid.Repository.Data.Configurations
{
    public class SnakeSpeciesNameConfiguration : IEntityTypeConfiguration<SnakeSpeciesName>
    {
        public void Configure(EntityTypeBuilder<SnakeSpeciesName> builder)
        {
            builder.ToTable("SnakeSpeciesNames");

            // Relationship với SnakeSpecies đã config tại SnakeSpeciesConfiguration

            // Indexes
            builder.HasIndex(n => n.Name)
                .HasDatabaseName("IX_SnakeSpeciesNames_Name");

            builder.HasIndex(n => n.Slug)
                .IsUnique()
                .HasDatabaseName("IX_SnakeSpeciesNames_Slug");

            builder.HasIndex(n => n.SnakeSpeciesId)
                .HasDatabaseName("IX_SnakeSpeciesNames_SnakeSpeciesId");
        }
    }
}
