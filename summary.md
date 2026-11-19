# PetShopWeb: .NET 6.0 → .NET 10 migration summary

## Result
- `dotnet build PetShopWeb/PetShopWeb.csproj` and `dotnet build PetShopWeb.sln`: **0 errors, 0 warnings**.
- The solution has no test projects, so there were no unit tests to run.

## Starting state
The app was already an ASP.NET Core MVC app (SDK-style `Microsoft.NET.Sdk.Web`, minimal-hosting `Program.cs`, EF Core and ASP.NET Core Identity). It contains no `System.Web`, `System.Data.Entity`, or `Global.asax`/`Web.config` code. The project file already targeted `net10.0` with `10.0.15` EF Core/Identity packages and `Microsoft.VisualStudio.Web.CodeGeneration.Design` 10.0.3. On the first build, the project compiled with 0 errors and 21 warnings.

## Changes
| File | Change |
| --- | --- |
| `PetShopWeb/PetShopWeb.csproj` | Pinned `NuGet.Packaging` and `NuGet.Protocol` to 6.12.5. Both are transitive dependencies of `CodeGeneration.Design`, and 6.12.1 was flagged by NU1901 (GHSA-g4vj-cjjj-v7hg). 6.12.5 is the first patched release in the same minor line. |
| `PetShopWeb/Program.cs` | Replaced `UseEndpoints(...)` with top-level `app.MapControllerRoute(...)` (ASP0014). The route template and middleware order are unchanged. The connection string is now read with `GetConnectionString("DefaultConnection")`, and startup fails with a clear `InvalidOperationException` if it is missing. Before, startup failed later inside EF. Added a null-forgiving `!` on the seeded admin role name, which is always set by the constructor. |
| `PetShopWeb/Controllers/AccountController.cs` | Fixed nullable warnings. Added `!` on `[Required]` view-model fields after `ModelState.IsValid`, and on Identity `Role.Name`/`UserName`, so behavior is unchanged. When `EditUser`/`EditRole` POST receive a null `Id`, they now return the existing not-found result instead of throwing `ArgumentNullException`. `EditUsersInRole` POST now skips rows whose user id is null or unknown. Before, such a row threw `ArgumentNullException` (HTTP 500). |
| `PetShopWeb/Attributes/ImageExtensionAttribute.cs` | Changed the local `path` to `string?` (CS8600). Logic is unchanged. |
| `Dockerfile` | Changed the base images from `sdk:6.0`/`aspnet:6.0` to `sdk:10.0`/`aspnet:10.0`. Added `ENV ASPNETCORE_HTTP_PORTS=80`, because .NET 8+ images listen on 8080 by default and `docker-compose.yml` maps `3000:80`. |

## Next steps
- `ViewModels/CreateRoleViewModel.cs` uses `[Required]` from `Microsoft.Build.Framework`, an MSBuild attribute that ASP.NET Core model validation does not enforce. It compiles only because `CodeGeneration.Design` pulls in MSBuild. This bug existed before the migration and was left unchanged. Switching to `System.ComponentModel.DataAnnotations` would turn on server-side "role name required" validation, which needs a product decision.
- `Program.cs` calls `EnsureDeleted()` followed by `EnsureCreated()` on every start, so the database is wiped on each launch in every environment, including production. This is pre-existing behavior and was not changed. Consider limiting it to Development, or moving to EF migrations.
- The admin seeding in `Program.cs` checks `UserName == "Admin"` but creates `admin@admin.com`, so the check never matches. This is pre-existing and was not changed.
- The `Dockerfile` copies the whole repo into `/app`, next to the copied `.csproj`, and then runs `dotnet publish` with no project argument. Because `/app` then contains both `PetShopWeb.csproj` and `PetShopWeb.sln`, `publish` may fail with MSB1011. This is pre-existing. Consider `dotnet publish PetShopWeb/PetShopWeb.csproj`. The Docker image was not built in this cycle.
- `docker-compose.yml` uses prebuilt images (`yarins2000/petshopapp:1.0`), which need to be rebuilt from the updated Dockerfile.
- `PetShopWeb.csproj` contains `Content Remove` entries for absolute Windows NuGet cache paths (`C:\Users\PC\.nuget\...`). They do nothing on Linux and are harmless. They were kept to avoid unrelated changes.
- `appsettings.json` contains a commented-out connection string with a plaintext `sa` password. Move secrets to environment variables or user secrets.
