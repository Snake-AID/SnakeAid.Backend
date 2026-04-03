using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SnakeAid.Core.Domains;

namespace SnakeAid.Repository.Data.Configurations
{
    public class WalletWithdrawAuditConfiguration : IEntityTypeConfiguration<WalletWithdrawAudit>
    {
        public void Configure(EntityTypeBuilder<WalletWithdrawAudit> builder)
        {
            builder.ToTable("WalletWithdrawAudits");

            builder.HasKey(a => a.Id);

            builder.Property(a => a.Action)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(a => a.ActorRole)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(a => a.DetailsJson)
                .HasMaxLength(4000);

            builder.HasIndex(a => a.WithdrawalId)
                .HasDatabaseName("IX_WalletWithdrawAudits_WithdrawalId");

            builder.HasIndex(a => a.Action)
                .HasDatabaseName("IX_WalletWithdrawAudits_Action");

            builder.HasOne(a => a.Withdrawal)
                .WithMany(w => w.Audits)
                .HasForeignKey(a => a.WithdrawalId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
