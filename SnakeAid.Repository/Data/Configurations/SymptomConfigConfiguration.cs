using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SnakeAid.Core.Domains;

namespace SnakeAid.Repository.Data.Configurations
{
    public class SymptomConfigConfiguration : IEntityTypeConfiguration<SymptomConfig>
    {
        public void Configure(EntityTypeBuilder<SymptomConfig> builder)
        {
            builder.ToTable("SymptomConfigs");

            // Enum conversions
            builder.Property(c => c.UIHint)
                .HasConversion<int>()
                .IsRequired();

            builder.Property(c => c.Category)
                .HasConversion<int>()
                .IsRequired();

            // Relationship: SymptomConfig -> VenomType (optional)
            builder.HasOne(c => c.VenomType)
                .WithMany(v => v.SymptomConfigs)
                .HasForeignKey(c => c.VenomTypeId)
                .OnDelete(DeleteBehavior.SetNull);

            // Indexes
            builder.HasIndex(c => c.AttributeKey)
                .HasDatabaseName("IX_SymptomConfigs_AttributeKey");

            builder.HasIndex(c => c.VenomTypeId)
                .HasDatabaseName("IX_SymptomConfigs_VenomTypeId");

            builder.HasIndex(c => c.IsActive)
                .HasDatabaseName("IX_SymptomConfigs_IsActive");

            builder.HasIndex(c => c.DisplayOrder)
                .HasDatabaseName("IX_SymptomConfigs_DisplayOrder");
        }
    }
}
