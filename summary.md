# PetShopWeb: .NET 6.0 → .NET 10 migration summary

## Changes
- `PetShopWeb/PetShopWeb.csproj`: targets `net10.0`. EF Core and Identity packages are at 10.0.15 and `Microsoft.VisualStudio.Web.CodeGeneration.Design` is at 10.0.3.
- The csproj now references `NuGet.Packaging` and `NuGet.Protocol` 6.12.5 directly. This overrides the vulnerable 6.12.1 versions (NU1901, GHSA-g4vj-cjjj-v7hg) pulled in transitively by CodeGeneration.Design.
- `Program.cs`:
  - Reads the connection string with `GetConnectionString("DefaultConnection")`. If it is missing, startup now fails with a clear `InvalidOperationException` instead of passing null to `UseSqlServer`.
  - `UseEndpoints` is replaced with top-level `app.MapControllerRoute` (ASP0014). The route template and middleware order are unchanged.
- Nullable warnings (CS8600/CS8604/CS8620) are fixed in `AccountController.cs`, `ImageExtensionAttribute.cs` and `Program.cs`. Most fixes are null-forgiving operators on values that `[Required]` model validation or ASP.NET Identity already guarantee.
- Behavior change in `AccountController.EditUsersInRole` (POST): a user id that no longer exists is now skipped. Before, it threw `ArgumentNullException` from `IsInRoleAsync`.
- `Dockerfile`: the base images are now `sdk:10.0` / `aspnet:10.0`. It sets `ASPNETCORE_HTTP_PORTS=80` because .NET 8+ images listen on 8080 by default, and this keeps the `docker-compose.yml` port mapping (`3000:80`) working.

## Build result
`dotnet build PetShopWeb/PetShopWeb.csproj --no-incremental` succeeded with 0 errors and 0 warnings. The solution has no test projects.

## Next steps
- `ViewModels/CreateRoleViewModel.cs` uses `[Required]` from `Microsoft.Build.Framework`, not `System.ComponentModel.DataAnnotations`. That attribute is not an MVC validation attribute, so `RoleName` is not validated as required. It only compiles because CodeGeneration.Design brings in Microsoft.Build.Framework transitively. This bug predates the migration and was left unchanged. Consider switching to `System.ComponentModel.DataAnnotations`.
- `Dockerfile` (unchanged from before): it copies the csproj into `/app`, then copies the whole repo (including `PetShopWeb.sln`) into the same folder. `dotnet publish` with no project argument may then fail because more than one project or solution file is present (MSB1011). Verify the image build, and consider `dotnet publish PetShopWeb/PetShopWeb.csproj`.
- `Program.cs` calls `EnsureDeleted()` / `EnsureCreated()` on every startup, and its admin-seed check compares `UserName == "Admin"` while the seeded user is `admin@admin.com`. This existing behavior was left unchanged. Review it before production use.
- `appsettings.json` contains a commented-out connection string with an `sa` password. Move secrets to environment variables or user secrets.
- Consider removing `Microsoft.VisualStudio.Web.CodeGeneration.Design` (design-time scaffolding only) from the runtime project. That would also make the NuGet.Packaging/NuGet.Protocol overrides unnecessary.
- The csproj has many `<Content Remove="C:\Users\PC\.nuget\...">` entries left over from the original Windows environment. They do nothing on other machines and can be cleaned up.
