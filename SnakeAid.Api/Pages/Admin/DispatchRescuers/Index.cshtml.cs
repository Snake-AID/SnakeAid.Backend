using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SnakeAid.Api.Services;
using SnakeAid.Core.Domains;
using SnakeAid.Core.Settings;
using SnakeAid.Repository.Data;
using SnakeAid.Repository.Interfaces;

namespace SnakeAid.Api.Pages.Admin.DispatchRescuers
{
    public class IndexModel : PageModel
    {
        private static readonly Guid[] DemoRescuerIds =
        {
            DemoDataSeeder.DEMO_RESCUER_A_ID,
            DemoDataSeeder.DEMO_RESCUER_B_ID,
            DemoDataSeeder.DEMO_RESCUER_C_ID,
            DemoDataSeeder.DEMO_RESCUER_D_ID
        };

        private readonly JwtSettings _jwtSettings;
        private readonly IUnitOfWork<SnakeAidDbContext> _unitOfWork;

        public IReadOnlyList<RescuerClientModel> Rescuers { get; private set; } = Array.Empty<RescuerClientModel>();

        public IndexModel(IOptions<JwtSettings> jwtSettings, IUnitOfWork<SnakeAidDbContext> unitOfWork)
        {
            _jwtSettings = jwtSettings.Value;
            _unitOfWork = unitOfWork;
        }

        public async Task OnGetAsync()
        {
            var accounts = await _unitOfWork.GetRepository<Account>()
                .CreateBaseQuery(asNoTracking: true)
                .Where(a => DemoRescuerIds.Contains(a.Id))
                .OrderBy(a => a.FullName)
                .ToListAsync();

            var fallbackNames = new Dictionary<Guid, string>
            {
                [DemoDataSeeder.DEMO_RESCUER_A_ID] = "Rescuer A - Quận 1",
                [DemoDataSeeder.DEMO_RESCUER_B_ID] = "Rescuer B - Quận 3",
                [DemoDataSeeder.DEMO_RESCUER_C_ID] = "Rescuer C - Quận 7",
                [DemoDataSeeder.DEMO_RESCUER_D_ID] = "Rescuer D - Tân Bình"
            };

            var source = accounts.Count > 0
                ? accounts.Select(account => new { account.Id, account.FullName })
                : DemoRescuerIds.Select(id => new { Id = id, FullName = fallbackNames[id] });

            Rescuers = source.Select(account => new RescuerClientModel
            {
                Id = account.Id.ToString(),
                FullName = account.FullName,
                Token = CreateJwtToken(account.Id, account.FullName, nameof(AccountRole.Rescuer))
            }).ToList();
        }

        public async Task<IActionResult> OnGetRosterAsync()
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);

            var rescuers = await _unitOfWork.GetRepository<RescuerProfile>()
                .CreateBaseQuery(asNoTracking: true)
                .Where(r => DemoRescuerIds.Contains(r.AccountId))
                .Include(r => r.Account)
                .OrderBy(r => r.Account.FullName)
                .ToListAsync();

            var assignments = await _unitOfWork.GetRepository<ShiftAssignment>()
                .CreateBaseQuery(asNoTracking: true)
                .Where(a => DemoRescuerIds.Contains(a.RescuerId) && a.Date == today)
                .Include(a => a.Shift)
                .ToListAsync();

            var requests = await _unitOfWork.GetRepository<RescuerRequest>()
                .CreateBaseQuery(asNoTracking: true)
                .Where(r => DemoRescuerIds.Contains(r.RescuerId))
                .Include(r => r.Incident)
                .OrderByDescending(r => r.DispatchedAt)
                .Take(20)
                .ToListAsync();

            var missions = await _unitOfWork.GetRepository<RescueMission>()
                .CreateBaseQuery(asNoTracking: true)
                .Where(m => DemoRescuerIds.Contains(m.RescuerId))
                .OrderByDescending(m => m.CreatedAt)
                .Take(20)
                .ToListAsync();

            var payload = rescuers.Select(rescuer =>
            {
                var assignment = assignments
                    .Where(a => a.RescuerId == rescuer.AccountId)
                    .OrderByDescending(a => a.CheckInAt ?? a.CreatedAt)
                    .FirstOrDefault();

                var request = requests
                    .Where(r => r.RescuerId == rescuer.AccountId)
                    .OrderByDescending(r => r.DispatchedAt)
                    .FirstOrDefault();

                var mission = missions
                    .Where(m => m.RescuerId == rescuer.AccountId)
                    .OrderByDescending(m => m.CreatedAt)
                    .FirstOrDefault();

                return new
                {
                    Id = rescuer.AccountId,
                    FullName = rescuer.Account.FullName,
                    rescuer.IsOnline,
                    rescuer.IsAvailable,
                    ShiftStatus = assignment?.Status.ToString(),
                    ShiftName = assignment?.Shift?.Name,
                    LatestRequest = request == null ? null : new
                    {
                        request.Id,
                        request.IncidentId,
                        Status = request.Status.ToString(),
                        request.DispatchedAt,
                        request.ResponseAt
                    },
                    LatestMission = mission == null ? null : new
                    {
                        mission.Id,
                        mission.IncidentId,
                        Status = mission.Status.ToString(),
                        mission.StartedAt,
                        mission.CreatedAt
                    }
                };
            });

            return new JsonResult(new { Rescuers = payload });
        }

        private string CreateJwtToken(Guid userId, string fullName, string role)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Name, fullName),
                new Claim(ClaimTypes.Role, role)
            };

            var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
            var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _jwtSettings.Issuer,
                audience: _jwtSettings.Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddHours(8),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public class RescuerClientModel
        {
            public string Id { get; set; } = string.Empty;
            public string FullName { get; set; } = string.Empty;
            public string Token { get; set; } = string.Empty;
        }
    }
}
