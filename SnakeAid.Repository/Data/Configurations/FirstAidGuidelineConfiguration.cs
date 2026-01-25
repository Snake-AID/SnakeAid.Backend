using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SnakeAid.Core.Domains;

namespace SnakeAid.Repository.Data.Configurations
{
    public class FirstAidGuidelineConfiguration : IEntityTypeConfiguration<FirstAidGuideline>
    {
        public void Configure(EntityTypeBuilder<FirstAidGuideline> builder)
        {
            builder.ToTable("FirstAidGuidelines");

            // Enum conversion
            builder.Property(g => g.Type)
                .HasConversion<int>()
                .IsRequired();

            // Indexes
            builder.HasIndex(g => g.Type)
                .HasDatabaseName("IX_FirstAidGuidelines_Type");

            // Removed VenomTypeId index as FK is now in VenomType
        }
    }
}
