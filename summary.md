# PetShopWeb: .NET 6.0 to .NET 10.0 migration summary

AWS Transform Custom (atx) stopped before it changed anything: the service quota was exceeded (see `/data/iteration-2/code-upgrade.log`). I did the whole migration in this cycle.

## Starting state
- `PetShopWeb/PetShopWeb.csproj` was already an SDK-style ASP.NET Core project (`Microsoft.NET.Sdk.Web`) using EF Core and ASP.NET Core Identity.
- The project had no System.Web, EF6, packages.config, Web.config or Global.asax. No System.Web or EF6 migration was needed.

## Changes
- **PetShopWeb.csproj**
  - The target framework changed from `net6.0` to `net10.0`.
  - These packages moved to 10.0.15: `Microsoft.AspNetCore.Identity.EntityFrameworkCore`, `Microsoft.EntityFrameworkCore.Proxies`, `Microsoft.EntityFrameworkCore.SqlServer` and `Microsoft.EntityFrameworkCore.Tools`.
  - `Microsoft.VisualStudio.Web.CodeGeneration.Design` moved to 10.0.3 and is marked `PrivateAssets=all` because it is a design-time tool.
  - The transitive `NuGet.Packaging` and `NuGet.Protocol` packages are pinned to 6.12.5. This fixes NU1901 for GHSA-g4vj-cjjj-v7hg.
  - I removed the stale `<Content Remove="C:\Users\PC\.nuget\...bootstrap...">` entries. They were Windows-only absolute paths for a package the project no longer references.
- **Program.cs**
  - The connection string now comes from `GetConnectionString("DefaultConnection")` and is declared as `string?`. It reads the same configuration key as before.
  - `UseEndpoints` was replaced with top-level `app.MapControllerRoute`, using the same route template (fixes ASP0014).
  - The middleware order is unchanged.
- **AccountController.cs and ImageExtensionAttribute.cs**: these changes fix nullable warnings (CS8600, CS8604, CS8620) raised by the stricter nullable annotations in Identity 7+.
  - Values that are `[Required]` and already checked by `ModelState.IsValid` use the `!` operator. Role and user names loaded from the Identity store also use `!`.
  - When an Id from the form is null, `EditUser` (POST) and `EditRole` (POST) now take their existing "not found" branch. Before, the null Id was passed to `FindByIdAsync`.
  - **Behaviour change:** `EditUsersInRole` (POST) now skips an entry whose user can no longer be found. Before, it threw an exception.
  - **Behaviour change:** `ManageUserRoles` (POST) now filters out null role names before calling `AddToRolesAsync`. Before, a null name caused an exception.
- **Dockerfile**
  - The SDK and runtime images moved from `6.0` to `10.0`.
  - `ASPNETCORE_HTTP_PORTS=80` is set. .NET 8+ images listen on 8080 by default, and this keeps the existing `EXPOSE 80` and docker-compose `3000:80` mapping working.

## Verification
- `dotnet build PetShopWeb/PetShopWeb.csproj` succeeds in both Debug and RELEASE with 0 errors and 0 warnings.
- `dotnet build PetShopWeb.sln` succeeds with 0 errors.
- The atx validation command `dotnet test ./PetShopWeb/PetShopWeb.csproj -p:WarningLevel=0 --configuration RELEASE --no-build` exits with code 0. The solution has no test projects.

## Next steps
- `ViewModels/CreateRoleViewModel.cs` uses `Microsoft.Build.Framework.Required`, which is an MSBuild attribute and not the DataAnnotations `[Required]`. As a result, `RoleName` is never validated.
  - The code only compiles because `Microsoft.VisualStudio.Web.CodeGeneration.Design` brings in `Microsoft.Build.Framework` transitively. If that package is removed, the build breaks.
  - The same problem existed before the migration, so I left it unchanged. Recommended fix: change the using to `System.ComponentModel.DataAnnotations`. This would turn on validation that is currently skipped.
- `Program.cs` calls `EnsureDeleted()` and then `EnsureCreated()` on every startup in every environment. That wipes the database each time. This also predates the migration and needs a product decision, for example limiting it to Development.
- EF Core 10 and SqlClient 6 encrypt SQL Server connections by default. Both connection strings in `appsettings.json` already set `TrustServerCertificate=True`, so this is fine today. The connection strings also contain a hardcoded `sa` password in a comment and use a LocalDB instance. Move secrets to environment variables or user-secrets, and set environment-specific connection strings for test and prod.
- `docker-compose.yml` refers to the pre-built images `yarins2000/petshopapp:1.0` and `petshopdb:1.0`. They need to be rebuilt and republished from the updated Dockerfile.
