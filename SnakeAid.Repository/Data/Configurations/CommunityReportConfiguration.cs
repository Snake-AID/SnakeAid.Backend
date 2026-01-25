using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SnakeAid.Core.Domains;

namespace SnakeAid.Repository.Data.Configurations
{
    public class CommunityReportConfiguration : IEntityTypeConfiguration<CommunityReport>
    {
        public void Configure(EntityTypeBuilder<CommunityReport> builder)
        {
            builder.ToTable("CommunityReports");

            // Relationship: CommunityReport -> Account (User)
            builder.HasOne(r => r.User)
                .WithMany()
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Indexes
            builder.HasIndex(r => r.UserId)
                .HasDatabaseName("IX_CommunityReports_UserId");
        }
    }
}
