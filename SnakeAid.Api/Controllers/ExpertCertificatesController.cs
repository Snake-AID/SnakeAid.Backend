using MapsterMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SnakeAid.Core.Meta;
using SnakeAid.Core.Requests.ExpertCertificate;
using SnakeAid.Core.Responses.ExpertCertificate;
using SnakeAid.Core.Validators;
using SnakeAid.Service.Interfaces;
using Swashbuckle.AspNetCore.Annotations;

namespace SnakeAid.Api.Controllers;

[ApiController]
[Route("api/experts/me/certificates")]
[Authorize(Roles = "Expert")]
public class ExpertCertificatesController : BaseController<ExpertCertificatesController>
{
    private readonly IExpertCertificateService _expertCertificateService;

    public ExpertCertificatesController(
        ILogger<ExpertCertificatesController> logger,
        IHttpContextAccessor httpContextAccessor,
        IMapper mapper,
        IExpertCertificateService expertCertificateService)
        : base(logger, httpContextAccessor, mapper)
    {
        _expertCertificateService = expertCertificateService;
    }

    [HttpPost]
    [ValidateModel]
    [SwaggerOperation(Summary = "Create a certificate for the current expert")]
    [SwaggerResponse(StatusCodes.Status201Created, "Certificate created", typeof(ApiResponse<ExpertCertificateResponse>))]
    public async Task<IActionResult> Create([FromBody] CreateExpertCertificateRequest request, CancellationToken cancellationToken = default)
    {
        var result = await _expertCertificateService.CreateMyAsync(GetCurrentUserId(), request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, ApiResponseBuilder.BuildCreatedResponse(result, "Certificate created successfully."));
    }

    [HttpGet]
    [SwaggerOperation(Summary = "List certificates for the current expert")]
    [SwaggerResponse(StatusCodes.Status200OK, "Success", typeof(ApiResponse<IReadOnlyList<ExpertCertificateResponse>>))]
    public async Task<IActionResult> GetMyList(CancellationToken cancellationToken = default)
    {
        var result = await _expertCertificateService.GetMyListAsync(GetCurrentUserId(), cancellationToken);
        return Ok(ApiResponseBuilder.BuildSuccessResponse(result, "Expert certificates retrieved."));
    }

    [HttpGet("{certificateId:guid}")]
    [SwaggerOperation(Summary = "Get certificate detail for the current expert")]
    [SwaggerResponse(StatusCodes.Status200OK, "Success", typeof(ApiResponse<ExpertCertificateResponse>))]
    public async Task<IActionResult> GetMyDetail([FromRoute] Guid certificateId, CancellationToken cancellationToken = default)
    {
        var result = await _expertCertificateService.GetMyDetailAsync(GetCurrentUserId(), certificateId, cancellationToken);
        return Ok(ApiResponseBuilder.BuildSuccessResponse(result, "Expert certificate retrieved."));
    }

    [HttpPut("{certificateId:guid}")]
    [ValidateModel]
    [SwaggerOperation(Summary = "Update a certificate for the current expert")]
    [SwaggerResponse(StatusCodes.Status200OK, "Success", typeof(ApiResponse<ExpertCertificateResponse>))]
    public async Task<IActionResult> Update(
        [FromRoute] Guid certificateId,
        [FromBody] UpdateExpertCertificateRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await _expertCertificateService.UpdateMyAsync(GetCurrentUserId(), certificateId, request, cancellationToken);
        return Ok(ApiResponseBuilder.BuildSuccessResponse(result, "Certificate updated successfully."));
    }

    [HttpDelete("{certificateId:guid}")]
    [SwaggerOperation(Summary = "Delete a certificate for the current expert")]
    [SwaggerResponse(StatusCodes.Status200OK, "Success", typeof(ApiResponse<object>))]
    public async Task<IActionResult> Delete([FromRoute] Guid certificateId, CancellationToken cancellationToken = default)
    {
        await _expertCertificateService.DeleteMyAsync(GetCurrentUserId(), certificateId, cancellationToken);
        return Ok(ApiResponseBuilder.BuildSuccessResponse("Certificate deleted successfully."));
    }
}
