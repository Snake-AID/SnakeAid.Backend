using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SnakeAid.Core.Domains;

namespace SnakeAid.Repository.Data.Configurations
{
    public class ExpertSpecializationConfiguration : IEntityTypeConfiguration<ExpertSpecialization>
    {
        public void Configure(EntityTypeBuilder<ExpertSpecialization> builder)
        {
            builder.ToTable("ExpertSpecializations");

            // Relationship: ExpertSpecialization -> ExpertProfile
            builder.HasOne(es => es.Expert)
                .WithMany(ep => ep.Specializations)
                .HasForeignKey(es => es.ExpertId)
                .OnDelete(DeleteBehavior.Cascade);

            // Relationship: ExpertSpecialization -> Specialization
            builder.HasOne(es => es.Specialization)
                .WithMany(s => s.ExpertSpecializations)
                .HasForeignKey(es => es.SpecializationId)
                .OnDelete(DeleteBehavior.Cascade);

            // Unique constraint
            builder.HasIndex(es => new { es.ExpertId, es.SpecializationId })
                .IsUnique()
                .HasDatabaseName("IX_ExpertSpecializations_ExpertId_SpecializationId");
        }
    }
}
