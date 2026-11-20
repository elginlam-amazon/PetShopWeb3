# 📊 Executive Report: PetShopWeb .NET 6.0 → .NET 10.0 Upgrade  
  
## 🧭 Executive Summary  
PetShopWeb is an ASP.NET Core MVC app that uses Identity and EF Core with SQL Server. It was upgraded from .NET 6.0 to .NET 10.0. The run started on 20 Nov 2026 at 03:03 UTC. AWS Transform Custom (atx) stopped immediately with exit code 1, because the account had used up its monthly quota for `AWS/dotnet-modernization` (`SERVICE_QUOTA_EXCEEDED`, not retryable). It changed no code. Kiro CLI then did the full migration on its first attempt. The project now builds with **0 errors and 0 warnings**, and `dotnet test` exits with code 0. The solution has no test projects, so no tests actually ran. The change touched 5 files (34 lines added, 30 removed). The app was not started and the Docker image was not built.  
  
## 🏗️ Application Changes  
- 🎯 **Target framework**: `net6.0` → `net10.0`  
- 📦 **NuGet packages**: Identity and EF Core packages brought to one version, `10.0.15`. Before, they were a mix of 6.x and 7.x.  
- 🔐 **Security**: patched a known vulnerability in a transitive dependency (NU1901, GHSA-g4vj-cjjj-v7hg)  
- 🧹 **Code quality**: cleared 20 new compiler and analyzer warnings caused by stricter nullable annotations in .NET 10  
- 🐳 **Container**: the Dockerfile now uses the .NET 10 SDK and runtime images, and keeps the existing port mapping  
- ℹ️ **Starting point**: the project was already SDK-style ASP.NET Core. It had no System.Web, EF6, Web.config, Global.asax or packages.config to migrate.  
  
## 🛠️ Tools Used  
- ⚙️ **.NET SDK 10.0.404**: restore, build (`--no-incremental`) and test  
- ☁️ **AWS Transform Custom CLI (atx)**: transformation `AWS/dotnet-modernization`, region `ap-southeast-2`  
  - ❌ Failed at startup (exit 1, monthly quota exhausted) and made no code changes  
- 🤖 **Kiro CLI 2.35.1** with model **Claude Opus 5.5**  
  - ✅ Finished the migration on the first attempt (1 of 5 allowed) and exited with code 0  
  - 📚 Used the .NET migration knowledge base and nuget.org to choose package versions  
  
## 💻 Code Changes  
- 📄 **`PetShopWeb/PetShopWeb.csproj`**  
  - 🔼 `TargetFramework` changed to `net10.0`  
  - 🔼 `Microsoft.AspNetCore.Identity.EntityFrameworkCore` 6.0.11 → 10.0.15  
  - 🔼 `Microsoft.EntityFrameworkCore.Proxies` 7.0.0 → 10.0.15  
  - 🔼 `Microsoft.EntityFrameworkCore.SqlServer` 7.0.0 → 10.0.15  
  - 🔼 `Microsoft.EntityFrameworkCore.Tools` 6.0.11 → 10.0.15  
  - 🔼 `Microsoft.VisualStudio.Web.CodeGeneration.Design` 6.0.10 → 10.0.3  
  - 🔒 Pinned the transitive `NuGet.Packaging` and `NuGet.Protocol` to 6.12.5, the patched version  
- 📄 **`PetShopWeb/Program.cs`**  
  - 🔀 Replaced `UseEndpoints(...)` with top-level `app.MapControllerRoute(...)` (ASP0014). The route template is unchanged.  
  - ❔ Added nullable fixes: `string? connectionString`, `adminRole.Name!`  
- 📄 **`PetShopWeb/Controllers/AccountController.cs`**  
  - ❔ Added null-forgiving annotations where `[Required]` validation or the Identity store already guarantee a value  
  - ⚠️ **Behavior change**: the POST action of `EditUsersInRole` now skips user ids that no longer exist. Before, they caused an `ArgumentNullException` and a 500 error.  
- 📄 **`PetShopWeb/Attributes/ImageExtensionAttribute.cs`**: `string path` → `string? path`. The logic is unchanged.  
- 📄 **`Dockerfile`**: base images changed to `sdk:10.0` and `aspnet:10.0`. Added `ASPNETCORE_HTTP_PORTS=80` because .NET 8+ images listen on 8080 by default and docker-compose maps `3000:80`.  
  
## ⏱️ Time Savings Estimate  
- 🧮 **Scope**: 1 project, 5 files, low complexity. This was a .NET Core to .NET upgrade with no Framework-era rewrite.  
- 👩‍💻 **Manual estimate**: about **4–6 hours** for a developer, covering:  
  - 🔍 Researching versions and package compatibility (about 1 h)  
  - 🔧 Retargeting, restoring and fixing 20 warnings (about 1.5–2 h)  
  - 🔐 Tracing and fixing the vulnerable transitive package (about 0.5–1 h)  
  - 🐳 Updating the Dockerfile and checking port behavior (about 0.5 h)  
  - 📝 Reviewing and documenting the changes (about 1 h)  
- ⚡ **Automated**: done in one Kiro CLI session  
- 💰 **Estimated saving**: about **80–90%** of the hands-on engineering time. Human review time is still needed.  
  
## 🚀 Next Steps  
### ✅ Validation  
- 🧪 Run the app against a real SQL Server and smoke-test login, register, catalog, animal admin pages, and role and user management  
- 🐳 Build the Docker image, then rebuild and push `yarins2000/petshopapp`. `docker-compose.yml` still points to the .NET 6 `:1.0` tag.  
- 🔎 Check that the `EditUsersInRole` behavior change is acceptable  
- 🔁 Fix the atx quota (ask AWS Support for an increase) if future runs should use AWS Transform Custom  
  
### 🌱 Improvements  
- 🛑 **High priority**: `Program.cs` runs `EnsureDeleted()` and `EnsureCreated()` on every startup, which wipes the database in production. Switch to EF Core migrations or limit this to Development.  
- 🔑 Remove the commented-out `sa` password from `appsettings.json`. Use environment variables, user-secrets or a vault instead.  
- 🐞 Fix the admin seeding check: it compares `UserName == "Admin"`, but the code creates `admin@admin.com`.  
- 🐞 `CreateRoleViewModel` uses `Microsoft.Build.Framework.RequiredAttribute`. Change it to the DataAnnotations `[Required]` so the role name is validated.  
- 🧹 Remove the leftover Windows-only `<Content Remove="C:\Users\PC\...">` entries from the `.csproj`  
- 🧪 Add a test project for the controllers and repositories. There are no automated tests today.  

## Kiro credit usage

💳 Kiro CLI credits used: 10.08  
Includes 1.19 for job setup (startup smoke test and knowledge-base indexing).  
