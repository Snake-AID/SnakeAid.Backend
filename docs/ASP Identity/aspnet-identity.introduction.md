# ASP.NET Core Identity - Introduction

> [!NOTE]
> **Goals**
> - Use ASP.NET Core Identity as the internal authentication/authorization system for SnakeAid
> - Remove Auth0 and focus on Identity + EF Core + PostgreSQL (Supabase)

## Overview
ASP.NET Core Identity provides user/role/claim models, password management, sign-in, lockout, email confirmation, password reset, and external login (Google, etc.).
- Data is stored in the database via EF Core and migrations.
- .NET 8 provides built-in Identity API endpoints, but we will use custom JWT endpoints.

## Database Schema (generated via migrations)
Default Identity tables:
- AspNetUsers
- AspNetRoles
- AspNetUserRoles
- AspNetUserClaims
- AspNetRoleClaims
- AspNetUserLogins
- AspNetUserTokens

> [!TIP]
> You can extend the `Account` model to add custom fields like `FullName`, `AvatarUrl`, `IsActive`, etc.

## Swagger & Auth Endpoints
Identity does not auto-generate Swagger endpoints unless you map them.
When you call AddIdentityApiEndpoints + MapIdentityApi<TUser>(), the default auth endpoints appear in Swagger.

## External Sign-In
Identity supports external sign-in via OAuth/OIDC providers (Google, Microsoft, Facebook, etc.).
You must configure provider client ID/secret to enable them.

## Project Fit Notes
- Use custom auth endpoints (register/login/refresh) backed by ASP.NET Identity under `/api/auth`.
- Use JWT access + refresh tokens issued by the API.
- Clients authenticate via `Authorization: Bearer <jwt_access_token>`.

## Integration Notes
- DB: PostgreSQL (Npgsql) already exists in the project.

> [!WARNING]
> Current auth middleware is JWT + Auth0; it **must be refactored** to use ASP.NET Core Identity instead.
