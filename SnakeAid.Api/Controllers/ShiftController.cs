using MapsterMapper;
using Microsoft.AspNetCore.Mvc;
using SnakeAid.Core.Meta;
using SnakeAid.Core.Requests.Shift;
using SnakeAid.Core.Responses.Shift;
using SnakeAid.Core.Validators;
using SnakeAid.Service.Interfaces;
using Swashbuckle.AspNetCore.Annotations;

using Microsoft.AspNetCore.Authorization;
using SnakeAid.Core.Utils;

namespace SnakeAid.Api.Controllers
{
    [Route("api/shifts")]
    [ApiController]
    [Authorize]
    public class ShiftController : BaseController<ShiftController>
    {
        private readonly IShiftService _shiftService;

        public ShiftController(
            ILogger<ShiftController> logger,
            IHttpContextAccessor httpContextAccessor,
            IMapper mapper,
            IShiftService shiftService)
            : base(logger, httpContextAccessor, mapper)
        {
            _shiftService = shiftService;
        }

        [HttpGet]
        [SwaggerOperation(Summary = "Get Work Shifts", Description = "Get all shift templates")]
        [SwaggerResponse(200, "Success", typeof(ApiResponse<List<WorkShiftResponse>>))]
        public async Task<IActionResult> GetWorkShifts()
        {
            var result = await _shiftService.GetWorkShiftsAsync();
            return Ok(ApiResponseBuilder.BuildSuccessResponse(result));
        }

        [HttpGet("{id}")]
        [SwaggerOperation(Summary = "Get Work Shift By Id", Description = "Get a shift template by id")]
        [SwaggerResponse(200, "Success", typeof(ApiResponse<WorkShiftResponse>))]
        [SwaggerResponse(404, "Work shift not found")]
        public async Task<IActionResult> GetWorkShiftById(Guid id)
        {
            var result = await _shiftService.GetWorkShiftByIdAsync(id);
            return Ok(ApiResponseBuilder.BuildSuccessResponse(result));
        }

        [HttpPost]
        [ValidateModel]
        [SwaggerOperation(Summary = "Create Work Shift", Description = "Create a new shift template")]
        [SwaggerResponse(200, "Created", typeof(ApiResponse<WorkShiftResponse>))]
        [SwaggerResponse(400, "Bad Request", typeof(ApiResponse<object>))]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateWorkShift([FromBody] CreateWorkShiftRequest request)
        {
            var result = await _shiftService.CreateWorkShiftAsync(request);
            return Ok(ApiResponseBuilder.BuildSuccessResponse(result, "Work shift created successfully."));
        }

        [HttpPut("{id}")]
        [ValidateModel]
        [SwaggerOperation(Summary = "Update Work Shift", Description = "Update a shift template")]
        [SwaggerResponse(200, "Updated", typeof(ApiResponse<WorkShiftResponse>))]
        [SwaggerResponse(404, "Work shift not found")]
        [SwaggerResponse(400, "Bad Request", typeof(ApiResponse<object>))]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateWorkShift(Guid id, [FromBody] UpdateWorkShiftRequest request)
        {
            var result = await _shiftService.UpdateWorkShiftAsync(id, request);
            return Ok(ApiResponseBuilder.BuildSuccessResponse(result, "Work shift updated successfully."));
        }

        [HttpDelete("{id}")]
        [SwaggerOperation(Summary = "Delete Work Shift", Description = "Delete a shift template")]
        [SwaggerResponse(200, "Deleted", typeof(ApiResponse<bool>))]
        [SwaggerResponse(404, "Work shift not found")]
        [SwaggerResponse(400, "Bad Request", typeof(ApiResponse<object>))]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteWorkShift(Guid id)
        {
            var result = await _shiftService.DeleteWorkShiftAsync(id);
            return Ok(ApiResponseBuilder.BuildSuccessResponse(result, "Work shift deleted successfully."));
        }

        [HttpPost("{id}/assign")]
        [ValidateModel]
        [SwaggerOperation(Summary = "Assign Shift To Rescuer", Description = "Assign a shift template to a rescuer on a specific date")]
        [SwaggerResponse(200, "Assigned", typeof(ApiResponse<ShiftAssignmentResponse>))]
        [SwaggerResponse(404, "Work shift not found")]
        [SwaggerResponse(400, "Bad Request", typeof(ApiResponse<object>))]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AssignShift(Guid id, [FromBody] AssignWorkShiftRequest request)
        {
            var result = await _shiftService.AssignWorkShiftAsync(id, request);
            return Ok(ApiResponseBuilder.BuildSuccessResponse(result, "Shift assigned successfully."));
        }

        [HttpPost("{id}/assign/bulk")]
        [ValidateModel]
        [SwaggerOperation(Summary = "Bulk Assign Shift To Rescuers", Description = "Assign a shift template to multiple rescuers for a specific date")]
        [SwaggerResponse(200, "Assigned", typeof(ApiResponse<List<ShiftAssignmentResponse>>))]
        [SwaggerResponse(404, "Work shift not found")]
        [SwaggerResponse(400, "Bad Request", typeof(ApiResponse<object>))]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AssignShiftBulk(Guid id, [FromBody] AssignWorkShiftBulkRequest request)
        {
            var result = await _shiftService.AssignWorkShiftBulkAsync(id, request);
            return Ok(ApiResponseBuilder.BuildSuccessResponse(result, "Shift assignments created successfully."));
        }

        [HttpPatch("assignments/{assignmentId}/checkin")]
        [SwaggerOperation(Summary = "Check In Shift Assignment", Description = "Mark a scheduled assignment as active")]
        [SwaggerResponse(200, "Checked in", typeof(ApiResponse<ShiftAssignmentResponse>))]
        [SwaggerResponse(404, "Shift assignment not found")]
        [SwaggerResponse(400, "Bad Request", typeof(ApiResponse<object>))]
        [Authorize(Roles = "Admin, Rescuer")]
        public async Task<IActionResult> CheckInAssignment(Guid assignmentId)
        {
            var result = await _shiftService.CheckInAssignmentAsync(assignmentId);
            return Ok(ApiResponseBuilder.BuildSuccessResponse(result, "Shift assignment checked in successfully."));
        }

        [HttpPatch("assignments/{assignmentId}/checkout")]
        [SwaggerOperation(Summary = "Check Out Shift Assignment", Description = "Mark an active assignment as completed")]
        [SwaggerResponse(200, "Checked out", typeof(ApiResponse<ShiftAssignmentResponse>))]
        [SwaggerResponse(404, "Shift assignment not found")]
        [SwaggerResponse(400, "Bad Request", typeof(ApiResponse<object>))]
        [Authorize(Roles = "Admin, Rescuer")]
        public async Task<IActionResult> CheckOutAssignment(Guid assignmentId)
        {
            var result = await _shiftService.CheckOutAssignmentAsync(assignmentId);
            return Ok(ApiResponseBuilder.BuildSuccessResponse(result, "Shift assignment checked out successfully."));
        }

        [HttpGet("assignments")]
        [SwaggerOperation(Summary = "Get Shift Assignments By Date or Range", Description = "Get shift assignments for a specific date or date range")]
        [SwaggerResponse(200, "Success", typeof(ApiResponse<List<ShiftAssignmentResponse>>))]
        public async Task<IActionResult> GetAssignmentsByDate([FromQuery] DateOnly? date, [FromQuery] DateOnly? startDate, [FromQuery] DateOnly? endDate)
        {
            if (startDate.HasValue || endDate.HasValue)
            {
                if (!startDate.HasValue || !endDate.HasValue)
                {
                    return BadRequest(ApiResponseBuilder.BuildErrorResponse("Both startDate and endDate are required for date range query."));
                }

                var rangeResult = await _shiftService.GetAssignmentsByDateRangeAsync(startDate.Value, endDate.Value);
                return Ok(ApiResponseBuilder.BuildSuccessResponse(rangeResult));
            }

            if (!date.HasValue)
            {
                return BadRequest(ApiResponseBuilder.BuildErrorResponse("Either date or startDate/endDate must be provided."));
            }

            var result = await _shiftService.GetAssignmentsByDateAsync(date.Value);
            return Ok(ApiResponseBuilder.BuildSuccessResponse(result));
        }

        [HttpPut("assignments/{assignmentId}")]
        [ValidateModel]
        [SwaggerOperation(Summary = "Update Shift Assignment", Description = "Update resuer/date/notes/status of an assignment")]
        [SwaggerResponse(200, "Updated", typeof(ApiResponse<ShiftAssignmentResponse>))]
        [SwaggerResponse(404, "Shift assignment not found")]
        [SwaggerResponse(400, "Bad Request", typeof(ApiResponse<object>))]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateAssignment(Guid assignmentId, [FromBody] UpdateShiftAssignmentRequest request)
        {
            var result = await _shiftService.UpdateShiftAssignmentAsync(assignmentId, request);
            return Ok(ApiResponseBuilder.BuildSuccessResponse(result, "Shift assignment updated successfully."));
        }

        [HttpDelete("assignments/{assignmentId}")]
        [SwaggerOperation(Summary = "Delete Shift Assignment", Description = "Remove a rescuer from a shift assignment")]
        [SwaggerResponse(200, "Deleted", typeof(ApiResponse<bool>))]
        [SwaggerResponse(404, "Shift assignment not found")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteAssignment(Guid assignmentId)
        {
            var result = await _shiftService.DeleteShiftAssignmentAsync(assignmentId);
            return Ok(ApiResponseBuilder.BuildSuccessResponse(result, "Shift assignment deleted successfully."));
        }

        [HttpGet("rescuer/{id}/my-assignments-today")]
        [SwaggerOperation(Summary = "Get My Shift Assignments for Today", Description = "Retrieve shift assignments for the logged-in rescuer for the current day")]
        [SwaggerResponse(200, "Success", typeof(ApiResponse<List<ShiftAssignmentResponse>>))]
        [Authorize(Roles = "Admin, Rescuer")]
        public async Task<IActionResult> GetMyAssignmentsToday(Guid id)
        {
            var today = AppTime.TodayLocalDate;
            var result = await _shiftService.GetAssignmentsByRescuerIdAsync(id, today);
            return Ok(ApiResponseBuilder.BuildSuccessResponse(result));
        }
    }
}
