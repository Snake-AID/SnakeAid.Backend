using SnakeAid.Core.Responses.Shift;
using SnakeAid.Core.Requests.Shift;

namespace SnakeAid.Service.Interfaces
{
    public interface IShiftService
    {
        Task<WorkShiftResponse> CreateWorkShiftAsync(CreateWorkShiftRequest request);
        Task<WorkShiftResponse> GetWorkShiftByIdAsync(Guid shiftId);
        Task<List<WorkShiftResponse>> GetWorkShiftsAsync();
        Task<WorkShiftResponse> UpdateWorkShiftAsync(Guid shiftId, UpdateWorkShiftRequest request);
        Task<bool> DeleteWorkShiftAsync(Guid shiftId);

        Task<ShiftAssignmentResponse> AssignWorkShiftAsync(Guid shiftId, AssignWorkShiftRequest request);
        Task<List<ShiftAssignmentResponse>> AssignWorkShiftBulkAsync(Guid shiftId, AssignWorkShiftBulkRequest request);
        Task<ShiftAssignmentResponse> UpdateShiftAssignmentAsync(Guid assignmentId, UpdateShiftAssignmentRequest request);
        Task<bool> DeleteShiftAssignmentAsync(Guid assignmentId);
        Task<ShiftAssignmentResponse> CheckInAssignmentAsync(Guid assignmentId);
        Task<ShiftAssignmentResponse> CheckOutAssignmentAsync(Guid assignmentId);
        Task<List<ShiftAssignmentResponse>> GetAssignmentsByDateAsync(DateOnly date);
        Task<List<ShiftAssignmentResponse>> GetAssignmentsByDateRangeAsync(DateOnly startDate, DateOnly endDate);
    }
}
