using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SnakeAid.Core.Domains;

namespace SnakeAid.Repository.Data.Configurations
{
    public class ReputationTransactionConfiguration : IEntityTypeConfiguration<ReputationTransaction>
    {
        public void Configure(EntityTypeBuilder<ReputationTransaction> builder)
        {
            builder.ToTable("ReputationTransactions");

            // Relationship: ReputationTransaction -> Account (User)
            builder.HasOne(t => t.User)
                .WithMany()
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Relationship: ReputationTransaction -> ReputationRawEvent
            builder.HasOne(t => t.RawEvent)
                .WithMany()
                .HasForeignKey(t => t.RawEventId)
                .OnDelete(DeleteBehavior.Restrict);

            // Relationship: ReputationTransaction -> ReputationRule
            builder.HasOne(t => t.ReputationRule)
                .WithMany()
                .HasForeignKey(t => t.ReputationRuleId)
                .OnDelete(DeleteBehavior.Restrict);

            // Indexes
            builder.HasIndex(t => t.UserId)
                .HasDatabaseName("IX_ReputationTransactions_UserId");

            builder.HasIndex(t => t.RawEventId)
                .HasDatabaseName("IX_ReputationTransactions_RawEventId");
        }
    }
}
