using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SnakeAid.Core.Requests.Consultation;
using SnakeAid.Core.Responses.Consultation;

namespace SnakeAid.Service.Interfaces;

public interface IBookingService
{
    Task<ConsultationBookingResponse> CreateScheduledBookingAsync(Guid userId, CreateConsultationBookingRequest request);
    Task<IEnumerable<ConsultationBookingResponse>> GetMyBookingsAsync(Guid userId);
    Task<IEnumerable<ConsultationBookingResponse>> GetExpertBookingsAsync(Guid expertId);
    Task<int> AutoCompleteElapsedScheduledConsultationsAsync(CancellationToken cancellationToken = default);
}
