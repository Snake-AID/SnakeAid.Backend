using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SnakeAid.Core.Domains;

namespace SnakeAid.Repository.Data.Configurations
{
    public class ExpertCertificateConfiguration : IEntityTypeConfiguration<ExpertCertificate>
    {
        public void Configure(EntityTypeBuilder<ExpertCertificate> builder)
        {
            builder.ToTable("ExpertCertificates");

            builder.Property(c => c.VerificationStatus)
                .HasConversion<int>()
                .IsRequired();

            builder.Property(c => c.RejectionReason)
                .IsRequired(false);

            builder.Property(c => c.IssueDate)
                .HasColumnType("timestamp with time zone");

            builder.Property(c => c.ExpiryDate)
                .HasColumnType("timestamp with time zone")
                .IsRequired(false);
        }
    }
}