using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SnakeAid.Core.Domains;

namespace SnakeAid.Repository.Data.Configurations
{
    public class VenomTypeConfiguration : IEntityTypeConfiguration<VenomType>
    {
        public void Configure(EntityTypeBuilder<VenomType> builder)
        {
            builder.ToTable("VenomTypes");

            // Relationship: VenomType -> FirstAidGuideline (1-1)
            builder.HasOne(v => v.FirstAidGuideline)
                .WithOne()
                .HasForeignKey<VenomType>(v => v.FirstAidGuidelineId)
                .OnDelete(DeleteBehavior.Restrict);

            // Indexes
            builder.HasIndex(v => v.Name)
                .IsUnique()
                .HasDatabaseName("IX_VenomTypes_Name");

            builder.HasIndex(v => v.IsActive)
                .HasDatabaseName("IX_VenomTypes_IsActive");
        }
    }
}
