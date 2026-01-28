# Cloudinary Integration - Introduction

> [!NOTE]
> Goals
> - Standardize file and image storage using Cloudinary
> - Deliver a safe Cloudinary foundation that other domains can plug into later

## Overview
Cloudinary provides managed media storage, CDN delivery, and on-the-fly transformations. In SnakeAid, it will act as the canonical storage layer for user-uploaded images and files. In this turn, the backend will upload to Cloudinary and return the normalized upload result. Domain persistence (for example `ReportMedia`, `LibraryMedia`, and `Account.AvatarUrl`) is intentionally deferred to a future phase.

This project already references `CloudinaryDotNet` in central package management and in `SnakeAid.Service`, but the integration is not yet implemented.

## Scope for This Turn (Cloudinary-Centric)
- Implement Cloudinary configuration, service abstraction, and generic upload endpoints.
- Avoid domain-specific persistence to reduce risk and scope creep.

## Why Cloudinary for SnakeAid
- Reliable storage plus global CDN delivery for media-heavy flows.
- Easy generation of public HTTPS URLs (`secure_url`) suitable for AI pipelines and frontend display.
- Built-in transformations can reduce payload size while preserving recognition quality.
- Straightforward .NET SDK (`CloudinaryDotNet`) with async uploads.

## Primary Use Cases
- Generic upload foundation: clients can upload images and receive a stable public URL.
- Future domain integrations: identification media, library media, avatars, certificates, and attachments can plug into the foundation later.

## Integration Approach (Chosen)
SnakeAid will use server-side uploads via the backend API:
1. Client sends `multipart/form-data` to SnakeAid API.
2. API validates file type and size.
3. API uploads to Cloudinary using signed credentials.
4. API returns a normalized upload result. Persistence is a future plan.

This mirrors the structure used in EZYFIX:
- `EzyFix.Service/Services/Implements/CloudinaryService.cs`
- `EzyFix.Service/Services/Interfaces/ICloudinaryService.cs`
- `EzyFix.API/Program.cs` Scrutor scanning
- `EzyFix.API/appsettings.example.json` Cloudinary section

## Folder and Tagging Conventions
To keep media organized and make cleanup easier, use a consistent folder structure:
- Folder pattern: `snakeaid/{environment}/{domain}/{userId}`
- Example domains: `uploads`, `library-media`, `avatars`, `certificates`, `chat`, `files`
- Domain-specific folders such as `report-media` can be introduced in a later phase.

Recommended tags:
- `snakeaid`
- `{environment}`
- `{domain}`
- `{referenceType}` when applicable

## Key Design Notes for SnakeAid
- Identity user ID is available via `ClaimTypes.NameIdentifier` (not `UserId`).
- Existing file validation can be reused via `SnakeAid.Core/Validators/ValidateFileAttribute.cs`.
- Existing exception middleware maps `ApiException` types to consistent responses, so Cloudinary integration should prefer `BadRequestException`, `ValidationException`, and `UnauthorizedException` over raw exceptions.
