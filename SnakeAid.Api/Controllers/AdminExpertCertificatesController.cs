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
[Route("api/admin/expert/certificates")]
[Authorize(Roles = "Admin")]
public class AdminExpertCertificatesController : BaseController<AdminExpertCertificatesController>
{
    private readonly IExpertCertificateService _expertCertificateService;

    public AdminExpertCertificatesController(
        ILogger<AdminExpertCertificatesController> logger,
        IHttpContextAccessor httpContextAccessor,
        IMapper mapper,
        IExpertCertificateService expertCertificateService)
        : base(logger, httpContextAccessor, mapper)
    {
        _expertCertificateService = expertCertificateService;
    }

    [HttpPost]
    [ValidateModel]
    [SwaggerOperation(Summary = "Create a certificate for an expert account")]
    [SwaggerResponse(StatusCodes.Status201Created, "Certificate created", typeof(ApiResponse<ExpertCertificateResponse>))]
    public async Task<IActionResult> Create([FromBody] AdminCreateExpertCertificateRequest request, CancellationToken cancellationToken = default)
    {
        var result = await _expertCertificateService.AdminCreateAsync(request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, ApiResponseBuilder.BuildCreatedResponse(result, "Certificate created successfully."));
    }

    [HttpGet]
    [SwaggerOperation(Summary = "List expert certificates for admin")]
    [SwaggerResponse(StatusCodes.Status200OK, "Success", typeof(ApiResponse<PagedData<ExpertCertificateResponse>>))]
    public async Task<IActionResult> GetList([FromQuery] AdminExpertCertificateQueryRequest request, CancellationToken cancellationToken = default)
    {
        var result = await _expertCertificateService.AdminGetListAsync(request, cancellationToken);
        return Ok(ApiResponseBuilder.BuildSuccessResponse(result, "Expert certificates retrieved."));
    }

    [HttpGet("{certificateId:guid}")]
    [SwaggerOperation(Summary = "Get expert certificate detail for admin")]
    [SwaggerResponse(StatusCodes.Status200OK, "Success", typeof(ApiResponse<ExpertCertificateResponse>))]
    public async Task<IActionResult> GetDetail([FromRoute] Guid certificateId, CancellationToken cancellationToken = default)
    {
        var result = await _expertCertificateService.AdminGetDetailAsync(certificateId, cancellationToken);
        return Ok(ApiResponseBuilder.BuildSuccessResponse(result, "Expert certificate retrieved."));
    }

    [HttpPut("{certificateId:guid}")]
    [ValidateModel]
    [SwaggerOperation(Summary = "Update expert certificate and review state")]
    [SwaggerResponse(StatusCodes.Status200OK, "Success", typeof(ApiResponse<ExpertCertificateResponse>))]
    public async Task<IActionResult> Update(
        [FromRoute] Guid certificateId,
        [FromBody] AdminUpdateExpertCertificateRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await _expertCertificateService.AdminUpdateAsync(certificateId, request, cancellationToken);
        return Ok(ApiResponseBuilder.BuildSuccessResponse(result, "Certificate updated successfully."));
    }

    [HttpDelete("{certificateId:guid}")]
    [SwaggerOperation(Summary = "Delete expert certificate")]
    [SwaggerResponse(StatusCodes.Status200OK, "Success", typeof(ApiResponse<object>))]
    public async Task<IActionResult> Delete([FromRoute] Guid certificateId, CancellationToken cancellationToken = default)
    {
        await _expertCertificateService.DeleteAdminAsync(certificateId, cancellationToken);
        return Ok(ApiResponseBuilder.BuildSuccessResponse("Certificate deleted successfully."));
    }
}
