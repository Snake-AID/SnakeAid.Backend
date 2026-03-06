using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SnakeAid.Core.Domains;
using SnakeAid.Core.Enums;
using SnakeAid.Core.Exceptions;
using SnakeAid.Core.Responses.FirstAid;
using SnakeAid.Core.Responses.SymptomConfig;
using SnakeAid.Repository.Data;
using SnakeAid.Repository.Interfaces;
using SnakeAid.Service.Interfaces;

namespace SnakeAid.Service.Implements;

/// <summary>
/// Service implementation for first aid guideline recommendations
/// </summary>
public class FirstAidRecommendationService : IFirstAidRecommendationService
{
    private readonly IUnitOfWork<SnakeAidDbContext> _unitOfWork;
    private readonly ILogger<FirstAidRecommendationService> _logger;

    public FirstAidRecommendationService(
        IUnitOfWork<SnakeAidDbContext> unitOfWork,
        ILogger<FirstAidRecommendationService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<FirstAidRecommendationResponse> GetRecommendationForIncidentAsync(
        Guid incidentId,
        CancellationToken ct = default)
    {
        var incident = await _unitOfWork.GetRepository<SnakebiteIncident>()
            .FirstOrDefaultAsync(
                predicate: i => i.Id == incidentId,
                include: query => query
                    .Include(i => i.IdentifiedSnakeSpecies!)
                        .ThenInclude(s => s.SpeciesVenoms)
                            .ThenInclude(sv => sv.VenomType)
                                .ThenInclude(v => v.FirstAidGuideline!)
                    .Include(i => i.AIRecognitionResult),
                cancellationToken: ct);

        if (incident == null)
        {
            _logger.LogWarning("Snakebite incident not found: {IncidentId}", incidentId);
            throw new NotFoundException("Snakebite incident not found.");
        }

        // Nếu chưa xác định được rắn → trả về general guideline
        if (incident.IdentifiedSnakeSpeciesId == null || incident.IdentifiedSnakeSpecies == null)
        {
            _logger.LogInformation("No snake identified for incident {IncidentId}, returning general guideline", incidentId);
            return await GetGeneralRecommendationAsync(ct);
        }

        // Đã xác định được rắn → lấy guideline cho species đó
        var response = await GetRecommendationForSpeciesAsync(
            incident.IdentifiedSnakeSpeciesId.Value,
            ct);

        // Thêm context về cách xác định
        response.IdentificationContext = new SnakeIdentificationContext
        {
            Method = incident.IdentificationMethod,
            IdentifiedAt = incident.IdentifiedAt ?? DateTime.UtcNow
        };

        // Nếu là AI detection, thêm confidence score
        if (incident.IdentificationMethod == SnakeIdentificationMethod.AIDetection
            && incident.AIRecognitionResult != null)
        {
            response.IdentificationContext.AIConfidence = (float)incident.AIRecognitionResult.Confidence;

            // Warning nếu confidence thấp
            if (incident.AIRecognitionResult.Confidence < 0.7m)
            {
                response.Warnings.Add($"AI confidence is low ({incident.AIRecognitionResult.Confidence:P0}). Consider manual verification.");
            }
        }

        return response;
    }

    public async Task<FirstAidRecommendationResponse> GetRecommendationForSpeciesAsync(
        int snakeSpeciesId,
        CancellationToken ct = default)
    {
        var species = await _unitOfWork.GetRepository<SnakeSpecies>()
            .FirstOrDefaultAsync(
                predicate: s => s.Id == snakeSpeciesId && s.IsActive,
                include: query => query
                    .Include(s => s.SpeciesVenoms)
                        .ThenInclude(sv => sv.VenomType)
                            .ThenInclude(v => v.FirstAidGuideline),
                cancellationToken: ct);

        if (species == null)
        {
            _logger.LogWarning("Snake species not found: {SpeciesId}", snakeSpeciesId);
            throw new NotFoundException("Snake species not found.");
        }

        FirstAidContent content;
        GuidelineSource source;
        int? guidelineId = null;
        string guidelineName;

        // Priority 1: Species-specific override
        if (species.FirstAidGuidelineOverride != null)
        {
            content = species.FirstAidGuidelineOverride.Content;
            source = GuidelineSource.SpeciesOverride;
            guidelineName = $"Species-specific guideline for {species.CommonName ?? species.ScientificName}";

            _logger.LogInformation("Using species override guideline for {SpeciesId}", snakeSpeciesId);
        }
        // Priority 2: VenomType guideline
        else if (species.SpeciesVenoms.Any())
        {
            var venomWithGuideline = species.SpeciesVenoms
                .Select(sv => sv.VenomType)
                .FirstOrDefault(v => v.FirstAidGuideline != null);

            if (venomWithGuideline?.FirstAidGuideline != null)
            {
                content = venomWithGuideline.FirstAidGuideline.Content;
                source = GuidelineSource.VenomType;
                guidelineId = venomWithGuideline.FirstAidGuideline.Id;
                guidelineName = venomWithGuideline.FirstAidGuideline.Name;

                _logger.LogInformation("Using venom type guideline {GuidelineId} for species {SpeciesId}",
                    guidelineId, snakeSpeciesId);
            }
            else
            {
                // Có venom nhưng không có guideline → dùng general
                _logger.LogWarning("Species {SpeciesId} has venoms but no associated guideline, using general",
                    snakeSpeciesId);
                return await GetGeneralRecommendationAsync(ct);
            }
        }
        // Priority 3: General guideline (nếu không có gì)
        else
        {
            _logger.LogInformation("No specific guideline for species {SpeciesId}, using general", snakeSpeciesId);
            return await GetGeneralRecommendationAsync(ct);
        }

        return new FirstAidRecommendationResponse
        {
            GuidelineId = guidelineId,
            GuidelineName = guidelineName,
            Content = content,
            Source = source,
            IdentifiedSnake = new SnakeSpeciesResponse
            {
                Id = species.Id,
                ScientificName = species.ScientificName,
                CommonName = species.CommonName ?? string.Empty,
                Slug = species.Slug,
                ImageUrl = species.ImageUrl,
                Description = species.Description ?? string.Empty,
                IdentificationSummary = species.IdentificationSummary ?? string.Empty,
                PrimaryVenomType = species.PrimaryVenomType,
                RiskLevel = species.RiskLevel,
                IsVenomous = species.IsVenomous,
                IsActive = species.IsActive
            }
        };
    }

    public async Task<FirstAidRecommendationResponse> GetGeneralRecommendationAsync(
        CancellationToken ct = default)
    {
        // Lấy general guideline từ database
        var generalGuideline = await _unitOfWork.GetRepository<FirstAidGuideline>()
            .FirstOrDefaultAsync(
                predicate: g => g.Type == GuidelineType.General,
                cancellationToken: ct);

        if (generalGuideline == null)
        {
            _logger.LogError("CRITICAL: No general first aid guideline found in database. Please run data seeder.");
            throw new NotFoundException(
                "General first aid guideline not found in database. " +
                "Please contact system administrator to seed baseline data.");
        }

        return new FirstAidRecommendationResponse
        {
            GuidelineId = generalGuideline.Id,
            GuidelineName = generalGuideline.Name,
            Content = generalGuideline.Content,
            Source = GuidelineSource.General,
            Warnings = new List<string>
            {
                "Snake species not identified. Using general guidelines.",
                "Seek medical attention immediately."
            }
        };
    }
}
