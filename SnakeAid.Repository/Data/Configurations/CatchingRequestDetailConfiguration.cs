using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SnakeAid.Core.Domains;

namespace SnakeAid.Repository.Data.Configurations
{
    public class CatchingRequestDetailConfiguration : IEntityTypeConfiguration<CatchingRequestDetail>
    {
        public void Configure(EntityTypeBuilder<CatchingRequestDetail> builder)
        {
            builder.ToTable("CatchingRequestDetails");

            // Relationship: Detail -> SnakeCatchingRequest
            builder.HasOne(d => d.SnakeCatchingRequest)
                .WithMany()
                .HasForeignKey(d => d.SnakeCatchingRequestId)
                .OnDelete(DeleteBehavior.Cascade);

            // Relationship: Detail -> SnakeSpecies
            builder.HasOne(d => d.SnakeSpecies)
                .WithMany()
                .HasForeignKey(d => d.SnakeSpeciesId)
                .OnDelete(DeleteBehavior.Restrict);

            // Indexes
            builder.HasIndex(d => d.SnakeCatchingRequestId)
                .HasDatabaseName("IX_CatchingRequestDetails_SnakeCatchingRequestId");

            builder.HasIndex(d => d.SnakeSpeciesId)
                .HasDatabaseName("IX_CatchingRequestDetails_SnakeSpeciesId");
        }
    }
}
