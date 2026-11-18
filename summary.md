# PetShopWeb: .NET 6.0 → .NET 10.0 upgrade

## Result
`dotnet build PetShopWeb/PetShopWeb.csproj --no-incremental` succeeds with **0 errors and 0 warnings**. The solution has no test projects.

## What the app did before (from CodeKB, pre-upgrade code)
- ASP.NET Core 6 MVC app with minimal-hosting `Program.cs`. It registers repositories (Animal, Comment, Category, Account), `PetShopContext` (EF Core + SQL Server + lazy-loading proxies) and ASP.NET Core Identity (`IdentityUser`/`IdentityRole`). It also seeds an `admin` role and the `admin@admin.com` user at startup.
- `PetShopContext.OnModelCreating` seeds categories and animals with `HasData`.
- `AccountController` handles login/registration plus admin management of users, roles and user-role assignments through `UserManager`/`RoleManager`/`SignInManager`.
- No System.Web, EF6, Web.config or Global.asax was present, so those KB steps did not apply.

## Changes
- **PetShopWeb.csproj**: `net6.0` → `net10.0`. Packages moved to 10.x: Identity.EntityFrameworkCore, EF Core Proxies/SqlServer/Tools 10.0.15, and CodeGeneration.Design 10.0.3. Added direct references to `NuGet.Packaging`/`NuGet.Protocol` 6.12.5, which pin the transitive packages from CodeGeneration.Design to patched versions (NU1901, GHSA-g4vj-cjjj-v7hg).
- **PetShopWeb.sln**: project type GUID changed to the SDK-style C# GUID.
- **Program.cs**:
  - The connection string is read with `GetConnectionString("DefaultConnection")`, which reads the same key.
  - `UseEndpoints(...)` was replaced by a top-level `app.MapControllerRoute(...)` with the same route (ASP0014). `UseRouting()`/`UseAuthorization()` are still called explicitly, so middleware order is unchanged.
  - Added a null-forgiving operator on `adminRole.Name`.
- **AccountController.cs / ImageExtensionAttribute.cs**: Identity 10 has stricter nullability annotations, which caused CS8600/CS8604/CS8620 warnings. These were fixed with null-forgiving operators and a `string?` local. Runtime behavior is unchanged, and the affected values are either `[Required]` model fields or values the original code already assumed were non-null.
- **Dockerfile**: SDK/runtime images 6.0 → 10.0. Added `ENV ASPNETCORE_HTTP_PORTS=80` because .NET 8+ container images listen on 8080 by default. This keeps the `3000:80` mapping in docker-compose.yml working.

## Next steps
- `ViewModels/CreateRoleViewModel.cs` imports `Microsoft.Build.Framework` and so uses MSBuild's `[Required]` attribute, not `System.ComponentModel.DataAnnotations.RequiredAttribute`. The project only compiles because CodeGeneration.Design brings in MSBuild assemblies transitively. As a result, `RoleName` is not validated on the server side. This existed before the upgrade and was left unchanged. Recommended fix: switch the using to `System.ComponentModel.DataAnnotations`. This would turn on required-field validation for role creation.
- Pre-existing behavior was left as is and needs a product decision:
  - `Program.cs` calls `EnsureDeleted()` on every startup, which drops the database in every environment.
  - The admin seed checks `UserName == "Admin"` but creates `admin@admin.com`.
  - The seed password and the commented-out SQL connection string are stored in source and config.
- `EditUsersInRole` (POST) still throws if a posted `UserId` doesn't match a user. This is unchanged from before; adding a null check would be a behavior improvement.
- The docker-compose images (`yarins2000/petshopapp:1.0`) are prebuilt .NET 6 images and need to be rebuilt from the updated Dockerfile.
- README.md still says "ASP.NET Core 6".
