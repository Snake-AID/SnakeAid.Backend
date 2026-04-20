using Mapster;
using SnakeAid.Core.Domains;
using SnakeAid.Core.Responses.Consultation;

namespace SnakeAid.Core.Mappings
{
    public class AdminConsultationMapper : IRegister
    {
        public void Register(TypeAdapterConfig config)
        {
            config.NewConfig<Consultation, AdminConsultationResponse>()
                .Map(dest => dest.ConsultationId, src => src.Id)
                .Map(dest => dest.Type, src => src.Type.ToString())
                .Map(dest => dest.Status, src => src.Status.ToString())
                .Map(dest => dest.UserId, src => src.CallerId)
                .Map(dest => dest.UserName, src => src.Caller != null ? src.Caller.FullName : null)
                .Map(dest => dest.ExpertId, src => src.CalleeId)
                .Map(dest => dest.ExpertName, src => src.Callee != null ? src.Callee.FullName : null)
                .Map(dest => dest.RoomId, src => src.RoomId)
                .Map(dest => dest.StartTime, src => src.StartTime)
                .Map(dest => dest.EndTime, src => src.EndTime)
                .Map(dest => dest.CustomerReport, src => src.CustomerReport)
                .Map(dest => dest.CustomerReportSubmittedAt, src => src.CustomerReportSubmittedAt);

            config.NewConfig<ConsultationBooking, AdminConsultationResponse>()
                .Map(dest => dest.UserId, src => src.UserId)
                .Map(dest => dest.UserName, src => src.User != null ? src.User.FullName : null)
                .Map(dest => dest.ExpertId, src => src.ExpertId)
                .Map(dest => dest.ExpertName, src => src.Expert != null ? src.Expert.FullName : null)
                .Map(dest => dest.Price, src => src.Price)
                .Map(dest => dest.ProblemDescription, src => src.ProblemDescription)
                .Map(dest => dest.BookingId, src => src.Id)
                .Map(dest => dest.BookingStatus, src => src.Status.ToString())
                .Map(dest => dest.BookedAt, src => src.BookedAt)
                .Map(dest => dest.PaymentDeadline, src => src.PaymentDeadline)
                .Map(dest => dest.CancelledAt, src => src.CancelledAt)
                .Map(dest => dest.CancellationReason, src => src.CancellationReason)
                .Map(dest => dest.SlotStartTime, src => src.TimeSlot != null ? (DateTime?)src.TimeSlot.StartTime : null)
                .Map(dest => dest.SlotEndTime, src => src.TimeSlot != null ? (DateTime?)src.TimeSlot.EndTime : null);

            config.NewConfig<ConsultationPingRequest, AdminConsultationResponse>()
                .Map(dest => dest.UserId, src => src.RescuerId)
                .Map(dest => dest.UserName, src => src.Rescuer != null ? src.Rescuer.FullName : null)
                .Map(dest => dest.ExpertId, src => src.ExpertId)
                .Map(dest => dest.ExpertName, src => src.Expert != null ? src.Expert.FullName : null)
                .Map(dest => dest.EmergencyRequestId, src => src.Id)
                .Map(dest => dest.EmergencyRequestStatus, src => src.Status.ToString())
                .Map(dest => dest.RequestedAt, src => src.RequestedAt)
                .Map(dest => dest.RespondedAt, src => src.RespondedAt)
                .Map(dest => dest.ExpiresAt, src => src.ExpiresAt);
        }
    }
}
