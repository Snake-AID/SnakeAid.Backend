using System.Security.Claims;
using System.Text.RegularExpressions;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SnakeAid.Core.Exceptions;
using SnakeAid.Core.Responses.Media;
using SnakeAid.Core.Settings;
using SnakeAid.Service.Interfaces;

namespace SnakeAid.Service.Implements;

public class CloudinaryService : ICloudinaryService
{
    private static readonly string[] AllowedImageExtensions = { ".jpg", ".jpeg", ".png", ".webp" };
    private static readonly string[] AllowedFileExtensions =
    {
        ".jpg", ".jpeg", ".png", ".webp",
        ".pdf", ".txt", ".doc", ".docx"
    };

    private const long MaxImageSizeBytes = 10 * 1024 * 1024;
    private const long MaxFileSizeBytes = 100 * 1024 * 1024;

    private readonly Cloudinary _cloudinary;
    private readonly CloudinarySettings _settings;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<CloudinaryService> _logger;

    public CloudinaryService(
        IOptions<CloudinarySettings> options,
        IHostEnvironment environment,
        ILogger<CloudinaryService> logger)
    {
        _settings = options.Value;
        _environment = environment;
        _logger = logger;

        if (string.IsNullOrWhiteSpace(_settings.CloudName) ||
            string.IsNullOrWhiteSpace(_settings.ApiKey) ||
            string.IsNullOrWhiteSpace(_settings.ApiSecret))
        {
            throw new InvalidOperationException("Cloudinary settings are not configured properly.");
        }

        var account = new Account(_settings.CloudName, _settings.ApiKey, _settings.ApiSecret);
        _cloudinary = new Cloudinary(account);
    }

    public async Task<CloudinaryUploadResult> UploadImageAsync(
        IFormFile file,
        ClaimsPrincipal user,
        string domain,
        CancellationToken cancellationToken = default)
    {
        ValidateFile(file, AllowedImageExtensions, MaxImageSizeBytes, "image");

        var userId = GetUserId(user);
        var normalizedDomain = NormalizeDomain(domain, "uploads");
        var folder = BuildFolder(normalizedDomain, userId);
        var tags = BuildTags(normalizedDomain);
        var tagsValue = string.Join(",", tags);

        var publicId = BuildPublicId(file.FileName);

        await using var stream = file.OpenReadStream();

        var uploadParams = new ImageUploadParams
        {
            File = new FileDescription(file.FileName, stream),
            Folder = folder,
            PublicId = publicId,
            Overwrite = false,
            Tags = tagsValue,
            Transformation = new Transformation()
                .Width(1600)
                .Crop("limit")
                .Quality("auto")
                .FetchFormat("auto")
        };

        var uploadResult = await _cloudinary.UploadAsync(uploadParams);

        return MapUploadResult(uploadResult, folder, tags);
    }

    public async Task<CloudinaryUploadResult> UploadFileAsync(
        IFormFile file,
        ClaimsPrincipal user,
        string domain,
        CancellationToken cancellationToken = default)
    {
        ValidateFile(file, AllowedFileExtensions, MaxFileSizeBytes, "file");

        var userId = GetUserId(user);
        var normalizedDomain = NormalizeDomain(domain, "files");
        var folder = BuildFolder(normalizedDomain, userId);
        var tags = BuildTags(normalizedDomain);
        var tagsValue = string.Join(",", tags);

        var publicId = BuildPublicId(file.FileName);

        await using var stream = file.OpenReadStream();

        var uploadParams = new AutoUploadParams
        {
            File = new FileDescription(file.FileName, stream),
            Folder = folder,
            PublicId = publicId,
            Overwrite = false,
            Tags = tagsValue
        };

        var uploadResult = await _cloudinary.UploadAsync(uploadParams);

        return MapUploadResult(uploadResult, folder, tags);
    }

    private static void ValidateFile(IFormFile? file, IEnumerable<string> allowedExtensions, long maxSizeBytes, string kind)
    {
        if (file is null || file.Length == 0)
        {
            throw new ValidationException($"{kind.ToUpperInvariant()} upload requires a non-empty file.", new Dictionary<string, string[]>
            {
                ["file"] = new[] { "No file was uploaded." }
            });
        }

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(extension))
        {
            throw new ValidationException($"Unsupported {kind} extension '{extension}'.", new Dictionary<string, string[]>
            {
                ["file"] = new[] { $"Allowed extensions: {string.Join(", ", allowedExtensions)}" }
            });
        }

        if (file.Length > maxSizeBytes)
        {
            var maxMb = maxSizeBytes / (1024 * 1024);
            throw new ValidationException($"{kind.ToUpperInvariant()} exceeds the maximum size of {maxMb}MB.", new Dictionary<string, string[]>
            {
                ["file"] = new[] { $"Maximum size: {maxMb}MB." }
            });
        }
    }

    private static Guid GetUserId(ClaimsPrincipal user)
    {
        var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            throw new UnauthorizedException("User ID not found in token.");
        }

        return userId;
    }

    private string BuildFolder(string domain, Guid userId)
    {
        var baseFolder = string.IsNullOrWhiteSpace(_settings.BaseFolder) ? "snakeaid" : _settings.BaseFolder.Trim();
        var env = NormalizeSegment(_environment.EnvironmentName);

        return $"{NormalizeSegment(baseFolder)}/{env}/{domain}/{userId:D}";
    }

    private static string NormalizeDomain(string? domain, string defaultDomain)
    {
        var effective = string.IsNullOrWhiteSpace(domain) ? defaultDomain : domain;
        return NormalizeSegment(effective);
    }

    private static string NormalizeSegment(string? value)
    {
        var raw = string.IsNullOrWhiteSpace(value) ? "default" : value.Trim().ToLowerInvariant();
        raw = Regex.Replace(raw, "[^a-z0-9-_]", "-");
        raw = Regex.Replace(raw, "-{2,}", "-");
        return raw.Trim('-');
    }

    private static string BuildPublicId(string fileName)
    {
        var nameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
        var normalized = NormalizeSegment(nameWithoutExtension);
        if (normalized.Length > 80)
        {
            normalized = normalized[..80];
        }

        return $"{normalized}-{DateTime.UtcNow:yyyyMMddHHmmss}";
    }

    private IReadOnlyCollection<string> BuildTags(string domain)
    {
        return new[]
        {
            "snakeaid",
            NormalizeSegment(_environment.EnvironmentName),
            domain
        };
    }

    private CloudinaryUploadResult MapUploadResult(UploadResult uploadResult, string folder, IReadOnlyCollection<string> tags)
    {
        if (uploadResult.Error is not null)
        {
            _logger.LogError("Cloudinary upload failed: {Error}", uploadResult.Error.Message);
            throw new BadRequestException($"Cloudinary upload failed: {uploadResult.Error.Message}");
        }

        if (uploadResult.SecureUrl is null)
        {
            _logger.LogError("Cloudinary returned null SecureUrl: {@UploadResult}", uploadResult);
            throw new BadRequestException("Cloudinary upload did not return a valid secure URL.");
        }

        var resourceType = uploadResult switch
        {
            ImageUploadResult => "image",
            VideoUploadResult => "video",
            RawUploadResult => "raw",
            _ => "auto"
        };

        int? width = uploadResult is ImageUploadResult imageResult ? imageResult.Width : null;
        int? height = uploadResult is ImageUploadResult imageResult2 ? imageResult2.Height : null;

        return new CloudinaryUploadResult
        {
            SecureUrl = uploadResult.SecureUrl.ToString(),
            PublicId = uploadResult.PublicId ?? string.Empty,
            ResourceType = resourceType,
            Format = uploadResult.Format,
            Bytes = uploadResult.Bytes,
            Width = width,
            Height = height,
            Folder = folder,
            Tags = tags
        };
    }
}
