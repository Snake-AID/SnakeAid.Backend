using System.Security.Claims;
using SnakeAid.Core.Meta;
using SnakeAid.Core.Requests.LibraryMedia;
using SnakeAid.Core.Responses.LibraryMedia;

namespace SnakeAid.Service.Interfaces;

public interface ILibraryMediaService
{
    Task<LibraryMediaResponse> CreateAsync(CreateLibraryMediaRequest request, ClaimsPrincipal user, CancellationToken ct = default);

    Task<LibraryMediaResponse> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<PagedData<LibraryMediaResponse>> GetPagedAsync(GetLibraryMediaRequest request, CancellationToken ct = default);

    Task<LibraryMediaResponse> UpdateAsync(Guid id, UpdateLibraryMediaRequest request, ClaimsPrincipal user, CancellationToken ct = default);

    Task DeleteAsync(Guid id, CancellationToken ct = default);
}