# Cloudinary Integration - Agent Prompt

Use this prompt to implement Cloudinary uploads in SnakeAid, mirroring the working pattern from EZYFIX but aligning with SnakeAid's current architecture, claims, and response conventions.

## Goals
- Implement a production-ready `ICloudinaryService` + `CloudinaryService`.
- Add authenticated upload endpoints under `/api/media`.
- Return consistent `ApiResponse<T>` results.
- Keep the first iteration minimal: no database migrations required.

## Scope Guard for This Turn
- Implement Cloudinary foundation only.
- Do not implement `ReportMedia` persistence or any upload-and-persist endpoints in this turn.
- Keep the result generic and reusable across domains.

## Important SnakeAid Context
- Scrutor scanning in `SnakeAid.Api/Program.cs` auto-registers classes ending with `Service` as implemented interfaces.
- User ID is available via `ClaimTypes.NameIdentifier` (see `SnakeAid.Api/Controllers/BaseController.cs`).
- Use `ApiException` types for consistent error mapping (see `SnakeAid.Core/Exceptions/ApiException.cs`).
- There is already a reusable file validator: `SnakeAid.Core/Validators/ValidateFileAttribute.cs`.

## Step 1 - Add Cloudinary Settings
Create a strongly-typed settings class.

Recommended location:
- New file: `SnakeAid.Core/Settings/CloudinarySettings.cs`

Recommended content:
```csharp
namespace SnakeAid.Core.Settings;

public class CloudinarySettings
{
    public string CloudName { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string ApiSecret { get; set; } = string.Empty;
    public string BaseFolder { get; set; } = "snakeaid";
}
```

## Step 2 - Bind and Validate Settings in DI
Update `SnakeAid.Api/DI/DependencyInjection.cs` inside `AddServices`.

Add:
```csharp
services.Configure<CloudinarySettings>(configuration.GetSection("Cloudinary"));

var cloudinarySettings = configuration.GetSection("Cloudinary").Get<CloudinarySettings>();
if (cloudinarySettings is null ||
    string.IsNullOrWhiteSpace(cloudinarySettings.CloudName) ||
    string.IsNullOrWhiteSpace(cloudinarySettings.ApiKey) ||
    string.IsNullOrWhiteSpace(cloudinarySettings.ApiSecret))
{
    throw new InvalidOperationException("Cloudinary settings are not configured properly.");
}
```

Also add the required `using`:
```csharp
using SnakeAid.Core.Settings;
```

## Step 3 - Add Upload Contracts
Create simple request and response models.

Suggested files:
- `SnakeAid.Core/Requests/Media/UploadImageRequest.cs`
- `SnakeAid.Core/Requests/Media/UploadFileRequest.cs`
- `SnakeAid.Core/Responses/Media/CloudinaryUploadResult.cs`

Suggested response model:
```csharp
namespace SnakeAid.Core.Responses.Media;

public class CloudinaryUploadResult
{
    public string SecureUrl { get; set; } = string.Empty;
    public string PublicId { get; set; } = string.Empty;
    public string ResourceType { get; set; } = string.Empty;
    public string? Format { get; set; }
    public long? Bytes { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }
    public string Folder { get; set; } = string.Empty;
    public IReadOnlyCollection<string> Tags { get; set; } = Array.Empty<string>();
}
```

Suggested image request:
```csharp
namespace SnakeAid.Core.Requests.Media;

public class UploadImageRequest
{
    public IFormFile File { get; set; } = default!;
    public string Domain { get; set; } = "uploads";
}
```

Suggested file request:
```csharp
namespace SnakeAid.Core.Requests.Media;

public class UploadFileRequest
{
    public IFormFile File { get; set; } = default!;
    public string Domain { get; set; } = "files";
}
```

## Step 4 - Add ICloudinaryService
Create:
- `SnakeAid.Service/Interfaces/ICloudinaryService.cs`

Suggested contract:
```csharp
using System.Security.Claims;
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
```

## Step 5 - Implement CloudinaryService
Create:
- `SnakeAid.Service/Implements/CloudinaryService.cs`

Implementation requirements:
- Use `CloudinaryDotNet` and `CloudinaryDotNet.Actions`.
- Read and validate `CloudinarySettings`.
- Extract user ID from `ClaimTypes.NameIdentifier`.
- Validate extension and size limits.
- Use non-destructive transformations for snake recognition images.
- Prefer `BadRequestException`, `ValidationException`, and `UnauthorizedException` instead of raw exceptions.

Suggested defaults:
- Allowed image extensions: `.jpg`, `.jpeg`, `.png`, `.webp`
- Max image size: 10 MB
- Max file size: 100 MB

Suggested image transformation:
```csharp
new Transformation()
    .Width(1600)
    .Crop("limit")
    .Quality("auto")
    .FetchFormat("auto");
```

Folder convention:
- `snakeaid/{environment}/{domain}/{userId}`
- Environment can be derived from `IHostEnvironment.EnvironmentName`.

## Step 6 - Add MediaController
Create:
- `SnakeAid.Api/Controllers/MediaController.cs`

Controller requirements:
- Route: `[Route("api/media")]`
- Use `[Authorize]`
- Use `[Consumes("multipart/form-data")]` on upload actions
- Use `ValidateFileAttribute` for quick validation
- Use `ApiResponseBuilder.BuildSuccessResponse(...)`

Suggested endpoints:
- `POST /api/media/upload-image`
- `POST /api/media/upload-file`

Suggested structure:
```csharp
[ApiController]
[Authorize]
[Route("api/media")]
public class MediaController : BaseController<MediaController>
{
    private readonly ICloudinaryService _cloudinaryService;

    public MediaController(
        ILogger<MediaController> logger,
        IHttpContextAccessor httpContextAccessor,
        IMapper mapper,
        ICloudinaryService cloudinaryService)
        : base(logger, httpContextAccessor, mapper)
    {
        _cloudinaryService = cloudinaryService;
    }

    [HttpPost("upload-image")]
    [Consumes("multipart/form-data")]
    [ValidateFile(maxSizeInMB: 10, formFieldName: "file")]
    public async Task<IActionResult> UploadImage([FromForm] UploadImageRequest request, CancellationToken ct)
    {
        var result = await _cloudinaryService.UploadImageAsync(request.File, User, request.Domain, ct);
        return Ok(ApiResponseBuilder.BuildSuccessResponse(result, "Image uploaded successfully."));
    }
}
```

## Step 7 - Add Cloudinary Config to AppSettings Example
Update:
- `SnakeAid.Api/appsettings.Example.json`

Add:
```json
"Cloudinary": {
  "CloudName": "YOUR_CLOUD_NAME",
  "ApiKey": "YOUR_API_KEY",
  "ApiSecret": "YOUR_API_SECRET",
  "BaseFolder": "snakeaid"
}
```

## Step 8 - Manual Validation Checklist
After implementing:
1. Run the API locally.
2. Authenticate via `/api/auth/login`.
3. Use Swagger to call `/api/media/upload-image`.
4. Confirm a valid `secure_url` is returned.
5. Open the returned URL in a browser.

## Future Plan - ReportMedia Persistence (Do Not Implement in This Turn)
Add an upload-and-persist endpoint for `ReportMedia` later, once the Cloudinary foundation is stable.

Minimal future shape:
- Request includes `ReferenceId`, `ReferenceType`, `Purpose`, and `File`.
- Service uploads to Cloudinary and inserts a `ReportMedia` record via `IUnitOfWork<SnakeAidDbContext>`.
