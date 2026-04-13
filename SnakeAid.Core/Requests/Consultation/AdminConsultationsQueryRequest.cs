using SnakeAid.Core.Meta;

namespace SnakeAid.Core.Requests.Consultation;

public class AdminConsultationsQueryRequest : PaginationRequest
{
    public string? Status { get; set; }

    public string? Type { get; set; }
}
