using MapsterMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SnakeAid.Core.Meta;
using SnakeAid.Core.Requests.VenomType;
using SnakeAid.Core.Responses.VenomType;
using SnakeAid.Core.Validators;
using SnakeAid.Service.Interfaces;
using Swashbuckle.AspNetCore.Annotations;

namespace SnakeAid.Api.Controllers;

[Route("api/venom-types")]
[ApiController]
public class VenomTypeController : BaseController<VenomTypeController>
{
    private readonly IVenomTypeService _venomTypeService;

    public VenomTypeController(
        ILogger<VenomTypeController> logger,
        IHttpContextAccessor httpContextAccessor,
        IMapper mapper,
        IVenomTypeService venomTypeService)
        : base(logger, httpContextAccessor, mapper)
    {
        _venomTypeService = venomTypeService;
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateModel]
    [SwaggerOperation(Summary = "Create Venom Type")]
    [SwaggerResponse(200, "Created successfully", typeof(ApiResponse<VenomTypeResponse>))]
    public async Task<IActionResult> CreateVenomType([FromBody] CreateVenomTypeRequest request)
    {
        var result = await _venomTypeService.CreateVenomTypeAsync(request);
        return Ok(ApiResponseBuilder.BuildSuccessResponse(result, "Venom type created successfully."));
    }

    [HttpGet("{id}")]
    [Authorize]
    [SwaggerOperation(Summary = "Get Venom Type by ID")]
    [SwaggerResponse(200, "Success", typeof(ApiResponse<VenomTypeResponse>))]
    public async Task<IActionResult> GetVenomTypeById(int id)
    {
        var result = await _venomTypeService.GetVenomTypeByIdAsync(id);
        return Ok(ApiResponseBuilder.BuildSuccessResponse(result));
    }

    [HttpGet]
    [Authorize]
    [SwaggerOperation(Summary = "Get All Venom Types")]
    [SwaggerResponse(200, "Success", typeof(ApiResponse<List<VenomTypeResponse>>))]
    public async Task<IActionResult> GetAllVenomTypes()
    {
        var result = await _venomTypeService.GetAllVenomTypesAsync();
        return Ok(ApiResponseBuilder.BuildSuccessResponse(result));
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    [ValidateModel]
    [SwaggerOperation(Summary = "Update Venom Type")]
    [SwaggerResponse(200, "Updated successfully", typeof(ApiResponse<VenomTypeResponse>))]
    public async Task<IActionResult> UpdateVenomType(int id, [FromBody] UpdateVenomTypeRequest request)
    {
        var result = await _venomTypeService.UpdateVenomTypeAsync(id, request);
        return Ok(ApiResponseBuilder.BuildSuccessResponse(result, "Venom type updated successfully."));
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    [SwaggerOperation(Summary = "Delete Venom Type")]
    [SwaggerResponse(200, "Deleted successfully", typeof(ApiResponse<object>))]
    public async Task<IActionResult> DeleteVenomType(int id)
    {
        await _venomTypeService.DeleteVenomTypeAsync(id);
        return Ok(ApiResponseBuilder.BuildSuccessResponse("Venom type deleted successfully."));
    }
}
