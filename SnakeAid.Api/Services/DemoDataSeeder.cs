using Bogus.Extensions.UnitedKingdom;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using SnakeAid.Core.Domains;
using SnakeAid.Repository.Data;

namespace SnakeAid.Api.Services
{
    /// <summary>
    /// Seeds demo data into database for testing rescue flow with real services
    /// Creates Account + MemberProfile/RescuerProfile records
    /// </summary>
    public class DemoDataSeeder
    {
        private readonly UserManager<Account> _userManager;
        private readonly SnakeAidDbContext _dbContext;
        private readonly ILogger<DemoDataSeeder> _logger;

        // Demo user IDs (fixed GUIDs for easy reference)
        public static readonly Guid DEMO_USER_ID = Guid.Parse("11111111-1111-1111-1111-111111111111");
        public static readonly Guid DEMO_RESCUER_A_ID = Guid.Parse("22222222-2222-2222-2222-222222222221");
        public static readonly Guid DEMO_RESCUER_B_ID = Guid.Parse("22222222-2222-2222-2222-222222222222");
        public static readonly Guid DEMO_RESCUER_C_ID = Guid.Parse("22222222-2222-2222-2222-222222222223");
        public static readonly Guid DEMO_RESCUER_D_ID = Guid.Parse("22222222-2222-2222-2222-222222222224");

        // Demo locations (HCMC area)
        private static readonly GeometryFactory _geometryFactory = NetTopologySuite.NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);

        public DemoDataSeeder(
            UserManager<Account> userManager,
            SnakeAidDbContext dbContext,
            ILogger<DemoDataSeeder> logger)
        {
            _userManager = userManager;
            _dbContext = dbContext;
            _logger = logger;
        }

        /// <summary>
        /// Seed demo users and rescuers into database
        /// </summary>
        public async Task<bool> SeedDemoDataAsync()
        {
            try
            {
                // Check if demo data already exists
                var existingUser = await _userManager.FindByIdAsync(DEMO_USER_ID.ToString());
                if (existingUser != null)
                {
                    _logger.LogInformation("Demo data already exists, skipping seed");
                    return true;
                }

                // Create demo victim user
                var demoUser = new Account
                {
                    Id = DEMO_USER_ID,
                    UserName = "demo_user",
                    Email = "demo.user@snakeaid.test",
                    FullName = "Demo User (Victim)",
                    PhoneNumber = "0901111111",
                    Role = AccountRole.User,
                    IsActive = true,
                    EmailConfirmed = true,
                    PhoneNumberConfirmed = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                var userResult = await _userManager.CreateAsync(demoUser, "Demo@123");
                if (!userResult.Succeeded)
                {
                    _logger.LogError("Failed to create demo user: {Errors}", string.Join(", ", userResult.Errors.Select(e => e.Description)));
                    return false;
                }

                // Create MemberProfile for demo user
                var memberProfile = new MemberProfile
                {

                    AccountId = DEMO_USER_ID,
                    Rating = 0,
                    RatingCount = 0,
                    EmergencyContacts = new List<string> { "0909999999" },
                    HasUnderlyingDisease = false,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                var memberWallet = new Wallet
                {
                    Id = DEMO_USER_ID,
                    Balance = 500000,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                await _dbContext.MemberProfiles.AddAsync(memberProfile);
                await _dbContext.Wallets.AddAsync(memberWallet);
                await _dbContext.SaveChangesAsync();

                // Create demo rescuers with locations
                var rescuers = new[]
                {
                    // new { Id = DEMO_RESCUER_A_ID, Name = "Rescuer A - Quận 1", Phone = "0902222221", Lng = 106.699800, Lat = 10.775400 }, // Bến Thành
                    // new { Id = DEMO_RESCUER_B_ID, Name = "Rescuer B - Quận 3", Phone = "0902222222", Lng = 106.682166, Lat = 10.776889 }, // Lý Thái Tổ
                    // new { Id = DEMO_RESCUER_C_ID, Name = "Rescuer C - Quận 7", Phone = "0902222223", Lng = 106.722550, Lat = 10.733200 }, // Phú Mỹ Hưng
                    // new { Id = DEMO_RESCUER_D_ID, Name = "Rescuer D - Tân Bình", Phone = "0902222224", Lng = 106.652344, Lat = 10.799862 } // Sân bay TSN

                    // approximate positions around Tam Kỳ (15.5741,108.4796)
                    new { Id = DEMO_RESCUER_A_ID, Name = "Rescuer A - Tam Kỳ Ward 1", Phone = "0902222221", Lng = 108.4796, Lat = 15.6191 }, // ~5km north
                    new { Id = DEMO_RESCUER_B_ID, Name = "Rescuer B - Tam Kỳ Ward 2", Phone = "0902222222", Lng = 108.4796, Lat = 15.6371 }, // ~7km north
                    new { Id = DEMO_RESCUER_C_ID, Name = "Rescuer C - Tam Kỳ Ward 3", Phone = "0902222223", Lng = 108.5876, Lat = 15.5741 }, // ~12km east
                    new { Id = DEMO_RESCUER_D_ID, Name = "Rescuer D - Tam Kỳ Ward 4", Phone = "0902222224", Lng = 108.4796, Lat = 15.4391 } // ~15km south
                };

                foreach (var r in rescuers)
                {
                    var rescuerAccount = new Account
                    {
                        Id = r.Id,
                        UserName = r.Phone,
                        Email = $"{r.Phone}@snakeaid.test",
                        FullName = r.Name,
                        PhoneNumber = r.Phone,
                        Role = AccountRole.Rescuer,
                        IsActive = true,
                        EmailConfirmed = true,
                        PhoneNumberConfirmed = true,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    var rescuerResult = await _userManager.CreateAsync(rescuerAccount, "Demo@123");
                    if (!rescuerResult.Succeeded)
                    {
                        _logger.LogError("Failed to create {Name}: {Errors}", r.Name, string.Join(", ", rescuerResult.Errors.Select(e => e.Description)));
                        continue;
                    }

                    // Create RescuerProfile with location
                    var rescuerProfile = new RescuerProfile
                    {
                        AccountId = r.Id,
                        IsOnline = false, // Will be set to true when they connect via SignalR
                        Rating = 0,
                        RatingCount = 0,
                        Type = RescuerType.Emergency,
                        LastLocation = _geometryFactory.CreatePoint(new Coordinate(r.Lng, r.Lat)),
                        LastLocationUpdate = DateTime.UtcNow,
                        TotalMissions = 0,
                        CompletedMissions = 0,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    var rescuerWallet = new Wallet
                    {
                        Id = r.Id,
                        Balance = 10000,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    await _dbContext.RescuerProfiles.AddAsync(rescuerProfile);
                    await _dbContext.Wallets.AddAsync(rescuerWallet);
                }

                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Demo data seeded successfully: 1 user + 4 rescuers with profiles");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to seed demo data");
                return false;
            }
        }

        /// <summary>
        /// Clean up all demo data (missions, incidents, sessions, requests, profiles, accounts)
        /// </summary>
        public async Task<bool> CleanupDemoDataAsync()
        {
            try
            {
                var demoUserIds = new[] { DEMO_USER_ID, DEMO_RESCUER_A_ID, DEMO_RESCUER_B_ID, DEMO_RESCUER_C_ID, DEMO_RESCUER_D_ID };

                _logger.LogInformation("Starting cleanup of demo data...");

                // 1. Get all demo incidents (created by demo user)
                var demoIncidentIds = await _dbContext.SnakebiteIncidents
                    .Where(i => demoUserIds.Contains(i.UserId))
                    .Select(i => i.Id)
                    .ToListAsync();

                _logger.LogInformation("Found {Count} demo incidents", demoIncidentIds.Count);

                // 2. Delete RescuerRequests (child of RescueRequestSession)
                var rescuerRequests = await _dbContext.RescuerRequests
                    .Where(r => demoIncidentIds.Contains(r.IncidentId) || demoUserIds.Contains(r.RescuerId))
                    .ToListAsync();
                _dbContext.RescuerRequests.RemoveRange(rescuerRequests);
                _logger.LogInformation("Removing {Count} rescuer requests", rescuerRequests.Count);

                // 3. Delete RescueRequestSessions (child of SnakebiteIncident)
                var sessions = await _dbContext.RescueRequestSessions
                    .Where(s => demoIncidentIds.Contains(s.IncidentId))
                    .ToListAsync();
                _dbContext.RescueRequestSessions.RemoveRange(sessions);
                _logger.LogInformation("Removing {Count} rescue sessions", sessions.Count);

                // 4. Delete ConsultationPingRequests (if any related to rescue missions)
                var missionIds = await _dbContext.RescueMissions
                    .Where(m => demoUserIds.Contains(m.RescuerId) || demoIncidentIds.Contains(m.IncidentId))
                    .Select(m => m.Id)
                    .ToListAsync();

                var consultationPings = await _dbContext.ConsultationPingRequests
                    .Where(c => c.RescueMissionId.HasValue && missionIds.Contains(c.RescueMissionId.Value))
                    .ToListAsync();
                _dbContext.ConsultationPingRequests.RemoveRange(consultationPings);
                _logger.LogInformation("Removing {Count} consultation pings", consultationPings.Count);

                // 5. Delete RescueMissions
                var missions = await _dbContext.RescueMissions
                    .Where(m => missionIds.Contains(m.Id))
                    .ToListAsync();
                _dbContext.RescueMissions.RemoveRange(missions);
                _logger.LogInformation("Removing {Count} rescue missions", missions.Count);

                // 6. Delete SnakebiteIncidents
                var incidents = await _dbContext.SnakebiteIncidents
                    .Where(i => demoIncidentIds.Contains(i.Id))
                    .ToListAsync();
                _dbContext.SnakebiteIncidents.RemoveRange(incidents);
                _logger.LogInformation("Removing {Count} snakebite incidents", incidents.Count);

                // 7. Delete AppNotifications for demo users
                var notifications = await _dbContext.AppNotifications
                    .Where(n => demoUserIds.Contains(n.UserId))
                    .ToListAsync();
                _dbContext.AppNotifications.RemoveRange(notifications);
                _logger.LogInformation("Removing {Count} notifications", notifications.Count);

                // 8. Delete Transactions for demo users (if any)
                var transactions = await _dbContext.Transactions
                    .Where(t => demoUserIds.Contains(t.UserId))
                    .ToListAsync();
                _dbContext.Transactions.RemoveRange(transactions);
                _logger.LogInformation("Removing {Count} transactions", transactions.Count);

                // 9. Delete Profiles
                var memberProfiles = await _dbContext.MemberProfiles
                    .Where(m => demoUserIds.Contains(m.AccountId))
                    .ToListAsync();
                _dbContext.MemberProfiles.RemoveRange(memberProfiles);
                _logger.LogInformation("Removing {Count} member profiles", memberProfiles.Count);

                var rescuerProfiles = await _dbContext.RescuerProfiles
                    .Where(r => demoUserIds.Contains(r.AccountId))
                    .ToListAsync();
                _dbContext.RescuerProfiles.RemoveRange(rescuerProfiles);
                _logger.LogInformation("Removing {Count} rescuer profiles", rescuerProfiles.Count);

                // Save all deletions
                await _dbContext.SaveChangesAsync();
                _logger.LogInformation("All related data deleted from database");

                // 10. Delete accounts (through UserManager for proper cleanup)
                foreach (var userId in demoUserIds)
                {
                    var account = await _userManager.FindByIdAsync(userId.ToString());
                    if (account != null)
                    {
                        var result = await _userManager.DeleteAsync(account);
                        if (result.Succeeded)
                        {
                            _logger.LogInformation("Deleted account {UserId}", userId);
                        }
                        else
                        {
                            _logger.LogWarning("Failed to delete account {UserId}: {Errors}",
                                userId, string.Join(", ", result.Errors.Select(e => e.Description)));
                        }
                    }
                }

                _logger.LogInformation("Demo data cleanup completed successfully");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to clean up demo data");
                return false;
            }
        }

        /// <summary>
        /// Get demo data status
        /// </summary>
        public async Task<DemoDataStatus> GetStatusAsync()
        {
            var status = new DemoDataStatus();

            // Check users
            status.UserExists = await _userManager.FindByIdAsync(DEMO_USER_ID.ToString()) != null;
            status.RescuerAExists = await _userManager.FindByIdAsync(DEMO_RESCUER_A_ID.ToString()) != null;
            status.RescuerBExists = await _userManager.FindByIdAsync(DEMO_RESCUER_B_ID.ToString()) != null;
            status.RescuerCExists = await _userManager.FindByIdAsync(DEMO_RESCUER_C_ID.ToString()) != null;
            status.RescuerDExists = await _userManager.FindByIdAsync(DEMO_RESCUER_D_ID.ToString()) != null;

            return status;
        }
    }

    public class DemoDataStatus
    {
        public bool UserExists { get; set; }
        public bool RescuerAExists { get; set; }
        public bool RescuerBExists { get; set; }
        public bool RescuerCExists { get; set; }
        public bool RescuerDExists { get; set; }

        public bool IsSeeded => UserExists && RescuerAExists && RescuerBExists && RescuerCExists && RescuerDExists;
    }
}
