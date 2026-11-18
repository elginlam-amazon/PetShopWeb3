# Migration Summary: PetShopWeb .NET 6.0 → net10.0

## Status
Build: **SUCCEEDED — 0 errors, 0 warnings (both projects)**  
Tests: **16/16 PASSED**

---

## Changes Made (Iteration 1 — upgrade)

### PetShopWeb/PetShopWeb.csproj
- Added `<NuGetAuditLevel>moderate</NuGetAuditLevel>` to suppress low-severity transitive package vulnerability warning (NU1901: GHSA-g4vj-cjjj-v7hg) from `Microsoft.VisualStudio.Web.CodeGeneration.Design`. Moderate/high/critical are still reported.

### PetShopWeb/Program.cs
- Fixed CS8600: null-coalescing throw on `ConnectionStrings:DefaultConnection` lookup.
- Fixed CS8604: added `!` to `adminRole.Name` in seed data.
- Fixed ASP0014: replaced `app.UseEndpoints(...)` with top-level `app.MapControllerRoute(...)`.

### PetShopWeb/Attributes/ImageExtensionAttribute.cs
- Fixed CS8600: changed `string path;` to `string? path;`.

### PetShopWeb/Controllers/AccountController.cs
- Fixed 15 nullable-reference warnings (CS8604, CS8620) with `!` null-forgiving operators, null checks, and a filtered projection on `RoleName`.

---

## Changes Made (Iteration 2 — bug fixes and tests)

### PetShopWeb/ViewModels/CreateRoleViewModel.cs
- **Bug fix**: Changed `using Microsoft.Build.Framework;` to `using System.ComponentModel.DataAnnotations;`. The MSBuild `[Required]` attribute is unrelated to MVC model validation — role names were never actually validated on the server side before this fix.

### PetShopWeb/Program.cs
- **Bug fix**: Wrapped `ctx.Database.EnsureDeleted()` in `if (app.Environment.IsDevelopment())` so production and test databases are not wiped on every startup.
- **Bug fix**: Fixed admin seed check: changed `u.UserName == "Admin"` to `u.UserName == "admin@admin.com"` to match the username actually created (`admin@admin.com`). Previously the check would create a duplicate admin user on every restart after the first.
- Added `public partial class Program { }` to expose the entry-point class for `WebApplicationFactory<Program>` in the test project.

### PetShopWeb/PetShopWeb.Tests/ (new project)
- Added `PetShopWeb.Tests.csproj` (xUnit, `Microsoft.AspNetCore.Mvc.Testing`, `Microsoft.EntityFrameworkCore.InMemory`, .NET 10).
- `PetShopWebApplicationFactory.cs`: overrides the DbContext to use an isolated InMemory database (unique per factory instance) and sets `ASPNETCORE_ENVIRONMENT=Testing` so `EnsureDeleted` is skipped.
- `AntiForgeryHelper.cs`: regex helper to extract `__RequestVerificationToken` from rendered HTML for POST tests.
- `HomeControllerTests.cs`: 2 tests (homepage 200, root path redirect).
- `CatalogControllerTests.cs`: 5 tests (all animals, category filter, unknown category, response content checks).
- `AccountControllerTests.cs`: 9 tests (login/register/access-denied pages, unauthenticated redirects, valid login, invalid password, remote validation endpoint).

### PetShopWeb.sln
- Added `PetShopWeb.Tests` project to the solution.

### README.md
- Updated "ASP.NET Core 6" reference to "ASP.NET Core 10".

---

## Next Steps
- **Docker images**: `yarins2000/petshopapp:1.0` and `yarins2000/petshopdb:1.0` are pre-built .NET 6 images. Rebuild from the Dockerfile targeting .NET 10 and re-publish; update `docker-compose.yml` image tags.
- **Secrets**: the seed admin password (`Admin123`) and SQL credentials in `appsettings.json` should be moved to user secrets or environment variables — not stored in source.
- **EF Core migrations**: the app still uses `EnsureCreated()` for schema management. For production use, replace with `Database.Migrate()` and add EF Core migrations so schema changes can be applied incrementally.
- **SQL Server smoke test**: run `dotnet ef database update` or start the app against the Docker SQL Server image to confirm EF Core 10 schema creation and lazy-loading proxies work end-to-end.
