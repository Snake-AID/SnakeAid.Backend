using System;
using System.Threading.Tasks;
using SnakeAid.Core.Requests.Consultation;
using SnakeAid.Core.Responses.UserFeedback;

namespace SnakeAid.Service.Interfaces;

public interface IConsultationService
{
    Task EndConsultationAsync(Guid consultationId, Guid actorId);
    Task<UserFeedbackResponse> CreateConsultationReviewAsync(Guid consultationId, Guid raterId, CreateConsultationReviewRequest request);
}
