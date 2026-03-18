using SnakeAid.Core.Domains;

namespace SnakeAid.Core.Requests.SnakeCatchingRequest
{
    public class GetAllSnakeCatchingRequestsQuery
    {
        public Guid? UserId { get; set; }
        public Guid? HandlingOperatorId { get; set; }
        public Guid? AssignedRescuerId { get; set; }
        public RequestStatus? Status { get; set; }
    }
}