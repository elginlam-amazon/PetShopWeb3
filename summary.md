# PetShopWeb: .NET 6.0 → .NET 10 upgrade

## Before the upgrade
PetShopWeb was already an ASP.NET Core MVC app on .NET 6 with minimal hosting in `Program.cs`. It used EF Core (SQL Server with lazy-loading proxies) through `PetShopContext`, which seeds categories and animals in `OnModelCreating`. ASP.NET Core Identity handled users and roles, with an admin role and user seeded at startup. Repositories (Animal, Category, Comment, Account) were registered as scoped services. It had no System.Web, EF6 or Web.config code, so no framework-level rewrite was needed.

## Changes
- `PetShopWeb.csproj`: `net6.0` → `net10.0`. Packages updated to 10.x: Identity.EntityFrameworkCore, EF Core Proxies, SqlServer and Tools 10.0.15, CodeGeneration.Design 10.0.3. Direct references to `NuGet.Packaging` and `NuGet.Protocol` 6.12.5 were added. This pins patched versions of packages that CodeGeneration.Design pulls in transitively, which fixes NU1901 (GHSA-g4vj-cjjj-v7hg).
- `Program.cs`: replaced `UseEndpoints(...)` with top-level `app.MapControllerRoute(...)` (ASP0014). The route template is unchanged. Nullable annotations were added for the connection string and the admin role name.
- `AccountController.cs`: added null-forgiving operators where Identity 7+ nullable annotations caused CS8604/CS8620 warnings. Runtime behavior is unchanged.
- `ImageExtensionAttribute.cs`: `path` is now declared `string?`, which matches the existing null check.
- `Dockerfile`: SDK and runtime images moved from 6.0 to 10.0. Added `ASPNETCORE_HTTP_PORTS=80` because .NET 8+ images listen on 8080 by default, and docker-compose maps `3000:80`.

## Build result
`dotnet build PetShopWeb/PetShopWeb.csproj --no-incremental`: 0 errors, 0 warnings. The solution has no test projects.

## Next steps
- `ViewModels/CreateRoleViewModel.cs` imports `Microsoft.Build.Framework`, so its `[Required]` is the MSBuild attribute, not the DataAnnotations one. Role-name validation is therefore not enforced. This was true before the upgrade and was left alone. Recommended fix: `using System.ComponentModel.DataAnnotations;`.
- `Program.cs` calls `EnsureDeleted()` and then `EnsureCreated()` on every startup, which wipes the database each time. This is pre-existing behavior. It should be restricted to Development before any shared or production deployment.
- The admin seeding checks `UserName == "Admin"` but creates `admin@admin.com`, so the check never matches. It only works today because the database is recreated on every start. This is pre-existing.
- The docker-compose image tags (`yarins2000/petshopapp:1.0`, `petshopdb:1.0`) point to pre-built images. They need to be rebuilt from the updated Dockerfile.
- Port 433 in Dockerfile and docker-compose looks like a typo for 443. This was left unchanged.
