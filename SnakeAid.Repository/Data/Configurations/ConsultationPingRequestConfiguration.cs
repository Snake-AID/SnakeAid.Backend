using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SnakeAid.Core.Domains;

namespace SnakeAid.Repository.Data.Configurations
{
    public class ConsultationPingRequestConfiguration : IEntityTypeConfiguration<ConsultationPingRequest>
    {
        public void Configure(EntityTypeBuilder<ConsultationPingRequest> builder)
        {
            builder.ToTable("ConsultationPingRequests");

            // Enum conversion
            builder.Property(p => p.Status)
                .HasConversion<int>()
                .IsRequired();

            // Relationship: PingRequest -> Account (Rescuer)
            builder.HasOne(p => p.Rescuer)
                .WithMany()
                .HasForeignKey(p => p.RescuerId)
                .OnDelete(DeleteBehavior.Restrict);

            // Relationship: PingRequest -> Account (Expert)
            builder.HasOne(p => p.Expert)
                .WithMany()
                .HasForeignKey(p => p.ExpertId)
                .OnDelete(DeleteBehavior.Restrict);

            // Relationship: PingRequest -> RescueMission
            builder.HasOne(p => p.RescueMission)
                .WithMany()
                .HasForeignKey(p => p.RescueMissionId)
                .OnDelete(DeleteBehavior.SetNull);

            // Relationship: PingRequest -> Consultation
            builder.HasOne(p => p.Consultation)
                .WithMany()
                .HasForeignKey(p => p.ConsultationId)
                .OnDelete(DeleteBehavior.SetNull);

            // Indexes
            builder.HasIndex(p => p.Status)
                .HasDatabaseName("IX_ConsultationPingRequests_Status");

            builder.HasIndex(p => p.RescuerId)
                .HasDatabaseName("IX_ConsultationPingRequests_RescuerId");

            builder.HasIndex(p => p.ExpertId)
                .HasDatabaseName("IX_ConsultationPingRequests_ExpertId");

            builder.HasIndex(p => p.ExpiresAt)
                .HasDatabaseName("IX_ConsultationPingRequests_ExpiresAt");
        }
    }
}
