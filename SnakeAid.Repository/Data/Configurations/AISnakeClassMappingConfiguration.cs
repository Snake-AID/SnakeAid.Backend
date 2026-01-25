using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SnakeAid.Core.Domains;

namespace SnakeAid.Repository.Data.Configurations
{
    public class AISnakeClassMappingConfiguration : IEntityTypeConfiguration<AISnakeClassMapping>
    {
        public void Configure(EntityTypeBuilder<AISnakeClassMapping> builder)
        {
            builder.ToTable("AISnakeClassMappings");

            // Relationship: Mapping -> AIModel
            builder.HasOne(m => m.AIModel)
                .WithMany(ai => ai.ClassMappings)
                .HasForeignKey(m => m.AIModelId)
                .OnDelete(DeleteBehavior.Cascade);

            // Relationship: Mapping -> SnakeSpecies
            builder.HasOne(m => m.SnakeSpecies)
                .WithMany()
                .HasForeignKey(m => m.SnakeSpeciesId)
                .OnDelete(DeleteBehavior.Cascade);

            // Unique constraint
            builder.HasIndex(m => new { m.AIModelId, m.YoloClassId })
                .IsUnique()
                .HasDatabaseName("IX_AISnakeClassMappings_AIModelId_YoloClassId");

            builder.HasIndex(m => m.IsActive)
                .HasDatabaseName("IX_AISnakeClassMappings_IsActive");
        }
    }
}
