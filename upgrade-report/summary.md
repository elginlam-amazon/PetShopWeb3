# 🚀 .NET Upgrade — Executive Report  
  
**Module:** PetShopWeb (ASP.NET Core MVC pet-shop web application)  
**Upgrade:** .NET 6.0 ➜ .NET 10.0 (`net6.0` ➜ `net10.0`)  
**Iteration:** 2 · **Date:** 19 Nov 2026 · **Branch:** `ec2-jobrole-mwo1jm8n-ua-claude-upgrade`  
**Engine:** `DOTNET_UPGRADE_ASSISTANT_WITH_CLAUDE_CLI` · **Outcome:** ✅ Success  
  
---  
  
## 1. 📋 Executive Summary  
  
- 🎯 The PetShopWeb application — a single-project ASP.NET Core MVC app using EF Core, ASP.NET Core Identity, SQL Server, Bootstrap and Docker — was upgraded end-to-end from the **out-of-support .NET 6.0** runtime to **.NET 10.0**, fully automatically and with no human intervention.  
- 🤖 The run combined two tools: **Microsoft .NET Upgrade Assistant** performed the mechanical in-place project upgrade (target framework, package version mapping, solution metadata), then **Claude Code (Opus 5)** took over to resolve the residual build issues, drive warnings to zero, and modernise the container/deployment assets.  
- 🟢 **Final state: `dotnet build` succeeds with 0 errors and 0 warnings** in both Debug and Release, verified from a clean `obj/`/`bin/` state, and `dotnet publish -c Release` produces a working publish output.  
- 🧹 Beyond the version bump, the run eliminated **4 known-vulnerability package advisories (NU1901)**, **19 nullable-reference-safety warnings**, **1 ASP.NET Core analyzer advisory (ASP0014)**, and found two latent defects the upgrade exposed — a non-functional validation attribute and a broken Docker build command.  
- ⏱️ Total automated wall-clock: **~8 minutes** (Upgrade Assistant ~1.5 min + Claude Code ~5.5 min), replacing an estimated **4–6 hours** of manual engineering effort — a **~97% reduction in hands-on time**.  
- ⚠️ No blocking issues remain. Seven non-blocking, human-decision follow-ups (database seeding strategy, hard-coded credentials, environment connection strings, container image republish) are documented in `/data/code/summary.md` and repeated in §6 below.  
  
---  
  
## 2. 🔧 Application Changes  
  
### Framework & project  
  
- 🆙 `PetShopWeb.csproj`: `TargetFramework` `net6.0` ➜ `net10.0` (project was already SDK-style, so no `System.Web`/`Global.asax`/`Web.config` port was required).  
- 📦 All framework-tied NuGet packages realigned to the target runtime major (`10.0.x`):  
  | Package | Before | After |  
  | --- | --- | --- |  
  | `Microsoft.AspNetCore.Identity.EntityFrameworkCore` | 6.0.11 | **10.0.15** |  
  | `Microsoft.EntityFrameworkCore.SqlServer` | 7.0.0 | **10.0.15** |  
  | `Microsoft.EntityFrameworkCore.Proxies` | 7.0.0 | **10.0.15** |  
  | `Microsoft.EntityFrameworkCore.Tools` | 6.0.11 | **10.0.15** (dev-time only, `PrivateAssets=all`) |  
  | `Microsoft.VisualStudio.Web.CodeGeneration.Design` | 6.0.10 | **removed** 🗑️ |  
- 🔐 **Security:** removing the unused design-time scaffolding package eliminated the only source of 4 `NU1901` advisories (transitive `NuGet.Packaging` / `NuGet.Protocol` 6.12.1 — GHSA-g4vj-cjjj-v7hg). Removal was chosen over suppression per the migration knowledge base.  
- 🧽 Deleted **44 dead `<Content Remove="C:\Users\PC\.nuget\...">` items** hard-coded to a Windows user profile for a NuGet package the project does not reference.  
- 🗂️ `PetShopWeb.sln`: project type GUID normalised to the SDK-style GUID (`9A19103F-…`).  
  
### Hosting & container  
  
- 🐳 `Dockerfile`: base images `sdk:6.0`/`aspnet:6.0` ➜ `sdk:10.0`/`aspnet:10.0`.  
- 🛠️ Fixed a latent container-build defect: restore/publish now name the project explicitly (the previous bare `dotnet publish` was ambiguous between `PetShopWeb.csproj` and `PetShopWeb.sln` in the build directory — `MSB1011`).  
- 🔌 Pinned `ASPNETCORE_URLS=http://+:80` so the .NET 8+ default-port change (8080) does not break the existing `docker-compose` `3000:80` mapping; `docker-compose.yml` deliberately left untouched.  
- 🧾 Removed the inert `EXPOSE 433` line (typo for 443; no HTTPS endpoint configured in the image).  
  
### Documentation  
  
- 📝 `README.md`: "ASP.NET Core 6" ➜ "ASP.NET Core 10".  
- 📄 `/data/code/summary.md` created: full change log, intentional behavioural changes, and prioritised follow-ups.  
  
---  
  
## 3. 🧰 Tools Used  
  
- 🟣 **.NET SDK 10.0.404** — build, restore, publish and verification host (SDKs 5.0/6.0/7.0/8.0/9.0/10.0 all available in the container).  
- 🔵 **Microsoft .NET Upgrade Assistant `1.0.518.26217`** — CLI, non-interactive, `framework.inplace` upgrade to .NET 10.0:  
  - ✅ Result: **7 steps succeeded, 0 failed, 349 skipped** (skips were `.js`/`.scss`/`.json` client assets with nothing to transform).  
  - 🔁 Transformers applied: `TargetFrameworkTransformer`, `DefaultPackageMapTransformer`, `WebPackageMapTransformer`, `SolutionProjectTypeTransformer`, `ProjectDependenciesFinalizerTransformer`, `InplacePackageReferenceTransformer`.  
  - ℹ️ Logged `Msbuild was not found` during its internal build probes — benign under the CLI; the subsequent `dotnet build` is authoritative.  
- 🟠 **Claude CLI `2.1.266` (Claude Code)** — model **`global.anthropic.claude-opus-5` (Claude Opus 5)**, running non-interactively as the automated build-fix and reporting agent:  
  - 📊 42 turns · 332 s wall-clock (246 s API) · 19,759 output tokens (7,936 reasoning) · 1.56 M cached input tokens read · **$1.84 run cost**.  
  - 🧠 Guided by the 36-document .NET migration knowledge base (`/app/knowledge`), notably KB 27 (blocked packages & build integrity) and KB 19 (Linux containerization).  
  - 🛡️ Zero permission denials, zero subagents, zero API errors; peak memory 4 GB within a 4 GB cap, **0 OOM kills**.  
- 🧪 **Test tooling** — baseline test sweep ran and exited cleanly, but discovered **0 test projects / 0 test cases**, so there was no automated test signal to preserve. No test was deleted, skipped or weakened.  
  
---  
  
## 4. 👨‍💻 Code Changes  
  
- 📁 **8 files modified, 1 added, 0 deleted** — `Dockerfile`, `PetShopWeb.sln`, `PetShopWeb.csproj`, `Program.cs`, `Controllers/AccountController.cs`, `Attributes/ImageExtensionAttribute.cs`, `ViewModels/CreateRoleViewModel.cs`, `README.md`, plus new `summary.md`.  
  
### `Program.cs`  
  
- 🧭 `app.UseEndpoints(endpoints => endpoints.MapControllerRoute(...))` ➜ top-level `app.MapControllerRoute("default", "{controller=Home}/{action=HomePage}/{id?}")` — clears `ASP0014`; identical route table and middleware order.  
- 🔗 Connection string now read via `Configuration.GetConnectionString("DefaultConnection")` with an explicit `InvalidOperationException` when absent (same config key; now fails fast at startup with a clear message instead of a late SqlClient error).  
- 🧷 `AddToRoleAsync(adminUser, adminRole.Name!)` — null-forgiving on a role constructed from a literal.  
  
### `Controllers/AccountController.cs` — 19 nullable warnings resolved (`CS8604`, `CS8620`)  
  
- ✅ `[Required]` view-model values (`Email`, `Password`, `RoleName`) and Identity-owned values (`IdentityUser.UserName`, `IdentityRole.Name`) now use the null-forgiving operator — assertions only, no behavioural change.  
- 🛟 `EditUser` / `EditRole` (POST) no longer pass a possibly-null `Id` into `FindByIdAsync`; a null `Id` takes the **existing** not-found branch instead of throwing `ArgumentNullException` (HTTP 500). *Intentional behavioural improvement.*  
- 🛟 `EditUsersInRole` (POST) skips a posted `UserId` that is null or no longer resolves to a user, instead of passing `null` into `IsInRoleAsync`/`AddToRoleAsync`; remaining users still process. *Intentional behavioural improvement.*  
  
### `Attributes/ImageExtensionAttribute.cs`  
  
- 🔤 Local `path` declared `string?` to match `value as string` (`CS8600`); validation outcome unchanged (null path ⇒ invalid).  
  
### `ViewModels/CreateRoleViewModel.cs` — 🐛 latent defect fixed  
  
- ⚠️ The file imported `Microsoft.Build.Framework`, so its `[Required]` was **MSBuild's** `RequiredAttribute` — it performed **no model validation at all**. Switched to `System.ComponentModel.DataAnnotations`, so `RoleName` is now genuinely validated, consistent with every other view model and the view's validation message. This also removed an accidental compile-time dependency on `Microsoft.Build`.  
  
### Build progression  
  
| Stage | Errors | Warnings |  
| --- | --- | --- |  
| 🟡 Pre-upgrade baseline (`net6.0`) | 0 | 3 (`NETSDK1138` — framework out of support) |  
| 🔴 First post-upgrade build | 1 (`MSB3374` — stale root-owned `obj/` artifacts, not a code issue) | 4 |  
| 🟠 After artifact cleanup | 0 | 23 |  
| 🟢 **Final (Debug + Release, clean state)** | **0** | **0** |  
  
---  
  
## 5. ⏱️ Time Savings Estimate  
  
- 🤖 **Automated run: ~8 minutes** wall-clock (Upgrade Assistant ~1.5 min, Claude Code ~5.5 min, build/verify loops in between) at a compute cost of **$1.84**.  
- 👷 **Estimated manual equivalent: 4–6 engineer-hours**, based on the actual scope delivered:  
  | Task | Manual estimate |  
  | --- | --- |  
  | 🔍 TFM bump + researching/validating 5 package versions across 3 package families | 0.5–1.0 h |  
  | 🧪 Analysing and correctly resolving 23 build warnings (19 nullable sites across one 390-line controller, each needing a judgement call: assert vs. guard) | 1.5–2.5 h |  
  | 🕵️ Tracing the 4 `NU1901` advisories to their single transitive source (`dotnet nuget why` / assets inspection) and discovering the `Microsoft.Build.Framework` leak | 0.5–1.0 h |  
  | 🐳 Dockerfile modernisation + diagnosing the ambiguous-publish defect + port-contract reasoning | 0.5–1.0 h |  
  | ✅ Clean-state build/publish verification in Debug and Release, plus change documentation | 0.5 h |  
- 📉 **Net saving: ~4–6 hours per module, ≈97% reduction in hands-on engineering time.**  
- 💡 Qualitative savings beyond the clock: three issues a time-boxed manual bump would plausibly have shipped unnoticed — the **inert `[Required]` attribute**, the **vulnerable transitive NuGet chain**, and the **broken container publish command**.  
- 📐 *Assumptions:* one mid-level .NET engineer familiar with ASP.NET Core but not this codebase; excludes code review, QA, and deployment; excludes the human-decision items in §6, which remain outstanding for both the automated and manual paths.  
  
---  
  
## 6. ✅ Next Steps  
  
### Validation (recommended before merge)  
  
- 🧷 **Code review the diff** — 8 files; pay particular attention to the three intentional behavioural changes (null-`Id` handling in `EditUser`/`EditRole`/`EditUsersInRole`) and to `CreateRoleViewModel` now actually enforcing `[Required]`.  
- 🗄️ **Run the app against a real SQL Server** and smoke-test the core journeys: browse catalog, filter by category, add a comment, register, log in as `admin@admin.com`, create/edit/delete a role, assign users to roles, add/edit/delete an animal. The app was intentionally **not started** during the upgrade run.  
- 🐳 **Build and run the container** (`docker build . && docker run -p 3000:80 …`) to confirm the .NET 10 image boots and serves on port 80 as before.  
- 🔁 **Verify EF Core 10 behaviour** against a live database — lazy-loading proxies, Identity schema creation, and `HasData` seeding all exercise paths that cannot be validated by a build alone.  
  
### Improvement recommendations (prioritised)  
  
- 🚨 **P1 — Startup wipes the database.** `Program.cs` calls `EnsureDeleted()` then `EnsureCreated()` unconditionally on every boot. Pre-existing behaviour, deliberately left unchanged, but unsafe outside development. Guard with `app.Environment.IsDevelopment()` or move to EF Core migrations (`dotnet ef migrations add` + `Database.Migrate()`), which the retained `EntityFrameworkCore.Tools` reference already supports.  
- 🔐 **P1 — Hard-coded seed admin credentials** (`admin@admin.com` / `Admin123`) in `Program.cs` and documented in `README.md`. Move to configuration/user-secrets and force a password change on first login. Needs a product decision.  
- 🧪 **P1 — No automated tests exist.** There is no regression safety net for this or any future upgrade. Highest-leverage follow-up: add an xUnit project with controller/repository unit tests and a `WebApplicationFactory` integration smoke test.  
- 🌐 **P2 — Connection string** in `appsettings.json` still targets Windows LocalDB (`Server=(localdb)\AspNetCoreCourse`) and cannot work in the Linux container. Override per environment via `ConnectionStrings__DefaultConnection` or an environment-specific `appsettings.*.json` rather than editing the committed default.  
- 📦 **P2 — Republish the container image.** `docker-compose.yml` pins pre-built images (`yarins2000/petshopapp:1.0`), so it will keep running the old .NET 6 build until a new tag is published and referenced — or switch the `app` service to `build: .`. The obsolete `version: '3.3'` key can also be dropped.  
- 🎨 **P3 — Adopt `MapStaticAssets()`** (.NET 9+) for fingerprinted, pre-compressed static assets. Deferred because it changes asset URL generation and all views use plain `~/` paths — worth a dedicated pass.  
- 🧩 **P3 — Refresh client-side libraries** (bootstrap 5.2.2, jQuery 3.6.1, jquery-validation 1.19.5 via `libman.json`/`package.json`). Unrelated to the .NET target; carries visual-regression risk, so schedule separately.  
- 🔎 **P3 — Re-run `dotnet list package --outdated`** before release in case a newer `10.0.x` patch has shipped since this run.  
- 🏗️ **P3 — Process note:** the single transient build failure was `MSB3374`, caused by `obj/`/`bin/` artifacts left owned by a different user between iterations — not a code defect. Adding a `dotnet clean` / artifact-purge step at the start of each pipeline iteration removes this class of false failure.  

## Claude CLI usage

💳 Estimated Claude CLI usage (USD): 4.201215  
