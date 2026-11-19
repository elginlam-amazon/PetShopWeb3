# 📊 Executive Report: PetShopWeb .NET 6.0 → .NET 10 Upgrade  
  
## 1. 🧭 Executive Summary  
  
PetShopWeb is an ASP.NET Core MVC pet shop app that uses EF Core and ASP.NET Core Identity. It was moved from **.NET 6.0**, which is out of support (the SDK raised `NETSDK1138`), to **.NET 10.0**. The Microsoft .NET Upgrade Assistant changed the target framework and updated the framework packages. Kiro CLI then cleared all build warnings, patched a vulnerable transitive dependency and updated the container image. ✅ The upgraded project and solution build with **0 errors and 0 warnings**. Before the upgrade the build had 0 errors and 3 warnings, and right after the Upgrade Assistant step it had 21 warnings. The whole run took about **5 minutes** (17:43 to 17:48 UTC, Nov 19, 2026). ⚠️ The repo has no test projects (0 tests in the baseline and after the upgrade), so behavior has not been checked by automated tests.  
  
| Metric | Before | After |  
| --- | --- | --- |  
| 🎯 Target framework | `net6.0` (end of support) | `net10.0` |  
| ❌ Build errors | 0 | 0 |  
| ⚠️ Build warnings | 3 (`NETSDK1138`), then 21 after the TFM change | **0** |  
| 🔐 Known vulnerable packages | 2 (`NuGet.Packaging`, `NuGet.Protocol` 6.12.1) | 0 |  
| 🐳 Container base images | `sdk:6.0` / `aspnet:6.0` | `sdk:10.0` / `aspnet:10.0` |  
| 🧪 Unit tests | 0 | 0 |  
  
## 2. 🏗️ Application Changes  
  
- 🎯 **Framework:** the target framework changed from `net6.0` to `net10.0`. The project was already SDK-style (`Microsoft.NET.Sdk.Web`) with minimal hosting, so it needed no structural rewrite.  
- 📦 **Framework packages:** EF Core and Identity packages moved from 6.0.11 / 7.0.0 to 10.0.15. The scaffolding package moved from 6.0.10 to 10.0.3.  
- 🔐 **Security:** the patched version (6.12.5) of the NuGet client libraries is now pinned, fixing advisory [GHSA-g4vj-cjjj-v7hg](https://github.com/advisories/GHSA-g4vj-cjjj-v7hg).  
- 🛣️ **Routing:** routes are now registered at the top level with `app.MapControllerRoute`, the current ASP.NET Core pattern (fixes `ASP0014`).  
- 🧹 **Null safety:** all nullable-reference warnings are fixed. Two admin error paths are now handled safely (see section 4).  
- 🐳 **Hosting:** the Dockerfile uses the .NET 10 images. The app still listens on port 80, so the existing `docker-compose.yml` mapping (`3000:80`) keeps working.  
- ✅ **No migration needed for:** `System.Web`, EF6, `Web.config`, `Global.asax`, OWIN. The app already used ASP.NET Core.  
  
## 3. 🛠️ Tools Used  
  
- ⚙️ **.NET SDK 10.0.404:** used to restore, build and test. The SDKs for 5.0 through 9.0 were also installed.  
- 🔄 **Microsoft .NET Upgrade Assistant 1.0.518.26217:** did the in-place framework upgrade (`framework.inplace`) in non-interactive mode.  
  - 📝 Ran 7 operations successfully, with 0 failures and 349 skipped. The skipped items were source files that needed no changes.  
  - 🎯 Ran `TargetFrameworkTransformer` (net6.0 → net10.0) and the package-map transformers for 5 package references.  
- 🤖 **Kiro CLI 2.35.1** with the **Claude Opus 5.5** model (`claude-opus-5.5`): fixed the build warnings, analyzed vulnerabilities and updated the Dockerfile. It succeeded on the first attempt (1 of 5 allowed).  
  - 📚 It used the "application modernisation knowledge" base under `/app/knowledge` for containerization and package guidance.  
  - 🔎 It checked package versions live on the NuGet API and the advisory's patched versions on the GitHub Advisory API.  
  
## 4. 💻 Code Changes  
  
The change spans 6 files, with 40 lines added and 37 removed.  
  
- 📦 **`PetShopWeb/PetShopWeb.csproj`**  
  - 🎯 Changed `TargetFramework` to `net10.0`.  
  - ⬆️ Set these package versions:  
    - `Microsoft.AspNetCore.Identity.EntityFrameworkCore`: 10.0.15  
    - `Microsoft.EntityFrameworkCore.Proxies`: 10.0.15  
    - `Microsoft.EntityFrameworkCore.SqlServer`: 10.0.15  
    - `Microsoft.EntityFrameworkCore.Tools`: 10.0.15  
    - `Microsoft.VisualStudio.Web.CodeGeneration.Design`: 10.0.3  
  - 🔐 Added pins for `NuGet.Packaging` and `NuGet.Protocol` 6.12.5 (fixes the `NU1901` warning).  
- 🚀 **`PetShopWeb/Program.cs`**  
  - 🛣️ Replaced `UseEndpoints(...)` with `app.MapControllerRoute(...)`. The route template and middleware order are the same.  
  - 🔌 Reads the connection string with `GetConnectionString("DefaultConnection")`. If it is missing, startup now fails right away with a clear error.  
- 👤 **`PetShopWeb/Controllers/AccountController.cs`**  
  - 🧹 Fixed 15 nullable warnings (`CS8604`, `CS8620`).  
  - 🛡️ When the `EditUser` and `EditRole` POST actions get a null id, they now return the existing "not found" result instead of throwing.  
  - 🛡️ The `EditUsersInRole` POST action now skips rows with a missing or unknown user, instead of returning an HTTP 500.  
- 🖼️ **`PetShopWeb/Attributes/ImageExtensionAttribute.cs`:** made a local variable nullable (`string?`, fixes `CS8600`). The logic is unchanged.  
- 🐳 **`Dockerfile`:** changed the base images to `sdk:10.0` and `aspnet:10.0`, and added `ENV ASPNETCORE_HTTP_PORTS=80`.  
- 🗂️ **`PetShopWeb.sln`:** the Upgrade Assistant updated the project type GUID to the SDK-style C# GUID.  
  
## 5. ⏱️ Time Savings Estimate  
  
- 🤖 **Automated run:** about 5 minutes from start to a clean build.  
- 👩‍💻 **Manual equivalent:** about **1 to 1.5 developer-days**. This is a rough estimate based on the scope, not a measurement. It breaks down as:  
  - 📦 Retargeting the framework, researching package versions and restoring: 1 to 2 hours.  
  - 🔐 Tracing the vulnerable transitive dependency and choosing a patched version: 1 to 2 hours.  
  - 🧹 Reviewing and fixing 21 warnings across 3 C# files without changing behavior: 2 to 3 hours.  
  - 🐳 Updating the Dockerfile and catching the port change from 80 to 8080 in .NET 8 and later: 1 hour.  
  - 📝 Reviewing existing issues and writing documentation: 1 to 2 hours.  
- 💰 **Estimated saving:** about **90% or more** of the effort. This is a small, low-complexity upgrade (one project, already ASP.NET Core). The saving would be larger for .NET Framework apps.  
  
## 6. 🚀 Next Steps  
  
### ✅ Validation  
  
- 🧪 **Add automated tests.** There are no tests, so behavior hasn't been checked. Start with integration tests for login, registration and admin role and user management.  
- 🐳 **Build and run the Docker image.** It wasn't built in this run. Also change `dotnet publish` to name the project (`PetShopWeb/PetShopWeb.csproj`). The build folder contains both the `.csproj` and the `.sln`, which can cause error `MSB1011`.  
- 🔄 **Rebuild and republish** the prebuilt `yarins2000/petshopapp:1.0` image that `docker-compose.yml` references.  
- 🖱️ **Smoke test by hand** against SQL Server: login, registration, catalog, animal details and comments, and the admin pages.  
  
### 🔧 Recommendations  
  
- 🗄️ **High priority:** `Program.cs` calls `EnsureDeleted()` and then `EnsureCreated()` on every start, which **wipes the database in every environment**. Limit this to Development, or switch to EF Core migrations.  
- ✏️ **Fix role validation:** `CreateRoleViewModel` uses an MSBuild `[Required]` attribute that ASP.NET Core doesn't enforce, so a role can be created with an empty name. Switch to `System.ComponentModel.DataAnnotations`.  
- 👤 **Fix the admin seed check:** it looks for user name `Admin`, but the code creates `admin@admin.com`, so the check never matches.  
- 🔑 **Move secrets out of config:** `appsettings.json` contains a commented-out `sa` password. Use environment variables, user secrets or a vault.  
- 🧽 **Clean up the project file:** remove the leftover `Content Remove` entries that point to absolute Windows NuGet cache paths in `PetShopWeb.csproj`.  
- ⬆️ **Keep packages current:** move to the latest 10.0.x patches as they ship. Consider removing `CodeGeneration.Design` from production builds, since it is the source of the vulnerable NuGet dependencies.  

## Kiro credit usage

💳 Kiro CLI credits used: 13.51  
Includes 1.18 for job setup (startup smoke test and knowledge-base indexing).  
