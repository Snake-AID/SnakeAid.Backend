# ASP.NET Identity - Source Code Documentation

## Overview
SnakeAid uses ASP.NET Core Identity for authentication and user management, integrated with JWT Bearer tokens for API security. The implementation uses a custom `Account` entity and `Guid` keys.

---

## Configuration

### Dependency Injection
**Location**: `SnakeAid.Api/DI/DependencyInjection.cs`

The `AddAuthenticateAuthor` extension method configures:
1.  **Identity Core**: Adds `Account` user type, SignInManager, and EntityFramework stores.
2.  **Identity Options**: Configures Password, Lockout, and SignIn settings from `appsettings.json`.
3.  **JWT Authentication**: Configures `JwtBearer` authentication with validation parameters.

```csharp
public static IServiceCollection AddAuthenticateAuthor(this IServiceCollection services, IConfiguration configuration)
{
    // ... Settings binding ...

    // Add Identity Core services (without roles management)
    services.AddIdentityCore<Account>(options =>
        {
             // Password, Lockout, SignIn settings...
        })
        .AddSignInManager()
        .AddEntityFrameworkStores<SnakeAidDbContext>()
        .AddDefaultTokenProviders();

    // ... JWT Configuration ...
}
```

### Database Context
**Location**: `SnakeAid.Repository/Data/SnakeAidDbContext.cs`

Inherits from `IdentityDbContext` with Custom User (`Account`) and Key (`Guid`).
Identity tables are mapped to the `AspNetIdentity` schema.

```csharp
public class SnakeAidDbContext : IdentityDbContext<Account, IdentityRole<Guid>, Guid>
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Identity tables configuration
        modelBuilder.Entity<Account>().ToTable("Accounts", "AspNetIdentity");
        modelBuilder.Entity<IdentityUserClaim<Guid>>().ToTable("UserClaims", "AspNetIdentity");
        modelBuilder.Entity<IdentityUserLogin<Guid>>().ToTable("UserLogins", "AspNetIdentity");
        modelBuilder.Entity<IdentityUserToken<Guid>>().ToTable("UserTokens", "AspNetIdentity");
        
        // ...
    }
}
```

---

## Email & OTP Services

### Email Service
**Interface**: `SnakeAid.Service/Interfaces/IEmailService.cs`
**Implementation**: `SnakeAid.Service/Implements/Email/Providers/ResendEmailSender.cs`

Uses **Resend** to send emails. Configuration in `appsettings.json`:
```json
"EmailSettings": {
  "DefaultFrom": "onboarding@resend.dev",
  "Resend": {
    "ApiKey": "re_...",
    "Endpoint": "https://api.resend.com"
  }
}
```

### OTP Service
**Interface**: `SnakeAid.Service/Interfaces/IOtpService.cs`
**Implementation**: `SnakeAid.Service/Implements/OtpService.cs`

Manages OTP lifecycles using `IMemoryCache`.
- **Expiry**: 5 minutes.
- **Max Attempts**: 3 failed attempts before invalidation.

---

## Domain Models

### Account
**Location**: `SnakeAid.Core/Domains/Account.cs`

Inherits `IdentityUser<Guid>`.

```csharp
public class Account : IdentityUser<Guid>
{
    [Required] [MaxLength(200)] public string FullName { get; set; }
    [MaxLength(1000)] public string? AvatarUrl { get; set; }
    [Required] public AccountRole Role { get; set; } = AccountRole.User;
    
    // Status & Audit
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsActive { get; set; } = true;

    // Reputation System
    public int ReputationPoints { get; set; } = 100;
    public ReputationStatus ReputationStatus { get; set; } = ReputationStatus.Good;
    
    // ... Navigation Properties
}

public enum AccountRole { User=0, Admin=1, Expert=2, Rescuer=3 }
public enum ReputationStatus { Excellent=0, Good=1, Average=2, Poor=3, Suspended=4 }
```

---


## Controllers

### AuthController
**Location**: `SnakeAid.Api/Controllers/AuthController.cs`
**Route**: `api/auth`

Handles authentication flows.

| Endpoint | Method | Summary | Auth Required |
|----------|--------|---------|---------------|
| `/register` | POST | Register new user | No |
| `/login` | POST | Login with email/pass | No |
| `/refresh` | POST | Refresh access token | No |
| `/google` | POST | Login/Register with Google | No |
| `/logout` | POST | Logout user | Yes |
| `/me` | GET | Get current user info | Yes |
| `/verify-account` | POST | Verify account with OTP | No |

### EmailController
**Location**: `SnakeAid.Api/Controllers/EmailController.cs`
**Route**: `api/email`

Handles email-related operations.

| Endpoint | Method | Summary | Auth Required |
|----------|--------|---------|---------------|
| `/send-otp` | POST | Send OTP to user's email | No |


---

## AuthService
**Location**: `SnakeAid.Service/Implements/AuthService.cs`

Core logic for authentication.

### Key Logic Flows

#### Register (`RegisterAsync`)
1.  Check if Email exists (`_userManager.FindByEmailAsync`).
2.  Create `Account` entity with `IsActive = false`.
3.  `_userManager.CreateAsync` with password.
4.  Generate Tokens (Access + Refresh).
    *Note: The user is created as inactive and must verify their account.*

#### Verify Account (`VerifyAccountAsync`)
1.  Find user by Email.
2.  Validate OTP using `_otpService.ValidateOtp`.
3.  If valid:
    -   Set `IsActive = true`.
    -   Update user (`_userManager.UpdateAsync`).
    -   Generate new Tokens for immediate login.

#### Login (`LoginAsync`)
1.  Find user by Email.
2.  Check `IsActive`.
3.  `_signInManager.CheckPasswordSignInAsync` (updates Lockout).
4.  Generate Tokens.

#### Refresh Token (`RefreshTokenAsync`)
1.  Find user by ID from request.
2.  Check `IsActive`.
3.  Validate Refresh Token (matches DB stored value & expiry).
4.  Rotate Tokens (Remove old, Generate new).

#### Google Login (`GoogleLoginAsync`)
1.  Validate Google ID Token using `GoogleJsonWebSignature`.
2.  Check Email Verified in payload.
3.  Find User by Email.
    -   If not found: Create new user with info from Google.
4.  Link Google Login (`_userManager.AddLoginAsync`) if not linked.
5.  Generate Tokens.

#### Token Generation
-   **Access Token**: JWT with claims (Id, Email, Role, Claims). Expires in `jwtSettings.AccessTokenExpirationMinutes`.
-   **Refresh Token**: Random 64-byte Base64 string. Stored in `AspNetUserTokens` via `_userManager.SetAuthenticationTokenAsync`. Expires in `jwtSettings.RefreshTokenExpirationDays`.

---

## Request/Response Models
**Location**: `SnakeAid.Core/Requests/Auth` and `Responses/Auth`

```csharp
// Requests
public class RegisterRequest { Email, Password, FullName, PhoneNumber }
public class LoginRequest { Email, Password }
public class RefreshTokenRequest { UserId, RefreshToken }
public class GoogleLoginRequest { IdToken }
public class SendOtpEmailRequest { Email }
public class VerifyAccountRequest { Email, Otp }

// Response
public class AuthResponse 
{
    public string AccessToken { get; set; }
    public string RefreshToken { get; set; }
    public DateTime AccessTokenExpiresAt { get; set; }
    public DateTime RefreshTokenExpiresAt { get; set; }
    public UserInfo User { get; set; }
}

public class VerifyAccountResponse
{
    public bool Success { get; set; }
    public string Message { get; set; }
    public AuthResponse AuthData { get; set; }
}

public class UserInfo { Id, Email, FullName, AvatarUrl, Role, IsActive }
```
