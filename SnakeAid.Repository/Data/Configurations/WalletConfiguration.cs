using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SnakeAid.Core.Domains;

namespace SnakeAid.Repository.Data.Configurations
{
    public class WalletConfiguration : IEntityTypeConfiguration<Wallet>
    {
        public void Configure(EntityTypeBuilder<Wallet> builder)
        {
            builder.ToTable("Wallets");

            // Relationship: Wallet -> Account
            builder.HasOne(w => w.Account)
                .WithMany()
                .HasForeignKey(w => w.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Unique constraint - mỗi user chỉ có 1 wallet
            builder.HasIndex(w => w.UserId)
                .IsUnique()
                .HasDatabaseName("IX_Wallets_UserId");
        }
    }
}
