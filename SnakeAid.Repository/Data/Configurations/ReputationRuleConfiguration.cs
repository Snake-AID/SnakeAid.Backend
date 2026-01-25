using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SnakeAid.Core.Domains;

namespace SnakeAid.Repository.Data.Configurations
{
    public class ReputationRuleConfiguration : IEntityTypeConfiguration<ReputationRule>
    {
        public void Configure(EntityTypeBuilder<ReputationRule> builder)
        {
            builder.ToTable("ReputationRules");

            // Indexes
            builder.HasIndex(r => r.Name)
                .IsUnique()
                .HasDatabaseName("IX_ReputationRules_Name");
        }
    }
}
