# PetShopWeb: .NET 6.0 → .NET 10.0 upgrade

## What the application did before the upgrade (from CodeKB, old code)
- An ASP.NET Core MVC app (minimal-hosting `Program.cs`) with a pet catalog, animal details/comments, and an admin area.
- `PetShopContext` is an `IdentityDbContext`. It seeds categories and animals through `HasData`, uses SQL Server with lazy-loading proxies, and is recreated on every startup (`EnsureDeleted`/`EnsureCreated`). Startup also seeds the `admin` role and the admin user.
- `AccountController` handles login and registration, plus user and role management through `UserManager`/`RoleManager`. `AccountRepository` exposes `Roles` and `Users`.

## Changes
- **PetShopWeb.csproj**: targets `net10.0`. EF Core, Identity, Proxies and Tools are on 10.0.15, and `Microsoft.VisualStudio.Web.CodeGeneration.Design` is on 10.0.3. I added direct references to `NuGet.Packaging`/`NuGet.Protocol` 6.12.5. They pin the patched versions of the scaffolding package's transitive dependencies and clear NU1901 (GHSA-g4vj-cjjj-v7hg).
- **Program.cs**: replaced `UseEndpoints` with a top-level `app.MapControllerRoute` (fixes ASP0014). Fixed nullable warnings: the connection string is now `string?` and the admin role name uses `!`. The route pattern and middleware order are unchanged.
- **AccountController.cs / ImageExtensionAttribute.cs**: Identity 10 has nullable annotations, which raised new CS8600/CS8604/CS8620 warnings. I resolved them with null-forgiving operators and a `string?` local. Runtime behavior is unchanged: the same calls run with the same values.
- **Dockerfile**: the base images are now `sdk:10.0` and `aspnet:10.0`. I set `ASPNETCORE_HTTP_PORTS=80` because .NET 8+ images listen on 8080 by default. Without it, the `3000:80` mapping in docker-compose would stop working.

## Result
- `dotnet build PetShopWeb/PetShopWeb.csproj` finishes with 0 errors and 0 warnings. The solution builds too.
- The solution has no test projects.

## Next steps
- `CreateRoleViewModel` imports `Microsoft.Build.Framework`, so its `[Required]` is the MSBuild attribute, not DataAnnotations, and it does no MVC validation. This bug predates the upgrade, so I left it as is. Switch the import to `System.ComponentModel.DataAnnotations` if validation is wanted.
- `PetShopWeb.csproj` has many `<Content Remove="C:\Users\PC\.nuget\...">` entries left over from a developer machine. They do nothing on other machines and can be removed.
- Startup calls `EnsureDeleted()` on every run, in all environments, and the admin password is hard-coded. Both predate the upgrade. Consider limiting the reset to Development and moving the seed credentials to configuration or secrets.
- Several `!` operators in `AccountController` keep the old behavior: a missing posted id or user throws, as before. Explicit null checks that return `NotFound()` would be safer.
- The docker-compose image tags (`yarins2000/petshopapp:1.0`) need to be rebuilt and republished from the updated Dockerfile.
