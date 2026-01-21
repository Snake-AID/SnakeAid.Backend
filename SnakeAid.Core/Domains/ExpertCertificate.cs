using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SnakeAid.Core.Domains
{
    public class ExpertCertificate : BaseEntity
    {
        public Guid Id { get; set; }
        public Guid ExpertId { get; set; }
        public string CertificateName { get; set; }
        public string IssuingOrganization { get; set; }
        public DateTime IssueDate { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public string CertificateUrl { get; set; } = "";
        public VerificationStatus VerificationStatus { get; set; } = VerificationStatus.Pending;
        public string RejectionReason { get; set; } = "";
    }

    public enum VerificationStatus
    {
        Pending = 0,
        Verified = 1,
        Rejected = 2,
    }
}