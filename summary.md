# PetShopWeb: .NET 6.0 → net10.0 migration summary

## Result
- `dotnet build ./PetShopWeb/PetShopWeb.csproj --no-incremental`: **0 errors, 0 warnings**.
- `dotnet build PetShopWeb.sln`: succeeds.
- The solution has no test projects, so `dotnet test` has nothing to run.
- The codebase does not reference `System.Web.*`, `System.Data.Entity` or `System.Web.Http.*`. It was already ASP.NET Core 6 with EF Core.

## Changes
- **PetShopWeb.csproj**: targets `net10.0`. EF Core and Identity packages are on 10.0.15, and `Microsoft.VisualStudio.Web.CodeGeneration.Design` is on 10.0.3. Added direct references to `NuGet.Packaging` and `NuGet.Protocol` 6.12.5. This pins the patched versions of these transitive scaffolding dependencies and clears NU1901 for GHSA-g4vj-cjjj-v7hg.
- **Program.cs**:
  - The connection string is now read with `GetConnectionString("DefaultConnection")` into a `string?`. Same config key as before.
  - Replaced `UseEndpoints` with top-level `app.MapControllerRoute` (ASP0014). The route template is unchanged.
  - Added a null-forgiving operator on `adminRole.Name`. The value is always "admin".
- **AccountController.cs**: Identity 7+ added nullable annotations, which raised CS8604/CS8620 warnings. I resolved them with null-forgiving operators where `[Required]` validation or Identity's guarantees already ensure a value.
  - **Behavioral change**: in `EditUsersInRole` (POST), a user id that no longer exists is now skipped. Before, this case threw an exception inside `UserManager`.
- **ImageExtensionAttribute.cs**: `path` is now declared `string?`. No logic change.
- **Dockerfile**: base images changed to `sdk:10.0` and `aspnet:10.0`. Added `ASPNETCORE_HTTP_PORTS=80`, because .NET 8+ images listen on 8080 by default. This keeps the `3000:80` mapping in docker-compose.yml working.
- **PetShopWeb.sln**: the project type GUID was already updated to the SDK-style C# GUID before this cycle.

## Next steps
- `ViewModels/CreateRoleViewModel.cs` applies `[Required]` from `Microsoft.Build.Framework` (an MSBuild attribute) instead of `System.ComponentModel.DataAnnotations`. As a result, `RoleName` is not validated. This bug predates the migration. Switching the namespace would turn validation on, which changes behavior. It also stops the file from depending on an assembly that only arrives transitively through the scaffolding package. Needs a product decision.
- `Program.cs` calls `EnsureDeleted()` and then `EnsureCreated()` on every startup, in every environment. It also seeds an admin user with a hard-coded password (`Admin123`). Both predate the migration. Consider limiting this to Development and moving the seed credentials into configuration or secrets.
- `appsettings.json` contains a commented-out SQL connection string with an `sa` password. It should be moved to environment variables or user secrets.
- The admin seed checks `u.UserName == "Admin"` but creates the user as `admin@admin.com`, so the check never matches. This is pre-existing and was left unchanged.
- The csproj has `<Content Remove="C:\Users\PC\.nuget\...">` entries with Windows-specific paths. They do nothing on Linux and can be removed after a check on Windows.
- `Microsoft.VisualStudio.Web.CodeGeneration.Design` is only used for design-time scaffolding. Consider removing it, together with the NuGet.* pins, if scaffolding is no longer needed.
- I did not run the app or test the container image in this cycle. Smoke-test the Docker image and SQL Server connectivity on .NET 10.
