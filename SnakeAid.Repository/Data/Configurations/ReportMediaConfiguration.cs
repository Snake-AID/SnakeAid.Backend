using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SnakeAid.Core.Domains;

namespace SnakeAid.Repository.Data.Configurations
{
    public class ReportMediaConfiguration : IEntityTypeConfiguration<ReportMedia>
    {
        public void Configure(EntityTypeBuilder<ReportMedia> builder)
        {
            builder.ToTable("ReportMedias");

            // Enum conversions
            builder.Property(m => m.ReferenceType)
                .HasConversion<int>()
                .IsRequired();

            builder.Property(m => m.Purpose)
                .HasConversion<int>()
                .IsRequired();

            // Indexes
            builder.HasIndex(m => m.ReferenceId)
                .HasDatabaseName("IX_ReportMedias_ReferenceId");

            builder.HasIndex(m => m.ReferenceType)
                .HasDatabaseName("IX_ReportMedias_ReferenceType");

            builder.HasIndex(m => new { m.ReferenceId, m.ReferenceType })
                .HasDatabaseName("IX_ReportMedias_ReferenceId_ReferenceType");

            builder.HasIndex(m => m.RequiresAIProcessing)
                .HasDatabaseName("IX_ReportMedias_RequiresAIProcessing");
        }
    }
}
