# PetShopWeb: .NET 6.0 → .NET 10.0 upgrade

## Result
`dotnet build PetShopWeb/PetShopWeb.csproj --no-incremental`: **0 errors, 0 warnings**.
The solution has no test projects.

## Starting point (pre-upgrade code, mapped via CodeKB)
PetShopWeb is an ASP.NET Core 6 MVC app with minimal hosting (`Program.cs`). It uses EF Core with SQL Server and lazy-loading proxies (`PetShopContext`, which seeds categories and animals), ASP.NET Core Identity (an admin role and user are seeded at startup), and repository classes for animals, categories, comments and accounts. `AccountController` handles login, registration, and user, role and user-role management. The app had no System.Web, EF6 or Web.config code, so the System.Web, EF6 and Web.config migration steps did not apply.

## Changes
- **PetShopWeb.csproj**: `net6.0` → `net10.0`. Packages updated to 10.0.x: Identity.EntityFrameworkCore, EF Core Proxies/SqlServer/Tools 10.0.15, and CodeGeneration.Design 10.0.3. Added direct references to `NuGet.Packaging` and `NuGet.Protocol` 6.12.5. These pin patched versions of transitive dependencies of CodeGeneration.Design and fix NU1901 (GHSA-g4vj-cjjj-v7hg, which affects 6.12.1).
- **Program.cs**: `app.UseEndpoints(...)` replaced with top-level `app.MapControllerRoute(...)` (ASP0014). The route is unchanged. Nullable annotations were adjusted (`string?` connection string, `adminRole.Name!`).
- **AccountController.cs**: Identity 7+ added nullable annotations, which produced CS8604/CS8620 warnings. Fixed as follows:
  - Used `!` where the value is known to be non-null: `[Required]` view-model fields after `ModelState.IsValid`, and role/user names loaded from the store.
  - `EditUser`/`EditRole` POST: a null `Id` now goes to the existing "not found" branch. Before, `FindByIdAsync(null)` threw `ArgumentNullException`.
  - `EditUsersInRole` POST: a user id that is null or unknown is now skipped (`continue`). Before, the action threw. This is a small behavior change that only affects invalid input.
- **ImageExtensionAttribute.cs**: `string? path` (CS8600). No behavior change.
- **Dockerfile**: SDK and runtime images moved to `10.0`. Added `ENV ASPNETCORE_HTTP_PORTS=80` because .NET 8+ images listen on 8080 by default, and docker-compose maps `3000:80`.

## Next steps
- `Program.cs` calls `EnsureDeleted()` and then `EnsureCreated()` on every startup, in every environment. This is the original behavior and was kept, but it wipes the database on each start. Decide whether it should run only in Development, or move to EF migrations.
- The seeded admin password (`Admin123`) is hardcoded in `Program.cs`. A commented-out `sa` connection string with a password is in `appsettings.json`. Move both to user-secrets or environment variables.
- `docker-compose.yml` maps port `433`. This looks like a typo for `443`, and no HTTPS endpoint is configured. Check it.
- The `NuGet.Packaging`/`NuGet.Protocol` pins can be removed once a CodeGeneration.Design release references patched versions. You could also remove CodeGeneration.Design completely, because it is only needed for scaffolding.
- The application was not run, as instructed. Do a smoke test against SQL Server: login, registration, role management and the animal catalog.
