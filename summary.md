# PetShopWeb: .NET 6.0 → .NET 10.0 upgrade

## Result
- `dotnet build ./PetShopWeb/PetShopWeb.csproj --no-incremental`: **0 errors, 0 warnings**.
- The solution has no test projects, so no tests were run.

## Starting point
The project was already ASP.NET Core 6 (SDK-style, minimal hosting, EF Core, ASP.NET Core Identity). It had no System.Web, EF6, OWIN or Web.config usage, so the pre-build cleanup steps (KB 00) found nothing to change.

## Changes
- `PetShopWeb.csproj`
  - `TargetFramework` set to `net10.0`. EF Core and Identity packages moved to `10.0.15`, and `Microsoft.VisualStudio.Web.CodeGeneration.Design` to `10.0.3`.
  - Added direct references to `NuGet.Packaging` and `NuGet.Protocol` `6.12.5`. These pin patched versions of transitive dependencies of CodeGeneration.Design and fix NU1901 (GHSA-g4vj-cjjj-v7hg).
- `Program.cs`
  - Replaced `UseEndpoints` with top-level `app.MapControllerRoute` (ASP0014). The route is unchanged.
  - Fixed nullability annotations: `string? connectionString` and `adminRole.Name!`.
- `Controllers/AccountController.cs`: fixed the CS8604/CS8620 nullability warnings with `!` on values that are guaranteed by `[Required]` + `ModelState.IsValid` or by Identity data.
  - **Small behavioral change:** in `EditUsersInRole` (POST), a posted user id that no longer exists is now skipped. Before, it threw a null-reference exception.
- `Attributes/ImageExtensionAttribute.cs`: `string? path` (CS8600).
- `Dockerfile`
  - Base images changed from `sdk:6.0`/`aspnet:6.0` to `sdk:10.0`/`aspnet:10.0`.
  - Added `ENV ASPNETCORE_HTTP_PORTS=80`. .NET 8+ images listen on 8080 by default, and this keeps the `3000:80` mapping in `docker-compose.yml` working.

## Next steps
- `ViewModels/CreateRoleViewModel.cs` uses `using Microsoft.Build.Framework;`. Its `[Required]` is MSBuild's attribute, not `System.ComponentModel.DataAnnotations.RequiredAttribute`, so `RoleName` is not validated. It compiles only because `Microsoft.Build.Framework` comes in through CodeGeneration.Design. This was left unchanged to preserve existing behavior. Recommend switching it to `System.ComponentModel.DataAnnotations`.
- `Program.cs` calls `EnsureDeleted()` and then `EnsureCreated()` on every startup in all environments, so the database is wiped on each start. This was pre-existing and is unchanged. Consider limiting it to Development, or moving to EF migrations, before production use.
- The admin seed password (`Admin123`) and the commented-out SQL connection string in `appsettings.json` are hard-coded. Move them to user-secrets or environment variables.
- `docker-compose.yml` references prebuilt images (`yarins2000/petshopapp:1.0`). These need rebuilding from the updated Dockerfile.
- The `aspnet:10.0` image still runs as root by default. To run as non-root (`USER app`), the app must listen on a port above 1024 (for example 8080), and the compose mapping must be updated to match.
