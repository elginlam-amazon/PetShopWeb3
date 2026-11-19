# PetShopWeb: .NET 6.0 → .NET 10 upgrade

## Result
- `dotnet build ./PetShopWeb/PetShopWeb.csproj --no-incremental`: **0 errors, 0 warnings**.
- The solution has no test projects, so no tests were run.

## Changes
- **PetShopWeb.csproj**: targets `net10.0`. `Microsoft.AspNetCore.Identity.EntityFrameworkCore`, `Microsoft.EntityFrameworkCore.Proxies/SqlServer/Tools` are on 10.0.15, and `Microsoft.VisualStudio.Web.CodeGeneration.Design` is on 10.0.3.
- **NU1901 (GHSA-g4vj-cjjj-v7hg)**: `CodeGeneration.Design` pulls in `NuGet.Packaging` and `NuGet.Protocol` 6.12.1. Both are now pinned directly to the patched 6.12.5 with `PrivateAssets="all"`, because they are only needed at design time.
- **Program.cs**: replaced `UseEndpoints` with top-level `app.MapControllerRoute` (ASP0014). Fixed nullable warnings: the connection string is now `string?`, and `adminRole.Name!` is used because the name is set in the constructor.
- **AccountController.cs**: fixed the nullable warnings that came from the newly nullable-annotated Identity APIs. Behavior is unchanged except for these cases:
  - `EditUser` (POST) and `EditRole` (POST): a null `Id` now goes to the existing "not found" path. Before, a null `Id` was passed to `FindByIdAsync`.
  - `EditUsersInRole` (POST): an entry with a null or unknown `UserId` is now skipped. Before, it threw `ArgumentNullException` inside `IsInRoleAsync`.
- **ImageExtensionAttribute.cs**: `path` is now declared `string?`. Behavior is unchanged.
- **Dockerfile**: the base images are now `sdk:10.0` and `aspnet:10.0`. The file sets `ASPNETCORE_HTTP_PORTS=80` because .NET 8+ images listen on 8080 by default, and `docker-compose.yml` maps container port 80.
- **PetShopWeb.sln**: the project type GUID is updated to the SDK-style C# GUID.

## Next steps
- `ViewModels/CreateRoleViewModel.cs` uses `Microsoft.Build.Framework.RequiredAttribute`. That attribute is not a validation attribute, so `RoleName` is never validated. It compiles only because `Microsoft.Build.Framework` arrives transitively through the scaffolding package. This existed before the upgrade and was not changed. Recommendation: switch to `System.ComponentModel.DataAnnotations`.
- `Program.cs` calls `EnsureDeleted()` and `EnsureCreated()` on every startup, which wipes the database in every environment. This existed before the upgrade and was not changed. Consider limiting it to Development, or moving to EF Core migrations.
- The seeded admin password (`Admin123`) and the connection string are in source and config. Consider moving them to user secrets or environment variables for test and prod.
- Dockerfile (existing issue): `COPY . .` puts the repo's `.sln` and `PetShopWeb/` alongside the copied root `.csproj`, which may make `dotnet publish` ambiguous. Not checked, because no container build was run. Consider `RUN dotnet publish PetShopWeb/PetShopWeb.csproj -c Release -o out`.
- `PetShopWeb.csproj` has `<Content Remove="C:\Users\PC\.nuget\...">` entries left over from a developer machine. They are harmless and were kept.
