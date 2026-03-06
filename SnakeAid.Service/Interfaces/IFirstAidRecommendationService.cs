using SnakeAid.Core.Responses.FirstAid;

namespace SnakeAid.Service.Interfaces;

/// <summary>
/// Service interface for first aid guideline recommendations
/// </summary>
public interface IFirstAidRecommendationService
{
    /// Lấy first aid guideline khuyến nghị cho snakebite incident
    Task<FirstAidRecommendationResponse> GetRecommendationForIncidentAsync(
        Guid incidentId,
        CancellationToken ct = default);

    /// Lấy first aid guideline cho một loài rắn cụ thể
    Task<FirstAidRecommendationResponse> GetRecommendationForSpeciesAsync(
        int snakeSpeciesId,
        CancellationToken ct = default);

    /// Lấy general first aid guideline (khi chưa xác định được rắn)
    Task<FirstAidRecommendationResponse> GetGeneralRecommendationAsync(
        CancellationToken ct = default);
}
