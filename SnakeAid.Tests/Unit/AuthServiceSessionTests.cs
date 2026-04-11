using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SnakeAid.Core.Domains;
using SnakeAid.Core.Exceptions;
using SnakeAid.Core.Requests.Auth;
using SnakeAid.Core.Responses.Otp;
using SnakeAid.Core.Settings;
using SnakeAid.Repository.Data;
using SnakeAid.Repository.Implements;
using SnakeAid.Service.Implements;
using SnakeAid.Service.Interfaces;

namespace SnakeAid.Tests.Unit;

public class AuthServiceSessionTests : IDisposable
{
    private readonly string _databaseName;
    private readonly ServiceProvider _rootProvider;

    public AuthServiceSessionTests()
    {
        _databaseName = $"snakeaid-auth-{Guid.NewGuid():N}";

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHttpContextAccessor();
        services.AddAuthentication();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:SecretKey"] = "this-is-a-test-secret-key-with-minimum-length-1234567890",
                ["Jwt:Issuer"] = "SnakeAid.Tests",
                ["Jwt:Audience"] = "SnakeAid.Tests.Client",
                ["Jwt:AccessTokenExpirationMinutes"] = "15",
                ["Jwt:RefreshTokenExpirationDays"] = "30"
            })
            .Build());
        services.Configure<JwtSettings>(options =>
        {
            options.SecretKey = "this-is-a-test-secret-key-with-minimum-length-1234567890";
            options.Issuer = "SnakeAid.Tests";
            options.Audience = "SnakeAid.Tests.Client";
            options.AccessTokenExpirationMinutes = 15;
            options.RefreshTokenExpirationDays = 30;
        });
        services.AddDbContext<SnakeAidDbContext>(options =>
            options.UseInMemoryDatabase(_databaseName));
        services.AddIdentityCore<Account>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.Lockout.AllowedForNewUsers = true;
            })
            .AddSignInManager()
            .AddEntityFrameworkStores<SnakeAidDbContext>()
            .AddDefaultTokenProviders();
        services.AddScoped<IOtpService, NoOpOtpService>();

        _rootProvider = services.BuildServiceProvider();

        using var scope = _rootProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SnakeAidDbContext>();
        dbContext.Database.EnsureDeleted();
        dbContext.Database.EnsureCreated();
    }

    [Fact]
    public async Task LoginAndRefresh_OnDifferentDevices_DoesNotInvalidateOtherSession()
    {
        using var scope = _rootProvider.CreateScope();
        var service = CreateAuthService(scope.ServiceProvider);
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<Account>>();
        var dbContext = scope.ServiceProvider.GetRequiredService<SnakeAidDbContext>();
        var user = await CreateActiveUserAsync(userManager, "multi-device@example.com");

        var sessionA = await service.LoginAsync(new LoginRequest { Email = user.Email!, Password = TestPassword });
        var sessionB = await service.LoginAsync(new LoginRequest { Email = user.Email!, Password = TestPassword });

        Assert.NotEqual(ReadSessionId(sessionA.AccessToken), ReadSessionId(sessionB.AccessToken));
        Assert.Equal(2, await dbContext.AuthSessions.CountAsync(x => x.UserId == user.Id && x.RevokedAt == null));

        var refreshedB = await service.RefreshTokenAsync(new RefreshTokenRequest
        {
            UserId = user.Id,
            RefreshToken = sessionB.RefreshToken
        });

        var refreshedA = await service.RefreshTokenAsync(new RefreshTokenRequest
        {
            UserId = user.Id,
            RefreshToken = sessionA.RefreshToken
        });

        Assert.NotEqual(sessionB.RefreshToken, refreshedB.RefreshToken);
        Assert.NotEqual(sessionA.RefreshToken, refreshedA.RefreshToken);
        Assert.Equal(2, await dbContext.AuthSessions.CountAsync(x => x.UserId == user.Id && x.RevokedAt == null));
    }

    [Fact]
    public async Task Logout_CurrentSessionOnly_RevokesOnlyThatSession()
    {
        using var scope = _rootProvider.CreateScope();
        var service = CreateAuthService(scope.ServiceProvider);
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<Account>>();
        var dbContext = scope.ServiceProvider.GetRequiredService<SnakeAidDbContext>();
        var user = await CreateActiveUserAsync(userManager, "logout-current@example.com");

        var sessionA = await service.LoginAsync(new LoginRequest { Email = user.Email!, Password = TestPassword });
        var sessionB = await service.LoginAsync(new LoginRequest { Email = user.Email!, Password = TestPassword });
        var sessionAId = ReadSessionId(sessionA.AccessToken);

        await service.LogoutAsync(user.Id, sessionAId);

        var revokedSession = await dbContext.AuthSessions.FirstAsync(x => x.Id == sessionAId);
        Assert.NotNull(revokedSession.RevokedAt);

        await Assert.ThrowsAsync<UnauthorizedException>(() => service.RefreshTokenAsync(new RefreshTokenRequest
        {
            UserId = user.Id,
            RefreshToken = sessionA.RefreshToken
        }));

        var refreshedB = await service.RefreshTokenAsync(new RefreshTokenRequest
        {
            UserId = user.Id,
            RefreshToken = sessionB.RefreshToken
        });

        Assert.NotNull(refreshedB.AccessToken);
    }

    [Fact]
    public async Task Refresh_WithSameTokenConcurrently_OnlyOneRequestSucceeds()
    {
        Guid userId;
        string refreshToken;
        string email = "race@example.com";

        using (var seedScope = _rootProvider.CreateScope())
        {
            var seedService = CreateAuthService(seedScope.ServiceProvider);
            var userManager = seedScope.ServiceProvider.GetRequiredService<UserManager<Account>>();
            var user = await CreateActiveUserAsync(userManager, email);
            var login = await seedService.LoginAsync(new LoginRequest { Email = email, Password = TestPassword });
            userId = user.Id;
            refreshToken = login.RefreshToken;
        }

        using var scope1 = _rootProvider.CreateScope();
        using var scope2 = _rootProvider.CreateScope();
        var service1 = CreateAuthService(scope1.ServiceProvider);
        var service2 = CreateAuthService(scope2.ServiceProvider);

        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var task1 = RunRefreshAsync(service1, userId, refreshToken, gate.Task);
        var task2 = RunRefreshAsync(service2, userId, refreshToken, gate.Task);
        gate.SetResult();

        var results = await Task.WhenAll(task1, task2);

        Assert.Single(results, x => x.Succeeded);
        Assert.Single(results, x => x.Exception is UnauthorizedException);
    }

    [Fact]
    public async Task Refresh_WithLegacyIdentityToken_MigratesToSessionStore()
    {
        using var scope = _rootProvider.CreateScope();
        var service = CreateAuthService(scope.ServiceProvider);
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<Account>>();
        var dbContext = scope.ServiceProvider.GetRequiredService<SnakeAidDbContext>();
        var user = await CreateActiveUserAsync(userManager, "legacy@example.com");
        var legacyRefreshToken = "legacy-refresh-token";
        var legacyExpiry = DateTime.UtcNow.AddDays(5).ToString("O");

        await userManager.SetAuthenticationTokenAsync(user, "SnakeAid", "RefreshToken", legacyRefreshToken);
        await userManager.SetAuthenticationTokenAsync(user, "SnakeAid", "RefreshTokenExpiry", legacyExpiry);

        var refreshed = await service.RefreshTokenAsync(new RefreshTokenRequest
        {
            UserId = user.Id,
            RefreshToken = legacyRefreshToken
        });

        Assert.NotEqual(legacyRefreshToken, refreshed.RefreshToken);
        Assert.Single(await dbContext.AuthSessions.Where(x => x.UserId == user.Id).ToListAsync());
        Assert.Null(await userManager.GetAuthenticationTokenAsync(user, "SnakeAid", "RefreshToken"));
        Assert.Null(await userManager.GetAuthenticationTokenAsync(user, "SnakeAid", "RefreshTokenExpiry"));
    }

    public void Dispose()
    {
        _rootProvider.Dispose();
    }

    private AuthService CreateAuthService(IServiceProvider serviceProvider)
    {
        var dbContext = serviceProvider.GetRequiredService<SnakeAidDbContext>();
        var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();

        return new AuthService(
            serviceProvider.GetRequiredService<UserManager<Account>>(),
            serviceProvider.GetRequiredService<SignInManager<Account>>(),
            serviceProvider.GetRequiredService<IOptions<JwtSettings>>(),
            serviceProvider.GetRequiredService<IConfiguration>(),
            serviceProvider.GetRequiredService<ILogger<AuthService>>(),
            serviceProvider.GetRequiredService<IOtpService>(),
            new UnitOfWork<SnakeAidDbContext>(dbContext, loggerFactory));
    }

    private static Guid ReadSessionId(string accessToken)
    {
        var token = new JwtSecurityTokenHandler().ReadJwtToken(accessToken);
        return Guid.Parse(token.Claims.Single(x => x.Type == JwtRegisteredClaimNames.Sid).Value);
    }

    private static async Task<Account> CreateActiveUserAsync(UserManager<Account> userManager, string email)
    {
        var existing = await userManager.FindByEmailAsync(email);
        if (existing != null)
        {
            return existing;
        }

        var user = new Account
        {
            Id = Guid.NewGuid(),
            UserName = email,
            Email = email,
            FullName = "Test User",
            IsActive = true,
            Role = AccountRole.User
        };

        var result = await userManager.CreateAsync(user, TestPassword);
        Assert.True(result.Succeeded, string.Join("; ", result.Errors.Select(x => x.Description)));
        return user;
    }

    private static async Task<RefreshAttemptResult> RunRefreshAsync(AuthService service, Guid userId, string refreshToken, Task gate)
    {
        await gate;

        try
        {
            var response = await service.RefreshTokenAsync(new RefreshTokenRequest
            {
                UserId = userId,
                RefreshToken = refreshToken
            });

            return new RefreshAttemptResult(true, response, null);
        }
        catch (Exception ex)
        {
            return new RefreshAttemptResult(false, null, ex);
        }
    }

    private sealed record RefreshAttemptResult(bool Succeeded, object? Response, Exception? Exception);

    private sealed class NoOpOtpService : IOtpService
    {
        public Task CreateOtpEntity(string email, string otp) => Task.CompletedTask;

        public Task<ValidateOtpResponse> CheckOtp(string email, string otp)
            => Task.FromResult(new ValidateOtpResponse { Success = true, AttemptsLeft = 3, Message = "ok" });

        public Task<ValidateOtpResponse> ValidateOtp(string email, string otp)
            => Task.FromResult(new ValidateOtpResponse { Success = true, AttemptsLeft = 3, Message = "ok" });
    }

    private const string TestPassword = "P@ssw0rd123!";
}
