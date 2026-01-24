# ASP.NET Identity Implementation - Source Code Documentation

**Last Updated**: 2026-01-24  
**Purpose**: Complete function-level documentation to avoid future source code crawling

---

## Table of Contents
1. [Architecture Overview](#architecture-overview)
2. [DbContext Implementation](#dbcontext-implementation)
3. [Dependency Injection Setup](#dependency-injection-setup)
4. [AuthController - Public Endpoints](#authcontroller---public-endpoints)
5. [AuthController - Private Helper Methods](#authcontroller---private-helper-methods)
6. [DTOs and Models](#dtos-and-models)
7. [Entity Configurations](#entity-configurations)
8. [Configuration Settings](#configuration-settings)

---

## Architecture Overview

### Technology Stack
- **Framework**: ASP.NET Core 8.0
- **Identity**: ASP.NET Core Identity with Entity Framework Core
- **Authentication**: JWT Bearer Tokens
- **Database**: PostgreSQL (via Npgsql)
- **ORM**: Entity Framework Core
- **External Auth**: Google Sign-In (via Google.Apis.Auth)

### Key Design Decisions
- **Primary Key Type**: `Guid` (instead of default `string`)
- **Inheritance Strategy**: Table-Per-Type (TPT) for profile entities
- **Token Rotation**: Implemented (old tokens removed on refresh)
- **Schema**: All tables in `SnakeAidDb` schema

> [!WARNING]
> **Token Storage**: Refresh tokens are stored in `AspNetUserTokens` in **plain text** (not hashed). Consider hashing tokens for production if stricter security is required.

---

## DbContext Implementation

### File Location
`SnakeAid.Repository/Data/SnakeAidDbContext.cs`

### Class Definition
```csharp
public class SnakeAidDbContext : IdentityDbContext<Account, IdentityRole<Guid>, Guid>
```

**Inheritance**: `IdentityDbContext<TUser, TRole, TKey>`
- `TUser`: `Account` (custom user entity)
- `TRole`: `IdentityRole<Guid>` (Identity role with Guid key)
- `TKey`: `Guid` (primary key type)

### DbSets
```csharp
public DbSet<Account> Accounts { get; set; }
public DbSet<MemberProfile> MemberProfiles { get; set; }
public DbSet<ExpertProfile> ExpertProfiles { get; set; }
public DbSet<RescuerProfile> RescuerProfiles { get; set; }
public DbSet<Specialization> Specializations { get; set; }
```

### OnModelCreating Method
**Location**: Lines 17-23

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);  // CRITICAL: Must call first
    modelBuilder.HasDefaultSchema("SnakeAidDb");
    modelBuilder.ApplyConfigurationsFromAssembly(typeof(SnakeAidDbContext).Assembly);
}
```

> [!ATTENTION]
> **CRITICAL**: You **MUST** call `base.OnModelCreating(modelBuilder)` first to configure Identity tables. Failing to do so will cause migration errors.

**Flow**:
1. Call `base.OnModelCreating()` to configure Identity tables
2. Set default schema to `"SnakeAidDb"`
3. Auto-apply all `IEntityTypeConfiguration<T>` from assembly

**Tables Created**:
- `AspNetUsers` (Account entity)
- `AspNetRoles`
- `AspNetUserRoles`
- `AspNetUserClaims`
- `AspNetRoleClaims`
- `AspNetUserLogins` (for Google sign-in)
- `AspNetUserTokens` (for refresh tokens)
- `MemberProfiles` (TPT table)
- `ExpertProfiles` (TPT table)
- `RescuerProfiles` (TPT table)
- `Specializations`
- `ExpertSpecializations` (many-to-many join table)

### SaveChanges Override
**Location**: Lines 25-35

```csharp
public override int SaveChanges()
{
    UpdateTimestamps();
    return base.SaveChanges();
}

public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
{
    UpdateTimestamps();
    return base.SaveChangesAsync(cancellationToken);
}
```

**Purpose**: Automatically update `CreatedAt` and `UpdatedAt` timestamps

### UpdateTimestamps Method
**Location**: Lines 37-52

```csharp
private void UpdateTimestamps()
{
    var entries = ChangeTracker
        .Entries()
        .Where(e => e.Entity is IHasTimestamps && 
                    (e.State == EntityState.Added || e.State == EntityState.Modified));

    foreach (var entityEntry in entries)
    {
        var now = DateTime.UtcNow;
        var entity = (IHasTimestamps)entityEntry.Entity;

        if (entityEntry.State == EntityState.Added) 
            entity.CreatedAt = now;

        entity.UpdatedAt = now;
    }
}
```

**Logic**:
- Find all tracked entities implementing `IHasTimestamps`
- Filter to `Added` or `Modified` states
- Set `CreatedAt` only for new entities
- Always update `UpdatedAt` to current UTC time

---

## Dependency Injection Setup

### File Location
`SnakeAid.Api/DI/DependencyInjection.cs`

### AddAuthenticateAuthor Method
**Location**: Lines 28-73  
**Called From**: `Program.cs` line 126

```csharp
public static IServiceCollection AddAuthenticateAuthor(
    this IServiceCollection services,
    IConfiguration configuration)
```

#### Step 1: Configure JWT Settings (Line 31)
```csharp
services.Configure<JwtSettings>(configuration.GetSection("Jwt"));
```
Binds `appsettings.json:Jwt` section to `JwtSettings` class

#### Step 2: Register Identity Services (Lines 33-38)
```csharp
services.AddIdentityCore<Account>(options =>
        configuration.GetSection("IdentityOptions").Bind(options))
    .AddRoles<IdentityRole<Guid>>()
    .AddSignInManager()
    .AddEntityFrameworkStores<SnakeAidDbContext>()
    .AddDefaultTokenProviders();
```

**Breakdown**:
- `AddIdentityCore<Account>`: Register core Identity services for `Account` entity
- `Bind(options)`: Apply password/lockout/signin settings from `appsettings.json:IdentityOptions`
- `AddRoles<IdentityRole<Guid>>()`: Enable role management with Guid keys
- `AddSignInManager()`: Add `SignInManager<Account>` for password verification
- `AddEntityFrameworkStores<SnakeAidDbContext>()`: Use EF Core for data persistence
- `AddDefaultTokenProviders()`: Enable token generation for refresh tokens

#### Step 3: Validate JWT Settings (Lines 40-44)
```csharp
var jwtSettings = configuration.GetSection("Jwt").Get<JwtSettings>();
if (jwtSettings == null || string.IsNullOrWhiteSpace(jwtSettings.SecretKey))
{
    throw new InvalidOperationException("JWT settings are not configured.");
}
```

**Throws**: `InvalidOperationException` if JWT config missing

#### Step 4: Configure JWT Authentication (Lines 46-69)
```csharp
var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey));

services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = signingKey,
            RoleClaimType = ClaimTypes.Role,
            NameClaimType = ClaimTypes.NameIdentifier
        };
        options.RequireHttpsMetadata = true;
        options.SaveToken = true;
    });
```

**Key Parameters**:
- `ValidateIssuer/Audience/Lifetime/IssuerSigningKey`: All `true` for security
- `RoleClaimType`: `ClaimTypes.Role` (for `[Authorize(Roles = "Admin")]`)
- `NameClaimType`: `ClaimTypes.NameIdentifier` (for `User.Identity.Name`)
- `RequireHttpsMetadata`: `true` (enforce HTTPS in production)
- `SaveToken`: `true` (store token in `AuthenticationProperties`)

#### Step 5: Add Authorization (Line 71)
```csharp
services.AddAuthorization();
```

---

## AuthController - Public Endpoints

### File Location
`SnakeAid.Api/Controllers/AuthController.cs`

### Controller Setup
**Location**: Lines 18-44

```csharp
[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private const string RefreshTokenProvider = "SnakeAid";
    private const string RefreshTokenName = "RefreshToken";
    private const string RefreshTokenExpiryName = "RefreshTokenExpiry";

    private readonly UserManager<Account> _userManager;
    private readonly SignInManager<Account> _signInManager;
    private readonly RoleManager<IdentityRole<Guid>> _roleManager;
    private readonly JwtSettings _jwtSettings;
    private readonly IConfiguration _configuration;
}
```

**Dependencies Injected**:
- `UserManager<Account>`: User CRUD operations
- `SignInManager<Account>`: Password verification
- `RoleManager<IdentityRole<Guid>>`: Role management
- `JwtSettings`: JWT configuration (from DI)
- `IConfiguration`: Access to `appsettings.json`

---

### 1. Register Endpoint

**Location**: Lines 46-80  
**Route**: `POST /api/auth/register`  
**Attributes**: `[ValidateModel]` (auto-validates request model)

```csharp
public async Task<IActionResult> Register([FromBody] RegisterRequest request)
```

#### Request Model
```csharp
public class RegisterRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; }

    [Required]
    [MinLength(6)]
    public string Password { get; set; }

    public string? FullName { get; set; }
    public string? PhoneNumber { get; set; }
}
```

#### Flow
1. **Check Email Uniqueness** (Lines 50-54)
   ```csharp
   var existingUser = await _userManager.FindByEmailAsync(request.Email);
   if (existingUser != null)
       return BadRequest(ApiResponseBuilder.BuildErrorResponse("Email is already in use.", "EMAIL_IN_USE"));
   ```

2. **Create Account Entity** (Lines 56-65)
   ```csharp
   var user = new Account
   {
       Id = Guid.NewGuid(),  // Generate new Guid
       UserName = request.Email,  // Email as username
       Email = request.Email,
       FullName = request.FullName ?? string.Empty,
       PhoneNumber = request.PhoneNumber,
       IsActive = true,
       PhoneVerified = false
   };
   ```

3. **Create User with Password** (Lines 67-71)
   ```csharp
   var result = await _userManager.CreateAsync(user, request.Password);
   if (!result.Succeeded)
       return IdentityError(result, "Registration failed");
   ```
   - Password is hashed automatically by `UserManager`
   - Validates against `IdentityOptions.Password` rules

4. **Assign Default Role** (Lines 73-76)
   ```csharp
   if (await _roleManager.RoleExistsAsync(AccountRole.User.ToString()))
       await _userManager.AddToRoleAsync(user, AccountRole.User.ToString());
   ```
   - Assigns "User" role if it exists
   - Role stored in `AspNetUserRoles` table

5. **Generate Tokens** (Lines 78-79)
   ```csharp
   var tokens = await GenerateTokensAsync(user);
   return Ok(ApiResponseBuilder.BuildSuccessResponse(tokens, "Registration successful"));
   ```

#### Response
**Success (200)**:
```json
{
  "success": true,
  "message": "Registration successful",
  "data": {
    "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "refreshToken": "base64-encoded-random-bytes",
    "accessTokenExpiresAt": "2026-01-24T17:49:43Z",
    "refreshTokenExpiresAt": "2026-02-23T16:49:43Z"
  }
}
```

**Error (400)**: Email already exists
```json
{
  "success": false,
  "message": "Email is already in use.",
  "errorCode": "EMAIL_IN_USE"
}
```

**Error (422)**: Validation failed (e.g., weak password)
```json
{
  "success": false,
  "message": "Registration failed",
  "errors": {
    "Identity": [
      "Passwords must have at least one digit ('0'-'9').",
      "Passwords must have at least one uppercase ('A'-'Z')."
    ]
  }
}
```

---

### 2. Login Endpoint

**Location**: Lines 82-112  
**Route**: `POST /api/auth/login`

```csharp
public async Task<IActionResult> Login([FromBody] LoginRequest request)
```

#### Request Model
```csharp
public class LoginRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; }

    [Required]
    public string Password { get; set; }
}
```

#### Flow
1. **Find User by Email** (Lines 86-90)
   ```csharp
   var user = await _userManager.FindByEmailAsync(request.Email);
   if (user == null)
       return Unauthorized(ApiResponseBuilder.BuildUnauthorizedResponse("Invalid email or password."));
   ```

2. **Check IsActive Flag** (Lines 92-96)
   ```csharp
   if (!user.IsActive)
       return StatusCode(StatusCodes.Status403Forbidden,
           ApiResponseBuilder.BuildForbiddenResponse("Account is inactive."));
   ```

3. **Verify Password with Lockout** (Lines 98-103)
   ```csharp
   var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
   if (result.IsLockedOut)
       return StatusCode(StatusCodes.Status403Forbidden,
           ApiResponseBuilder.BuildForbiddenResponse("Account is locked."));
   ```
   - `lockoutOnFailure: true`: Increments failed attempt counter
   - Lockout triggered after `IdentityOptions.Lockout.MaxFailedAccessAttempts` (default: 5)
   - Lockout duration: `IdentityOptions.Lockout.DefaultLockoutTimeSpan` (default: 15 minutes)

4. **Check Password Result** (Lines 105-108)
   ```csharp
   if (!result.Succeeded)
       return Unauthorized(ApiResponseBuilder.BuildUnauthorizedResponse("Invalid email or password."));
   ```

5. **Generate Tokens** (Lines 110-111)
   ```csharp
   var tokens = await GenerateTokensAsync(user);
   return Ok(ApiResponseBuilder.BuildSuccessResponse(tokens, "Login successful"));
   ```

#### Response
**Success (200)**: Same as Register  
**Error (401)**: Invalid credentials  
**Error (403)**: Account inactive or locked

---

### 3. Refresh Token Endpoint

**Location**: Lines 114-142  
**Route**: `POST /api/auth/refresh`

```csharp
public async Task<IActionResult> Refresh([FromBody] RefreshRequest request)
```

#### Request Model
```csharp
public class RefreshRequest
{
    [Required]
    public Guid UserId { get; set; }

    [Required]
    public string RefreshToken { get; set; }
}
```

#### Flow
1. **Find User** (Lines 118-122)
   ```csharp
   var user = await _userManager.FindByIdAsync(request.UserId.ToString());
   if (user == null)
       return Unauthorized(ApiResponseBuilder.BuildUnauthorizedResponse("Invalid refresh token."));
   ```

2. **Check IsActive** (Lines 124-128)
   ```csharp
   if (!user.IsActive)
       return StatusCode(StatusCodes.Status403Forbidden,
           ApiResponseBuilder.BuildForbiddenResponse("Account is inactive."));
   ```

3. **Validate Refresh Token** (Lines 130-134)
   ```csharp
   var isValid = await ValidateRefreshTokenAsync(user, request.RefreshToken);
   if (!isValid)
       return Unauthorized(ApiResponseBuilder.BuildUnauthorizedResponse("Invalid refresh token."));
   ```
   - Checks token matches stored value
   - Validates expiry date

4. **Token Rotation: Remove Old Token** (Lines 136-138)
   ```csharp
   await _userManager.RemoveAuthenticationTokenAsync(user, RefreshTokenProvider, RefreshTokenName);
   await _userManager.RemoveAuthenticationTokenAsync(user, RefreshTokenProvider, RefreshTokenExpiryName);
   ```
   - **CRITICAL**: Old token is deleted before issuing new one
   - Prevents token reuse attacks

5. **Generate New Tokens** (Lines 140-141)
   ```csharp
   var tokens = await GenerateTokensAsync(user);
   return Ok(ApiResponseBuilder.BuildSuccessResponse(tokens, "Token refreshed"));
   ```

#### Response
**Success (200)**: New access + refresh token pair  
**Error (401)**: Invalid/expired refresh token  
**Error (403)**: Account inactive

---

### 4. Google Sign-In Endpoint

**Location**: Lines 144-214  
**Route**: `POST /api/auth/google`

```csharp
public async Task<IActionResult> GoogleSignIn([FromBody] GoogleLoginRequest request)
```

#### Request Model
```csharp
public class GoogleLoginRequest
{
    [Required]
    public string IdToken { get; set; }  // JWT from Google
}
```

#### Flow
1. **Get Google Client ID** (Lines 148-152)
   ```csharp
   var clientId = _configuration["Authentication:Google:ClientId"];
   if (string.IsNullOrWhiteSpace(clientId))
       return BadRequest(ApiResponseBuilder.BuildErrorResponse("Google client ID is not configured.", "GOOGLE_CONFIG"));
   ```

2. **Validate Google ID Token** (Lines 154-166)
   ```csharp
   GoogleJsonWebSignature.Payload payload;
   try
   {
       payload = await GoogleJsonWebSignature.ValidateAsync(request.IdToken,
           new GoogleJsonWebSignature.ValidationSettings
           {
               Audience = new[] { clientId }
           });
   }
   catch
   {
       return Unauthorized(ApiResponseBuilder.BuildUnauthorizedResponse("Invalid Google token."));
   }
   ```
   - Validates JWT signature from Google
   - Checks audience matches client ID
   - Extracts user info from payload

3. **Check Email Verification** (Lines 168-171)
   ```csharp
   if (!payload.EmailVerified)
       return Unauthorized(ApiResponseBuilder.BuildUnauthorizedResponse("Google email is not verified."));
   ```

4. **Find or Create User** (Lines 173-197)
   ```csharp
   var user = await _userManager.FindByEmailAsync(payload.Email);
   if (user == null)
   {
       user = new Account
       {
           Id = Guid.NewGuid(),
           UserName = payload.Email,
           Email = payload.Email,
           EmailConfirmed = payload.EmailVerified,
           FullName = payload.Name ?? string.Empty,
           AvatarUrl = payload.Picture,
           IsActive = true
       };

       var createResult = await _userManager.CreateAsync(user);  // No password
       if (!createResult.Succeeded)
           return IdentityError(createResult, "Google sign-in failed");

       if (await _roleManager.RoleExistsAsync(AccountRole.User.ToString()))
           await _userManager.AddToRoleAsync(user, AccountRole.User.ToString());
   }
   ```
   - Creates user without password if new
   - Sets `EmailConfirmed = true` from Google
   - Populates `AvatarUrl` from Google profile picture

5. **Check IsActive** (Lines 199-203)
   ```csharp
   if (!user.IsActive)
       return StatusCode(StatusCodes.Status403Forbidden,
           ApiResponseBuilder.BuildForbiddenResponse("Account is inactive."));
   ```

6. **Link Google Login** (Lines 205-210)
   ```csharp
   var userLoginInfo = new UserLoginInfo("Google", payload.Subject, "Google");
   var logins = await _userManager.GetLoginsAsync(user);
   if (logins.All(l => l.LoginProvider != "Google"))
       await _userManager.AddLoginAsync(user, userLoginInfo);
   ```
   - Stores Google login in `AspNetUserLogins` table
   - `payload.Subject`: Google user ID (unique identifier)
   - Only adds if not already linked

7. **Generate Tokens** (Lines 212-213)
   ```csharp
   var tokens = await GenerateTokensAsync(user);
   return Ok(ApiResponseBuilder.BuildSuccessResponse(tokens, "Google sign-in successful"));
   ```

#### Response
**Success (200)**: Access + refresh tokens  
**Error (400)**: Google client ID not configured  
**Error (401)**: Invalid Google token or email not verified  
**Error (403)**: Account inactive

---

## AuthController - Private Helper Methods

### 1. GenerateTokensAsync

**Location**: Lines 216-235

```csharp
private async Task<AuthResponse> GenerateTokensAsync(Account user)
{
    var accessExpiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes);
    var refreshExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays);

    var accessToken = await GenerateAccessTokenAsync(user, accessExpiresAt);
    var refreshToken = GenerateRefreshToken();

    await _userManager.SetAuthenticationTokenAsync(user, RefreshTokenProvider, RefreshTokenName, refreshToken);
    await _userManager.SetAuthenticationTokenAsync(user, RefreshTokenProvider, RefreshTokenExpiryName,
        refreshExpiresAt.ToString("O"));

    return new AuthResponse
    {
        AccessToken = accessToken,
        RefreshToken = refreshToken,
        AccessTokenExpiresAt = accessExpiresAt,
        RefreshTokenExpiresAt = refreshExpiresAt
    };
}
```

**Flow**:
1. Calculate expiry times from `JwtSettings`
2. Generate access token (JWT) with claims
3. Generate refresh token (random bytes)
4. Store refresh token in `AspNetUserTokens`:
   - Provider: `"SnakeAid"`
   - Token name: `"RefreshToken"`
   - Value: Base64-encoded random bytes
5. Store expiry in `AspNetUserTokens`:
   - Token name: `"RefreshTokenExpiry"`
   - Value: ISO 8601 datetime string (`"O"` format)
6. Return `AuthResponse` DTO

**Database Storage**:
```sql
-- AspNetUserTokens table
UserId | LoginProvider | Name                | Value
-------|---------------|---------------------|------------------
{guid} | SnakeAid      | RefreshToken        | {base64-string}
{guid} | SnakeAid      | RefreshTokenExpiry  | 2026-02-23T16:49:43.0000000Z
```

---

### 2. GenerateAccessTokenAsync

**Location**: Lines 237-270

```csharp
private async Task<string> GenerateAccessTokenAsync(Account user, DateTime expiresAt)
{
    var roles = await _userManager.GetRolesAsync(user);
    var claims = new List<Claim>
    {
        new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
        new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
        new(JwtRegisteredClaimNames.UniqueName, user.UserName ?? string.Empty),
        new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        new(ClaimTypes.NameIdentifier, user.Id.ToString()),
        new(ClaimTypes.Name, user.UserName ?? string.Empty),
        new(ClaimTypes.Email, user.Email ?? string.Empty),
        new("full_name", user.FullName ?? string.Empty),
        new("phone_number", user.PhoneNumber ?? string.Empty),
        new("is_active", user.IsActive.ToString().ToLowerInvariant())
    };

    foreach (var role in roles)
    {
        claims.Add(new Claim(ClaimTypes.Role, role));
    }

    var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
    var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

    var token = new JwtSecurityToken(
        _jwtSettings.Issuer,
        _jwtSettings.Audience,
        claims,
        expires: expiresAt,
        signingCredentials: credentials);

    return new JwtSecurityTokenHandler().WriteToken(token);
}
```

**Claims Included**:
| Claim Type | Value | Purpose |
|------------|-------|---------|
| `sub` | User ID (Guid) | Subject (standard JWT claim) |
| `email` | Email address | Standard JWT claim |
| `unique_name` | Username (email) | Standard JWT claim |
| `jti` | Random Guid | JWT ID (for token revocation) |
| `nameidentifier` | User ID | ASP.NET Core identity claim |
| `name` | Username | ASP.NET Core identity claim |
| `full_name` | Full name | Custom claim |
| `phone_number` | Phone number | Custom claim |
| `is_active` | "true"/"false" | Custom claim |
| `role` | Role name(s) | Multiple claims if user has multiple roles |

**Signing**:
- Algorithm: HMAC-SHA256
- Key: `JwtSettings.SecretKey` (from config)
- No `notBefore` claim (token valid immediately)

---

### 3. GenerateRefreshToken

**Location**: Lines 272-276

```csharp
private static string GenerateRefreshToken()
{
    var bytes = RandomNumberGenerator.GetBytes(64);
    return Convert.ToBase64String(bytes);
}
```

**Implementation**:
- Generates 64 random bytes using cryptographically secure RNG
- Encodes as Base64 string
- **Not hashed** before storage (plain text in database)

---

### 4. ValidateRefreshTokenAsync

**Location**: Lines 278-298

```csharp
private async Task<bool> ValidateRefreshTokenAsync(Account user, string refreshToken)
{
    var storedToken = await _userManager.GetAuthenticationTokenAsync(user, RefreshTokenProvider, RefreshTokenName);
    if (string.IsNullOrEmpty(storedToken) || !string.Equals(storedToken, refreshToken, StringComparison.Ordinal))
    {
        return false;
    }

    var expiryString = await _userManager.GetAuthenticationTokenAsync(user, RefreshTokenProvider, RefreshTokenExpiryName);
    if (string.IsNullOrWhiteSpace(expiryString))
    {
        return false;
    }

    if (!DateTime.TryParse(expiryString, null, DateTimeStyles.RoundtripKind, out var expiry))
    {
        return false;
    }

    return expiry >= DateTime.UtcNow;
}
```

**Validation Steps**:
1. Retrieve stored token from `AspNetUserTokens`
2. Compare with provided token (case-sensitive, ordinal comparison)
3. Retrieve expiry datetime string
4. Parse expiry using `RoundtripKind` (preserves UTC)
5. Check if expiry is in the future

**Returns**: `true` if valid, `false` otherwise

---

### 5. IdentityError

**Location**: Lines 300-312

```csharp
private static IActionResult IdentityError(IdentityResult result, string message)
{
    var errors = new Dictionary<string, string[]>
    {
        ["Identity"] = result.Errors.Select(e => e.Description).ToArray()
    };

    var response = ApiResponseBuilder.BuildValidationErrorResponse<object>(errors, message);
    return new ObjectResult(response)
    {
        StatusCode = StatusCodes.Status422UnprocessableEntity
    };
}
```

**Purpose**: Convert `IdentityResult` errors to API response format

**Response Format**:
```json
{
  "success": false,
  "message": "Registration failed",
  "errors": {
    "Identity": [
      "Passwords must have at least one digit ('0'-'9').",
      "Passwords must have at least one uppercase ('A'-'Z')."
    ]
  }
}
```

**Status Code**: 422 Unprocessable Entity

---

## DTOs and Models

### AuthResponse
**Location**: Lines 354-360

```csharp
public class AuthResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime AccessTokenExpiresAt { get; set; }
    public DateTime RefreshTokenExpiresAt { get; set; }
}
```

---

## Entity Configurations

### AccountConfiguration
**File**: `SnakeAid.Repository/Data/Configurations/AccountConfiguration.cs`

```csharp
public class AccountConfiguration : IEntityTypeConfiguration<Account>
{
    public void Configure(EntityTypeBuilder<Account> builder)
    {
        builder.ToTable("AspNetUsers");

        builder.Property(a => a.FullName).HasMaxLength(100);
        builder.Property(a => a.AvatarUrl).HasMaxLength(500);
        builder.Property(a => a.PhoneVerified).IsRequired().HasDefaultValue(false);
        builder.Property(a => a.IsActive).IsRequired().HasDefaultValue(true);
        builder.Property(a => a.CreatedAt).IsRequired();
        builder.Property(a => a.UpdatedAt).IsRequired();
    }
}
```

### MemberProfileConfiguration
**File**: `SnakeAid.Repository/Data/Configurations/MemberProfileConfiguration.cs`

```csharp
public class MemberProfileConfiguration : IEntityTypeConfiguration<MemberProfile>
{
    public void Configure(EntityTypeBuilder<MemberProfile> builder)
    {
        builder.ToTable("MemberProfiles");
        builder.HasBaseType<Account>();  // TPT inheritance

        builder.Property(mp => mp.Rating).HasColumnType("decimal(3,2)").HasDefaultValue(0.0f);
        builder.Property(mp => mp.RatingCount).IsRequired().HasDefaultValue(0);
        builder.Property(mp => mp.HasUnderlyingDisease).IsRequired().HasDefaultValue(false);

        // Convert List<string> to delimited string
        builder.Property(mp => mp.EmergencyContacts)
            .HasConversion(
                v => string.Join(';', v ?? new List<string>()),
                v => string.IsNullOrEmpty(v) ? new List<string>() : v.Split(';', StringSplitOptions.RemoveEmptyEntries).ToList()
            )
            .HasMaxLength(1000);
    }
}
```

**TPT Inheritance**: `HasBaseType<Account>()` creates separate table with FK to `AspNetUsers`

---

## Configuration Settings

### JwtSettings
**File**: `SnakeAid.Core/Settings/AppSettings.cs`

```csharp
public class JwtSettings
{
    public string SecretKey { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public int AccessTokenExpirationMinutes { get; set; } = 15;
    public int RefreshTokenExpirationDays { get; set; } = 7;
}
```

### appsettings.json Structure
```json
{
  "Jwt": {
    "SecretKey": "your-secret-key-min-32-chars",
    "Issuer": "SnakeAid",
    "Audience": "SnakeAid.Clients",
    "AccessTokenExpirationMinutes": 60,
    "RefreshTokenExpirationDays": 30
  },
  "IdentityOptions": {
    "Password": {
      "RequiredLength": 8,
      "RequireDigit": true,
      "RequireLowercase": true,
      "RequireUppercase": true,
      "RequireNonAlphanumeric": false
    },
    "Lockout": {
      "MaxFailedAccessAttempts": 5,
      "DefaultLockoutTimeSpan": "00:15:00",
      "AllowedForNewUsers": true
    },
    "SignIn": {
      "RequireConfirmedEmail": false
    }
  },
  "Authentication": {
    "Google": {
      "ClientId": "your-google-client-id.apps.googleusercontent.com"
    }
  }
}
```

---

## Role Seeding

### SeedRolesAsync Method
**File**: `SnakeAid.Api/Program.cs`  
**Location**: Lines 265-291

```csharp
private static async Task SeedRolesAsync(IHost app)
{
    try
    {
        using var scope = app.Services.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();

        foreach (var roleName in Enum.GetNames<AccountRole>())
        {
            if (await roleManager.RoleExistsAsync(roleName))
                continue;

            var result = await roleManager.CreateAsync(new IdentityRole<Guid>(roleName));
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                Log.Warning("Failed to seed role {Role}. Errors: {Errors}", roleName, errors);
            }
        }
    }
    catch (Exception ex)
    {
        Log.Warning(ex, "Role seeding skipped due to startup error.");
    }
}
```

**Roles Seeded** (from `AccountRole` enum):
- `User` (0)
- `Admin` (1)
- `Expert` (2)
- `Rescuer` (3)

**Called From**: `Program.cs` line 199 (before `app.Run()`)

---

## Security Features

### 1. Token Rotation
- Old refresh tokens deleted before issuing new ones
- Prevents token reuse attacks
- Implemented in `Refresh` endpoint (lines 136-138)

### 2. Account Lockout
- Triggered after 5 failed login attempts (configurable)
- Lockout duration: 15 minutes (configurable)
- Implemented via `CheckPasswordSignInAsync(lockoutOnFailure: true)`

### 3. IsActive Flag
- Checked in all auth endpoints
- Inactive accounts cannot login or refresh tokens
- Returns 403 Forbidden

### 4. HTTPS Enforcement
- `RequireHttpsMetadata = true` in JWT config
- Rejects tokens over HTTP in production

### 5. Email Verification (Google)
- Google sign-in validates `EmailVerified` flag
- Rejects unverified Google accounts

---

## Database Schema

### Identity Tables (Auto-generated)
```
AspNetUsers (Account entity)
├── Id (Guid, PK)
├── UserName (string)
├── Email (string)
├── PasswordHash (string)
├── FullName (string, custom)
├── AvatarUrl (string, custom)
├── IsActive (bool, custom)
├── PhoneVerified (bool, custom)
├── CreatedAt (DateTime, custom)
└── UpdatedAt (DateTime, custom)

AspNetRoles
├── Id (Guid, PK)
└── Name (string)

AspNetUserRoles
├── UserId (Guid, FK -> AspNetUsers)
└── RoleId (Guid, FK -> AspNetRoles)

AspNetUserTokens (Refresh tokens)
├── UserId (Guid, FK -> AspNetUsers)
├── LoginProvider (string) = "SnakeAid"
├── Name (string) = "RefreshToken" or "RefreshTokenExpiry"
└── Value (string) = token or datetime

AspNetUserLogins (Google sign-in)
├── UserId (Guid, FK -> AspNetUsers)
├── LoginProvider (string) = "Google"
├── ProviderKey (string) = Google user ID
└── ProviderDisplayName (string) = "Google"
```

### Profile Tables (TPT)
```
MemberProfiles
├── Id (Guid, PK, FK -> AspNetUsers)
├── Rating (decimal)
├── RatingCount (int)
├── EmergencyContacts (string, delimited)
└── HasUnderlyingDisease (bool)

ExpertProfiles
├── Id (Guid, PK, FK -> AspNetUsers)
├── Biography (string)
├── IsOnline (bool)
├── ConsultationFee (decimal)
├── Rating (decimal)
└── RatingCount (int)

RescuerProfiles
├── Id (Guid, PK, FK -> AspNetUsers)
├── IsOnline (bool)
├── Rating (decimal)
├── RatingCount (int)
└── Type (int) = 0 (Emergency), 1 (Catching), 2 (Both)
```

---

## Known Limitations

1. **Refresh Token Security**: Stored in plain text (not hashed)
2. **No Email Confirmation**: Email/password registration doesn't send confirmation email
3. **No `/me` Endpoint**: No endpoint to get current user profile
4. **No Token Revocation**: No way to manually revoke access tokens before expiry
5. **No Password Reset**: No forgot password flow

---

**End of Documentation**
