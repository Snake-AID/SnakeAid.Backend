# Implementation Plan - ASP.NET Core Identity (Prompt)

## Sprint Goals
- Replace Auth0 with ASP.NET Core Identity.
- Ensure register/login/refresh works with JWT access + refresh tokens.
- Swagger shows auth endpoints.
- Support Flutter (main) and React (admin) clients.

## Implementation Decisions
- Reuse `SnakeAidDbContext` for Identity tables (same schema).
- Use `Account : IdentityUser<Guid>` as the Identity user model (front-facing).
- Add custom fields to `Account`: `FullName`, `AvatarUrl`, `PhoneVerified`.
- Use JWT access + refresh tokens (no opaque tokens, no cookies).
- Custom auth endpoints (register/login/refresh) under `/api/auth`.
- Login identifier: Email.
- Support external identity provider: Google sign-in.
- Seed one record for each role (Admin, User, Expert, Rescuer).
- Refresh tokens stored in `AspNetUserTokens` (raw).
- JWT claims derived from `Account` fields (UserName, FullName, Email, PhoneNumber, IsActive) + Identity roles.
- JWT settings use `JwtSettings` from `appsettings.json`.
- No existing database to migrate at this time.

## Proposed Steps
1) Add Identity to the project
- Add Identity packages and EF Core store (if missing).
- Update `Account` to inherit `IdentityUser<Guid>` and add required custom fields.

2) Configure DbContext
- Update `SnakeAidDbContext` to inherit from `IdentityDbContext<Account, IdentityRole<Guid>, Guid>`.
- Register `AddIdentityCore<Account>()` + `AddRoles<IdentityRole<Guid>>()` + `AddEntityFrameworkStores<SnakeAidDbContext>()`.

3) Add Auth Endpoints (JWT)
- Create endpoints for register/login/refresh under `/api/auth` using `UserManager` + `SignInManager`.
- Issue JWT access token + refresh token using `JwtSecurityTokenHandler`.
- Return tokens in the API response.

4) Remove Auth0
- Remove Authority/Audience Auth0 settings in AddJwtBearer.
- Configure `AddAuthentication().AddJwtBearer(...)` with `Issuer`, `Audience`, and `SecretKey`.

5) External Provider (Google)
- Configure Google authentication and map to Identity.
- After external login, issue JWT tokens from our API.

6) Seed Roles
- Insert one record for each role (Admin, User, Expert, Rescuer) on startup if not present.

7) Testing
- Register a new user.
- Login and obtain JWT access/refresh tokens.
- Call [Authorize] endpoints.
- Refresh the access token and retry an authorized call.
- Sign in with Google and obtain JWT tokens.

## Required Config
- Valid PostgreSQL connection string.
- Identity options (Password, Lockout, SignIn) from `appsettings.json`.
- JWT settings (Issuer, Audience, SecretKey, RefreshSecretKey, AccessTokenExpirationMinutes, RefreshTokenExpirationDays).
- Google Client ID for external sign-in (Authentication:Google:ClientId).

## Suggested appsettings.json structure (to be implemented)
```
{
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
  "Jwt": {
    "SecretKey": "your-access-token-secret",
    "RefreshSecretKey": "your-refresh-token-secret",
    "Issuer": "SnakeAid",
    "Audience": "SnakeAid.Clients",
    "AccessTokenExpirationMinutes": 60,
    "RefreshTokenExpirationDays": 30
  },
  "Authentication": {
    "Google": {
      "ClientId": "your-google-client-id"
    }
  }
}
```

## Expected Output
- Auth endpoints working.
- DB contains AspNet* tables.
- Swagger can test login/register.
