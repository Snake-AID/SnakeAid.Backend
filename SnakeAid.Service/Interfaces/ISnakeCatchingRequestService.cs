using SnakeAid.Core.Meta;
using SnakeAid.Core.Requests.SnakeCatchingRequest;
using SnakeAid.Core.Domains;
using SnakeAid.Core.Responses.SnakeCatchingRequest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnakeAid.Service.Interfaces
{
    public interface ISnakeCatchingRequestService
    {
        Task<CreateSnakeCatchingRequestResponse> CreateSnakeCatchingRequestAsync(Guid userId, CreateSnakeCatchingRequestRequest request);

        Task<CreateSnakeCatchingRequestResponse> ConfirmSnakeCatchingRequestAsync(Guid requestId);

        Task<CreateSnakeCatchingRequestResponse> AssignSnakeCatchingRequestAsync(Guid requestId, AssignSnakeCatchingRequestRequest request);

        Task<DetailSnakeCatchingRequestResponse> GetDetailAsync(Guid requestId);

        Task<List<ListSnakeCatchingRequestResponse>> GetAllRequestAsync(GetAllSnakeCatchingRequestsQuery? query = null);

        Task<PagedData<OperatorSnakeCatchingRequestSummaryResponse>> GetActiveRequestsAsync(
            IEnumerable<RequestStatus>? statuses,
            DateTimeOffset? since,
            DateTimeOffset? until,
            int page,
            int pageSize);

        Task<DetailSnakeCatchingRequestResponse> CancelSnakeCatchingRequestAsync(Guid userId, Guid requestId, CancelSnakeCatchingRequestRequest request);

    }
}
