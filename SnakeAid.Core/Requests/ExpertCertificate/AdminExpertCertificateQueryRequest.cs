using SnakeAid.Core.Domains;
using SnakeAid.Core.Meta;

namespace SnakeAid.Core.Requests.ExpertCertificate;

public class AdminExpertCertificateQueryRequest : PaginationRequest
{
    public Guid? ExpertId { get; set; }

    public VerificationStatus? VerificationStatus { get; set; }
}
