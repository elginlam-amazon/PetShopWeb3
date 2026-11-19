# PetShopWeb: .NET 6.0 → .NET 10.0 upgrade

AWS Transform Custom stopped before making any changes (service quota exceeded), so this cycle did the whole upgrade. The app was already ASP.NET Core 6 (SDK-style project, `Program.cs` minimal hosting, EF Core, ASP.NET Core Identity). There was no System.Web, EF6, Global.asax, Web.config or packages.config to migrate.

## Changes

- `PetShopWeb/PetShopWeb.csproj`
  - `TargetFramework`: `net6.0` → `net10.0`.
  - `Microsoft.AspNetCore.Identity.EntityFrameworkCore`, `Microsoft.EntityFrameworkCore.Proxies`, `.SqlServer` and `.Tools` → `10.0.15`. Before this, the project mixed 6.0.11 and 7.0.0.
  - `Microsoft.VisualStudio.Web.CodeGeneration.Design` → `10.0.3`.
  - Pinned the transitive `NuGet.Packaging` / `NuGet.Protocol` to `6.12.5`. These come in through CodeGeneration.Design, and the pin fixes NU1901 (GHSA-g4vj-cjjj-v7hg).
- `PetShopWeb/Program.cs`
  - Replaced `app.UseEndpoints(...)` with a top-level `app.MapControllerRoute(...)` (ASP0014). The route template is unchanged.
  - Nullable annotations: `string? connectionString` and `adminRole.Name!`.
- `PetShopWeb/Controllers/AccountController.cs`, `PetShopWeb/Attributes/ImageExtensionAttribute.cs`: nullable-reference fixes (CS8600/CS8604/CS8620). The fixes are `!` on values that are `[Required]`-validated or set by Identity, and `string?` on a local variable. Runtime behaviour is unchanged.
- `Dockerfile`: base images `sdk:6.0`/`aspnet:6.0` → `sdk:10.0`/`aspnet:10.0`. Added `ASPNETCORE_HTTP_PORTS=80` because .NET 8+ images listen on 8080 by default. This keeps the `3000:80` mapping in docker-compose working.

## Result

`dotnet build ./PetShopWeb/PetShopWeb.csproj --configuration RELEASE`: 0 errors, 0 warnings. The solution has no test projects, so `dotnet test` runs nothing.

## Carry-over items from the previous iteration

- Review of code changes: I re-reviewed the diff in `Dockerfile`, `PetShopWeb.csproj`, `Program.cs`, `AccountController.cs` and `ImageExtensionAttribute.cs`. All edits are target-framework/package bumps, the ASP0014 route change, or nullable annotations. No business logic changed.
- Test suite: the repository has no test projects. A clean rebuild (`--no-incremental`) gives 0 errors and 0 warnings; `dotnet test` has nothing to run.
- Deployment documentation: `README.md` now says ASP.NET Core 10. It notes that the published `yarins2000/petshopapp:1.0` image is the old .NET 6 build and shows how to rebuild it from this repo. It also has a "Building from source" section (requires the .NET 10 SDK, connection string location and env-var override).
- Integration testing in staging and production deployment planning need an environment and human decisions; they are listed below.

## Next steps

- Integration test in staging: build the image with the updated `Dockerfile`, run it with `docker-compose`, and check login (admin@admin.com), the catalog, comments and admin animal/user/role management against SQL Server. The .NET 10 container build has not been run yet.
- Production deployment: publish a new app image tag (for example `petshopapp:2.0`) instead of overwriting `1.0`, and update `docker-compose.yml` to use it. Do not deploy to production before the `EnsureDeleted()` issue below is fixed, because it wipes the database on every start.
- No automated tests exist. Adding a test project (for example xUnit with `Microsoft.AspNetCore.Mvc.Testing` and the EF Core InMemory or SQLite provider) would cover the controllers and repositories; this needs a decision on scope.

- `ViewModels/CreateRoleViewModel.cs` imports `Microsoft.Build.Framework`, so its `[Required]` is MSBuild's attribute and not `System.ComponentModel.DataAnnotations.RequiredAttribute`. As a result, MVC does not validate `RoleName` as required. This bug was already there before the upgrade. I left it alone because fixing it changes validation behaviour. Changing the `using` to `System.ComponentModel.DataAnnotations` is probably what the author intended.
- `Program.cs` calls `EnsureDeleted()` + `EnsureCreated()` on every startup, which drops the database each time. This is pre-existing behaviour and I did not change it. Consider making it Development-only before any production use.
- The admin seed checks `ctx.Users.Any(u => u.UserName == "Admin")`, but the user it creates is `admin@admin.com`, so the check never matches. This is pre-existing; I did not change it.
- In `AccountController.EditUsersInRole` (POST), an unknown `UserId` still throws from `IsInRoleAsync` (same as before). Consider returning `NotFound()` instead.
- The csproj has many `<Content Remove="C:\Users\PC\.nuget\...">` entries, which are leftovers from the author's machine. They are harmless, so I kept them. They can be removed in a cleanup pass.
- The Dockerfile copies both the `.csproj` (into `/app`) and the full repo, including `PetShopWeb.sln`, before `dotnet publish`. That build layout was already there and I have not checked it in a container build.
- `Microsoft.VisualStudio.Web.CodeGeneration.Design` is only needed for design-time scaffolding. Consider marking it `PrivateAssets="all"` or removing it, which would also remove the NuGet.* pins.
