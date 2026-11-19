# 🐾 PetShopWeb: .NET 6.0 → .NET 10 Upgrade, Executive Report  
  
## 📋 Executive Summary  
PetShopWeb is an ASP.NET Core MVC app that uses EF Core, SQL Server and ASP.NET Core Identity. It was upgraded from .NET 6.0, which is out of support, to .NET 10 (`net10.0`). AWS Transform Custom CLI was run first but stopped before changing any code, because the account's monthly quota for `AWS/dotnet-modernization` was used up (`SERVICE_QUOTA_EXCEEDED`). Kiro CLI then did the whole migration in one attempt. It upgraded the project file and packages, fixed every nullable and analyzer warning, patched a vulnerable transitive dependency, and updated the Dockerfile. The Release build now finishes with ✅ **0 errors and 0 warnings**. The baseline .NET 6 build had 3 warnings. The solution has no test projects, so no automated tests could be run.  
  
- 🎯 **Scope:** 1 project (`./PetShopWeb/PetShopWeb.csproj`)  
- 🔄 **Framework:** `net6.0` → `net10.0`  
- 🏗️ **Build:** `dotnet build --configuration RELEASE` passes with 0 errors and 0 warnings  
- 🧪 **Tests:** none exist (the before-upgrade test inventory found 0 test cases)  
- ⏱️ **Run time:** about 3.5 minutes from start (19:36 UTC) to a clean build  
  
## 🧩 Application Changes  
- 📦 **Project file:** target framework changed to `net10.0`, and all Microsoft packages moved to versions built for .NET 10  
- 🔐 **Security:** pinned `NuGet.Packaging` and `NuGet.Protocol` to 6.12.5 to clear NU1901 (GHSA-g4vj-cjjj-v7hg)  
- 🧹 **Cleanup:** removed 44 `<Content Remove>` entries that pointed at bootstrap files on one developer's Windows machine (`C:\Users\PC\.nuget\...`)  
- 🛣️ **Routing:** replaced `UseEndpoints` with top-level `app.MapControllerRoute`. The route and middleware order are unchanged.  
- 🛡️ **Null safety:** fixed the nullable warnings caused by the Identity 10 API annotations  
- 🐳 **Container:** Dockerfile base images moved to 10.0, keeping port 80  
- 📝 **Docs:** added a migration summary at `/data/code/summary.md`  
  
## 🛠️ Tools Used  
- ⚙️ **.NET SDK 10.0.404:** restore, build and verification. SDKs 5.0 to 9.0 are also installed.  
- ☁️ **AWS Transform Custom CLI (atx):** transformation `AWS/dotnet-modernization`, region `ap-southeast-2`  
  - ❌ Exited with code 1 because the service quota was used up. It made no code changes.  
- 🤖 **Kiro CLI 2.35.1:** took over automatically when atx failed  
  - 🧠 Model: **Claude Opus 5.5** (`claude-opus-5.5`). It succeeded on attempt 1 of 5.  
  - 📚 Used the .NET migration knowledge base under `/app/knowledge` (package versions and project-file guidance)  
- 🌐 **NuGet API:** used to look up the latest 10.0.x package versions and patched NuGet.* versions  
  
## 💻 Code Changes  
| 📄 File | ✏️ Change |  
|---|---|  
| `PetShopWeb/PetShopWeb.csproj` | `net6.0` → `net10.0`. Identity.EntityFrameworkCore, EF Core Proxies, SqlServer and Tools: 6.0.11/7.0.0 → **10.0.15**. CodeGeneration.Design: 6.0.10 → **10.0.3**. Added NuGet.Packaging/Protocol **6.12.5**. Removed the stale `Content Remove` entries. |  
| `PetShopWeb/Program.cs` | Connection string declared as `string?`. `adminRole.Name!` used when seeding. `UseEndpoints` replaced with `MapControllerRoute` (ASP0014). |  
| `PetShopWeb/Controllers/AccountController.cs` | Null-forgiving `!` on values that `[Required]` validation already guarantees and on Identity `Name`/`UserName`. `FindByIdAsync(id ?? string.Empty)` in `EditUser`/`EditRole` (POST). `EditUsersInRole` (POST) skips user ids that don't exist. |  
| `PetShopWeb/Attributes/ImageExtensionAttribute.cs` | `path` declared as `string?`. Behaviour unchanged. |  
| `Dockerfile` | `sdk:6.0`/`aspnet:6.0` → `sdk:10.0`/`aspnet:10.0`. Added `ENV ASPNETCORE_HTTP_PORTS=80`, because .NET 8+ images listen on 8080 by default. |  
  
- 📊 **Diff size:** 5 files changed, 38 insertions, 77 deletions  
- ⚠️ **Behaviour changes, both minor and defensive:**  
  - A missing id posted to `EditUser`/`EditRole` now returns the existing not-found result instead of throwing `ArgumentNullException`  
  - An unknown user id posted to `EditUsersInRole` is now skipped instead of throwing  
  
## ⏳ Time Savings Estimate  
- 👩‍💻 **Manual estimate:** about **6–10 engineer-hours**:  
  - Research package versions and advisories: about 1–2 h  
  - Upgrade the project and restore: about 1 h  
  - Triage and fix 23 warnings (nullable, NU1901, ASP0014): about 2–3 h  
  - Update the Dockerfile and port behaviour: about 1 h  
  - Verify and write documentation: about 1–2 h  
- 🤖 **Automated run:** about **3.5 minutes** from start to a clean build, including the atx attempt and handover  
- 💰 **Saving:** about **95%** of hands-on effort. Human time is still needed for the runtime validation listed below.  
- 📝 **Note:** this is a small, low-complexity upgrade (one project, .NET Core to .NET Core, no System.Web or EF6), so the absolute saving is modest. The share of effort saved is high.  
  
## 🚀 Next Steps  
### ✅ Validation  
- 🧪 Run the app against SQL Server and smoke-test these areas:  
  - Login, logout and registration  
  - User and role management pages  
  - The catalog and animal-details pages, which use EF Core lazy-loading proxies  
  - Photo upload in the admin add/edit pages  
- 🐳 Rebuild the container images from the new Dockerfile, because `docker-compose.yml` still uses `yarins2000/petshopapp:1.0` and `petshopdb:1.0`. Then confirm the `3000:80` port mapping works.  
- 🔍 Run a vulnerability scan (`dotnet list package --vulnerable --include-transitive`) in CI  
  
### 🔧 Recommended Improvements  
- 🐞 **Validation bug:** `CreateRoleViewModel` imports `Microsoft.Build.Framework`, so its `[Required]` attribute is MSBuild's and never validates `RoleName`. Switch the import to `System.ComponentModel.DataAnnotations`. This bug existed before the upgrade.  
- 🗄️ **Data loss risk:** `Program.cs` calls `EnsureDeleted()` and `EnsureCreated()` on every startup, which wipes the database in every environment. Limit this to Development, or move to EF Core migrations.  
- 🔑 **Secrets:** move the seeded admin password (`Admin123`) and connection strings out of code and `appsettings.json`, into environment variables or a secret store  
- 🧪 **Tests:** add a test project (xUnit plus `Microsoft.AspNetCore.Mvc.Testing`) so later upgrades can check behaviour automatically  
- ☁️ **Quota:** request a quota increase for `AWS/dotnet-modernization` from AWS Support if more atx runs are planned  

## Kiro credit usage

💳 Kiro CLI credits used: 9.79  
Includes 0.91 for job setup (startup smoke test and knowledge-base indexing).  
