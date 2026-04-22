using SnakeAid.Core.Meta;
using SnakeAid.Core.Requests.ExpertCertificate;
using SnakeAid.Core.Responses.ExpertCertificate;

namespace SnakeAid.Service.Interfaces;

public interface IExpertCertificateService
{
    Task<ExpertCertificateResponse> CreateMyAsync(Guid expertId, CreateExpertCertificateRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ExpertCertificateResponse>> GetMyListAsync(Guid expertId, CancellationToken cancellationToken = default);
    Task<ExpertCertificateResponse> GetMyDetailAsync(Guid expertId, Guid certificateId, CancellationToken cancellationToken = default);
    Task<ExpertCertificateResponse> UpdateMyAsync(Guid expertId, Guid certificateId, UpdateExpertCertificateRequest request, CancellationToken cancellationToken = default);
    Task DeleteMyAsync(Guid expertId, Guid certificateId, CancellationToken cancellationToken = default);
    Task<ExpertCertificateResponse> AdminCreateAsync(AdminCreateExpertCertificateRequest request, CancellationToken cancellationToken = default);
    Task<PagedData<ExpertCertificateResponse>> AdminGetListAsync(AdminExpertCertificateQueryRequest request, CancellationToken cancellationToken = default);
    Task<ExpertCertificateResponse> AdminGetDetailAsync(Guid certificateId, CancellationToken cancellationToken = default);
    Task<ExpertCertificateResponse> AdminUpdateAsync(Guid certificateId, AdminUpdateExpertCertificateRequest request, CancellationToken cancellationToken = default);
    Task DeleteAdminAsync(Guid certificateId, CancellationToken cancellationToken = default);
}
