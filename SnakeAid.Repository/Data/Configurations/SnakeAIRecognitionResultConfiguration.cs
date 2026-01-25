using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SnakeAid.Core.Domains;

namespace SnakeAid.Repository.Data.Configurations
{
    public class SnakeAIRecognitionResultConfiguration : IEntityTypeConfiguration<SnakeAIRecognitionResult>
    {
        public void Configure(EntityTypeBuilder<SnakeAIRecognitionResult> builder)
        {
            builder.ToTable("SnakeAIRecognitionResults");

            // Enum conversion
            builder.Property(r => r.Status)
                .HasConversion<int>()
                .IsRequired();

            // Relationship: Result -> ReportMedia
            builder.HasOne(r => r.ReportMedia)
                .WithMany(m => m.AIRecognitionResults)
                .HasForeignKey(r => r.ReportMediaId)
                .OnDelete(DeleteBehavior.Cascade);

            // Relationship: Result -> AIModel
            builder.HasOne(r => r.AIModel)
                .WithMany(m => m.RecognitionResults)
                .HasForeignKey(r => r.AIModelId)
                .OnDelete(DeleteBehavior.Restrict);

            // Relationship: Result -> SnakeSpecies (DetectedSpecies)
            builder.HasOne(r => r.DetectedSpecies)
                .WithMany()
                .HasForeignKey(r => r.DetectedSpeciesId)
                .OnDelete(DeleteBehavior.SetNull);

            // Relationship: Result -> SnakeSpecies (ExpertCorrectedSpecies)
            builder.HasOne(r => r.ExpertCorrectedSpecies)
                .WithMany()
                .HasForeignKey(r => r.ExpertCorrectedSpeciesId)
                .OnDelete(DeleteBehavior.SetNull);

            // Relationship: Result -> Account (Expert)
            builder.HasOne(r => r.Expert)
                .WithMany()
                .HasForeignKey(r => r.ExpertId)
                .OnDelete(DeleteBehavior.SetNull);

            // Indexes
            builder.HasIndex(r => r.Status)
                .HasDatabaseName("IX_SnakeAIRecognitionResults_Status");

            builder.HasIndex(r => r.ReportMediaId)
                .HasDatabaseName("IX_SnakeAIRecognitionResults_ReportMediaId");

            builder.HasIndex(r => r.AIModelId)
                .HasDatabaseName("IX_SnakeAIRecognitionResults_AIModelId");

            builder.HasIndex(r => r.IsMapped)
                .HasDatabaseName("IX_SnakeAIRecognitionResults_IsMapped");
        }
    }
}
