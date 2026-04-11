using Microsoft.Extensions.Logging;
using SnakeAid.Core.Domains;
using SnakeAid.Core.Exceptions;
using SnakeAid.Core.Requests.Consultation;
using SnakeAid.Core.Responses.Consultation;
using SnakeAid.Repository.Data;
using SnakeAid.Repository.Interfaces;
using SnakeAid.Service.Interfaces;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SnakeAid.Service.Implements
{
    public class EmergencyConsultationService : IEmergencyConsultationService
    {
        private static readonly TimeSpan DefaultRequestTtl = TimeSpan.FromMinutes(2);

        private readonly IUnitOfWork<SnakeAidDbContext> _unitOfWork;
        private readonly IExpertEmergencyNotificationService _notificationService;
        private readonly IConsultationPaymentService _consultationPaymentService;
        private readonly ILogger<EmergencyConsultationService> _logger;

        public EmergencyConsultationService(
            IUnitOfWork<SnakeAidDbContext> unitOfWork,
            IExpertEmergencyNotificationService notificationService,
            IConsultationPaymentService consultationPaymentService,
            ILogger<EmergencyConsultationService> logger)
        {
            _unitOfWork = unitOfWork;
            _notificationService = notificationService;
            _consultationPaymentService = consultationPaymentService;
            _logger = logger;
        }

        public async Task<EmergencyConsultationRequestResponse> CreateEmergencyRequestAsync(Guid requesterId, CreateEmergencyConsultationRequest request)
        {
            var now = DateTime.UtcNow;
            var expertProfile = await _unitOfWork.GetRepository<ExpertProfile>().FirstOrDefaultAsync(
                predicate: p => p.AccountId == request.ExpertId && p.Account.IsActive);

            if (expertProfile == null)
            {
                throw new NotFoundException("Selected expert was not found.");
            }

            var activeRequest = await _unitOfWork.GetRepository<ConsultationPingRequest>().FirstOrDefaultAsync(
                predicate: p =>
                    p.RescuerId == requesterId
                    && p.ExpertId == request.ExpertId
                    && (p.Status == ConsultationPingStatus.PendingPayment || p.Status == ConsultationPingStatus.PendingExpertResponse)
                    && (!p.ExpiresAt.HasValue || p.ExpiresAt > now));

            if (activeRequest != null)
            {
                throw new ConflictException("An active emergency request already exists for this expert.");
            }

            var ping = new ConsultationPingRequest
            {
                Id = Guid.NewGuid(),
                RescuerId = requesterId,
                ExpertId = request.ExpertId,
                Status = ConsultationPingStatus.PendingPayment,
                RequestedAt = now,
                ExpiresAt = now.Add(DefaultRequestTtl)
            };

            await _unitOfWork.GetRepository<ConsultationPingRequest>().InsertAsync(ping);
            await _unitOfWork.CommitAsync();

            _logger.LogInformation(
                "Emergency consultation request created. RequestId={RequestId}, RequesterId={RequesterId}, ExpertId={ExpertId}",
                ping.Id,
                requesterId,
                request.ExpertId);

            return ToResponse(ping, null);
        }

        public async Task<EmergencyConsultationRequestResponse> AcceptEmergencyRequestAsync(Guid requestId, Guid expertId)
        {
            var now = DateTime.UtcNow;

            var result = await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                var pingRepo = _unitOfWork.GetRepository<ConsultationPingRequest>();
                var ping = await pingRepo.FirstOrDefaultAsync(
                    predicate: p => p.Id == requestId,
                    asNoTracking: false);

                if (ping == null)
                {
                    throw new NotFoundException("Emergency consultation request was not found.");
                }

            if (ping.ExpertId != expertId)
            {
                throw new ForbiddenException("You are not allowed to accept this emergency request.");
            }

                EnsurePendingStateForResponse(ping, now);

                var consultationId = Guid.NewGuid();
                var consultation = new Consultation
                {
                    Id = consultationId,
                    CallerId = ping.RescuerId,
                    CalleeId = expertId,
                    RoomId = $"consultation-{consultationId}",
                    StartTime = now,
                    EndTime = null,
                    Status = ConsultationStatus.Ongoing,
                    Type = ConsultationType.Emergency
                };

                await _unitOfWork.GetRepository<Consultation>().InsertAsync(consultation);

                ping.Status = ConsultationPingStatus.AcceptedByExpert;
                ping.RespondedAt = now;
                ping.ConsultationId = consultationId;
                pingRepo.Update(ping);

                var slotWindowEnd = now.AddMinutes(30);
                var overlappingSlots = await _unitOfWork.GetRepository<ExpertTimeSlot>().GetListAsync(
                    predicate: s =>
                        s.ExpertId == expertId
                        && s.Status == TimeSlotStatus.Available
                        && s.StartTime < slotWindowEnd
                        && s.EndTime > now);

                var slotRepo = _unitOfWork.GetRepository<ExpertTimeSlot>();
                foreach (var slot in overlappingSlots)
                {
                    slot.Status = TimeSlotStatus.Reserved;
                    slotRepo.Update(slot);
                }

                _logger.LogInformation(
                    "Emergency request accepted. RequestId={RequestId}, ExpertId={ExpertId}, ReservedSlotCount={SlotCount}",
                    requestId,
                    expertId,
                    overlappingSlots.Count);

                return ToResponse(ping, consultation.RoomId);
            });

            await _notificationService.NotifyEmergencyRequestStatusChangedAsync(requestId, result);
            return result;
        }

        public async Task<EmergencyConsultationRequestResponse> RejectEmergencyRequestAsync(Guid requestId, Guid expertId)
        {
            var now = DateTime.UtcNow;

            var ping = await _unitOfWork.GetRepository<ConsultationPingRequest>().FirstOrDefaultAsync(
                predicate: p => p.Id == requestId,
                asNoTracking: false);

            if (ping == null)
            {
                throw new NotFoundException("Emergency consultation request was not found.");
            }

            if (ping.ExpertId != expertId)
            {
                throw new ForbiddenException("You are not allowed to reject this emergency request.");
            }

            EnsurePendingStateForResponse(ping, now);

            ping.Status = ConsultationPingStatus.DeclinedByExpert;
            ping.RespondedAt = now;
            _unitOfWork.GetRepository<ConsultationPingRequest>().Update(ping);
            await _unitOfWork.CommitAsync();
            await _consultationPaymentService.RefundEmergencyEscrowAsync(requestId, "Emergency consultation request rejected by expert.");

            _logger.LogInformation(
                "Emergency request rejected. RequestId={RequestId}, ExpertId={ExpertId}",
                requestId,
                expertId);

            var result = ToResponse(ping, null);
            await _notificationService.NotifyEmergencyRequestStatusChangedAsync(requestId, result);
            return result;
        }

        private static void EnsurePendingStateForResponse(ConsultationPingRequest ping, DateTime now)
        {
            if (ping.Status != ConsultationPingStatus.PendingExpertResponse)
            {
                throw new ConflictException("Emergency request is no longer pending.");
            }

            if (ping.ExpiresAt.HasValue && ping.ExpiresAt.Value <= now)
            {
                ping.Status = ConsultationPingStatus.Expired;
                ping.RespondedAt = now;
                throw new ConflictException("Emergency request has expired.");
            }
        }

        private static EmergencyConsultationRequestResponse ToResponse(ConsultationPingRequest ping, string? roomId)
        {
            return new EmergencyConsultationRequestResponse
            {
                RequestId = ping.Id,
                RequesterId = ping.RescuerId,
                ExpertId = ping.ExpertId,
                Status = ping.Status,
                RequestedAt = ping.RequestedAt,
                ExpiresAt = ping.ExpiresAt,
                RespondedAt = ping.RespondedAt,
                ConsultationId = ping.ConsultationId,
                RoomId = roomId
            };
        }
    }
}
