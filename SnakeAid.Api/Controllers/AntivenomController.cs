using MapsterMapper;
using Microsoft.AspNetCore.Mvc;
using SnakeAid.Core.Meta;
using SnakeAid.Core.Requests.Antivenom;
using SnakeAid.Core.Responses.Antivenom;
using SnakeAid.Core.Validators;
using SnakeAid.Service.Interfaces;
using Swashbuckle.AspNetCore.Annotations;

namespace SnakeAid.Api.Controllers;

[Route("api/antivenoms")]
[ApiController]
public class AntivenomController : BaseController<AntivenomController>
{
    private readonly IAntivenomService _antivenomService;

    public AntivenomController(
        ILogger<AntivenomController> logger,
        IHttpContextAccessor httpContextAccessor,
        IMapper mapper,
        IAntivenomService antivenomService)
        : base(logger, httpContextAccessor, mapper)
    {
        _antivenomService = antivenomService;
    }

    [HttpPost]
    [ValidateModel]
    [SwaggerOperation(Summary = "Create Antivenom")]
    [SwaggerResponse(200, "Created successfully", typeof(ApiResponse<AntivenomResponse>))]
    public async Task<IActionResult> CreateAntivenom([FromBody] CreateAntivenomRequest request)
    {
        var result = await _antivenomService.CreateAntivenomAsync(request);
        return Ok(ApiResponseBuilder.BuildSuccessResponse(result, "Antivenom created successfully."));
    }

    [HttpGet("{id}")]
    [SwaggerOperation(Summary = "Get Antivenom by ID")]
    [SwaggerResponse(200, "Success", typeof(ApiResponse<AntivenomResponse>))]
    public async Task<IActionResult> GetAntivenomById(int id)
    {
        var result = await _antivenomService.GetAntivenomByIdAsync(id);
        return Ok(ApiResponseBuilder.BuildSuccessResponse(result));
    }

    [HttpGet]
    [SwaggerOperation(Summary = "Get All Antivenoms")]
    [SwaggerResponse(200, "Success", typeof(ApiResponse<List<AntivenomResponse>>))]
    public async Task<IActionResult> GetAllAntivenoms()
    {
        var result = await _antivenomService.GetAllAntivenomsAsync();
        return Ok(ApiResponseBuilder.BuildSuccessResponse(result));
    }

    [HttpPut("{id}")]
    [ValidateModel]
    [SwaggerOperation(Summary = "Update Antivenom")]
    [SwaggerResponse(200, "Updated successfully", typeof(ApiResponse<AntivenomResponse>))]
    public async Task<IActionResult> UpdateAntivenom(int id, [FromBody] UpdateAntivenomRequest request)
    {
        var result = await _antivenomService.UpdateAntivenomAsync(id, request);
        return Ok(ApiResponseBuilder.BuildSuccessResponse(result, "Antivenom updated successfully."));
    }

    [HttpDelete("{id}")]
    [SwaggerOperation(Summary = "Delete Antivenom")]
    [SwaggerResponse(200, "Deleted successfully", typeof(ApiResponse<object>))]
    public async Task<IActionResult> DeleteAntivenom(int id)
    {
        await _antivenomService.DeleteAntivenomAsync(id);
        return Ok(ApiResponseBuilder.BuildSuccessResponse("Antivenom deleted successfully."));
    }
}