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

            // Relationship: WalletWithdraw -> Account (ProcessedByAdmin)
            builder.HasOne(w => w.ProcessedByAdmin)
                .WithMany()
                .HasForeignKey(w => w.ProcessedByAdminId)
                .OnDelete(DeleteBehavior.SetNull);

            // Indexes
            builder.HasIndex(w => w.Status)
                .HasDatabaseName("IX_WalletWithdraws_Status");

            builder.HasIndex(w => w.UserId)
                .HasDatabaseName("IX_WalletWithdraws_UserId");

            builder.HasIndex(w => w.WalletId)
                .HasDatabaseName("IX_WalletWithdraws_WalletId");

            builder.HasIndex(w => w.ProcessedByAdminId)
                .HasDatabaseName("IX_WalletWithdraws_ProcessedByAdminId");

            // PostgreSQL-specific column types
            builder.Property(w => w.AccountHolderName)
                .HasColumnType("character varying(150)")
                .HasMaxLength(150)
                .IsRequired();

            builder.Property(w => w.BankBin)
                .HasColumnType("character varying(6)")
                .HasMaxLength(6)
                .IsRequired(false);

            builder.Property(w => w.AdminNotes)
                .HasColumnType("character varying(1000)")
                .HasMaxLength(1000)
                .IsRequired(false);

            builder.Property(w => w.VietQrPayload)
                .HasColumnType("character varying(500)")
                .IsRequired(false);

            builder.Property(w => w.VietQrImageBase64)
                .HasColumnType("text")
                .IsRequired(false);
        }
    }
}
