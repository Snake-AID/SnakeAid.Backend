using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace SnakeAid.Core.Domains
{
    public class ExpertCertificate : BaseEntity, IHasReportMedia
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid ExpertId { get; set; }

        [Required]
        [StringLength(250)]
        public string CertificateName { get; set; }

        [Required]
        [StringLength(250)]
        public string IssuingOrganization { get; set; }

        public DateTime? IssueDate { get; set; }

        public DateTime? ExpiryDate { get; set; }

        [Required]
        public string CertificateUrl { get; set; } = "";

        [Required]
        public VerificationStatus VerificationStatus { get; set; } = VerificationStatus.Pending;

        [StringLength(500)]
        public string RejectionReason { get; set; } = "";

        [NotMapped]
        public ICollection<ReportMedia> Media { get; set; } = new List<ReportMedia>();
    }

    public enum VerificationStatus
    {
        Pending = 0,
        Verified = 1,
        Rejected = 2,
    }
}
