using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SnakeAid.Core.Domains;

namespace SnakeAid.Repository.Data.Configurations
{
    public class CatchingMissionDetailConfiguration : IEntityTypeConfiguration<CatchingMissionDetail>
    {
        public void Configure(EntityTypeBuilder<CatchingMissionDetail> builder)
        {
            builder.ToTable("CatchingMissionDetails");

            // Relationship: Detail -> SnakeCatchingMission
            builder.HasOne(d => d.SnakeCatchingMission)
                .WithMany()
                .HasForeignKey(d => d.SnakeCatchingMissionId)
                .OnDelete(DeleteBehavior.Cascade);

            // Relationship: Detail -> SnakeSpecies
            builder.HasOne(d => d.SnakeSpecies)
                .WithMany()
                .HasForeignKey(d => d.SnakeSpeciesId)
                .OnDelete(DeleteBehavior.Restrict);

            // Indexes
            builder.HasIndex(d => d.SnakeCatchingMissionId)
                .HasDatabaseName("IX_CatchingMissionDetails_SnakeCatchingMissionId");

            builder.HasIndex(d => d.SnakeSpeciesId)
                .HasDatabaseName("IX_CatchingMissionDetails_SnakeSpeciesId");
        }
    }
}
