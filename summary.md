# PetShopWeb: .NET 6.0 → .NET 10.0 upgrade

## Before the upgrade (from CodeKB)
PetShopWeb was already an ASP.NET Core 6 MVC app. It used EF Core (`PetShopContext`, an IdentityDbContext with seeded categories and animals), ASP.NET Core Identity (`AccountController` handled login, registration, and user and role management), and a repository layer (Animal, Category, Comment, Account). It had no System.Web, EF6 or Web.config code, so the System.Web, EF6 and Global.asax parts of the knowledge base did not apply.

## Changes
- `PetShopWeb.csproj`: `net10.0`. Identity.EntityFrameworkCore, EF Core Proxies, SqlServer and Tools are on 10.0.15, and CodeGeneration.Design is on 10.0.3. (These changes were already applied when this cycle started.)
- `PetShopWeb.csproj`: pinned the transitive `NuGet.Packaging` and `NuGet.Protocol` packages (they come in through CodeGeneration.Design) to 6.12.5. This clears NU1901 for advisory GHSA-g4vj-cjjj-v7hg.
- `Program.cs`:
  - Replaced `UseEndpoints` with a top-level `app.MapControllerRoute` (ASP0014). The route template is unchanged.
  - The connection string is now read with `GetConnectionString("DefaultConnection")`. If it is missing, startup fails immediately with a clear error. This is a small behaviour change: before, the failure only showed up when the app first connected to the database.
- `AccountController.cs`, `ImageExtensionAttribute.cs`: fixed nullable warnings that the .NET 10 Identity annotations raise. The `!` operator is used on values that are `[Required]` and validated, or that Identity always sets (`role.Name`, `user.UserName`). In `EditUsersInRole` (POST), a user id that is not found is now skipped. Before, Identity threw an `ArgumentNullException` in that case.
- `Dockerfile`: SDK and runtime images moved to `10.0`. Added `ASPNETCORE_HTTP_PORTS=80` so the container still listens on port 80, which is what docker-compose maps. .NET 8+ images listen on 8080 by default.

## Result
`dotnet build PetShopWeb/PetShopWeb.csproj --no-incremental`: 0 errors, 0 warnings. The solution has no test projects.

## Next steps
- Add a test project; there is currently no automated test coverage.
- Startup still calls `EnsureDeleted()`/`EnsureCreated()` on every run, in every environment. This behaviour is unchanged from before. Someone should decide whether production should use EF migrations instead.
- `appsettings.json` contains a commented-out connection string with an `sa` password. Move secrets to environment variables or user-secrets.
- The pre-built images that `docker-compose.yml` uses (`yarins2000/petshopapp:1.0`) are .NET 6 builds. Rebuild and republish them from the updated Dockerfile.
- `CreateRoleViewModel` uses `Microsoft.Build.Framework.RequiredAttribute`, which is probably an IDE auto-import mistake. That attribute does not do MVC validation. I left it as is to keep behaviour the same. Consider switching to `System.ComponentModel.DataAnnotations.Required`.
