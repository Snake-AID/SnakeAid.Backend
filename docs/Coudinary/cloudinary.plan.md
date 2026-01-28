# Cloudinary Integration - Implementation Plan

## Scope for This Turn (Cloudinary-Centric)
- Focus on Cloudinary foundation only: configuration, service abstraction, and upload endpoints.
- Do not implement database persistence such as `ReportMedia` in this turn.
- Keep the design compatible with future persistence, but defer it to a later phase.

## Current State (SnakeAid)
- `CloudinaryDotNet` is already referenced in `Directory.Packages.props` and `SnakeAid.Service/SnakeAid.Service.csproj`.
- There is no Cloudinary configuration section in `SnakeAid.Api/appsettings.Example.json` or `SnakeAid.Api/appsettings.json`.
- There is no `ICloudinaryService` or `CloudinaryService` implementation yet.
- There are no media upload controllers yet.
- Media URLs are already modeled in the domain layer:
  - `SnakeAid.Core/Domains/ReportMedia.cs`
  - `SnakeAid.Core/Domains/LibraryMedia.cs`
  - `SnakeAid.Core/Domains/Account.cs` (`AvatarUrl`)
  - `SnakeAid.Core/Domains/ExpertCertificate.cs` (`CertificateUrl`)
  - `SnakeAid.Core/Domains/ChatMessage.cs` (`AttachmentUrl`)
  - `SnakeAid.Core/Domains/FilterOption.cs` (`OptionImageUrl`)
- Scrutor scanning in `SnakeAid.Api/Program.cs` will automatically register classes ending with `Service` as their implemented interfaces.
- The authenticated user ID is available through `ClaimTypes.NameIdentifier` (see `SnakeAid.Api/Controllers/BaseController.cs`).
- File validation can be reused via `SnakeAid.Core/Validators/ValidateFileAttribute.cs`.
- Exceptions should preferably use `ApiException` types so the global middleware can produce consistent responses (see `SnakeAid.Core/Exceptions/ApiException.cs` and `SnakeAid.Core/Middlewares/ApiExceptionHandlerMiddleware.cs`).

## Reference Pattern (EZYFIX)
EZYFIX uses a simple but effective integration:
- `ICloudinaryService` interface.
- `CloudinaryService` constructs a `Cloudinary` client from `IConfiguration` keys under `Cloudinary:*`.
- Upload methods validate file extension and size, then return `SecureUrl`.
- Scrutor scanning registers the service via the `Program.cs` scan.

Relevant reference files:
- `D:\SourceCode\EZYFIX\EzyFix.aspnet\EzyFix.Service\Services\Interfaces\ICloudinaryService.cs`
- `D:\SourceCode\EZYFIX\EzyFix.aspnet\EzyFix.Service\Services\Implements\CloudinaryService.cs`
- `D:\SourceCode\EZYFIX\EzyFix.aspnet\EzyFix.API\Program.cs`
- `D:\SourceCode\EZYFIX\EzyFix.aspnet\EzyFix.API\appsettings.example.json`

## Proposed Design for SnakeAid

### 1) Configuration and Options Binding
Add a Cloudinary configuration section and a strongly-typed settings class.

Configuration shape:
```json
"Cloudinary": {
  "CloudName": "<cloud-name>",
  "ApiKey": "<api-key>",
  "ApiSecret": "<api-secret>",
  "BaseFolder": "snakeaid"
}
```

Notes:
- Environment variables follow the standard pattern: `Cloudinary__CloudName`, `Cloudinary__ApiKey`, `Cloudinary__ApiSecret`, `Cloudinary__BaseFolder`.
- Use `BaseFolder` to centralize naming and ensure folder consistency across environments.

### 2) Cloudinary Service Abstraction
Create a focused service that:
- Validates configuration at construction time.
- Validates files (extension and size).
- Derives user ID from `ClaimTypes.NameIdentifier` when needed.
- Uploads with safe defaults for AI and mobile clients.
- Returns a rich result (URL plus useful metadata).

Recommended service contract:
- `UploadImageAsync(IFormFile file, ClaimsPrincipal user, string domain, CancellationToken ct = default)`
- `UploadFileAsync(IFormFile file, ClaimsPrincipal user, string domain, CancellationToken ct = default)`

Recommended upload result model:
- `SecureUrl`
- `PublicId`
- `ResourceType`
- `Format`
- `Bytes`
- `Width`
- `Height`
- `Tags`

### 3) API Endpoints for Uploads
Add a dedicated controller to centralize uploads.

Recommended routes:
- `POST /api/media/upload-image`
- `POST /api/media/upload-file`

Design choices:
- Require authentication via `[Authorize]`.
- Use `[Consumes("multipart/form-data")]`.
- Reuse `ValidateFileAttribute` for baseline validation.
- Return `ApiResponse<T>` using `ApiResponseBuilder`.

### 4) Future Plan - Persistence Integration Points (Deferred)
Persistence is intentionally deferred to keep this iteration Cloudinary-centric and low-risk.

Future persistence targets:
- `ReportMedia`
- `LibraryMedia`
- `Account.AvatarUrl`
- `ExpertCertificate.CertificateUrl`
- `ChatMessage.AttachmentUrl`
- `FilterOption.OptionImageUrl`

## Implementation Phases

### Phase A - Cloudinary Foundation (Current Scope)
- Add `Cloudinary` config to appsettings example and environment docs.
- Add `CloudinarySettings`.
- Implement `ICloudinaryService` and `CloudinaryService`.
- Add `MediaController` upload endpoints.

### Phase B - ReportMedia Flow (Future Plan Only)
- Add a `ReportMediaService` (or similar) that uploads via `ICloudinaryService`.
- Persist a `ReportMedia` record via `IUnitOfWork<SnakeAidDbContext>`.
- Add an endpoint such as `POST /api/report-media/upload` that both uploads and persists.

### Phase C - Additional Domains (Future Plan Only)
- Apply the same upload pattern to avatars, library media, certificates, and chat attachments.

## Files to Create
- `SnakeAid.Core/Settings/CloudinarySettings.cs` (or extend `SnakeAid.Core/Settings/AppSettings.cs`)
- `SnakeAid.Core/Requests/Media/UploadImageRequest.cs`
- `SnakeAid.Core/Requests/Media/UploadFileRequest.cs`
- `SnakeAid.Core/Responses/Media/CloudinaryUploadResult.cs`
- `SnakeAid.Service/Interfaces/ICloudinaryService.cs`
- `SnakeAid.Service/Implements/CloudinaryService.cs`
- `SnakeAid.Api/Controllers/MediaController.cs`

## Future Plan Files (Do Not Implement in This Turn)
- `SnakeAid.Core/Requests/Media/UploadReportMediaRequest.cs`
- `SnakeAid.Service/Interfaces/IReportMediaService.cs`
- `SnakeAid.Service/Implements/ReportMediaService.cs`
- `SnakeAid.Api/Controllers/ReportMediaController.cs`

## Files to Modify
- `SnakeAid.Api/appsettings.Example.json` (add `Cloudinary` section)
- `SnakeAid.Api/appsettings.json` (local dev values only, or leave empty and use env vars)
- `SnakeAid.Api/DI/DependencyInjection.cs`

Recommended DI changes:
- Bind options: `services.Configure<CloudinarySettings>(configuration.GetSection("Cloudinary"));`
- Validate settings at startup and fail fast if missing.

## Cloudinary Upload Defaults (Recommended)
For snake recognition images, avoid destructive cropping and keep aspect ratio.

Recommended transformation for images:
- Width limit: 1600
- Crop mode: `limit`
- Quality: `auto`
- Format: `auto`

This reduces payload size while preserving details for AI.

## Risks and Mitigations
- Missing credentials causes runtime errors.
  - Mitigation: validate config at startup and throw a clear `InvalidOperationException` or `BadRequestException`.
- URL length constraints may be tight for some transformed URLs.
  - Mitigation: consider raising URL length limits to 2000 where possible.
- Current exception middleware maps only `ApiException` and `InvalidOperationException` explicitly.
  - Mitigation: use `BadRequestException`, `ValidationException`, or `InvalidOperationException` instead of raw `ArgumentException`.
- Deleting Cloudinary assets later will be harder if only the URL is stored.
  - Mitigation: include and persist `PublicId` in a follow-up migration if deletion is needed.

## Success Criteria
- Upload endpoints return a valid `secure_url`.
- No database schema changes are required in this phase.
- Folder and tagging conventions are consistent and ready for future persistence.
