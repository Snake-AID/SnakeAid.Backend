using System.ComponentModel.DataAnnotations;
using SnakeAid.Core.Domains;

namespace SnakeAid.Core.Requests.ExpertCertificate;

public class AdminUpdateExpertCertificateRequest
{
    [Required]
    [MaxLength(250)]
    public string CertificateName { get; set; } = string.Empty;

    [Required]
    [MaxLength(250)]
    public string IssuingOrganization { get; set; } = string.Empty;

    [Required]
    public DateTime IssueDate { get; set; }

    public DateTime? ExpiryDate { get; set; }

    [MinLength(1)]
    public List<Guid> ReportMediaIds { get; set; } = new();

    public VerificationStatus VerificationStatus { get; set; } = VerificationStatus.Pending;

    [MaxLength(1000)]
    public string? RejectionReason { get; set; }
}
