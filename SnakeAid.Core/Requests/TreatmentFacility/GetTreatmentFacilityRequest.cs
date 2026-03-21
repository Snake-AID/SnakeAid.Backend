using SnakeAid.Core.Meta;

namespace SnakeAid.Core.Requests.TreatmentFacility;

public class GetTreatmentFacilityRequest : PaginationRequest
{
    public string? Name { get; set; }
    public bool? IsActive { get; set; }
    public int? AntivenomId { get; set; }
}