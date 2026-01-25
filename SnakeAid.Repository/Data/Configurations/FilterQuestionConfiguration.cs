using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SnakeAid.Core.Domains;

namespace SnakeAid.Repository.Data.Configurations
{
    public class FilterQuestionConfiguration : IEntityTypeConfiguration<FilterQuestion>
    {
        public void Configure(EntityTypeBuilder<FilterQuestion> builder)
        {
            builder.ToTable("FilterQuestions");

            // Relationship: FilterQuestion -> FilterOptions (1-N)
            builder.HasMany(q => q.FilterOptions)
                .WithOne(o => o.Question)
                .HasForeignKey(o => o.QuestionId)
                .OnDelete(DeleteBehavior.Cascade);

            // Indexes
            builder.HasIndex(q => q.IsActive)
                .HasDatabaseName("IX_FilterQuestions_IsActive");
        }
    }
}
