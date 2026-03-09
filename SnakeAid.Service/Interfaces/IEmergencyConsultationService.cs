using SnakeAid.Core.Requests.Consultation;
using SnakeAid.Core.Responses.Consultation;
using System;
using System.Threading.Tasks;

namespace SnakeAid.Service.Interfaces;

public interface IEmergencyConsultationService
{
    Task<EmergencyConsultationRequestResponse> CreateEmergencyRequestAsync(Guid requesterId, CreateEmergencyConsultationRequest request);
    Task<EmergencyConsultationRequestResponse> AcceptEmergencyRequestAsync(Guid requestId, Guid expertId);
    Task<EmergencyConsultationRequestResponse> RejectEmergencyRequestAsync(Guid requestId, Guid expertId);
}
