using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SnakeAid.Core.Domains;

namespace SnakeAid.Repository.Data.Configurations
{
    public class FilterOptionConfiguration : IEntityTypeConfiguration<FilterOption>
    {
        public void Configure(EntityTypeBuilder<FilterOption> builder)
        {
            builder.ToTable("FilterOptions");

            // Indexes
            builder.HasIndex(o => o.QuestionId)
                .HasDatabaseName("IX_FilterOptions_QuestionId");

            builder.HasIndex(o => o.IsActive)
                .HasDatabaseName("IX_FilterOptions_IsActive");
        }
    }
}
