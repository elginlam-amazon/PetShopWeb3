# PetShopWeb: .NET 6.0 → .NET 10.0 upgrade

## Result
- `dotnet build PetShopWeb/PetShopWeb.csproj --no-incremental`: **0 errors, 0 warnings**.
- `dotnet test PetShopWeb/PetShopWeb.csproj`: exit code 0. The solution has no test projects, which matches the baseline.

## Starting state
The AWS Transform run left the project unchanged: it was already SDK-style ASP.NET Core 6 (`Microsoft.NET.Sdk.Web`) and had no System.Web, EF6, packages.config, Web.config or Global.asax.

## Changes
- **PetShopWeb.csproj**
  - `TargetFramework`: `net6.0` → `net10.0`.
  - `Microsoft.AspNetCore.Identity.EntityFrameworkCore`, `Microsoft.EntityFrameworkCore.Proxies`, `Microsoft.EntityFrameworkCore.SqlServer`, `Microsoft.EntityFrameworkCore.Tools` → `10.0.15`. Before this change the versions were mixed 6.x/7.x.
  - `Microsoft.VisualStudio.Web.CodeGeneration.Design` → `10.0.3`.
  - Pinned the transitive `NuGet.Packaging` / `NuGet.Protocol` to `6.12.5`, which fixes NU1901 (GHSA-g4vj-cjjj-v7hg in 6.12.1, pulled in by the scaffolding package).
- **Program.cs**
  - Replaced `app.UseEndpoints(...)` with top-level `app.MapControllerRoute(...)` (ASP0014). The route template is unchanged.
  - Fixed nullable annotations: `string? connectionString` and `adminRole.Name!`.
- **AccountController.cs**: Fixed nullable warnings that appeared because Identity 10 has stricter nullable annotations. Most fixes add `!` on values that are already guaranteed, for example `[Required]` view-model fields after `ModelState.IsValid`, or role and user names loaded from the store.
  - **Small behavior change**: in `EditUsersInRole` (POST), a user id that no longer exists is now skipped (`continue`). Before, it caused an `ArgumentNullException` and a 500 error.
- **ImageExtensionAttribute.cs**: `string path` → `string? path`. The logic is unchanged.
- **Dockerfile**: changed the base images from `sdk:6.0` / `aspnet:6.0` to `sdk:10.0` / `aspnet:10.0`. Added `ENV ASPNETCORE_HTTP_PORTS=80` because .NET 8+ images listen on 8080 by default, and docker-compose maps `3000:80`.

## Next steps
- Rebuild and push the `yarins2000/petshopapp` image. `docker-compose.yml` points to the prebuilt `:1.0` tag, which is still .NET 6.
- `Program.cs` calls `EnsureDeleted()` + `EnsureCreated()` on every startup, in every environment. This is unchanged from the original, but it wipes the database on each start in production. A human should decide whether to keep it.
- `appsettings.json` contains a commented-out connection string with an `sa` password. Move secrets to environment variables or user-secrets.
- The admin seeding code checks `UserName == "Admin"` but creates `admin@admin.com`, so the check never matches. This is an existing logic bug and was left as-is.
- `CreateRoleViewModel` uses `Microsoft.Build.Framework.RequiredAttribute` instead of `System.ComponentModel.DataAnnotations.RequiredAttribute`, so MVC does not validate it. This is existing behavior and was left as-is.
- The `.csproj` has `<Content Remove="C:\Users\PC\.nuget\...">` entries left over from a Windows machine. They do nothing on Linux and could be cleaned up.
- No automated tests exist. Consider adding tests for the controllers and repositories.
