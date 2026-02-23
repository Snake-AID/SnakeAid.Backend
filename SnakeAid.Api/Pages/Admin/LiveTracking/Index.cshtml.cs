using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SnakeAid.Api.Hubs;
using SnakeAid.Core.Domains;
using SnakeAid.Core.Settings;
using SnakeAid.Repository.Data;
using SnakeAid.Repository.Interfaces;

namespace SnakeAid.Api.Pages.Admin.LiveTracking
{
    public class IndexModel : PageModel
    {
        private readonly JwtSettings _jwtSettings;
        private readonly IUnitOfWork<SnakeAidDbContext> _unitOfWork;

        public string AdminToken { get; private set; } = string.Empty;

        public IndexModel(IOptions<JwtSettings> jwtSettings, IUnitOfWork<SnakeAidDbContext> unitOfWork)
        {
            _jwtSettings = jwtSettings.Value;
            _unitOfWork = unitOfWork;
        }

        public void OnGet()
        {
            // Generate a temporary Admin JWT token exclusively for joining SignalR as a Monitor
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.Name, "LiveTrackingAdmin"),
                new Claim(ClaimTypes.Role, "Admin"),
                new Claim("is_monitor", "true")
            };

            var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
            var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _jwtSettings.Issuer,
                audience: _jwtSettings.Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddHours(2), // 2 hours expiration for the monitor session
                signingCredentials: credentials);

            AdminToken = new JwtSecurityTokenHandler().WriteToken(token);
        }

        public async Task<IActionResult> OnGetGhostsAsync()
        {
            var activeSignalRIds = RescuerHub.ConnectedRescuers.Keys.ToList();

            var onlineProfiles = await _unitOfWork.GetRepository<RescuerProfile>()
                .GetListAsync(
                    predicate: r => r.IsOnline == true,
                    include: q => q.Include(p => p.Account),
                    asNoTracking: true
                );

            var ghosts = onlineProfiles
                .Where(p => !activeSignalRIds.Contains(p.AccountId.ToString(), StringComparer.OrdinalIgnoreCase))
                .Select(p => MapToBriefInfo(p))
                .ToList();

            return new JsonResult(new { Count = ghosts.Count, Rescuers = ghosts });
        }

        public async Task<IActionResult> OnGetAllRescuersAsync()
        {
            var allProfiles = await _unitOfWork.GetRepository<RescuerProfile>()
                .GetListAsync(
                    include: q => q.Include(p => p.Account),
                    asNoTracking: true
                );

            var all = allProfiles.Select(p => MapToBriefInfo(p)).ToList();

            return new JsonResult(new { Count = all.Count, Rescuers = all });
        }

        private object MapToBriefInfo(RescuerProfile profile)
        {
            var user = profile.Account;
            var userId = profile.AccountId.ToString();

            return new
            {
                Id = userId,
                FullName = user != null && !string.IsNullOrWhiteSpace(user.FullName) ? user.FullName : "Unknown Rescuer",
                AvatarUrl = user?.AvatarUrl,
                Type = profile.Type.ToString(),
                Rating = profile.Rating,
                TotalMissions = profile.TotalMissions,
                IsOnline = profile.IsOnline
            };
        }
    }
}
