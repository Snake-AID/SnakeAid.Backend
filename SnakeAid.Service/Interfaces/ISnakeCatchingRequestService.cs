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

        Task<CreateSnakeCatchingRequestResponse> AcceptSnakeCatchingRequestAsync(Guid rescuerId, Guid requestId);
    }
}
