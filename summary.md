# Migration Summary: PetShopWeb .NET 6 → .NET 10

## Result
`dotnet build PetShopWeb/PetShopWeb.csproj` exits with **0 errors, 0 warnings**.

## Changes Made

### Project file (`PetShopWeb.csproj`)
- Removed `Microsoft.VisualStudio.Web.CodeGeneration.Design` (design-time scaffolding tool, not needed at runtime; its transitive dependencies `NuGet.Packaging 6.12.1` and `NuGet.Protocol 6.12.1` carried low-severity NU1901 vulnerability advisories).

### Program.cs
- Fixed CS8600: connection string retrieval now uses `?? throw` to handle nullable Configuration value.
- Fixed CS8604: `adminRole.Name!` null-forgiving operator (name is set at construction).
- Fixed ASP0014: replaced `app.UseEndpoints(...)` with top-level `app.MapControllerRoute(...)`.

### ViewModels
- `LoginViewModel` — `Email` and `Password` changed from `string?` to `string` (both are `[Required]`).
- `RegisterViewModel` — `Email` and `Password` changed from `string?` to `string`.
- `EditUserViewModel` — `Id`, `UserName`, `Email` changed from `string?` to `string`.
- `EditRoleViewModel` — `Id` and `RoleName` changed from `string?` to `string`.
- `UserRoleViewModel` — `UserId` changed from `string?` to `string`.
- `CreateRoleViewModel` — fixed wrong `using Microsoft.Build.Framework` (MSBuild's Required attribute) → `using System.ComponentModel.DataAnnotations`; `RoleName` changed from `string?` to `string`.

### AccountController.cs
- Added null-forgiving `!` operators where ASP.NET Core Identity returns `string?` (`IdentityRole.Name`, `IdentityUser.UserName`, `IdentityUser.Email`) but values are guaranteed present by Identity internals.
- Added null guard (`if (user is null) continue`) in `EditUsersInRole` POST loop where `FindByIdAsync` returns `IdentityUser?`.
- Fixed CS8620: `.Select(u => u.RoleName!)` for `AddToRolesAsync` call.

### Attributes/ImageExtensionAttribute.cs
- Fixed CS8600: changed `string path` to `string? path` to allow `value as string` assignment.

## Next Steps
- None. Build is clean with 0 errors and 0 warnings.
