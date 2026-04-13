using System;
using System.Threading.Tasks;
using SnakeAid.Core.Meta;
using SnakeAid.Core.Requests.Consultation;
using SnakeAid.Core.Responses.Consultation;
using SnakeAid.Core.Responses.UserFeedback;

namespace SnakeAid.Service.Interfaces;

public interface IConsultationService
{
    Task EndConsultationAsync(Guid consultationId, Guid actorId);
    Task<UserFeedbackResponse> CreateConsultationReviewAsync(Guid consultationId, Guid raterId, CreateConsultationReviewRequest request);
    Task<UserFeedbackResponse?> GetConsultationReviewAsync(Guid consultationId, Guid actorId);
    Task<PagingResponse<AdminConsultationResponse>> GetAllConsultationsForAdminAsync(AdminConsultationsQueryRequest query);
    Task<PagingResponse<MyConsultationResponse>> GetMyConsultationsAsync(Guid userId, MyConsultationsQueryRequest query);
    Task<PagingResponse<ExpertConsultationResponse>> GetExpertConsultationsAsync(Guid expertId, MyConsultationsQueryRequest query);
}
