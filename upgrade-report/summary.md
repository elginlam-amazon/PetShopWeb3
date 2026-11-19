# 📊 Executive Report: PetShopWeb .NET 6.0 → .NET 10.0 Upgrade  
  
## 1. 🧭 Executive Summary  
  
PetShopWeb (ASP.NET Core 6 MVC, EF Core, ASP.NET Core Identity, SQL Server) was upgraded to **.NET 10.0** in iteration 3, which started on 19 Nov 2026 at 21:04 UTC. AWS Transform Custom CLI stopped before changing any code because the account had used up its monthly quota for `AWS/dotnet-modernization` (`SERVICE_QUOTA_EXCEEDED`, exit 1). Kiro CLI then did the full migration and the carry-over tasks from iteration 2. ✅ The Release build has **0 errors and 0 warnings**. The build started with 23 warnings once the target changed. ⚠️ The repository has no test projects, so the upgrade has no automated tests behind it, and the app has not been run in a container or a staging environment.  
  
## 2. 🏗️ Application Changes  
  
- 🎯 **Target framework:** `net6.0` → `net10.0` (1 project in scope: `./PetShopWeb/PetShopWeb.csproj`)  
- 📦 **Framework packages:** the project mixed 6.0.11 and 7.0.0 versions. All Identity and EF Core packages are now on `10.0.15`.  
- 🔐 **Security:** fixed a known vulnerability (NU1901, GHSA-g4vj-cjjj-v7hg) in the transitive `NuGet.Packaging` / `NuGet.Protocol` packages.  
- 🛣️ **Routing:** moved to top-level route registration, which is the current ASP.NET Core pattern. The route template is unchanged.  
- 🐳 **Container:** Docker base images moved to .NET 10. The listening port stays at 80, so the existing `docker-compose.yml` mapping still works.  
- 📝 **Docs:** `README.md` now describes ASP.NET Core 10 and has build-from-source instructions.  
- ✅ **Not needed:** the app was already ASP.NET Core, with no System.Web, EF6, Global.asax, Web.config or packages.config to migrate.  
  
## 3. 🛠️ Tools Used  
  
- ⚙️ **.NET SDK 10.0.404:** built and validated the project (`dotnet build … --configuration RELEASE`). SDKs 5.0 to 9.0 are also installed.  
- ☁️ **AWS Transform Custom CLI (atx):** ran the `AWS/dotnet-modernization` transformation in non-interactive mode, region `ap-southeast-2`. It failed on the service quota and made no code changes. CodeKB was not available for this job.  
- 🤖 **Kiro CLI 2.35.1 with model `claude-opus-5.5`:** took over after atx failed. It did the migration, fixed the warnings, and handled the carry-over prompt. Both runs succeeded on the first attempt (1 of 5 allowed).  
- 📚 **Migration knowledge base:** used as a reference (`/app/knowledge`, README error-code index).  
  
## 4. 💻 Code Changes  
  
The changes cover 6 files, with 50 lines added and 31 removed:  
  
- 📄 **`PetShopWeb/PetShopWeb.csproj`**  
  - 🎯 `TargetFramework` changed to `net10.0`  
  - ⬆️ `Microsoft.AspNetCore.Identity.EntityFrameworkCore`, `Microsoft.EntityFrameworkCore.Proxies`, `.SqlServer` and `.Tools` updated to `10.0.15`  
  - ⬆️ `Microsoft.VisualStudio.Web.CodeGeneration.Design` updated to `10.0.3`  
  - 📌 `NuGet.Packaging` and `NuGet.Protocol` pinned to the patched `6.12.5`  
- 📄 **`PetShopWeb/Program.cs`**  
  - 🛣️ `app.UseEndpoints(...)` replaced with `app.MapControllerRoute(...)` (ASP0014)  
  - ❔ Nullable annotations added: `string? connectionString`, `adminRole.Name!`  
- 📄 **`PetShopWeb/Controllers/AccountController.cs`:** nullable fixes (CS8604/CS8620) on values that `[Required]` validation or Identity guarantee. Runtime behaviour is unchanged.  
- 📄 **`PetShopWeb/Attributes/ImageExtensionAttribute.cs`:** local `path` declared as `string?` (CS8600)  
- 🐳 **`Dockerfile`:** `sdk:6.0`/`aspnet:6.0` → `sdk:10.0`/`aspnet:10.0`, plus `ENV ASPNETCORE_HTTP_PORTS=80`  
- 📝 **`README.md`:** framework version, a note about the old .NET 6 image, and build-from-source instructions  
- 📈 **Build result:** 23 warnings → 0 warnings, 0 errors  
  
## 5. ⏱️ Time Savings Estimate  
  
- 📏 **Scope:** small. There was 1 project and the app already ran on ASP.NET Core, so no architectural migration was needed.  
- 👩‍💻 **Manual estimate:** about **4 to 6 hours** for a developer:  
  - 🔎 Look up package versions and check compatibility (~1 h)  
  - 🔐 Trace the vulnerable transitive packages and confirm the patched versions (~0.5–1 h)  
  - ❔ Fix 23 nullable and analyzer warnings without changing behaviour (~1–1.5 h)  
  - 🐳 Update the Dockerfile, including the .NET 8+ default port change (~0.5 h)  
  - 📝 Review code, update docs, and record findings (~1–1.5 h)  
- 🤖 **Automated effort:** minutes of agent run time across two Kiro CLI runs, plus human review time.  
- 💰 **Estimated saving:** about **3.5 to 5.5 engineer-hours, roughly 85–90%**. This is an estimate from the scope of the work; the log has no end timestamp, so the actual run time was not measured.  
  
## 6. 🚀 Next Steps  
  
### 🧪 Validation  
  
- 🐳 Build the .NET 10 Docker image and run it with `docker-compose`. This has not been done yet.  
- 🔍 In staging, test against SQL Server:  
  - 🔑 admin login (`admin@admin.com`)  
  - 🐾 catalog and category filtering  
  - 💬 comments  
  - 🛠️ admin pages for animals, users and roles  
- 🏷️ Publish the new image under a new tag such as `petshopapp:2.0` instead of overwriting `1.0`, and update `docker-compose.yml` to use it.  
  
### 🛡️ Fix Before Production (existing bugs, left unchanged)  
  
- 🚨 **Database wipe:** `Program.cs` calls `EnsureDeleted()` + `EnsureCreated()` on every start, which drops the database each time. Run it only in Development, or switch to EF Core migrations.  
- ⚠️ **Role name not validated:** `CreateRoleViewModel` uses `Microsoft.Build.Framework.Required`, so MVC never checks `RoleName` as required. Switch the `using` to `System.ComponentModel.DataAnnotations`.  
- ⚠️ **Admin seed check:** the check looks for `UserName == "Admin"`, but the seeded user is `admin@admin.com`, so the check never matches.  
- ⚠️ **Unknown user ID:** in the `EditUsersInRole` POST action, an unknown user ID throws an exception. It should return `NotFound()`.  
  
### ✨ Improvements  
  
- 🧪 Add a test project: xUnit with `Microsoft.AspNetCore.Mvc.Testing` and the EF Core SQLite or InMemory provider, covering the controllers and repositories.  
- 🧹 Remove the leftover `<Content Remove="C:\Users\PC\.nuget\...">` entries from the csproj.  
- 📦 Mark `CodeGeneration.Design` as `PrivateAssets="all"`, or remove it. Removing it also makes the `NuGet.*` pins unnecessary.  
- ☁️ Ask AWS Support to raise the `AWS/dotnet-modernization` quota before the next atx run.  

## Kiro credit usage

💳 Kiro CLI credits used: 12.29  
Includes 0.90 for job setup (startup smoke test and knowledge-base indexing).  
