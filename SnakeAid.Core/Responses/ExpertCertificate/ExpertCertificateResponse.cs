using SnakeAid.Core.Domains;
using SnakeAid.Core.Responses.Media;

namespace SnakeAid.Core.Responses.ExpertCertificate;

public class ExpertCertificateResponse
{
    public Guid Id { get; set; }

    public Guid ExpertId { get; set; }

    public string CertificateName { get; set; } = string.Empty;

    public string IssuingOrganization { get; set; } = string.Empty;

    public DateTime IssueDate { get; set; }

    public DateTime? ExpiryDate { get; set; }

    public string CertificateUrl { get; set; } = string.Empty;

    public List<ReportMediaResponse> Media { get; set; } = new();

    public VerificationStatus VerificationStatus { get; set; }

    public string RejectionReason { get; set; } = string.Empty;
}
