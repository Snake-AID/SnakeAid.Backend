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

namespace SnakeAid.Api.Pages.Admin.DispatchOperator
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

        public string OperatorToken { get; private set; } = string.Empty;
        public string OperatorId => DemoDataSeeder.DEMO_OPERATOR_A_ID.ToString();

        public IndexModel(IOptions<JwtSettings> jwtSettings, IUnitOfWork<SnakeAidDbContext> unitOfWork)
        {
            _jwtSettings = jwtSettings.Value;
            _unitOfWork = unitOfWork;
        }

        public void OnGet()
        {
            OperatorToken = CreateJwtToken(
                DemoDataSeeder.DEMO_OPERATOR_A_ID,
                "Demo Operator A",
                nameof(AccountRole.Operator));
        }

        public async Task<IActionResult> OnGetSnapshotAsync()
        {
            var seeded = await IsDemoSeededAsync();
            if (!seeded)
            {
                return new JsonResult(new
                {
                    Seeded = false,
                    Incidents = Array.Empty<object>(),
                    Rescuers = Array.Empty<object>()
                });
            }

            var today = DateOnly.FromDateTime(DateTime.UtcNow);

            var incidents = await _unitOfWork.GetRepository<SnakebiteIncident>()
                .CreateBaseQuery(asNoTracking: true)
                .Where(i => i.UserId == DemoDataSeeder.DEMO_USER_ID)
                .Include(i => i.User)
                    .ThenInclude(u => u.Account)
                .Include(i => i.AssignedRescuer)
                    .ThenInclude(r => r.Account)
                .Include(i => i.DispatchRequests)
                    .ThenInclude(r => r.Rescuer)
                        .ThenInclude(r => r.Account)
                .Include(i => i.Missions)
                .OrderByDescending(i => i.CreatedAt)
                .Take(12)
                .ToListAsync();

            var shifts = await _unitOfWork.GetRepository<ShiftAssignment>()
                .CreateBaseQuery(asNoTracking: true)
                .Where(a => DemoRescuerIds.Contains(a.RescuerId) && a.Date == today)
                .Include(a => a.Shift)
                .ToListAsync();

            var rescuers = await _unitOfWork.GetRepository<RescuerProfile>()
                .CreateBaseQuery(asNoTracking: true)
                .Where(r => DemoRescuerIds.Contains(r.AccountId))
                .Include(r => r.Account)
                .OrderBy(r => r.Account.FullName)
                .ToListAsync();

            var payload = new
            {
                Seeded = true,
                Incidents = incidents.Select(incident =>
                {
                    var excludedRescuerIds = incident.DispatchRequests
                        .Where(request => request.Status == RescueRequestStatus.Declined)
                        .Select(request => request.RescuerId)
                        .Concat(incident.Missions
                            .Where(mission => mission.Status == RescueMissionStatus.MissionAborted)
                            .Select(mission => mission.RescuerId))
                        .Distinct()
                        .ToList();

                    return new
                    {
                        incident.Id,
                        Status = incident.Status.ToString(),
                        incident.HandlingOperatorId,
                        incident.AssignedRescuerId,
                        incident.CreatedAt,
                        incident.ConfirmedAt,
                        incident.DispatchedAt,
                        UserName = incident.User.Account.FullName,
                        UserPhone = incident.User.Account.PhoneNumber,
                        Latitude = incident.LocationCoordinates.Y,
                        Longitude = incident.LocationCoordinates.X,
                        AssignedRescuerName = incident.AssignedRescuer?.Account?.FullName,
                        ExcludedRescuerIds = excludedRescuerIds,
                        DispatchRequests = incident.DispatchRequests
                            .OrderByDescending(r => r.DispatchedAt)
                            .Select(request => new
                            {
                                request.Id,
                                request.RescuerId,
                                RescuerName = request.Rescuer.Account.FullName,
                                Status = request.Status.ToString(),
                                request.DispatchedAt,
                                request.ResponseAt,
                                request.DeclineReason
                            })
                            .ToList()
                    };
                }),
                Rescuers = rescuers.Select(rescuer =>
                {
                    var assignment = shifts
                        .Where(a => a.RescuerId == rescuer.AccountId)
                        .OrderByDescending(a => a.CheckInAt ?? a.CreatedAt)
                        .FirstOrDefault();

                    return new
                    {
                        Id = rescuer.AccountId,
                        FullName = rescuer.Account.FullName,
                        rescuer.IsOnline,
                        rescuer.IsAvailable,
                        rescuer.LastLocationUpdate,
                        rescuer.TotalMissions,
                        ShiftStatus = assignment?.Status.ToString(),
                        ShiftName = assignment?.Shift?.Name,
                        IsOnDuty = assignment != null &&
                                   (assignment.Status == ShiftAssignmentStatus.Active
                                    || assignment.Status == ShiftAssignmentStatus.Scheduled)
                    };
                })
            };

            return new JsonResult(payload);
        }

        private async Task<bool> IsDemoSeededAsync()
        {
            var seededCount = await _unitOfWork.GetRepository<Account>()
                .CreateBaseQuery(asNoTracking: true)
                .CountAsync(a =>
                    a.Id == DemoDataSeeder.DEMO_USER_ID ||
                    a.Id == DemoDataSeeder.DEMO_OPERATOR_A_ID ||
                    DemoRescuerIds.Contains(a.Id));

            return seededCount >= 6;
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
    }
}
