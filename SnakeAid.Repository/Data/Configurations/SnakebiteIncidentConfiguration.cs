using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SnakeAid.Core.Domains;
using System.Text.Json;

namespace SnakeAid.Repository.Data.Configurations
{
    public class SnakebiteIncidentConfiguration : IEntityTypeConfiguration<SnakebiteIncident>
    {
        public void Configure(EntityTypeBuilder<SnakebiteIncident> builder)
        {
            builder.ToTable("SnakebiteIncidents");

            // Enum conversion
            builder.Property(i => i.Status)
                .HasConversion<int>()
                .IsRequired();

            builder.Property(i => i.IdentificationMethod)
                .HasConversion<int>()
                .IsRequired();

            // JSON conversions for JSONB columns
            // SymptomsReport - Allow NULL from DB, converter will return empty list
            builder.Property(i => i.SymptomsReport)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => string.IsNullOrWhiteSpace(v)
                        ? new List<ReportSymptom>()
                        : JsonSerializer.Deserialize<List<ReportSymptom>>(v, (JsonSerializerOptions?)null) ?? new List<ReportSymptom>())
                .HasColumnType("jsonb")
                .IsRequired(false);  // Allow NULL from DB

            // FilterAnswers - Nullable property
            builder.Property(i => i.FilterAnswers)
                .HasConversion(
                    v => v == null ? (string?)null : JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => string.IsNullOrWhiteSpace(v)
                        ? null
                        : JsonSerializer.Deserialize<FilterAnswerData>(v, (JsonSerializerOptions?)null))
                .HasColumnType("jsonb")
                .IsRequired(false);  // Allow NULL from DB

            // Relationship: Incident -> RescuerProfile (AssignedRescuer)
            builder.HasOne(i => i.AssignedRescuer)
                .WithMany()
                .HasForeignKey(i => i.AssignedRescuerId)
                .OnDelete(DeleteBehavior.SetNull);

            // Relationship với MemberProfile (User) đã config tại MemberProfileConfiguration

            // Relationship: Incident -> Missions (1-N)
            // An incident can have multiple missions due to rescuer abort and retry
            builder.HasMany(i => i.Missions)
                .WithOne(m => m.Incident)
                .HasForeignKey(m => m.IncidentId)
                .OnDelete(DeleteBehavior.Cascade);

            // Relationship: Incident -> DispatchRequests (1-N)
            builder.HasMany(i => i.DispatchRequests)
                .WithOne(r => r.Incident)
                .HasForeignKey(r => r.IncidentId)
                .OnDelete(DeleteBehavior.Cascade);

            // Relationship: Incident -> HandlingOperator (operator account)
            builder.HasOne(i => i.HandlingOperator)
                .WithMany()
                .HasForeignKey(i => i.HandlingOperatorId)
                .OnDelete(DeleteBehavior.SetNull);

            // Indexes
            builder.HasIndex(i => i.Status)
                .HasDatabaseName("IX_SnakebiteIncidents_Status");

            builder.HasIndex(i => i.UserId)
                .HasDatabaseName("IX_SnakebiteIncidents_UserId");

            builder.HasIndex(i => i.AssignedRescuerId)
                .HasDatabaseName("IX_SnakebiteIncidents_AssignedRescuerId");

            builder.HasIndex(i => i.HandlingOperatorId)
                .HasDatabaseName("IX_SnakebiteIncidents_HandlingOperatorId");

            builder.HasIndex(i => i.LocationCoordinates)
                .HasMethod("GIST")
                .HasDatabaseName("IX_SnakebiteIncidents_Location");

            // Ignore polymorphic collection so EF core doesn't create Shadow Foreign Keys
            builder.Ignore(i => i.Media);
        }
    }
}
