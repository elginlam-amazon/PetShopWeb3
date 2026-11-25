# PetShopWeb: .NET 6.0 → net10.0 migration

## Result
- `dotnet build ./PetShopWeb/PetShopWeb.csproj` succeeds with 0 errors and 0 warnings (checked with `--no-incremental`).
- The solution has no test projects, so there were no unit tests to run.

## Changes
- **PetShopWeb.csproj**: targets `net10.0`. The EF Core and Identity packages are now 10.0.15, and `Microsoft.VisualStudio.Web.CodeGeneration.Design` is 10.0.3. `NuGet.Packaging` and `NuGet.Protocol` are pinned to 6.12.5. These are transitive dependencies of the scaffolding package, and 6.12.5 is the patched version for GHSA-g4vj-cjjj-v7hg (NU1901).
- **Program.cs**: the connection string is read with `GetConnectionString("DefaultConnection")`, which reads the same key as before and returns `string?`. `UseEndpoints` is replaced by a top-level `app.MapControllerRoute` with the same route (ASP0014). Middleware order is unchanged.
- **Nullable warnings** (CS8600/CS8604/CS8620) are fixed in `AccountController.cs`, `ImageExtensionAttribute.cs` and `Program.cs`. The fixes use null-forgiving operators where `[Required]` combined with `ModelState.IsValid`, or Identity itself, guarantees a value. They use `?? string.Empty` for IDs passed to `FindByIdAsync`, where a null ID already produced a "not found" result.
- **Behavioral notes**:
  - `CreateRoleViewModel` imported `[Required]` from `Microsoft.Build.Framework`, which is an MSBuild attribute and does no validation. It now uses `System.ComponentModel.DataAnnotations`, so an empty role name now fails model validation as the code intended.
  - `EditUsersInRole` (POST) now skips entries whose user ID no longer exists. Before, Identity threw `ArgumentNullException` for these entries.
  - `ManageUserRoles` (POST) ignores selected entries that have a null `RoleName`.
- **Dockerfile**: uses the `sdk:10.0` and `aspnet:10.0` images. It sets `ASPNETCORE_HTTP_PORTS=80` because .NET 8+ images listen on 8080 by default, and `docker-compose.yml` maps to container port 80.

## Next steps
- Dockerfile (problem that existed before this migration): `COPY PetShopWeb/*.csproj .` and then `COPY . .` leave both `PetShopWeb.csproj` and `PetShopWeb.sln` in `/app`. `dotnet restore` and `dotnet publish` with no argument may then fail with MSB1011 (more than one project or solution file). A fix is to copy into `PetShopWeb/` and publish `PetShopWeb/PetShopWeb.csproj` explicitly. This was not changed because it is outside the migration scope.
- `Program.cs` calls `EnsureDeleted()` at every startup, which wipes the database. This was already the behavior and was left as is. Consider limiting it to Development.
- The seeded admin password and the commented-out SQL `sa` password are stored in source and appsettings. Move them to user secrets or environment variables.
- `docker-compose.yml` uses the obsolete `version` key and maps port `433`, which is probably a typo for 443. Neither was changed.
- `Microsoft.EntityFrameworkCore.Proxies` lazy loading behaves the same in EF Core 10. Check the database runtime against SQL Server, because `Microsoft.Data.SqlClient` 6.x keeps `Encrypt=True` as the default (the connection strings already set `TrustServerCertificate=True`).
