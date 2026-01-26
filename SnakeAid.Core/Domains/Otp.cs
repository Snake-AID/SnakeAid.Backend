using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnakeAid.Core.Domains
{
    public class Otp
    {
        [Key] public Guid Id { get; set; }
        [MaxLength(150), Required, EmailAddress] public string Email { get; set; } = null!;
        // New: optional FK to Users to improve referential integrity and cascade delete
        public Guid? UserId { get; set; }
        public Account? User { get; set; }
        [MaxLength(10), Required] public string OtpCode { get; set; } = null!;
        public DateTime ExpirationTime { get; set; }
        public int AttemptLeft { get; set; }
    }

    public class OtpConfiguration : IEntityTypeConfiguration<Otp>
    {
        public void Configure(EntityTypeBuilder<Otp> builder)
        {
            builder.HasOne(o => o.User)
                .WithMany(u => u.Otps)
                .HasForeignKey(o => o.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
