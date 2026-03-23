using Mapster;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SnakeAid.Core.Domains;
using SnakeAid.Core.Exceptions;
using SnakeAid.Core.Requests.Shift;
using SnakeAid.Core.Responses.Shift;
using SnakeAid.Repository.Data;
using SnakeAid.Repository.Interfaces;
using SnakeAid.Service.Interfaces;

namespace SnakeAid.Service.Implements
{
    public class ShiftService : IShiftService
    {
        private readonly IUnitOfWork<SnakeAidDbContext> _unitOfWork;
        private readonly ILogger<ShiftService> _logger;

        public ShiftService(
            IUnitOfWork<SnakeAidDbContext> unitOfWork,
            ILogger<ShiftService> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<WorkShiftResponse> CreateWorkShiftAsync(CreateWorkShiftRequest request)
        {
            if (request == null)
            {
                throw new BadRequestException("Request data cannot be null.");
            }

            ValidateShiftTime(request.StartTime, request.EndTime);

            return await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                var shift = new WorkShift
                {
                    Id = Guid.NewGuid(),
                    Name = request.Name.Trim(),
                    StartTime = request.StartTime,
                    EndTime = request.EndTime,
                    RequiredRescuers = request.RequiredRescuers
                };

                await _unitOfWork.GetRepository<WorkShift>().InsertAsync(shift);

                _logger.LogInformation("Created work shift {ShiftId}", shift.Id);
                return shift.Adapt<WorkShiftResponse>();
            });
        }

        public async Task<WorkShiftResponse> GetWorkShiftByIdAsync(Guid shiftId)
        {
            var shift = await _unitOfWork.GetRepository<WorkShift>().FirstOrDefaultAsync(
                predicate: s => s.Id == shiftId);

            if (shift == null)
            {
                throw new NotFoundException("Work shift not found.");
            }

            return shift.Adapt<WorkShiftResponse>();
        }

        public async Task<List<WorkShiftResponse>> GetWorkShiftsAsync()
        {
            var shifts = await _unitOfWork.GetRepository<WorkShift>().GetListAsync(
                orderBy: q => q.OrderBy(s => s.StartTime));

            return shifts.Adapt<List<WorkShiftResponse>>();
        }

        public async Task<WorkShiftResponse> UpdateWorkShiftAsync(Guid shiftId, UpdateWorkShiftRequest request)
        {
            if (request == null)
            {
                throw new BadRequestException("Request data cannot be null.");
            }

            ValidateShiftTime(request.StartTime, request.EndTime);

            return await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                var shift = await _unitOfWork.GetRepository<WorkShift>().FirstOrDefaultAsync(
                    predicate: s => s.Id == shiftId,
                    asNoTracking: false);

                if (shift == null)
                {
                    throw new NotFoundException("Work shift not found.");
                }

                shift.Name = request.Name.Trim();
                shift.StartTime = request.StartTime;
                shift.EndTime = request.EndTime;
                shift.RequiredRescuers = request.RequiredRescuers;
                shift.IsActive = request.IsActive;


                _unitOfWork.GetRepository<WorkShift>().Update(shift);

                _logger.LogInformation("Updated work shift {ShiftId}", shiftId);
                return shift.Adapt<WorkShiftResponse>();
            });
        }

        public async Task<bool> DeleteWorkShiftAsync(Guid shiftId)
        {
            return await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                var shift = await _unitOfWork.GetRepository<WorkShift>().FirstOrDefaultAsync(
                    predicate: s => s.Id == shiftId && s.IsActive,
                    asNoTracking: false);

                if (shift == null)
                {
                    throw new NotFoundException("Work shift not found.");
                }

                var hasAssignments = await _unitOfWork.GetRepository<ShiftAssignment>()
                    .ExistsAsync(a => a.ShiftId == shiftId);

                if (hasAssignments)
                {
                    throw new BadRequestException("Cannot delete work shift that already has assignments.");
                }

                shift.IsActive = false;
                _unitOfWork.GetRepository<WorkShift>().Update(shift);

                _logger.LogInformation("Deleted work shift {ShiftId}", shiftId);
                return true;
            });
        }

        public async Task<ShiftAssignmentResponse> AssignWorkShiftAsync(Guid shiftId, AssignWorkShiftRequest request)
        {
            if (request == null)
            {
                throw new BadRequestException("Request data cannot be null.");
            }

            return await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                var shift = await _unitOfWork.GetRepository<WorkShift>().FirstOrDefaultAsync(
                    predicate: s => s.Id == shiftId && s.IsActive);

                if (shift == null)
                {
                    throw new NotFoundException("Work shift not found.");
                }

                var rescuer = await _unitOfWork.GetRepository<RescuerProfile>().FirstOrDefaultAsync(
                    predicate: r => r.AccountId == request.RescuerId);

                if (rescuer == null)
                {
                    throw new NotFoundException("Rescuer not found.");
                }

                var existed = await _unitOfWork.GetRepository<ShiftAssignment>().ExistsAsync(
                    a => a.RescuerId == request.RescuerId
                         && a.ShiftId == shiftId
                         && a.ShiftStartLocal == BuildShiftStartLocal(request.Date, shift));

                if (existed)
                {
                    throw new ConflictException("Rescuer already assigned to this shift on selected date.");
                }

                var (shiftStartLocal, shiftEndLocal) = BuildShiftWindow(request.Date, shift);

                var assignment = new ShiftAssignment
                {
                    Id = Guid.NewGuid(),
                    RescuerId = request.RescuerId,
                    ShiftId = shiftId,
                    ShiftStartLocal = shiftStartLocal,
                    ShiftEndLocal = shiftEndLocal,
                    Status = ShiftAssignmentStatus.Scheduled,
                    Notes = request.Notes,
                    CheckInAtUtc = null,
                    CheckOutAtUtc = null
                };

                await _unitOfWork.GetRepository<ShiftAssignment>().InsertAsync(assignment);
                await _unitOfWork.CommitAsync();

                _logger.LogInformation("Assigned shift {ShiftId} to rescuer {RescuerId} on {Date}", shiftId, request.RescuerId, request.Date);
                return assignment.Adapt<ShiftAssignmentResponse>();
            });
        }

        public async Task<List<ShiftAssignmentResponse>> AssignWorkShiftBulkAsync(Guid shiftId, AssignWorkShiftBulkRequest request)
        {
            if (request == null)
            {
                throw new BadRequestException("Request data cannot be null.");
            }

            if (request.RescuerIds == null || !request.RescuerIds.Any())
            {
                throw new BadRequestException("At least one rescuer id is required.");
            }

            return await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                var shift = await _unitOfWork.GetRepository<WorkShift>().FirstOrDefaultAsync(
                    predicate: s => s.Id == shiftId && s.IsActive);

                if (shift == null)
                {
                    throw new NotFoundException("Work shift not found.");
                }

                var rescuerIds = request.RescuerIds.Distinct().ToList();

                var rescuerProfiles = await _unitOfWork.GetRepository<RescuerProfile>().GetListAsync(
                    predicate: r => rescuerIds.Contains(r.AccountId));

                var missingRescuerIds = rescuerIds.Except(rescuerProfiles.Select(r => r.AccountId)).ToList();
                if (missingRescuerIds.Any())
                {
                    throw new NotFoundException($"Rescuer(s) not found: {string.Join(',', missingRescuerIds)}");
                }

                var existingAssignments = await _unitOfWork.GetRepository<ShiftAssignment>().GetListAsync(
                    predicate: a => a.ShiftId == shiftId
                                    && a.ShiftStartLocal == BuildShiftStartLocal(request.Date, shift)
                                    && rescuerIds.Contains(a.RescuerId));

                var alreadyAssignedIds = existingAssignments.Select(a => a.RescuerId).ToHashSet();

                var createdAssignments = new List<ShiftAssignment>();

                foreach (var rescuerId in rescuerIds)
                {
                    if (alreadyAssignedIds.Contains(rescuerId))
                    {
                        continue;
                    }

                    var (shiftStartLocal, shiftEndLocal) = BuildShiftWindow(request.Date, shift);

                    var assignment = new ShiftAssignment
                    {
                        Id = Guid.NewGuid(),
                        RescuerId = rescuerId,
                        ShiftId = shiftId,
                        ShiftStartLocal = shiftStartLocal,
                        ShiftEndLocal = shiftEndLocal,
                        Status = ShiftAssignmentStatus.Scheduled,
                        Notes = request.Notes,
                        CheckInAtUtc = null,
                        CheckOutAtUtc = null
                    };

                    createdAssignments.Add(assignment);
                }

                if (!createdAssignments.Any())
                {
                    return new List<ShiftAssignmentResponse>();
                }

                await _unitOfWork.GetRepository<ShiftAssignment>().InsertRangeAsync(createdAssignments);
                await _unitOfWork.CommitAsync();

                _logger.LogInformation("Bulk assigned {Count} rescuer(s) to shift {ShiftId} on {Date}", createdAssignments.Count, shiftId, request.Date);
                return createdAssignments.Adapt<List<ShiftAssignmentResponse>>();
            });
        }

        public async Task<ShiftAssignmentResponse> UpdateShiftAssignmentAsync(Guid assignmentId, UpdateShiftAssignmentRequest request)
        {
            if (request == null)
            {
                throw new BadRequestException("Request data cannot be null.");
            }

            return await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                var assignment = await _unitOfWork.GetRepository<ShiftAssignment>().FirstOrDefaultAsync(
                    predicate: a => a.Id == assignmentId,
                    asNoTracking: false);

                if (assignment == null)
                {
                    throw new NotFoundException("Shift assignment not found.");
                }

                var rescuer = await _unitOfWork.GetRepository<RescuerProfile>().FirstOrDefaultAsync(
                    predicate: r => r.AccountId == request.RescuerId);

                if (rescuer == null)
                {
                    throw new NotFoundException("Rescuer not found.");
                }

                var shift = await _unitOfWork.GetRepository<WorkShift>().FirstOrDefaultAsync(
                    predicate: s => s.Id == assignment.ShiftId);

                if (shift == null)
                {
                    throw new NotFoundException("Work shift not found.");
                }

                var duplicate = await _unitOfWork.GetRepository<ShiftAssignment>().ExistsAsync(
                    a => a.Id != assignmentId
                         && a.RescuerId == request.RescuerId
                         && a.ShiftId == assignment.ShiftId
                         && a.ShiftStartLocal == BuildShiftStartLocal(request.Date, shift));

                if (duplicate)
                {
                    throw new ConflictException("Rescuer already assigned to this shift on selected date.");
                }

                var (shiftStartLocal, shiftEndLocal) = BuildShiftWindow(request.Date, shift);

                assignment.RescuerId = request.RescuerId;
                assignment.ShiftStartLocal = shiftStartLocal;
                assignment.ShiftEndLocal = shiftEndLocal;
                assignment.Notes = request.Notes;

                if (request.Status.HasValue)
                {
                    assignment.Status = request.Status.Value;
                }

                _unitOfWork.GetRepository<ShiftAssignment>().Update(assignment);
                await _unitOfWork.CommitAsync();

                _logger.LogInformation("Updated shift assignment {AssignmentId}", assignmentId);
                return assignment.Adapt<ShiftAssignmentResponse>();
            });
        }

        public async Task<bool> DeleteShiftAssignmentAsync(Guid assignmentId)
        {
            return await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                var assignment = await _unitOfWork.GetRepository<ShiftAssignment>().FirstOrDefaultAsync(
                    predicate: a => a.Id == assignmentId,
                    asNoTracking: false);

                if (assignment == null)
                {
                    throw new NotFoundException("Shift assignment not found.");
                }

                _unitOfWork.GetRepository<ShiftAssignment>().Delete(assignment);
                await _unitOfWork.CommitAsync();

                _logger.LogInformation("Deleted shift assignment {AssignmentId}", assignmentId);
                return true;
            });
        }

        public async Task<ShiftAssignmentResponse> CheckInAssignmentAsync(Guid assignmentId)
        {
            return await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                var assignment = await _unitOfWork.GetRepository<ShiftAssignment>().FirstOrDefaultAsync(
                    predicate: a => a.Id == assignmentId,
                    asNoTracking: false);

                if (assignment == null)
                {
                    throw new NotFoundException("Shift assignment not found.");
                }

                if (assignment.Status != ShiftAssignmentStatus.Scheduled)
                {
                    throw new BadRequestException($"Cannot check in assignment with status: {assignment.Status}");
                }

                assignment.Status = ShiftAssignmentStatus.Active;
                assignment.CheckInAtUtc = DateTime.UtcNow;
                assignment.CheckOutAtUtc = null;

                _unitOfWork.GetRepository<ShiftAssignment>().Update(assignment);
                await _unitOfWork.CommitAsync();

                _logger.LogInformation("Checked in shift assignment {AssignmentId}", assignmentId);
                return assignment.Adapt<ShiftAssignmentResponse>();
            });
        }

        public async Task<ShiftAssignmentResponse> CheckOutAssignmentAsync(Guid assignmentId)
        {
            return await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                var assignment = await _unitOfWork.GetRepository<ShiftAssignment>().FirstOrDefaultAsync(
                    predicate: a => a.Id == assignmentId,
                    asNoTracking: false);

                if (assignment == null)
                {
                    throw new NotFoundException("Shift assignment not found.");
                }

                if (assignment.Status != ShiftAssignmentStatus.Active)
                {
                    throw new BadRequestException($"Cannot check out assignment with status: {assignment.Status}");
                }

                assignment.Status = ShiftAssignmentStatus.Completed;
                assignment.CheckOutAtUtc = DateTime.UtcNow;

                _unitOfWork.GetRepository<ShiftAssignment>().Update(assignment);
                await _unitOfWork.CommitAsync();

                _logger.LogInformation("Checked out shift assignment {AssignmentId}", assignmentId);
                return assignment.Adapt<ShiftAssignmentResponse>();
            });
        }

        public async Task<List<ShiftAssignmentResponse>> GetAssignmentsByDateAsync(DateOnly date)
        {
            var dayStart = date.ToDateTime(TimeOnly.MinValue);
            var dayEnd = dayStart.AddDays(1);

            var assignments = await _unitOfWork.GetRepository<ShiftAssignment>().GetListAsync(
                predicate: a => a.ShiftStartLocal >= dayStart && a.ShiftStartLocal < dayEnd,
                include: q => q
                    .Include(a => a.Shift)
                    .Include(a => a.Rescuer),
                orderBy: q => q.OrderBy(a => a.ShiftStartLocal));

            return assignments.Adapt<List<ShiftAssignmentResponse>>();
        }

        public async Task<List<ShiftAssignmentResponse>> GetAssignmentsByDateRangeAsync(DateOnly startDate, DateOnly endDate)
        {
            if (endDate < startDate)
            {
                throw new BadRequestException("endDate must be greater than or equal to startDate.");
            }

            var rangeStart = startDate.ToDateTime(TimeOnly.MinValue);
            var rangeEndExclusive = endDate.ToDateTime(TimeOnly.MinValue).AddDays(1);

            var assignments = await _unitOfWork.GetRepository<ShiftAssignment>().GetListAsync(
                predicate: a => a.ShiftStartLocal >= rangeStart && a.ShiftStartLocal < rangeEndExclusive,
                include: q => q
                    .Include(a => a.Shift)
                    .Include(a => a.Rescuer),
                orderBy: q => q.OrderBy(a => a.ShiftStartLocal));

            return assignments.Adapt<List<ShiftAssignmentResponse>>();
        }

        private static void ValidateShiftTime(TimeSpan startTime, TimeSpan endTime)
        {
            if (startTime == endTime)
            {
                throw new BadRequestException("Shift start time and end time cannot be the same.");
            }
        }

        private static DateTime BuildShiftStartLocal(DateOnly date, WorkShift shift)
        {
            return date.ToDateTime(TimeOnly.MinValue).Add(shift.StartTime);
        }

        private static (DateTime ShiftStartLocal, DateTime ShiftEndLocal) BuildShiftWindow(DateOnly date, WorkShift shift)
        {
            var shiftStartLocal = BuildShiftStartLocal(date, shift);
            var shiftEndLocal = date.ToDateTime(TimeOnly.MinValue).Add(shift.EndTime);

            if (shiftEndLocal <= shiftStartLocal)
            {
                shiftEndLocal = shiftEndLocal.AddDays(1);
            }

            return (shiftStartLocal, shiftEndLocal);
        }
    }
}
