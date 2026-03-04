using Mapster;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SnakeAid.Core.Domains;
using SnakeAid.Core.Exceptions;
using SnakeAid.Core.Requests.PayOs;
using SnakeAid.Core.Requests.SnakeCatchingMission;
using SnakeAid.Core.Responses.CatchingEnvironment;
using SnakeAid.Core.Responses.Media;
using SnakeAid.Core.Responses.SnakeCatchingMission;
using SnakeAid.Repository.Data;
using SnakeAid.Repository.Interfaces;
using SnakeAid.Service.Extensions;
using SnakeAid.Service.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SnakeAid.Service.Implements
{
    public class SnakeCatchingMissionService : ISnakeCatchingMissionService
    {
        private readonly IUnitOfWork<SnakeAidDbContext> _unitOfWork;
        private readonly ILogger<SnakeCatchingMissionService> _logger;
        private readonly IPayOsPaymentService _payOsPaymentService;

        private readonly decimal basePrice = 500000;
        private readonly decimal additionalSnakePrice = 100000;

        public SnakeCatchingMissionService(
            IUnitOfWork<SnakeAidDbContext> unitOfWork,
            ILogger<SnakeCatchingMissionService> logger,
            IPayOsPaymentService payOsPaymentService)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _payOsPaymentService = payOsPaymentService;
        }

        public async Task<SnakeCatchingMissionDetailResponse> StartMissionAsync(
            Guid rescuerId,
            Guid missionId,
            UpdateMissionStatusRequest request)
        {
            try
            {
                return await _unitOfWork.ExecuteInTransactionAsync(async () =>
                {
                    // Get mission
                    var mission = await _unitOfWork.GetRepository<SnakeCatchingMission>().FirstOrDefaultAsync(
                        predicate: m => m.Id == missionId && m.RescuerId == rescuerId);

                    if (mission == null)
                    {
                        throw new NotFoundException("Mission not found or you don't have permission to access it.");
                    }

                    // Validate current status
                    if (mission.Status != CatchingMissionStatus.Preparing)
                    {
                        throw new BadRequestException($"Cannot start mission. Current status: {mission.Status}. Mission must be in Preparing status.");
                    }

                    // Update to EnRoute
                    mission.Status = CatchingMissionStatus.EnRoute;
                    mission.StartedAt = DateTime.UtcNow;
                    if (!string.IsNullOrWhiteSpace(request.Notes))
                    {
                        mission.Notes = request.Notes;
                    }

                    _unitOfWork.GetRepository<SnakeCatchingMission>().Update(mission);
                    await _unitOfWork.CommitAsync();

                    _logger.LogInformation(
                        "Mission started successfully. MissionId: {MissionId}, RescuerId: {RescuerId}",
                        missionId, rescuerId);

                    await mission.AttachReportMediaAsync(_unitOfWork, MediaReferenceType.SnakeCatchingMission);
                    var response = mission.Adapt<SnakeCatchingMissionDetailResponse>();
                    return response;
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting mission: {Message}", ex.Message);
                throw;
            }
        }

        public async Task<SnakeCatchingMissionDetailResponse> MarkAsArrivedAsync(
            Guid rescuerId,
            Guid missionId,
            UpdateMissionStatusRequest request)
        {
            try
            {
                return await _unitOfWork.ExecuteInTransactionAsync(async () =>
                {
                    // Get mission
                    var mission = await _unitOfWork.GetRepository<SnakeCatchingMission>().FirstOrDefaultAsync(
                        predicate: m => m.Id == missionId && m.RescuerId == rescuerId);

                    if (mission == null)
                    {
                        throw new NotFoundException("Mission not found or you don't have permission to access it.");
                    }

                    // Validate current status
                    if (mission.Status != CatchingMissionStatus.EnRoute)
                    {
                        throw new BadRequestException($"Cannot mark as arrived. Current status: {mission.Status}. Mission must be EnRoute.");
                    }

                    // Update to Arrived
                    mission.Status = CatchingMissionStatus.Arrived;
                    mission.ArrivedAt = DateTime.UtcNow;
                    if (!string.IsNullOrWhiteSpace(request.Notes))
                    {
                        mission.Notes = request.Notes;
                    }

                    _unitOfWork.GetRepository<SnakeCatchingMission>().Update(mission);
                    await _unitOfWork.CommitAsync();

                    _logger.LogInformation(
                        "Mission marked as arrived. MissionId: {MissionId}, RescuerId: {RescuerId}",
                        missionId, rescuerId);

                    await mission.AttachReportMediaAsync(_unitOfWork, MediaReferenceType.SnakeCatchingMission);
                    var response = mission.Adapt<SnakeCatchingMissionDetailResponse>();
                    return response;
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking mission as arrived: {Message}", ex.Message);
                throw;
            }
        }

        public async Task<SnakeCatchingMissionDetailResponse> CompleteMissionAsync(
            Guid rescuerId,
            Guid missionId,
            UpdateMissionStatusRequest request)
        {
            try
            {
                return await _unitOfWork.ExecuteInTransactionAsync(async () =>
                {
                    // Get mission with related data
                    var mission = await _unitOfWork.GetRepository<SnakeCatchingMission>().FirstOrDefaultAsync(
                        predicate: m => m.Id == missionId && m.RescuerId == rescuerId,
                        include: q => q
                            .Include(m => m.SnakeCatchingRequest)
                            .Include(m => m.CatchingEnvironment)
                            .Include(m => m.MissionDetails)
                            .ThenInclude(d => d.SnakeSpecies));

                    if (mission == null)
                    {
                        throw new NotFoundException("Mission not found or you don't have permission to access it.");
                    }

                    // Validate current status
                    if (mission.Status != CatchingMissionStatus.Arrived)
                    {
                        throw new BadRequestException($"Cannot complete mission. Current status: {mission.Status}. Mission must be in Arrived status.");
                    }

                    var hasEvidence = await _unitOfWork.GetRepository<ReportMedia>()
                        .ExistsAsync(m => m.ReferenceId == missionId
                            && m.ReferenceType == MediaReferenceType.SnakeCatchingMission
                            && m.Purpose == MediaPurpose.Evidence);

                    if (!hasEvidence)
                    {
                        throw new BadRequestException("Cannot complete mission. SnakeCatchingMission must have at least one evidence media.");
                    }

                    // Update catching environment if provided
                    decimal envCost = 0;

                    if (request.CatchingEnvironmentId.HasValue)
                    {
                        // Validate catching environment exists
                        var catchingEnvExists = await _unitOfWork.GetRepository<CatchingEnvironment>()
                            .FirstOrDefaultAsync(predicate: ce => ce.Id == request.CatchingEnvironmentId.Value);

                        if (catchingEnvExists == null)
                        {
                            throw new NotFoundException($"Catching environment with ID {request.CatchingEnvironmentId.Value} not found.");
                        }

                        envCost = catchingEnvExists.Price;

                        mission.CatchingEnvironmentId = request.CatchingEnvironmentId.Value;
                        mission.CatchingEnvironment = catchingEnvExists;
                    }

                    //Update actual cost if provided
                    var snakeQuantity = mission.MissionDetails?.Sum(d => d.Quantity);
                    if (snakeQuantity > 0)
                    {
                        decimal additionalCosts = snakeQuantity.Value * additionalSnakePrice;
                        mission.ActualCost = basePrice + additionalCosts + envCost;
                    }
                    else
                    {
                        mission.ActualCost = 0;
                    }

                    mission.Price = basePrice;

                    // Update mission to MissionCompleted
                    mission.Status = CatchingMissionStatus.MissionCompleted;
                    mission.CompletedAt = DateTime.UtcNow;
                    if (!string.IsNullOrWhiteSpace(request.Notes))
                    {
                        mission.Notes = request.Notes;
                    }



                    // Update mission first
                    _unitOfWork.GetRepository<SnakeCatchingMission>().Update(mission);

                    // Update SnakeCatchingRequest to Finished
                    var catchingRequest = await _unitOfWork.GetRepository<SnakeCatchingRequest>()
                        .FirstOrDefaultAsync(predicate: r => r.Id == mission.SnakeCatchingRequestId);

                    if (catchingRequest != null)
                    {
                        catchingRequest.Status = RequestStatus.Finished;
                        _unitOfWork.GetRepository<SnakeCatchingRequest>().Update(catchingRequest);
                    }

                    await _unitOfWork.CommitAsync();

                    _logger.LogInformation(
                        "Mission completed successfully. MissionId: {MissionId}, RescuerId: {RescuerId}, RequestId: {RequestId}",
                        missionId, rescuerId, mission.SnakeCatchingRequestId);

                    await mission.AttachReportMediaAsync(_unitOfWork, MediaReferenceType.SnakeCatchingMission);
                    // Map to response with mission details
                    var response = mission.Adapt<SnakeCatchingMissionDetailResponse>();

                    // Map catching environment if any
                    if (mission.CatchingEnvironment != null)
                    {
                        response.CatchingEnvironment = mission.CatchingEnvironment.Adapt<CatchingEnvironmentResponse>();
                    }

                    // Map mission details if any
                    if (mission.MissionDetails != null && mission.MissionDetails.Any())
                    {
                        response.MissionDetails = mission.MissionDetails.Select(d => new CatchingMissionDetailResponse
                        {
                            Id = d.Id,
                            SnakeCatchingMissionId = d.SnakeCatchingMissionId,
                            SnakeSpeciesId = d.SnakeSpeciesId,
                            SnakeSpeciesName = d.SnakeSpecies?.CommonName,
                            Quantity = d.Quantity,
                            Price = d.Quantity * additionalSnakePrice,
                            CreatedAt = d.CreatedAt,
                            UpdatedAt = d.UpdatedAt
                        }).ToList();
                    }

                    return response;
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing mission: {Message}", ex.Message);
                throw;
            }
        }

        public async Task<SnakeCatchingMissionDetailResponse> AbortMissionAsync(
            Guid rescuerId,
            Guid missionId,
            AbortSnakeCatchingMissionRequest request)
        {
            try
            {
                return await _unitOfWork.ExecuteInTransactionAsync(async () =>
                {
                    // Get mission with related request
                    var mission = await _unitOfWork.GetRepository<SnakeCatchingMission>().FirstOrDefaultAsync(
                        predicate: m => m.Id == missionId && m.RescuerId == rescuerId,
                        include: q => q.Include(m => m.SnakeCatchingRequest));

                    if (mission == null)
                    {
                        throw new NotFoundException("Mission not found or you don't have permission to access it.");
                    }

                    // Validate current status - only allow abort from Preparing or EnRoute
                    if (mission.Status != CatchingMissionStatus.Preparing && mission.Status != CatchingMissionStatus.EnRoute)
                    {
                        throw new BadRequestException($"Cannot abort mission. Current status: {mission.Status}. Mission can only be aborted from Preparing or EnRoute status.");
                    }

                    // Update mission to MissionAborted
                    mission.Status = CatchingMissionStatus.MissionAborted;
                    mission.CancellationReason = request.Reason;
                    _unitOfWork.GetRepository<SnakeCatchingMission>().Update(mission);

                    // Reset SnakeCatchingRequest to Pending status
                    var catchingRequest = await _unitOfWork.GetRepository<SnakeCatchingRequest>()
                        .FirstOrDefaultAsync(predicate: r => r.Id == mission.SnakeCatchingRequestId);

                    if (catchingRequest != null)
                    {
                        catchingRequest.Status = RequestStatus.Pending;
                        catchingRequest.AssignedRescuerId = null;
                        catchingRequest.AssignedAt = null;
                        _unitOfWork.GetRepository<SnakeCatchingRequest>().Update(catchingRequest);

                        _logger.LogInformation(
                            "Snake catching request reset to Pending. RequestId: {RequestId}",
                            catchingRequest.Id);
                    }

                    await _unitOfWork.CommitAsync();

                    _logger.LogInformation(
                        "Mission aborted successfully. MissionId: {MissionId}, RescuerId: {RescuerId}, RequestId: {RequestId}, Reason: {Reason}",
                        missionId, rescuerId, mission.SnakeCatchingRequestId, request.Reason);

                    // Check for paid transactions and process refund (OUTSIDE the transaction)
                    var paidTransactions = await _unitOfWork.GetRepository<Transaction>().GetListAsync(
                        predicate: t => t.ReferenceId == mission.SnakeCatchingRequestId &&
                                       t.ExternalTransactionId != null &&
                                       (t.TransactionType == TransactionType.CatchingPayment ||
                                        t.TransactionType == TransactionType.CatchingDeposit),
                        asNoTracking: true,
                        cancellationToken: default);

                    if (paidTransactions != null && paidTransactions.Any())
                    {
                        var totalRefundAmount = paidTransactions.Sum(t => t.Amount);
                        var userId = catchingRequest?.UserId ?? mission.SnakeCatchingRequest?.UserId;

                        if (userId.HasValue)
                        {
                            _logger.LogInformation(
                                "Found {Count} paid transaction(s) for mission {MissionId}. Total refund amount: {Amount}",
                                paidTransactions.Count(), missionId, totalRefundAmount);

                            try
                            {
                                // Process refund to user wallet
                                var refundRequest = new RefundTransactionRequest
                                {
                                    ReceiverId = userId.Value,
                                    ReferenceId = mission.SnakeCatchingRequestId,
                                    Amount = totalRefundAmount,
                                    Description = $"Refund for aborted mission {missionId}: {request.Reason}",
                                    TransactionType = TransactionType.CatchingRefund
                                };

                                var refundResponse = await _payOsPaymentService.RefundTransactionAsync(
                                    refundRequest,
                                    cancellationToken: default);

                                _logger.LogInformation(
                                    "Refund processed successfully for mission {MissionId}. RefundAmount: {Amount}, RefundTransactionId: {TransactionId}",
                                    missionId, refundResponse.RefundAmount, refundResponse.RefundTransactionId);
                            }
                            catch (Exception refundEx)
                            {
                                // Log error but don't fail the abort operation
                                _logger.LogError(refundEx,
                                    "Failed to process refund for mission {MissionId}. User may need manual refund.",
                                    missionId);
                            }
                        }
                        else
                        {
                            _logger.LogWarning(
                                "Cannot process refund for mission {MissionId}. UserId not found.",
                                missionId);
                        }
                    }
                    else
                    {
                        _logger.LogInformation(
                            "No paid transactions found for mission {MissionId}. No refund needed.",
                            missionId);
                    }

                    await mission.AttachReportMediaAsync(_unitOfWork, MediaReferenceType.SnakeCatchingMission);
                    var response = mission.Adapt<SnakeCatchingMissionDetailResponse>();
                    return response;
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error aborting mission: {Message}", ex.Message);
                throw;
            }
        }
    }
}
