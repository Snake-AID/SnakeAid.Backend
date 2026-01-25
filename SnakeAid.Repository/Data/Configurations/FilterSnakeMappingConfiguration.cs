using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SnakeAid.Core.Domains;

namespace SnakeAid.Repository.Data.Configurations
{
    public class FilterSnakeMappingConfiguration : IEntityTypeConfiguration<FilterSnakeMapping>
    {
        public void Configure(EntityTypeBuilder<FilterSnakeMapping> builder)
        {
            builder.ToTable("FilterSnakeMappings");

            // Relationship: FilterSnakeMapping -> FilterOption
            builder.HasOne(m => m.FilterOption)
                .WithMany(o => o.FilterSnakeMappings)
                .HasForeignKey(m => m.FilterOptionId)
                .OnDelete(DeleteBehavior.Cascade);

            // Relationship: FilterSnakeMapping -> SnakeSpecies
            builder.HasOne(m => m.SnakeSpecies)
                .WithMany(s => s.FilterSnakeMappings)
                .HasForeignKey(m => m.SnakeSpeciesId)
                .OnDelete(DeleteBehavior.Cascade);

            // Unique constraint
            builder.HasIndex(m => new { m.FilterOptionId, m.SnakeSpeciesId })
                .IsUnique()
                .HasDatabaseName("IX_FilterSnakeMappings_FilterOptionId_SnakeSpeciesId");

            builder.HasIndex(m => m.IsActive)
                .HasDatabaseName("IX_FilterSnakeMappings_IsActive");
        }
    }
}
