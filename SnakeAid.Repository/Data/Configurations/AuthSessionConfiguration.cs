using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SnakeAid.Core.Domains;

namespace SnakeAid.Repository.Data.Configurations;

public class AuthSessionConfiguration : IEntityTypeConfiguration<AuthSession>
{
    public void Configure(EntityTypeBuilder<AuthSession> builder)
    {
        builder.ToTable("AuthSessions");

        builder.Property(x => x.RefreshTokenHash)
            .HasMaxLength(128)
            .IsConcurrencyToken()
            .IsRequired();

        builder.Property(x => x.RevokedReason)
            .HasMaxLength(200);

        builder.HasIndex(x => x.UserId)
            .HasDatabaseName("IX_AuthSessions_UserId");

        builder.HasIndex(x => x.RefreshTokenHash)
            .IsUnique()
            .HasDatabaseName("IX_AuthSessions_RefreshTokenHash");

        builder.HasIndex(x => new { x.UserId, x.RevokedAt, x.RefreshTokenExpiresAt })
            .HasDatabaseName("IX_AuthSessions_UserId_RevokedAt_RefreshTokenExpiresAt");

        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
