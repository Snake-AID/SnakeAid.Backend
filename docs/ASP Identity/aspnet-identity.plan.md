# Direction & QnA: ASP.NET Core Identity

## Decisions
- Remove Auth0.
- Use ASP.NET Core Identity directly.
- Use custom JWT auth endpoints (register/login/refresh) under `/api/auth`.
- Clients: Flutter (main) and React (admin).
- Use JWT access + refresh tokens (not opaque, not cookies).
- Reuse `SnakeAidDbContext`.
- Use `Account : IdentityUser<Guid>` as the Identity user model.
- Add custom user fields: `FullName`, `AvatarUrl`, `PhoneVerified`.
- Token lifetimes and password policy come from `appsettings.json` (`JwtSettings`).
- Auth endpoints are prefixed with `/api/auth`.
- Login by email.
- Support external provider: Google sign-in.
- Seed one record for each role (Admin, User, Expert, Rescuer).

> [!NOTE]
> No existing database to migrate yet. This is a fresh Identity implementation.

## QnA
### 1) Does the auth schema auto-generate in the DB?
Yes. With Identity + EF Core, the default model generates AspNet* tables via migrations.

### 2) Are Swagger auth endpoints auto-created?
Not automatically. Swagger will show auth endpoints only after we implement and map our JWT auth controller/endpoints.

### 3) Is external sign-in (Google, etc.) supported out of the box?
Yes, but you must configure each provider (client ID/secret).

### 4) How do clients authenticate after removing Auth0?
We will issue JWT access + refresh tokens from our API using ASP.NET Identity. Clients call APIs with `Authorization: Bearer <jwt_access_token>` and refresh when needed.

### 5) Can we still use claims for business logic with JWT?
Yes. The API reads claims from the JWT into `ClaimsPrincipal`, so `[Authorize]`, roles, and claims-based policies still work. Clients can decode JWTs, but the server remains the source of truth. Use `/me` if the client needs verified user info.

### 6) Why use `Account` as the Identity user?
`Account` becomes the Identity user entity stored in `AspNetUsers`. It powers authentication, password hashing, lockout, and roles/claims while keeping your existing domain name. JWT claims can be built directly from `Account` fields (UserName, FullName, Email, PhoneNumber, IsActive) plus Identity roles.

## Risks / Notes

> [!WARNING]
> **Security Considerations**
> - Keep JWT signing keys **secure** and plan for rotation
> - Refresh tokens stored in `AspNetUserTokens` are in **plain text**; consider hashing for production
> - Current Auth0 config should be **removed completely** to avoid conflicts

## Confirmed
- Client types: Flutter (primary), React (admin).
- Auth scheme: JWT bearer tokens (access + refresh).
- Refresh tokens: Yes.
- DbContext: Reuse `SnakeAidDbContext`.
- Custom user fields: Yes (FullName, AvatarUrl, PhoneVerified).
- Use `Account : IdentityUser<Guid>`.
- Config-driven password policy and token lifetimes (`JwtSettings`).
- Future: add a `/me` endpoint so clients can fetch verified user profile/claims.
