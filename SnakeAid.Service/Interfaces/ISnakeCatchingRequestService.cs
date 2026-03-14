using SnakeAid.Core.Requests.SnakeCatchingRequest;
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

        Task<CreateSnakeCatchingRequestResponse> AssignSnakeCatchingRequestAsync(Guid requestId, AssignSnakeCatchingRequestRequest request);

        Task<DetailSnakeCatchingRequestResponse> GetDetailAsync(Guid requestId);

        Task<List<ListSnakeCatchingRequestResponse>> GetAllRequestAsync();

        Task<DetailSnakeCatchingRequestResponse> CancelSnakeCatchingRequestAsync(Guid userId, Guid requestId, CancelSnakeCatchingRequestRequest request);

    }
}
