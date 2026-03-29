using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SnakeAid.Core.Domains;

namespace SnakeAid.Repository.Data.Configurations
{
    public class GeographicRegionConfiguration : IEntityTypeConfiguration<GeographicRegion>
    {
        public void Configure(EntityTypeBuilder<GeographicRegion> builder)
        {
            builder.ToTable("GeographicRegions");

            // PostGIS Polygon configuration (không thể dùng Data Annotation)
            builder.Property(g => g.Boundary)
                .HasColumnType("geography(Polygon, 4326)");

            // Indexes
            builder.HasIndex(g => g.Code)
                .IsUnique()
                .HasDatabaseName("IX_GeographicRegions_Code");

            builder.HasIndex(g => g.IsActive)
                .HasDatabaseName("IX_GeographicRegions_IsActive");

            builder.HasIndex(g => g.DisplayOrder)
                .HasDatabaseName("IX_GeographicRegions_DisplayOrder");

            // Spatial index for Boundary (PostGIS - không thể dùng Data Annotation)
            builder.HasIndex(g => g.Boundary)
                .HasDatabaseName("IX_GeographicRegions_Boundary")
                .HasMethod("gist");

            // Relationship: GeographicRegion -> RegionSnakeMappings
            builder.HasMany(g => g.RegionSnakeMappings)
                .WithOne(m => m.GeographicRegion)
                .HasForeignKey(m => m.GeographicRegionId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
