# PetShopWeb — .NET 6.0 → .NET 10 migration summary

## Scope

Single project: `PetShopWeb/PetShopWeb.csproj` (ASP.NET Core MVC, EF Core, ASP.NET Core Identity, SQL Server).
The project was already SDK-style (`Microsoft.NET.Sdk.Web`), so no System.Web / Global.asax / Web.config
migration was needed — this was a framework-version upgrade, not an ASP.NET Framework port.

## Build status

| Command | Result |
| --- | --- |
| `dotnet build PetShopWeb.sln -c Debug` | **0 errors, 0 warnings** |
| `dotnet build PetShopWeb.sln -c Release` | **0 errors, 0 warnings** |
| `dotnet publish PetShopWeb/PetShopWeb.csproj -c Release` | succeeds |

## Changes applied

### Project and solution
- `PetShopWeb.csproj`: `TargetFramework` `net6.0` → `net10.0`.
- Framework-tied packages aligned to the target runtime major (`10.0.x`):
  - `Microsoft.AspNetCore.Identity.EntityFrameworkCore` 6.0.11 → 10.0.15
  - `Microsoft.EntityFrameworkCore.Proxies` 7.0.0 → 10.0.15
  - `Microsoft.EntityFrameworkCore.SqlServer` 7.0.0 → 10.0.15
  - `Microsoft.EntityFrameworkCore.Tools` 6.0.11 → 10.0.15 (kept dev-time only via `PrivateAssets=all`)
- Removed `Microsoft.VisualStudio.Web.CodeGeneration.Design`. It is a design-time scaffolding tool that the
  application does not call, and it was the only source of the four `NU1901` advisories (transitive
  `NuGet.Packaging` / `NuGet.Protocol` 6.12.1, GHSA-g4vj-cjjj-v7hg). Per KB 27 Step 3 the package was removed
  rather than silenced. *Impact:* `dotnet aspnet-codegenerator` scaffolding is no longer available in this
  project; re-add the package temporarily if scaffolding is needed.
- Removed 44 dead `<Content Remove="C:\Users\PC\.nuget\packages\bootstrap\5.2.2\...">` items. They pointed at a
  hard-coded Windows user profile, the `bootstrap` NuGet package is not referenced (bootstrap is vendored under
  `wwwroot/lib/`), and the paths can never resolve in dev, test or the Linux container.
- `PetShopWeb.sln`: project type GUID updated to the SDK-style project GUID (`9A19103F-…`).

### Code
- `Program.cs`
  - `app.UseEndpoints(...)` → top-level `app.MapControllerRoute("default", "{controller=Home}/{action=HomePage}/{id?}")`
    (clears `ASP0014`; identical route table and middleware order).
  - Connection string now read through `Configuration.GetConnectionString("DefaultConnection")` with an explicit
    `InvalidOperationException` when absent, instead of assigning a possibly-null string. Same configuration key
    (`ConnectionStrings:DefaultConnection`); a missing value now fails fast at startup with a clear message
    instead of surfacing later as a SqlClient error.
  - `AddToRoleAsync(adminUser, adminRole.Name!)` — `adminRole` is constructed with the literal `"admin"`.
- `Controllers/AccountController.cs` — nullable-reference warnings (`CS8604`, `CS8620`) resolved:
  - `[Required]` view-model values (`Email`, `Password`, `RoleName`) and Identity-owned values
    (`IdentityUser.UserName`, `IdentityRole.Name`) use the null-forgiving operator; assertions only, no behaviour change.
  - `EditUser(EditUserViewModel)` and `EditRole(EditRoleViewModel)` no longer pass a possibly-null `Id` into
    `FindByIdAsync`. A null `Id` now takes the existing "not found" branch (`NotFound()` / `RolesManager` view)
    instead of throwing `ArgumentNullException` (HTTP 500). **Behavioural change, intentional.**
  - `EditUsersInRole` (POST): a posted `UserId` that is null or no longer resolves to a user is skipped instead
    of being passed as `null` into `IsInRoleAsync`/`AddToRoleAsync` (previously an `ArgumentNullException` →
    HTTP 500). The remaining posted users are still processed. **Behavioural change, intentional.**
- `Attributes/ImageExtensionAttribute.cs` — local `path` declared `string?` to match `value as string` (`CS8600`).
  Validation result unchanged (null path ⇒ invalid).
- `ViewModels/CreateRoleViewModel.cs` — `using Microsoft.Build.Framework;` → `using System.ComponentModel.DataAnnotations;`.
  The file's `[Required]` was binding to MSBuild's `RequiredAttribute` (leaked in transitively by the scaffolding
  package), which does nothing for model validation. **Behavioural change, intentional:** `RoleName` is now
  genuinely required by MVC validation, matching every other view model and the view's validation message.
  This also removes the project's accidental compile-time dependency on `Microsoft.Build`.

### Container / docs
- `Dockerfile`: `sdk:6.0`/`aspnet:6.0` → `sdk:10.0`/`aspnet:10.0`.
  - Restore/publish now name the project explicitly. The previous `COPY PetShopWeb/*.csproj .` + bare
    `dotnet publish` left both `PetShopWeb.csproj` and `PetShopWeb.sln` in the build directory, which is
    ambiguous for the CLI (`MSB1011`).
  - `ENV ASPNETCORE_URLS=http://+:80` added: .NET 8+ runtime images default to port 8080, and the existing
    `docker-compose.yml` maps `3000:80`. Pinning port 80 keeps the published image's port contract, so
    `docker-compose.yml` was left untouched (it consumes pinned published images, not a local build).
  - Dropped the `EXPOSE 433` line (a typo for 443; no HTTPS endpoint or certificate is configured in the image,
    so nothing ever listened there).
- `README.md`: "ASP.NET Core 6" → "ASP.NET Core 10".

## Verification notes

- No test project exists in this solution, so there were no unit tests to run; no test was deleted, skipped or
  weakened. The verification performed was Debug + Release builds and a Release publish.
- The application was not started, per the migration instructions.
- The first build attempt failed with `MSB3374` (`Access to the path '…/obj/Debug/net10.0/…cache' is denied`)
  because `obj/` and `bin/` from an earlier run were owned by a different user. Deleting `obj/` and `bin/`
  resolved it; it was not a code or project problem.

## Next steps

1. **Database schema is recreated on every startup.** `Program.cs` calls `ctx.Database.EnsureDeleted()` followed by
   `EnsureCreated()` unconditionally, so all data is dropped on each boot and an admin user is re-seeded. This is
   pre-existing behaviour and was deliberately left alone, but it is unsafe for test/prod. Recommended follow-up:
   guard it with `app.Environment.IsDevelopment()`, or switch to EF Core migrations
   (`dotnet ef migrations add` + `ctx.Database.Migrate()`), which the retained `EntityFrameworkCore.Tools`
   reference already supports.
2. **Hard-coded seed admin credentials** (`admin@admin.com` / `Admin123`) live in `Program.cs` and are documented in
   `README.md`. Move to configuration/user-secrets and require a password change on first login before any
   non-development deployment. Needs a product decision.
3. **Connection string in `appsettings.json`** still points at a Windows LocalDB instance
   (`Server=(localdb)\AspNetCoreCourse`), with the container connection string commented out just above it. It
   cannot work in the Linux container; override it per environment with
   `ConnectionStrings__DefaultConnection` (env var) or an environment-specific `appsettings.*.json` rather than
   editing the committed default. Left as-is to avoid changing the developer default without confirmation.
4. **`docker-compose.yml` uses pinned pre-built images** (`yarins2000/petshopapp:1.0`, `yarins2000/petshopdb:1.0`),
   so it will keep running the old .NET 6 image until the updated `Dockerfile` is built and published. Publish a
   new tag (e.g. `:2.0`) and bump the compose reference, or change the `app` service to `build: .`.
   Also note `version: '3.3'` in compose is obsolete for current Docker Compose and can be dropped.
5. **Static assets**: the app uses `app.UseStaticFiles()`. .NET 9+ offers `MapStaticAssets()` with build-time
   fingerprinting and compression. Not adopted here because it changes asset URL generation and all views
   reference assets with plain `~/` paths; worth a dedicated pass.
6. **`libman.json` / `package.json` client libraries** (bootstrap 5.2.2, jQuery 3.6.1, jquery-validation 1.19.5)
   are unrelated to the .NET target but are several major/minor versions behind. Updating them is a separate
   front-end change with visual-regression risk.
7. `Microsoft.EntityFrameworkCore.Design` / `Microsoft.EntityFrameworkCore.Tools` version `10.0.15` is used for all
   EF Core and Identity packages. Re-run `dotnet list package --outdated` before release in case a newer `10.0.x`
   patch ships.
