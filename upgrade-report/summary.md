# 🚀 .NET 6.0 → .NET 10.0 Upgrade — Executive Report  
  
**Project:** PetShopWeb (ASP.NET Core MVC) · **Module:** `ec2-jobrole-mwnntrbd-ua-claude`  
**Commit:** `ec96465` · **Iteration:** 2 · **Date:** 19 Nov 2026 · **Outcome:** ✅ **Success**  
  
---  
  
## 1. 📋 Executive Summary  
  
- ✅ **The upgrade is complete and verified.** PetShopWeb was moved from **.NET 6.0 (out of support)** to **.NET 10.0**, and the solution now builds with **zero errors and zero warnings** in both Debug and Release, plus a clean `dotnet publish -c Release`.  
- 🔁 **Two-stage automated pipeline.** Microsoft Upgrade Assistant performed the mechanical retargeting (framework + NuGet versions), then Claude CLI resolved everything the retarget exposed — a build blocker, 23 compiler/analyzer warnings, a deprecated hosting API, and the container definition.  
- 📊 **Measured progression:** pre-upgrade `net6.0` build succeeded with **0 errors / 3 warnings** (all `NETSDK1138` end-of-support notices) → immediately after retargeting to `net10.0`: **1 error / 23 warnings** → final state: **0 errors / 0 warnings**.  
- 🎯 **Low-risk upgrade profile.** This was already an ASP.NET Core application, not ASP.NET MVC 5 or Web Forms — so no `System.Web`, `Global.asax`, OWIN, WCF or EF6 rewrite was needed. A scan for legacy APIs (`System.Web.*`, `System.Data.Entity`, `[Index]`, `[AllowHtml]`, `PreApplicationStartMethod`, `HtmlTextWriter`, `IRegisteredObject`, `CacheDependency`) returns **no matches**.  
- 🐞 **Three defects found and fixed that pre-dated the upgrade**, including a validation attribute that silently never ran and two unhandled crash paths in user/role administration. These were latent in production on .NET 6.  
- ⚠️ **One decision needs team sign-off** (scaffolding package removal) and **nine non-blocking recommendations** are recorded in section 6 — none of them block release.  
- 📦 **Footprint:** 7 files changed, 46 insertions, 91 deletions — a net *reduction* in code, since most of the change was deleting dead project-file configuration.  
  
---  
  
## 2. 🔧 Application Changes  
  
### Framework & Packages (automated by Upgrade Assistant)  
- 🎯 `TargetFramework`: `net6.0` → `net10.0`  
- 🆔 Solution project-type GUID modernised in `PetShopWeb.sln` (`FAE04EC0…` → `9A19103F…`)  
- 📦 All five NuGet references aligned to the .NET 10 runtime band:  
  
| Package | Before | After |  
|---|---|---|  
| `Microsoft.AspNetCore.Identity.EntityFrameworkCore` | 6.0.11 | **10.0.15** |  
| `Microsoft.EntityFrameworkCore.Proxies` | 7.0.0 | **10.0.15** |  
| `Microsoft.EntityFrameworkCore.SqlServer` | 7.0.0 | **10.0.15** |  
| `Microsoft.EntityFrameworkCore.Tools` | 6.0.11 | **10.0.15** |  
| `Microsoft.VisualStudio.Web.CodeGeneration.Design` | 6.0.10 | 10.0.3 → *removed* ⬇️ |  
  
- 📝 Note: the project had **mixed runtime bands** before the upgrade (EF Core 7.0 packages alongside ASP.NET Core 6.0 ones) — an unsupported combination that is now consistent.  
  
### Application Code & Configuration (Claude CLI)  
- 🏗️ **Hosting API modernised** — deprecated `UseEndpoints(...)` replaced with top-level route registration; middleware reordered to the canonical ASP.NET Core sequence.  
- 🛡️ **Null-safety hardened** — all 21 nullable-reference warnings resolved, two of them real `NullReferenceException` / `ArgumentNullException` crash paths.  
- 🧹 **Project file cleaned** — 46 dead/redundant item declarations removed.  
- 🔐 **Supply-chain advisories cleared** — both `NU1901` vulnerability warnings eliminated.  
- 🐳 **Container definition upgraded** to .NET 10, including a pre-existing broken build stage and a breaking port-default change in the .NET 8+ base images.  
  
### Explicitly *not* changed (scope discipline)  
- 🚫 No test was deleted, skipped, or weakened — the repository contains **no test project** (`test-inventory/before.json` = `{"files":0,"tests":[]}`).  
- 🚫 Client-side libraries (bootstrap 5.2.2, jQuery 3.6.1) left alone — independent of the .NET target.  
- 🚫 Pre-existing design concerns (database wipe on startup, seeded credentials) documented rather than silently refactored.  
  
---  
  
## 3. 🛠️ Tools Used  
  
- ⚙️ **.NET SDK 10.0.404** — MSBuild 18.9.15, runtime host 10.0.15, RID `linux-arm64` on Amazon Linux 2023. Six SDKs side-by-side (5.0 → 10.0) enabled the pre-upgrade baseline build.  
- 🔄 **Microsoft .NET Upgrade Assistant 1.0.518.26217** — ran in `framework.inplace` mode targeting .NET 10.0.  
  - ✅ Result: **7 succeeded, 0 failed, 349 skipped**  
  - 🧩 Transformers applied: `TargetFrameworkTransformer`, `SolutionProjectTypeTransformer`, `DefaultPackageMapTransformer`, `WebPackageMapTransformer`, `InplaceDuplicatePackageReferenceTransformer`, `ProjectPackagesConfigCleanupTransformer`, `ProjectDependenciesFinalizerTransformer`  
  - ℹ️ It walked 349 source/asset files (`.cs`, `.cshtml`, `.scss`, `.js`) and **skipped every one** — no code rewrite was required, confirming the app was already ASP.NET Core.  
  - ⚠️ **Limitation observed:** UA logged `Msbuild was not found.` twice, so it could **not build or validate** its own output. Everything after the retarget fell to the next stage.  
- 🤖 **Claude CLI 2.1.266 (Claude Code)** — model **Claude Opus 5** (`claude-opus-5`, via AWS Bedrock), 200K context.  
  - 📈 53 turns · ~7.5 min wall-clock (372 s API time) · **$3.01** total cost  
  - 🧠 10,007 thinking tokens; 3.18 M cached-read tokens (aggressive cache reuse kept cost down)  
  - 🎯 Role: diagnose the build blocker, drive 23 warnings to zero, modernise the hosting API and Dockerfile, and verify  
- 📚 **Migration knowledge base** (36 domain-specific documents) — consulted for the error-code map and package-version matrix; the System.Web/Web Forms/EF6 chapters were correctly determined to be inapplicable.  
- 📊 **Resource profile:** 4 GiB cgroup limit, peak 4 GiB, **0 OOM kills**.  
  
---  
  
## 4. 💻 Code Changes  
  
**7 files · +46 / −91 lines**  
  
### 🚨 `PetShopWeb/ViewModels/CreateRoleViewModel.cs` — silent validation bug  
- 🐞 `RoleName` carried `[Required]` resolved from `using Microsoft.Build.Framework` — that is **MSBuild's task-parameter attribute**, not `System.ComponentModel.DataAnnotations.RequiredAttribute`.  
- 💥 **Impact:** it compiled cleanly, but MVC model validation ignored it entirely — "Create role" accepted **blank role names** on .NET 6.  
- ✅ Corrected the `using`. Also a prerequisite for removing the scaffolding package that was transitively supplying `Microsoft.Build.Framework`.  
  
### 🛡️ `PetShopWeb/Controllers/AccountController.cs` — 15 warnings, 2 real crash paths  
- 💥 `EditUsersInRole` (POST) passed a possibly-null `UserId` to `FindByIdAsync`, then the possibly-null result into `IsInRoleAsync`/`AddToRoleAsync` — a stale or tampered form **threw instead of skipping the row**. Now skips via `continue`, reusing the method's existing control flow.  
- 💥 `EditUser` / `EditRole` (POST) passed nullable `model.Id` to `FindByIdAsync`, which throws `ArgumentNullException` on null. Both now route to the **pre-existing** `NotFound()` / `View(nameof(RolesManager))` branch.  
- ✅ `AddToRolesAsync` no longer receives `IEnumerable<string?>` (`CS8620`); null role names are filtered first.  
- ✅ Remaining fixes are annotation-only and behaviour-preserving (`role.Name!` where the role is null-checked one line above; `!` on `[Required]` fields inside `if (ModelState.IsValid)`).  
  
### ⚙️ `PetShopWeb/Program.cs`  
- ✅ `app.UseEndpoints(e => e.MapControllerRoute(...))` → top-level `app.MapControllerRoute(...)` — clears `ASP0014`.  
- ✅ Middleware reordered to canonical `UseRouting` → `UseAuthentication` → `UseAuthorization` (was `UseAuthentication` → `UseRouting` → `UseAuthorization`). Behaviour-neutral here: cookie auth does not consult the matched endpoint, and authorization still runs after routing.  
- ✅ `Configuration["ConnectionStrings:DefaultConnection"]` → `GetConnectionString("DefaultConnection")` with `?? throw new InvalidOperationException(...)` — same lookup, clear startup failure instead of an obscure downstream one.  
- ✅ `const string adminRoleName = "admin"` replaces `adminRole.Name` in `AddToRoleAsync` (`CS8604`) — identical value.  
  
### 📄 `PetShopWeb/PetShopWeb.csproj` — −62 lines  
- 🧹 Removed **44 dead `<Content Remove="C:\Users\PC\.nuget\packages\bootstrap\5.2.2\...">` items** — an absolute path from a developer's Windows machine, with no `bootstrap` `PackageReference` to de-duplicate (client libs come via `libman.json`), unresolvable on Linux.  
- 🧹 Removed the redundant `<Folder Include="wwwroot\photos\" />` VS marker (the folder holds 18 committed files).  
- 🔐 Removed `Microsoft.VisualStudio.Web.CodeGeneration.Design` — the **sole source of both `NU1901` advisories** (pulls `NuGet.Packaging`/`NuGet.Protocol` 6.12.1, GHSA-g4vj-cjjj-v7hg). Design-time scaffolding only; not needed to build, publish or run. **See §6 item 1.**  
  
### 🐳 `Dockerfile`  
- ✅ Base images `sdk:6.0` / `aspnet:6.0` → `sdk:10.0` / `aspnet:10.0` (mandatory — a .NET 6 SDK image cannot build `net10.0`).  
- 🐞 **Fixed a pre-existing break:** the build stage copied the `.csproj` into the same `/app` directory it later filled with `COPY . .`, leaving both a `.csproj` and the `.sln` present — making the bare `dotnet publish` ambiguous. Now stages in `/src` and names the project explicitly; the exact command was verified locally.  
- ⚠️ Added `ENV ASPNETCORE_HTTP_PORTS=80`. **.NET 8+ images changed the default listen port from 80 to 8080** — without this pin, the existing `docker-compose` mapping (`3000:80`) would have silently broken.  
- 🧹 Dropped `EXPOSE 433` (a typo for 443; no HTTPS endpoint is configured, nothing listened there). `EXPOSE` is documentation-only, so compose behaviour is unchanged.  
  
### 🔩 Build blocker resolved (environment, not code)  
- 🚧 First post-retarget build failed: `error MSB3374 — The last access/last write time on file "obj/Debug/net10.0/PetShopWeb.MvcApplicationPartsAssemblyInfo.cache" cannot be set. Access … denied.`  
- 🔍 **Root cause:** stale artifacts from an earlier build were owned by `root` while the build ran as `claude`; the files were world-writable, but `utime()` requires ownership.  
- ✅ Deleted the regenerable `obj`/`bin` trees. **No application code was involved.**  
  
---  
  
## 5. ⏱️ Time Savings Estimate  
  
**Automated elapsed time: ~9 minutes** (Upgrade Assistant retarget + 7.5 min Claude CLI) at a compute cost of **$3.01**.  
  
Estimated manual effort for the same verified outcome, by a developer new to this codebase:  
  
| Task | Manual estimate |  
|---|---|  
| 🎯 Retarget + resolve 5-package version matrix across 3 source bands | 0.5 – 1.0 h |  
| 🔍 Diagnose `MSB3374` file-ownership build blocker | 0.25 – 0.5 h |  
| 🛡️ Fix 21 nullable warnings (incl. 2 genuine crash paths) | 2.0 – 3.0 h |  
| 🐞 Discover the `Microsoft.Build.Framework` `[Required]` bug | 1.0 – 2.0 h *(easily missed entirely)* |  
| 🧹 Audit & remove 46 dead project-file items | 0.25 h |  
| 🐳 Dockerfile: .NET 10 + port-default change + publish ambiguity | 1.0 – 1.5 h |  
| ✅ Verification builds, publish, documentation | 0.5 – 0.75 h |  
| **Total** | **≈ 5.5 – 9 hours (~1 developer-day)** |  
  
- 💰 **Net saving: roughly 5 – 9 hours of senior developer time**, i.e. **~97% reduction** in elapsed time (9 min vs. ~1 day).  
- 📌 *Assumptions:* single-project solution, no test suite to repair, no production data migration. Savings would scale substantially on a multi-project solution — but so would risk, so budget review time accordingly.  
- 🎁 **Unquantified upside:** the two crash paths and the dead validation attribute are latent production defects that a pure retargeting exercise would most likely have carried forward untouched.  
  
---  
  
## 6. 🔜 Next Steps  
  
### ✅ Validation before release (recommended order)  
1. 🔌 **Set a working connection string.** `appsettings.json` still ships a **LocalDB** value (`Server=(localdb)\AspNetCoreCourse`) with the containerised SQL Server string commented out above it. **LocalDB does not exist on Linux — the app cannot connect in the Docker target as configured.** Move it to environment variables / `appsettings.Development.json` per environment. *Blocks any runtime test.*  
2. 🧪 **Smoke-test the application.** It was never started during this cycle (per instructions, verification was build + publish only). Exercise: home/catalog pages, login, register, role CRUD, user-role assignment, animal add/edit/delete, and image upload validation.  
3. 🐳 **Rebuild and re-tag the container images.** `docker-compose.yml` pulls prebuilt images (`yarins2000/petshopapp:1.0`) rather than building the Dockerfile — **the .NET 10 change has no effect until those images are rebuilt and pushed.**  
4. 🔐 **Re-verify the role-based authorization paths**, since middleware order changed. Confirm `[Authorize(Roles = "admin")]` still gates the Account controller and that `AccessDenied` renders correctly.  
5. ✍️ **Review the three defect fixes** (§4) against intended business behaviour — particularly that skipping unknown users in `EditUsersInRole` is preferred over failing the request.  
  
### ⚠️ Needs a team decision  
6. 📦 **Scaffolding package removal.** `Microsoft.VisualStudio.Web.CodeGeneration.Design` was removed to clear both `NU1901` advisories. If the team uses `dotnet aspnet-codegenerator` or VS "Add → New Scaffolded Item", re-add it with `<PrivateAssets>all</PrivateAssets>` — the warnings return until its NuGet transitives move past 6.12.1. Middle option: keep it and pin those transitives. *The advisory is low severity and affects build-time tooling only, not the deployed app.*  
  
### 🛠️ Improvement recommendations (pre-existing; deliberately out of scope)  
7. 🔥 **`Program.cs` calls `ctx.Database.EnsureDeleted()` on every startup** — dropping and recreating the database, so **all user data is lost on each restart**. Unsafe for test/prod. Replace with `ctx.Database.Migrate()` + EF Core migrations, and gate seeding on `app.Environment.IsDevelopment()`. **Highest-priority item on this list.**  
8. 🔑 **Hard-coded seed credentials** (`admin@admin.com` / `Admin123`) with `RequireNonAlphanumeric = false`. Move to user-secrets/environment config and restrict to development.  
9. 🤫 **Seeding ignores its own result** — the `IdentityResult` from `CreateAsync` is assigned and never inspected, yet `AddToRoleAsync` runs regardless, so user-creation failure fails silently.  
10. 🔒 **No HTTPS in the container** — no `UseHttpsRedirection`/`UseHsts`, no certificate wiring. Fine if TLS terminates upstream; otherwise address before prod.  
11. 📁 **Consider `MapStaticAssets()`** — the .NET 10 idiom replacing `UseStaticFiles()`, adding fingerprinting and compression. `UseStaticFiles()` is still fully supported and not deprecated, so it was kept; adopting the new API changes asset URLs.  
12. 🧾 **`docker-compose.yml` housekeeping** — `version: '3.3'` is obsolete under Compose v2, and `3001:433` maps to nothing (it already did). Cosmetic only.  
13. 📚 **Refresh client-side libraries** — bootstrap 5.2.2, jQuery 3.6.1, jquery-validate 1.19.5 are unaffected by the .NET target and warrant a separate pass.  
14. 🧹 **Drop the `IIS Express` profile** from `launchSettings.json` — dead configuration for a Linux-hosted app.  
15. 🧪 **Add a test project.** The most significant structural gap: there are **zero automated tests**, so this upgrade could only be validated by compilation. Even a thin suite over `AccountController` role logic and `ImageExtensionAttribute` would de-risk future upgrades considerably.  

## Claude CLI usage

💳 Estimated Claude CLI usage (USD): 5.921329  
