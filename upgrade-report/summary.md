# 📊 Executive Report: PetShopWeb .NET 6.0 → .NET 10.0 Upgrade  
  
## 1. 🧭 Executive Summary  
  
PetShopWeb is an ASP.NET Core MVC app that uses EF Core and ASP.NET Core Identity. It was upgraded from .NET 6.0 to .NET 10.0 on 19 Nov 2026. AWS Transform Custom CLI (atx) started the job at 14:45:14 UTC but stopped right away with `SERVICE_QUOTA_EXCEEDED` and changed nothing. Kiro CLI then did the whole migration in one attempt, finishing at about 14:48:42 UTC (roughly 3.5 minutes end to end). ✅ The project and the solution build with **0 errors and 0 warnings** in both Debug and Release. ⚠️ The solution has no test projects, so no automated tests confirm runtime behaviour.  
  
- 🎯 **Outcome:** upgrade complete, build clean  
- 📦 **Scope:** 1 project (`./PetShopWeb/PetShopWeb.csproj`), about 1,180 lines of C# and 23 Razor views  
- 🔧 **Footprint:** 5 files changed (40 lines added, 77 removed)  
- 🧪 **Tests:** none exist, before or after the upgrade (0 test cases in the inventory)  
  
## 2. 🏗️ Application Changes  
  
- 🎯 **Target framework:** `net6.0` → `net10.0`  
- 📦 **NuGet packages:** EF Core and Identity packages moved to 10.0.15, and the scaffolding package moved to 10.0.3  
- 🔒 **Security:** two NuGet packages that come in through the scaffolding tool are pinned to patched versions (advisory GHSA-g4vj-cjjj-v7hg)  
- 🧹 **Project hygiene:** removed 44 stale `Content Remove` entries that pointed at a Windows user's local NuGet folder  
- 🛣️ **Routing:** moved to top-level route registration (same route template)  
- 🛡️ **Null safety:** fixed the nullable warnings raised by the stricter annotations in Identity 7+  
- 🐳 **Container:** Docker images moved to .NET 10, and the port setting keeps the existing `docker-compose.yml` mapping working  
- ℹ️ **Not needed:** the app was already ASP.NET Core, so there was no System.Web, EF6, Web.config or Global.asax to migrate  
  
## 3. 🛠️ Tools Used  
  
- ⚙️ **.NET SDK 10.0.404:** restore, build and test (SDKs 5.0 to 10.0 are installed on the host)  
- ☁️ **AWS Transform Custom CLI (atx):** ran the `AWS/dotnet-modernization` transformation in `ap-southeast-2`  
  - ❌ It failed at startup because the account's monthly quota for this transformation was used up (Request ID `7e333373-cfe3-440c-a7f0-dbd04806e5f7`)  
- 🤖 **Kiro CLI 2.35.1, model `claude-opus-5.5`:** did the full migration after atx failed  
  - ✅ Succeeded on attempt 1 of 5  
  - 📚 Used the .NET migration knowledge base under `/app/knowledge`  
- 🌐 **NuGet.org API:** looked up the latest stable 10.x package versions  
  
## 4. 💻 Code Changes  
  
- 📄 **`PetShopWeb/PetShopWeb.csproj`**  
  - 🔼 `TargetFramework` changed to `net10.0`  
  - 🔼 `Microsoft.AspNetCore.Identity.EntityFrameworkCore`: 6.0.11 → 10.0.15  
  - 🔼 `Microsoft.EntityFrameworkCore.Proxies` and `.SqlServer`: 7.0.0 → 10.0.15  
  - 🔼 `Microsoft.EntityFrameworkCore.Tools`: 6.0.11 → 10.0.15  
  - 🔼 `Microsoft.VisualStudio.Web.CodeGeneration.Design`: 6.0.10 → 10.0.3, now marked `PrivateAssets=all`  
  - 📌 `NuGet.Packaging` and `NuGet.Protocol` pinned to 6.12.5 (clears the NU1901 warnings)  
- 📄 **`PetShopWeb/Program.cs`**  
  - 🔑 The connection string is read with `GetConnectionString("DefaultConnection")` (same key as before)  
  - 🛣️ `UseEndpoints(...)` was replaced with `app.MapControllerRoute(...)` (clears ASP0014)  
- 📄 **`PetShopWeb/Controllers/AccountController.cs`**  
  - 🛡️ Null-forgiving operators added where values are `[Required]` or come from the Identity store  
  - 🔍 Null Ids posted to `EditUser` and `EditRole` now go to the existing "not found" branch  
  - ⚠️ **Behaviour change:** `EditUsersInRole` (POST) skips users that no longer exist instead of throwing an exception  
  - ⚠️ **Behaviour change:** `ManageUserRoles` (POST) leaves out roles with no name instead of throwing an exception  
- 📄 **`PetShopWeb/Attributes/ImageExtensionAttribute.cs`:** the local `path` variable is now `string?` (clears CS8600)  
- 📄 **`Dockerfile`**  
  - 🐳 `sdk:6.0` / `aspnet:6.0` → `sdk:10.0` / `aspnet:10.0`  
  - 🔌 Added `ASPNETCORE_HTTP_PORTS=80`. .NET 8+ images listen on 8080 by default, so this keeps the container on port 80  
- 📊 **Build warnings:** 23 → **0**  
  
## 5. ⏱️ Time Savings Estimate  
  
- 🤖 **Automated run:** about 3.5 minutes (14:45:14 to 14:48:42 UTC)  
- 👩‍💻 **Estimated manual effort:** about 4 to 6 hours for one engineer. This is a judgement based on the scope, not a measurement. It covers:  
  - 🔎 Researching .NET 10 package versions and compatibility: about 1 hour  
  - 🔒 Looking into and fixing the NuGet vulnerability warnings: about 0.5 hour  
  - 🛡️ Fixing 19 nullable and analyzer warnings, with a review of each: about 1.5 to 2 hours  
  - 🐳 Updating the Dockerfile, including the port 8080 default change: about 0.5 hour  
  - ✅ Build checks and documentation: about 1 to 1.5 hours  
- 💰 **Estimated saving:** about 4 to 6 engineer-hours (over 95% of the hands-on time)  
- ℹ️ **Note:** the saving is modest because the app was already on ASP.NET Core and is small. A .NET Framework app with System.Web or EF6 would save much more.  
  
## 6. 🚀 Next Steps  
  
### ✅ Validation  
- 🧪 Run the app against a real SQL Server and test login, registration, catalog, admin animal management and role management by hand. No automated tests cover these.  
- 👥 Test the two changed role-management flows (`EditUsersInRole` and `ManageUserRoles`)  
- 🐳 Rebuild the Docker image, check that the container serves on port 80, and republish `yarins2000/petshopapp` (it is still tagged 1.0 in `docker-compose.yml`)  
- 🗄️ Test EF Core 10 against the target database. SqlClient now encrypts connections by default. The current connection strings already set `TrustServerCertificate=True`.  
  
### 🔧 Recommended Improvements  
- 🐛 **Validation bug (existed before):** `CreateRoleViewModel` uses `Microsoft.Build.Framework.Required` instead of the DataAnnotations `[Required]`, so the role name is never validated. It also only compiles because the scaffolding package brings in that assembly.  
- 💥 **Data loss risk (existed before):** `Program.cs` deletes and recreates the database on every startup, in every environment. Limit this to Development or replace it with EF Core migrations.  
- 🔐 **Secrets:** `appsettings.json` has an `sa` password in a commented-out connection string. Move credentials to environment variables, user-secrets or a vault.  
- 🌍 **Environments:** add `appsettings.Staging.json` and `appsettings.Production.json` with their own connection strings. Production currently falls back to a LocalDB connection string.  
- 🧪 **Testing:** add a unit or integration test project (xUnit plus `WebApplicationFactory`) so future upgrades can be checked automatically  
- ☁️ **Tooling:** ask AWS Support for a quota increase for `AWS/dotnet-modernization` if atx is needed in future iterations  

## Kiro credit usage

💳 Kiro CLI credits used: 10.51  
Includes 0.91 for job setup (startup smoke test and knowledge-base indexing).  
