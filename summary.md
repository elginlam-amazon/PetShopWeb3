# PetShopWeb: .NET 6.0 → .NET 10 upgrade summary

## Result
- `dotnet build PetShopWeb/PetShopWeb.csproj --no-incremental`: **0 errors, 0 warnings**.
- The solution has no test projects, so no tests were run.

## Changes
- **PetShopWeb.csproj**: the project was already SDK-style and targeted `net10.0` with 10.0.x EF Core/Identity packages. I added direct references to `NuGet.Packaging` and `NuGet.Protocol` 6.12.5. These pin the patched versions of packages that `Microsoft.VisualStudio.Web.CodeGeneration.Design` pulls in at 6.12.1, which clears NU1901 (GHSA-g4vj-cjjj-v7hg).
- **Program.cs**:
  - Replaced `UseEndpoints` with a top-level `app.MapControllerRoute` call (ASP0014). The route template and middleware order are unchanged.
  - Fixed nullable warnings: `connectionString` is now `string?`, and `adminRole.Name!` is used because the role is always constructed with the name "admin".
- **Controllers/AccountController.cs**: fixed nullable-reference warnings (CS8604/CS8620) with `!`. Each `!` sits on a value that is either guaranteed by `[Required]` plus a `ModelState.IsValid` check, or non-null in Identity (`IdentityRole.Name`, `IdentityUser.UserName`). Runtime behavior is the same as before.
- **Attributes/ImageExtensionAttribute.cs**: the local `path` is now `string?` (CS8600). Behavior is unchanged.
- **Dockerfile**: changed the base images from `sdk:6.0`/`aspnet:6.0` to `sdk:10.0`/`aspnet:10.0`. Added `ENV ASPNETCORE_HTTP_PORTS=80`, because .NET 8+ images listen on 8080 by default and `docker-compose.yml` maps host port 3000 to container port 80.

## Next steps
- `ViewModels/CreateRoleViewModel.cs` uses `[Required]` from `Microsoft.Build.Framework`, not `System.ComponentModel.DataAnnotations`. That attribute has no effect on MVC validation, so an empty role name is not rejected by model validation. This bug was already there before the upgrade and was left unchanged. Switching the using to `System.ComponentModel.DataAnnotations` would turn validation on.
- `Program.cs` calls `EnsureDeleted()` and then `EnsureCreated()` at every startup, so the database is wiped on each start in every environment. This was already there before the upgrade. Decide whether it should run only in Development.
- The admin seed checks `u.UserName == "Admin"`, but it creates a user named `admin@admin.com`. Because of the `EnsureDeleted` above this mismatch does no harm today, but it would cause a duplicate-user attempt if the database were kept between starts.
- The `<Content Remove="C:\Users\PC\.nuget\...">` entries in the csproj are leftovers from the original developer's machine. They do nothing on other machines and can be removed.
- `Microsoft.VisualStudio.Web.CodeGeneration.Design` is only needed for Visual Studio scaffolding. Consider removing it (and the NuGet.* pins) if scaffolding is not used. Note that `CreateRoleViewModel` currently compiles only because this package brings in `Microsoft.Build.Framework` (see the first item).
- In `docker-compose.yml`, the HTTPS mapping `3001:433` looks like a typo for 443, and no HTTPS endpoint or certificate is configured. The Dockerfile `EXPOSE 433` was left as it was.
