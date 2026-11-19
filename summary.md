# PetShopWeb: .NET 6.0 to .NET 10 migration summary

AWS Transform Custom stopped before making any changes because its service quota ran out (`SERVICE_QUOTA_EXCEEDED`), so this cycle did the whole migration.

## Result
- `dotnet build ./PetShopWeb/PetShopWeb.csproj --configuration RELEASE --no-incremental` finishes with 0 errors and 0 warnings.
- The solution has no test projects, so `dotnet test` has nothing to run.

## Changes
### PetShopWeb/PetShopWeb.csproj
- `TargetFramework` changed from `net6.0` to `net10.0`.
- Packages upgraded:
  - `Microsoft.AspNetCore.Identity.EntityFrameworkCore`, `Microsoft.EntityFrameworkCore.Proxies`, `.SqlServer` and `.Tools`: 6.0.11/7.0.0 to 10.0.15.
  - `Microsoft.VisualStudio.Web.CodeGeneration.Design`: 6.0.10 to 10.0.3.
- Added direct references to `NuGet.Packaging` and `NuGet.Protocol` 6.12.5. These packages come in through the scaffolding package, and 6.12.1 has a known vulnerability (NU1901, GHSA-g4vj-cjjj-v7hg).
- Removed the `<Content Remove="C:\Users\PC\.nuget\...bootstrap...">` entries. They pointed at one developer's machine and did nothing, because the project no longer references the bootstrap package.

### Code (fixes for nullable warnings from the Identity APIs, which have nullable annotations since .NET 7)
- `Program.cs`: the connection string is now `string?`. `adminRole.Name!` is used because the constructor sets it. `UseEndpoints` was replaced with top-level `app.MapControllerRoute` (warning ASP0014). The route and the middleware order are unchanged.
- `Attributes/ImageExtensionAttribute.cs`: `path` is now `string?`. Behaviour is unchanged.
- `Controllers/AccountController.cs`:
  - Added `!` to view-model values that `[Required]` validation already guarantees, and to `IdentityRole.Name` and `IdentityUser.UserName`.
  - `EditUser` (POST) and `EditRole` (POST) now call `FindByIdAsync(id ?? string.Empty)`. **Behaviour change:** a missing id now returns the existing NotFound / RolesManager result instead of throwing `ArgumentNullException`.
  - `EditUsersInRole` (POST) now skips a posted user id that does not exist. **Behaviour change:** before, `IsInRoleAsync` threw `ArgumentNullException` for that id.

### Dockerfile
- The SDK and ASP.NET base images changed from `6.0` to `10.0`.
- Added `ENV ASPNETCORE_HTTP_PORTS=80`. Since .NET 8, the container images listen on port 8080 by default, and this setting keeps the app on port 80, which `docker-compose.yml` maps (`3000:80`).

## Next steps
- `ViewModels/CreateRoleViewModel.cs` imports `Microsoft.Build.Framework`, so its `[Required]` is MSBuild's attribute and not `System.ComponentModel.DataAnnotations.Required`. This means `RoleName` is never validated. The code only compiles because the scaffolding package brings in `Microsoft.Build.Framework` indirectly. This bug existed before the migration and was left unchanged. Recommended fix: change the using to `System.ComponentModel.DataAnnotations`.
- `docker-compose.yml` uses prebuilt images (`yarins2000/petshopapp:1.0`, `petshopdb:1.0`), which have to be rebuilt from the updated Dockerfile.
- `Program.cs` calls `EnsureDeleted()` and then `EnsureCreated()` on every startup, which wipes the database in every environment. This existing behaviour was kept; check whether it is intended outside development.
- The app has not been run, as this cycle's rules require. Smoke-test login, the role/user management pages and the lazy-loaded catalog pages against SQL Server, using EF Core 10 and Identity 10.
