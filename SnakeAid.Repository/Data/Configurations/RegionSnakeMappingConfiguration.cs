using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SnakeAid.Core.Domains;

namespace SnakeAid.Repository.Data.Configurations
{
    public class RegionSnakeMappingConfiguration : IEntityTypeConfiguration<RegionSnakeMapping>
    {
        public void Configure(EntityTypeBuilder<RegionSnakeMapping> builder)
        {
            builder.ToTable("RegionSnakeMappings");

            // Relationship: RegionSnakeMapping -> GeographicRegion
            builder.HasOne(m => m.GeographicRegion)
                .WithMany(g => g.RegionSnakeMappings)
                .HasForeignKey(m => m.GeographicRegionId)
                .OnDelete(DeleteBehavior.Cascade);

            // Relationship: RegionSnakeMapping -> SnakeSpecies
            builder.HasOne(m => m.SnakeSpecies)
                .WithMany(s => s.RegionSnakeMappings)
                .HasForeignKey(m => m.SnakeSpeciesId)
                .OnDelete(DeleteBehavior.Cascade);

            // Unique constraint: Một loài rắn chỉ có 1 mapping duy nhất với 1 khu vực
            builder.HasIndex(m => new { m.GeographicRegionId, m.SnakeSpeciesId })
                .IsUnique()
                .HasDatabaseName("IX_RegionSnakeMappings_GeographicRegionId_SnakeSpeciesId");

            // Indexes for filtering
            builder.HasIndex(m => m.IsActive)
                .HasDatabaseName("IX_RegionSnakeMappings_IsActive");

            builder.HasIndex(m => m.CommonLevel)
                .HasDatabaseName("IX_RegionSnakeMappings_CommonLevel");

            builder.HasIndex(m => m.Priority)
                .HasDatabaseName("IX_RegionSnakeMappings_Priority");
        }
    }
}
