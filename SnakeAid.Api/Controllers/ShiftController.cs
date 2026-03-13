using MapsterMapper;
using Microsoft.AspNetCore.Mvc;
using SnakeAid.Core.Meta;
using SnakeAid.Core.Requests.Shift;
using SnakeAid.Core.Responses.Shift;
using SnakeAid.Core.Validators;
using SnakeAid.Service.Interfaces;
using Swashbuckle.AspNetCore.Annotations;

namespace SnakeAid.Api.Controllers
{
    [Route("api/shifts")]
    [ApiController]
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
        public async Task<IActionResult> AssignShift(Guid id, [FromBody] AssignWorkShiftRequest request)
        {
            var result = await _shiftService.AssignWorkShiftAsync(id, request);
            return Ok(ApiResponseBuilder.BuildSuccessResponse(result, "Shift assigned successfully."));
        }

        [HttpPatch("assignments/{assignmentId}/checkin")]
        [SwaggerOperation(Summary = "Check In Shift Assignment", Description = "Mark a scheduled assignment as active")]
        [SwaggerResponse(200, "Checked in", typeof(ApiResponse<ShiftAssignmentResponse>))]
        [SwaggerResponse(404, "Shift assignment not found")]
        [SwaggerResponse(400, "Bad Request", typeof(ApiResponse<object>))]
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
        public async Task<IActionResult> CheckOutAssignment(Guid assignmentId)
        {
            var result = await _shiftService.CheckOutAssignmentAsync(assignmentId);
            return Ok(ApiResponseBuilder.BuildSuccessResponse(result, "Shift assignment checked out successfully."));
        }

        [HttpGet("assignments")]
        [SwaggerOperation(Summary = "Get Shift Assignments By Date", Description = "Get all shift assignments for a specific date")]
        [SwaggerResponse(200, "Success", typeof(ApiResponse<List<ShiftAssignmentResponse>>))]
        public async Task<IActionResult> GetAssignmentsByDate([FromQuery] DateOnly date)
        {
            var result = await _shiftService.GetAssignmentsByDateAsync(date);
            return Ok(ApiResponseBuilder.BuildSuccessResponse(result));
        }
    }
}
