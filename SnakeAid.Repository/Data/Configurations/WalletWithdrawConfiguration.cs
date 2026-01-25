using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SnakeAid.Core.Domains;

namespace SnakeAid.Repository.Data.Configurations
{
    public class WalletWithdrawConfiguration : IEntityTypeConfiguration<WalletWithdraw>
    {
        public void Configure(EntityTypeBuilder<WalletWithdraw> builder)
        {
            builder.ToTable("WalletWithdraws");

            // Enum conversion
            builder.Property(w => w.Status)
                .HasConversion<int>()
                .IsRequired();

            // Relationship: WalletWithdraw -> Account (User)
            builder.HasOne(w => w.User)
                .WithMany()
                .HasForeignKey(w => w.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Relationship: WalletWithdraw -> Wallet
            builder.HasOne(w => w.Wallet)
                .WithMany()
                .HasForeignKey(w => w.WalletId)
                .OnDelete(DeleteBehavior.Restrict);

            // Indexes
            builder.HasIndex(w => w.Status)
                .HasDatabaseName("IX_WalletWithdraws_Status");

            builder.HasIndex(w => w.UserId)
                .HasDatabaseName("IX_WalletWithdraws_UserId");

            builder.HasIndex(w => w.WalletId)
                .HasDatabaseName("IX_WalletWithdraws_WalletId");
        }
    }
}
