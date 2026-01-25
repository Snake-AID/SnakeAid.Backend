using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SnakeAid.Core.Domains;

namespace SnakeAid.Repository.Data.Configurations
{
    public class TreatmentFacilityConfiguration : IEntityTypeConfiguration<TreatmentFacility>
    {
        public void Configure(EntityTypeBuilder<TreatmentFacility> builder)
        {
            builder.ToTable("TreatmentFacilities");

            // Indexes
            builder.HasIndex(f => f.Name)
                .HasDatabaseName("IX_TreatmentFacilities_Name");

            builder.HasIndex(f => f.IsActive)
                .HasDatabaseName("IX_TreatmentFacilities_IsActive");
        }
    }
}
