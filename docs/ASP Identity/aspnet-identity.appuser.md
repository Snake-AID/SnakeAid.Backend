# Account as Identity User (AspNetUsers)

## Decision
- Use `Account` as the Identity user entity (front-facing model).
- `Account` will map to the `AspNetUsers` table via ASP.NET Identity.

## What this means
- `Account` must inherit `IdentityUser<Guid>`.
- Identity infrastructure (UserManager/RoleManager/SignInManager) will operate on `Account`.
- Custom fields live directly on `Account` (FullName, AvatarUrl, PhoneVerified).
- Roles are managed via Identity roles, not `AccountRole`.

## Impact
- Removes the need for a separate `AppUser` class.
- All auth flows (register/login/refresh) use `Account` directly.
- JWT claims can be built from `Account` fields plus Identity roles.

## Role Strategy
- Seed roles: Admin, User, Expert, Rescuer.
- Assign roles using UserManager.
- Use `[Authorize(Roles = "Admin")]` and `User.IsInRole`.

## Refresh Token Storage
- Use `AspNetUserTokens` (raw values).
