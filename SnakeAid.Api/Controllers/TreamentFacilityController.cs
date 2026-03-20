using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MapsterMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SnakeAid.Core.Meta;
using SnakeAid.Core.Requests.TreatmentFacility;
using SnakeAid.Core.Responses.TreatmentFacility;
using SnakeAid.Service.Interfaces;
using Swashbuckle.AspNetCore.Annotations;

namespace SnakeAid.Api.Controllers
{
    [Route("api/treatment-facilities")]
    public class TreamentFacilityController : BaseController<TreamentFacilityController>
    {
        private readonly ITreatmentFacilityService _treatmentFacilityService;
        public TreamentFacilityController(
            ILogger<TreamentFacilityController> logger,
            IHttpContextAccessor httpContextAccessor,
            IMapper mapper,
            ITreatmentFacilityService treatmentFacilityService) :
            base(logger, httpContextAccessor, mapper)
        {
            _treatmentFacilityService = treatmentFacilityService;
        }

        [HttpGet]
        [SwaggerOperation(Summary = "Get All Treatment Facilities", Description = "Get list of all active treatment facilities")]
        [SwaggerResponse(200, "Success", typeof(ApiResponse<IEnumerable<TreatmentFacilityResponse>>))]
        public async Task<IActionResult> GetAllTreatmentFacilities()
        {
            var result = await _treatmentFacilityService.GetAllTreatmentFacilitiesAsync();
            return Ok(ApiResponseBuilder.BuildSuccessResponse(result));
        }

        [HttpGet("filter")]
        [SwaggerOperation(Summary = "Filter Treatment Facilities", Description = "Get paginated treatment facilities filtered by name, active status and antivenom ID")]
        [SwaggerResponse(200, "Success", typeof(ApiResponse<PagedData<TreatmentFacilityResponse>>))]
        public async Task<IActionResult> FilterTreatmentFacilities([FromQuery] GetTreatmentFacilityRequest request)
        {
            var result = await _treatmentFacilityService.FilterTreatmentFacilitiesAsync(request);
            return Ok(ApiResponseBuilder.BuildSuccessResponse(result));
        }

        [HttpGet("{id}")]
        [SwaggerOperation(Summary = "Get Treatment Facility by ID", Description = "Get treatment facility detail by ID")]
        [SwaggerResponse(200, "Success", typeof(ApiResponse<TreatmentFacilityResponse>))]
        [SwaggerResponse(404, "Treatment facility not found")]
        public async Task<IActionResult> GetTreatmentFacilityById([FromRoute] int id)
        {
            var result = await _treatmentFacilityService.GetTreatmentFacilityByIdAsync(id);
            return Ok(ApiResponseBuilder.BuildSuccessResponse(result));
        }

        [HttpGet("find-hospital")]
        [SwaggerOperation(Summary = "Find Treatment Facility by Location", Description = "Find the nearest active treatment facility based on latitude and longitude")]
        [SwaggerResponse(200, "Success", typeof(ApiResponse<IEnumerable<TreatmentFacilityResponse>>))]
        [SwaggerResponse(404, "Treatment facility not found")]
        public async Task<IActionResult> FindTreatmentFacilityByLocation([FromQuery] double latitude, [FromQuery] double longitude)
        {
            var result = await _treatmentFacilityService.GetNearestActiveTreatmentFacilityAsync(latitude, longitude);
            return Ok(ApiResponseBuilder.BuildSuccessResponse(result));
        }

        [HttpPost("")]
        [SwaggerOperation(Summary = "Create Treatment Facility.", Description = "Create Treatment Facility.")]
        [SwaggerResponse(200, "Success", typeof(ApiResponse<IEnumerable<TreatmentFacilityResponse>>))]
        [SwaggerResponse(404, "Treatment facility not found")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateTreatmentFacility([FromBody] CreateTreatmentFacilityRequest request)
        {
            var result = await _treatmentFacilityService.CreateTreatmentFacilityAsync(request);
            return Ok(ApiResponseBuilder.BuildSuccessResponse(result));
        }

        [HttpPut("{id}")]
        [SwaggerOperation(Summary = "Update Treatment Facility.", Description = "Update Treatment Facility.")]
        [SwaggerResponse(200, "Success", typeof(ApiResponse<IEnumerable<TreatmentFacilityResponse>>))]
        [SwaggerResponse(404, "Treatment facility not found")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateTreatmentFacility([FromRoute] int id, [FromBody] UpdateTreatmentFacilityRequest request)
        {
            var result = await _treatmentFacilityService.UpdateTreatmentFacilityAsync(id, request);
            return Ok(ApiResponseBuilder.BuildSuccessResponse(result));
        }

        [HttpDelete("{id}")]
        [SwaggerOperation(Summary = "Delete Treatment Facility.", Description = "Delete Treatment Facility.")]
        [SwaggerResponse(200, "Success", typeof(ApiResponse<IEnumerable<TreatmentFacilityResponse>>))]
        [SwaggerResponse(404, "Treatment facility not found")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteTreatmentFacility([FromRoute] int id)
        {
            var result = await _treatmentFacilityService.DeleteTreatmentFacilityAsync(id);
            return Ok(ApiResponseBuilder.BuildSuccessResponse(result));
        }
    }
}