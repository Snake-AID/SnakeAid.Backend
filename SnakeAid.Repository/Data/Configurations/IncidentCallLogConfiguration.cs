using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SnakeAid.Core.Domains;

namespace SnakeAid.Repository.Data.Configurations
{
    public class IncidentCallLogConfiguration : IEntityTypeConfiguration<IncidentCallLog>
    {
        public void Configure(EntityTypeBuilder<IncidentCallLog> builder)
        {
            builder.ToTable("IncidentCallLogs");

            builder.Property(c => c.Outcome)
                .HasConversion<int>()
                .IsRequired();

            builder.HasOne(c => c.Incident)
                .WithMany()
                .HasForeignKey(c => c.IncidentId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(c => c.Operator)
                .WithMany()
                .HasForeignKey(c => c.OperatorId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(c => c.IncidentId)
                .HasDatabaseName("IX_IncidentCallLogs_IncidentId");

            builder.HasIndex(c => c.OperatorId)
                .HasDatabaseName("IX_IncidentCallLogs_OperatorId");

            builder.HasIndex(c => c.CalledAt)
                .HasDatabaseName("IX_IncidentCallLogs_CalledAt");

            builder.HasIndex(c => c.Outcome)
                .HasDatabaseName("IX_IncidentCallLogs_Outcome");
        }
    }
}
