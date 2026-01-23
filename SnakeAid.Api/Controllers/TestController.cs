using System.ComponentModel.DataAnnotations;
using MapsterMapper;
using Microsoft.AspNetCore.Authorization;
using SnakeAid.Core.Exceptions;
using SnakeAid.Core.Meta;
using SnakeAid.Core.Validators;

namespace SnakeAid.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TestController : BaseController<TestController>
    {
        public TestController(ILogger<TestController> logger, IHttpContextAccessor httpContextAccessor, IMapper mapper) : base(logger, httpContextAccessor, mapper)
        {
        }

        // Test success response without data
        [HttpGet("success")]
        public IActionResult TestSuccess()
        {
            return Ok(ApiResponseBuilder.BuildSuccessResponse("Test success completed"));
        }

        // Test success response with data
        [HttpGet("success-with-data")]
        public IActionResult TestSuccessWithData()
        {
            var testData = new
            {
                Id = 1,
                Name = "Test User",
                Email = "test@example.com",
                CreatedAt = DateTime.UtcNow
            };
            return Ok(ApiResponseBuilder.BuildSuccessResponse(testData, "User data retrieved"));
        }

        // Test paginated response
        [HttpGet("paged")]
        public IActionResult TestPaged([FromQuery] int page = 1, [FromQuery] int size = 10)
        {
            var items = Enumerable.Range(1, size).Select(i => new
            {
                Id = (page - 1) * size + i,
                Name = $"User {(page - 1) * size + i}",
                Email = $"user{(page - 1) * size + i}@example.com"
            });

            return Ok(ApiResponseBuilder.BuildPagedResponse(
                items,
                totalPages: 10,
                currentPage: page,
                pageSize: size,
                totalItems: 100,
                "Paged data retrieved successfully"
            ));
        }

        // Test model validation errors
        [HttpPost("validation-error")]
        [ValidateModel]
        public IActionResult TestValidationError([FromBody] TestModel model)
        {
            return Ok(ApiResponseBuilder.BuildSuccessResponse("This won't be reached if validation fails"));
        }

        // Test file validation errors
        [HttpPost("file-validation-error")]
        [ValidateFile(maxSizeInMB: 1, allowedExtensions: new[] { ".jpg", ".png" }, formFieldName: "testFile")]
        public IActionResult TestFileValidationError(IFormFile testFile)
        {
            return Ok(ApiResponseBuilder.BuildSuccessResponse("File uploaded successfully"));
        }

        // Test not found exception
        [HttpGet("not-found/{id}")]
        public IActionResult TestNotFound(int id)
        {
            if (id <= 0)
                throw new NotFoundException($"User with ID {id} not found");

            return Ok(ApiResponseBuilder.BuildSuccessResponse(new { Id = id, Name = "Found User" }));
        }

        // Test unauthorized (401) - requires authentication
        [HttpGet("unauthorized")]
        [Authorize]
        public IActionResult TestUnauthorized()
        {
            return Ok(ApiResponseBuilder.BuildSuccessResponse("You are authenticated"));
        }

        // Test forbidden (403) - requires specific role
        [HttpGet("forbidden")]
        [Authorize(Roles = "Admin")]
        public IActionResult TestForbidden()
        {
            return Ok(ApiResponseBuilder.BuildSuccessResponse("You have admin access"));
        }

        // Test rate limit exception
        [HttpGet("rate-limit")]
        public IActionResult TestRateLimit()
        {
            var retryAfter = DateTime.UtcNow.AddMinutes(1);
            throw new TooManyRequestsException(
                "Rate limit exceeded. Too many requests.",
                retryAfter,
                limit: 100,
                period: "1 hour",
                endpoint: "/api/test/rate-limit"
            );
        }

        // Test unhandled exception
        [HttpGet("unhandled-error")]
        public IActionResult TestUnhandledException()
        {
            throw new Exception("This is an unhandled exception for testing");
        }

        // Test manual error responses
        [HttpGet("manual-not-found")]
        public IActionResult TestManualNotFound()
        {
            throw new NotFoundException("Manual not found for testing");
        }

        [HttpGet("manual-bad-request")]
        public IActionResult TestManualBadRequest()
        {
            throw new BadRequestException("Manual bad request for testing");
        }

        // Test with query validation
        [HttpGet("query-validation")]
        [ValidateModel]
        public IActionResult TestQueryValidation([FromQuery] QueryTestModel query)
        {
            return Ok(ApiResponseBuilder.BuildSuccessResponse(query, "Query validation passed"));
        }
    }

    // Test models for validation
    public class TestModel
    {
        [Required(ErrorMessage = "Name is required")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "Name must be between 2 and 50 characters")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string Email { get; set; } = string.Empty;

        [Range(18, 100, ErrorMessage = "Age must be between 18 and 100")]
        public int Age { get; set; }

        [Required(ErrorMessage = "Phone number is required")]
        [Phone(ErrorMessage = "Invalid phone number format")]
        public string PhoneNumber { get; set; } = string.Empty;

        public DateTime? BirthDate { get; set; }
    }

    public class QueryTestModel
    {
        [Required(ErrorMessage = "Search term is required")]
        [StringLength(100, ErrorMessage = "Search term cannot exceed 100 characters")]
        public string Search { get; set; } = string.Empty;

        [Range(1, int.MaxValue, ErrorMessage = "Page must be greater than 0")]
        public int Page { get; set; } = 1;

        [Range(1, 100, ErrorMessage = "Page size must be between 1 and 100")]
        public int PageSize { get; set; } = 10;

        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }
}