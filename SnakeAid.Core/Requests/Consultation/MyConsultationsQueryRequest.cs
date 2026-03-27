using SnakeAid.Core.Meta;

namespace SnakeAid.Core.Requests.Consultation;

public class MyConsultationsQueryRequest : PaginationRequest
{
    /// <summary>
    /// Filter by consultation status: Ongoing, Completed. Null = all.
    /// </summary>
    public string? Status { get; set; }

    /// <summary>
    /// Filter by consultation type: Scheduled, Emergency. Null = all.
    /// </summary>
    public string? Type { get; set; }
}
