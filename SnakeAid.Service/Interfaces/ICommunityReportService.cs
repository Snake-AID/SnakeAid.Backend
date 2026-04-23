using SnakeAid.Core.Requests.CommunityReport;
using SnakeAid.Core.Responses.CommunityReport;

namespace SnakeAid.Service.Interfaces
{
    public interface ICommunityReportService
    {
        Task<CommunityReportResponse> CreateCommunityReportAsync(CreateCommunityReportRequest request, Guid userId);
        Task<CommunityReportResponse> GetCommunityReportByIdAsync(Guid id, Guid currentUserId, string currentUserRole);
        Task<List<CommunityReportResponse>> GetCommunityReportsAsync(Guid currentUserId, string currentUserRole);
        Task<CommunityReportResponse> UpdateCommunityReportAsync(Guid id, UpdateCommunityReportRequest request, Guid currentUserId, string currentUserRole);
        Task DeleteCommunityReportAsync(Guid id, Guid currentUserId, string currentUserRole);
        Task<List<CommunityReportResponse>> GetAllCommunityReportsAsync();
    }
}
