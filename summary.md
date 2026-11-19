# PetShopWeb: .NET 6.0 → .NET 10.0 upgrade

## Starting state
The AWS Transform Custom (atx) run stopped immediately with `SERVICE_QUOTA_EXCEEDED`, so it had made no changes. `PetShopWeb.csproj` was already SDK-style ASP.NET Core MVC on `net6.0`. It had no System.Web, EF6, packages.config, Web.config or Global.asax left to migrate.

## Changes
- **PetShopWeb/PetShopWeb.csproj**
  - `TargetFramework` changed from `net6.0` to `net10.0`.
  - These packages moved to `10.0.15`: `Microsoft.AspNetCore.Identity.EntityFrameworkCore`, `Microsoft.EntityFrameworkCore.Proxies`, `Microsoft.EntityFrameworkCore.SqlServer` and `Microsoft.EntityFrameworkCore.Tools`. Before the upgrade they were a mix of 6.0.11 and 7.0.0.
  - `Microsoft.VisualStudio.Web.CodeGeneration.Design` moved from 6.0.10 to `10.0.3`, the latest stable version.
  - Its transitive `NuGet.Packaging` and `NuGet.Protocol` dependencies are pinned to `6.12.5`, the patched version for GHSA-g4vj-cjjj-v7hg (NU1901).
  - Removed 46 `<Content Remove="C:\Users\PC\.nuget\...bootstrap...">` items. They pointed at one developer's local NuGet cache and had no effect.
- **Program.cs**
  - The connection string is now read with `GetConnectionString("DefaultConnection")`. It reads the same key as before and is nullable-correct.
  - `UseEndpoints(...)` was replaced with a top-level `app.MapControllerRoute(...)` to clear analyzer warning ASP0014. The route template is unchanged.
  - `adminRole.Name!` added to clear a nullable warning.
- **Controllers/AccountController.cs**: nullable-reference fixes (CS8604 and CS8620). Most are null-forgiving operators on view-model values that `[Required]` already validates, and on role or user names loaded from Identity.
  - Behavior change: in `EditUsersInRole` (POST), a posted `UserId` that is missing or unknown is now skipped. Before, it threw `ArgumentNullException` from `IsInRoleAsync`.
  - `EditUser` and `EditRole` (POST) now return the existing "not found" result when the posted `Id` is null, instead of throwing.
- **ViewModels/CreateRoleViewModel.cs**: `[Required]` came from `Microsoft.Build.Framework`. That attribute does not validate anything, and the namespace was only available through the scaffolding package's dependencies. It now uses `System.ComponentModel.DataAnnotations`.
  - Behavior change: an empty role name now fails model validation in the controller. Before, Identity rejected it in `RoleManager.CreateAsync`. Either way the form is shown again with an error.
- **Attributes/ImageExtensionAttribute.cs**: the local variable `path` is now `string?`. This only fixes a nullable warning.
- **Dockerfile**: base images changed from `sdk:6.0`/`aspnet:6.0` to `sdk:10.0`/`aspnet:10.0`. Added `ENV ASPNETCORE_HTTP_PORTS=80`, because .NET 8+ images listen on 8080 by default and `docker-compose.yml` maps to container port 80.

## Verification
- `dotnet build ./PetShopWeb/PetShopWeb.csproj --configuration RELEASE --no-incremental` gives 0 errors and 0 warnings.
- `dotnet test ./PetShopWeb/PetShopWeb.csproj --configuration RELEASE --no-build` exits 0. The solution has no test projects.

## Next steps
- **EF Core 6/7 → 10 runtime differences need a check against SQL Server:**
  - `Microsoft.Data.SqlClient` now defaults to `Encrypt=True`. The localdb connection string already sets `TrustServerCertificate=True`.
  - EF Core 9+ throws on pending model changes. It does not apply here because the app uses `EnsureCreated`, not migrations.
- **Startup deletes the database on every run:** `Program.cs` calls `EnsureDeleted()` and then `EnsureCreated()`. This was already true before the upgrade and was left unchanged. It needs a product decision before production use.
- **Connection string is not portable:** `appsettings.json` points at `(localdb)\AspNetCoreCourse`, which works only on Windows. The commented-out container connection string contains a plaintext `sa` password. Move it to environment variables or user secrets for each environment.
- **Scaffolding package ships with the app:** `Microsoft.VisualStudio.Web.CodeGeneration.Design` is design-time tooling, but it is not marked `PrivateAssets="all"`, so its assemblies go into the publish output. Consider marking it private or removing it.
- **Docker image not built in this cycle:** the updated Dockerfile was not tested by building an image.
