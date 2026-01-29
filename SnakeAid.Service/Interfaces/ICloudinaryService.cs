using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using SnakeAid.Core.Responses.Media;

namespace SnakeAid.Service.Interfaces;

public interface ICloudinaryService
{
    Task<CloudinaryUploadResult> UploadImageAsync(
        IFormFile file,
        ClaimsPrincipal user,
        string domain,
        CancellationToken cancellationToken = default);

    Task<CloudinaryUploadResult> UploadFileAsync(
        IFormFile file,
        ClaimsPrincipal user,
        string domain,
        CancellationToken cancellationToken = default);
}
