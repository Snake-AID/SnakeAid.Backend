using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using MapsterMapper;
using SnakeAid.Core.Exceptions;

namespace SnakeAid.Api.Controllers
{
    [ApiController]
    public class BaseController<T> : ControllerBase where T : BaseController<T>
    {
        protected ILogger<T> _logger;
        protected IHttpContextAccessor _httpContextAccessor;
        protected IMapper _mapper;

        public BaseController(ILogger<T> logger,
            IHttpContextAccessor httpContextAccessor, IMapper mapper)
        {
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
            _mapper = mapper;
        }

        protected Guid GetCurrentUserId()
        {
            var userIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                throw new UnauthorizedException("User ID not found in token");
            return userId;
        }

        protected string GetCurrentUserEmail()
        {
            // Try multiple claim types to find the email
            var emailClaim = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.Email)?.Value ??
                             _httpContextAccessor.HttpContext?.User?.FindFirst(JwtRegisteredClaimNames.UniqueName)?.Value ??
                             _httpContextAccessor.HttpContext?.User?.FindFirst("email")?.Value;

            return emailClaim ?? throw new UnauthorizedException("User email not found in token");
        }

        protected string GetCurrentUserRole()
        {
            return _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.Role)?.Value
                   ?? throw new UnauthorizedException("User role not found in token");
        }
    }
}