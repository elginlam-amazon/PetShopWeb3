# 🚀 .NET Version Upgrade — Executive Report  
  
**Module:** PetShopWeb (ASP.NET Core MVC pet shop web application)  
**Upgrade:** .NET 6.0 ➜ .NET 10.0 · **Iteration:** 2 (attempt 1) · **Date:** 2026-11-20  
**Engine:** `DOTNET_UPGRADE_ASSISTANT_WITH_CLAUDE_CLI` · **Commit:** `ec96465`  
**Outcome:** ✅ **Success — 0 errors, 0 warnings**  
  
---  
  
## 1. 📋 Executive Summary  
  
- ✅ **The upgrade completed successfully and is build-clean.** `PetShopWeb` now targets `net10.0`, compiles with **0 errors and 0 warnings** in both Debug and Release, publishes cleanly, and reports **no vulnerable NuGet packages**.  
- 🔁 **Two-stage automated pipeline.** Microsoft's .NET Upgrade Assistant performed the mechanical retarget (TFM, package versions, solution GUID), then Claude CLI took over to fix the handoff build break, resolve every remaining warning, and modernise the container/deployment assets.  
- 🧹 **Started from a dirty handoff, finished spotless.** The Upgrade Assistant left the project with **1 build error** (`MSB3374`) and **4 security advisories** (`NU1901`); the first clean build then surfaced **23 warnings**. All 28 issues were driven to zero.  
- 🎯 **Narrow-scope, low-risk upgrade.** The app was *already* ASP.NET Core (SDK-style project, `Program.cs`, EF Core, ASP.NET Core Identity), so none of the heavy legacy workstreams applied — no `System.Web`, `System.Data.Entity`, `Global.asax`, `Web.config`, WCF, Web Forms or legacy test-stack migration was needed.  
- 🐛 **A pre-existing functional bug was found and fixed as a side effect** — `CreateRoleViewModel.RoleName` carried a `[Required]` attribute resolved from `Microsoft.Build.Framework` (MSBuild's attribute), invisible to MVC validation, so the "Create role" form had been silently accepting empty role names.  
- ⚠️ **Caveat for sign-off:** the solution contains **no test project**, so there is no automated regression evidence behind these changes. Manual validation is required (see §6).  
  
---  
  
## 2. 🔧 Application Changes  
  
### 🎯 Framework & packages  
- ⬆️ `TargetFramework`: `net6.0` ➜ `net10.0`  
- 📦 Framework-tied packages aligned to the **`10.0.15`** band (they had drifted apart — EF Core 7 runtime running on EF Core 6 tooling and Identity stores):  
  - `Microsoft.AspNetCore.Identity.EntityFrameworkCore` `6.0.11` ➜ `10.0.15`  
  - `Microsoft.EntityFrameworkCore.Proxies` `7.0.0` ➜ `10.0.15`  
  - `Microsoft.EntityFrameworkCore.SqlServer` `7.0.0` ➜ `10.0.15`  
  - `Microsoft.EntityFrameworkCore.Tools` `6.0.11` ➜ `10.0.15`  
- 🛡️ `Microsoft.VisualStudio.Web.CodeGeneration.Design` **removed** — design-time scaffolding only, not needed to build or run, and the sole source of 4 × `NU1901` advisories (transitive `NuGet.Packaging` / `NuGet.Protocol` 6.12.1, GHSA-g4vj-cjjj-v7hg)  
  
### 🧱 Project & solution hygiene  
- 🗑️ **44 dead `<Content Remove>` items deleted** — absolute paths into a Windows developer's local NuGet cache (`C:\Users\PC\.nuget\...`) that can never resolve on Linux  
- 🗑️ Redundant `<Folder Include="wwwroot\photos\" />` placeholder removed  
- 🔑 Solution project-type GUID migrated from legacy C# `{FAE04EC0-…}` ➜ SDK-style `{9A19103F-…}`  
  
### 🌐 Runtime & hosting  
- 🛣️ `UseEndpoints(…)` ➜ top-level `app.MapControllerRoute(…)` (clears `ASP0014`; route template unchanged)  
- 🔐 Middleware order corrected to `UseStaticFiles` ➜ `UseRouting` ➜ `UseAuthentication` ➜ `UseAuthorization`  
- ⚙️ Connection string now read via `GetConnectionString("DefaultConnection")` with an explicit failure message instead of a raw indexer (same config key — the `ConnectionStrings__DefaultConnection` Docker override still works)  
  
### 🐳 Containerisation  
- 🖼️ Base images `sdk:6.0` / `aspnet:6.0` ➜ `sdk:10.0` / `aspnet:10.0`  
- 🔌 Added `ENV ASPNETCORE_URLS=http://+:8080` + `EXPOSE 8080` — .NET 8+ images run **non-root** and no longer bind port 80  
- 🧯 Removed the dead `EXPOSE 433` (a typo for 443, with no certificate in the image)  
- 🏗️ Dockerfile restore/publish now name the project explicitly — the previous bare `dotnet publish` ran in a directory where MSBuild could see the `.sln` **and two copies** of the `.csproj`  
- 📑 `docker-compose.yml` remapped `3000:80` ➜ `3000:8080`, dropped the dead `3001:433` mapping and the obsolete `version:` key  
- 📘 `README.md` updated: "ASP.NET Core 6" ➜ "ASP.NET Core 10"  
  
---  
  
## 3. 🛠️ Tools Used  
  
| Tool | Version | Role |  
| --- | --- | --- |  
| 🟣 **.NET SDK** | **10.0.404** | Build, restore, publish, vulnerability audit |  
| 🔵 **Microsoft .NET Upgrade Assistant** | **1.0.518.26217** | Stage 1 — `framework.inplace` retarget, non-interactive |  
| 🟠 **Claude CLI (Claude Code)** | **2.1.266** | Stage 2 — build-break repair, warning remediation, modernisation |  
| 🧠 **Model** | **`claude-opus-5`** (Opus 5, via Bedrock) | Reasoning engine driving stage 2 |  
| 📚 **Migration knowledge base** | 34 domain docs + index | Reference patterns (KB 00, 01, 19, 24, 27 applied) |  
  
- 🔵 **Upgrade Assistant result:** `Complete: 7 succeeded, 0 failed, 349 skipped` — it changed the TFM, 5 package references and the solution GUID, then **skipped every one of the 53 source files** (`.cs` / `.cshtml`) as needing no transformation.  
- ⚠️ **It could not self-verify:** its internal build step reported `Msbuild was not found` twice, so it handed off a project it had never successfully compiled — leaving the `MSB3374` error for stage 2 to find.  
- 🟠 **Claude CLI session:** 50 turns · ~5.7 min wall-clock · 21,417 output tokens (6,114 reasoning) · 2.51 M cached tokens read · **$2.44** total inference cost · 0 permission denials.  
- 🧰 Also available in the image for compatibility checks: SDKs 5.0.408, 6.0.428, 7.0.410, 8.0.428, 9.0.321.  
- 💾 Container footprint: 4 GB limit, 4.0 GB peak, **0 OOM kills**.  
  
---  
  
## 4. 💻 Code Changes  
  
- 📁 **13 files modified**, 0 added, 0 deleted (excluding the generated change record)  
- 🎯 **28 build issues resolved to zero:** 1 × `MSB3374`, 4 × `NU1901`, 12 × `CS8604`, 3 × `CS8600`, 1 × `CS8620`, 1 × `ASP0014` (plus the 3 × `NETSDK1138` end-of-support warnings eliminated by the retarget itself)  
  
| File | Change |  
| --- | --- |  
| 🧾 `PetShopWeb.csproj` | TFM, 4 package bumps, 1 package removal, 45 dead items deleted |  
| 🔑 `PetShopWeb.sln` | SDK-style project-type GUID |  
| 🚀 `Program.cs` | Top-level routing, middleware order, null-safe connection string, `const` admin role name |  
| 👤 `Controllers/AccountController.cs` | 12 nullable-reference fixes + null-id guards on 3 POST actions |  
| 🐛 `ViewModels/CreateRoleViewModel.cs` | **Functional fix** — `[Required]` repointed from `Microsoft.Build.Framework` to `System.ComponentModel.DataAnnotations` |  
| 🖼️ `Attributes/ImageExtensionAttribute.cs` | `string?` for `as string`; `ToLower()` ➜ `ToLowerInvariant()` (culture-independent on Linux) |  
| 🐳 `Dockerfile`, `docker-compose.yml` | .NET 10 images, port 8080, explicit project paths |  
| 📘 `README.md` | Version reference |  
| 🧼 `PetShopContext.cs`, `AccountRepository.cs`, `IAccountRepository.cs`, `EditRoleViewModel.cs` | Unused `using` removal (no behaviour change) |  
  
### ⚠️ Three changes alter runtime behaviour (intentional, documented)  
1. 🐛 **Empty role names are now rejected** by `ModelState.IsValid` — validation that never actually ran before.  
2. 🔐 **Authentication now runs after `UseRouting`**, so per-endpoint `[Authorize]` metadata is honoured rather than order-dependent. Cookie sign-in behaviour is unchanged.  
3. 🛡️ **Null/unknown ids posted to `EditUser` / `EditRole` / `EditUsersInRole`** take the existing "not found" path instead of throwing `ArgumentNullException`.  
  
### 🧪 Verification performed  
- ✅ `dotnet build PetShopWeb.sln` (Debug) — 0 errors, 0 warnings  
- ✅ `dotnet build -c Release` — 0 errors, 0 warnings  
- ✅ `dotnet publish -c Release` — succeeds  
- ✅ `dotnet list package --vulnerable --include-transitive` — clean  
- ✅ Re-verified from a **deleted `obj/` and `bin/`** (cold build, no stale artifacts)  
- ⚪ `dotnet test` — **not run: no test project exists** (baseline collected 0 test reports / 0 test cases, so there is no before/after comparison either)  
  
---  
  
## 5. ⏱️ Time Savings Estimate  
  
| Workstream | 👨‍💻 Manual estimate | 🤖 Automated |  
| --- | --- | --- |  
| 🎯 TFM retarget + package-band alignment | 0.5 – 1 h | seconds |  
| 🧯 Diagnose & clear the `MSB3374` handoff break | 0.5 – 1 h | 1 turn |  
| 🔍 Triage 23 analyzer/nullable warnings across 4 files | 2 – 3 h | ~2 min |  
| 🛡️ Root-cause 4 × `NU1901` to the scaffolding package | 0.5 – 1 h | ~1 min |  
| 🐳 Container port/non-root + Dockerfile correctness | 1 – 2 h | ~1 min |  
| ✅ Clean-rebuild verification + written change record | 1 h | ~1 min |  
| **Total** | **≈ 6 – 9 h (~1 engineer-day)** | **≈ 8 min wall-clock** |  
  
- ⚡ **Wall-clock:** 04:58:07 ➜ 05:05 UTC ≈ **7.9 minutes** end-to-end (Upgrade Assistant ~2 min + Claude CLI ~5.7 min).  
- 📉 **Effort reduction: ~98%** — roughly **one engineer-day saved** on this module, at **$2.44** of inference cost.  
- 🧭 **Honest framing:** this was a **Core-to-Core version bump**, not a Framework-to-Core rewrite. The knowledge base budgets **4–7 hours** for a full legacy migration; savings here are smaller in absolute terms because the app was already modern. The heavy lifting was warning remediation and deployment correctness, not re-platforming.  
- 🎁 **Uncounted value:** the `Microsoft.Build.Framework` validation bug and the broken Dockerfile `publish` were latent defects **not part of the upgrade brief** — a manual upgrade focused only on "make it compile on net10.0" would very likely have shipped both forward.  
- ⚠️ **Not included in the savings:** QA/validation time. With no test suite, manual regression testing (§6) is still required and is likely the largest remaining cost.  
  
---  
  
## 6. 🧭 Next Steps  
  
### 🔴 Required before production sign-off  
- 🧪 **Manual smoke test the auth surface** — the middleware-order change and the null-id guards touch login, logout, register, role CRUD and user-role assignment. Exercise each path signed-out, as a normal user, and as `admin@admin.com`.  
- ✅ **Verify the "Create role" form** now rejects an empty name (newly enforced validation) and that no caller depended on the old permissive behaviour.  
- 🐳 **Rebuild and run the container**, confirming the app answers on `localhost:3000` ➜ container `8080`. Any external ingress, reverse proxy, health check or Kubernetes manifest still pointing at **port 80 must be updated**.  
- 🔗 **Set `ConnectionStrings__DefaultConnection` in `docker-compose.yml`** — `appsettings.json` still defaults to a Windows-only `(localdb)` instance, so the containerised app depends entirely on the environment override.  
  
### 🟠 Strongly recommended  
- 🧪 **Add a test project (xUnit).** The highest-value targets: `ImageExtensionAttribute` validation matrix, `AccountController.EditUsersInRole` / `ManageUserRoles` role logic, and `CreateRoleViewModel` validation — the exact area that was silently broken. This also gives the *next* upgrade a regression net it did not have this time.  
- 💥 **Replace `ctx.Database.EnsureDeleted()` at startup.** `Program.cs` drops and recreates the database on **every process start**, destroying all users, roles and comments. Pre-existing behaviour, deliberately preserved — but unsafe outside a demo. Generate an initial EF Core migration and switch to `Database.Migrate()`, or at minimum guard it with `IsDevelopment()`.  
- 🧩 **Reconcile the seeding guard** — it checks for `UserName == "Admin"` but creates `admin@admin.com`, so it can never match an existing row.  
  
### 🟡 Opportunistic improvements  
- 📦 Consider .NET 10's `MapStaticAssets()` for build-time asset fingerprinting and compression (deferred: it changes asset URLs and requires every `<link>`/`<script>` to route through the `@Assets[...]` helper).  
- 🧹 Drop the Windows-only `IIS Express` profile from `launchSettings.json` if the team no longer develops on Windows.  
- 🧶 Tidy stale front-end manifests: `libman.json` lists `bootstrap-toggle@2.2.2` with no matching `wwwroot/lib/` directory, and `package.json` lists `jquery-validation-unobtrusive-bootstrap`, which no view references. Neither affects the .NET build.  
- 🔭 **Process improvement for the pipeline:** the Upgrade Assistant's `Msbuild was not found` failure meant it handed off an unverified, non-building project. Making MSBuild discoverable to the tool would let stage 1 validate its own output instead of relying on stage 2 to discover the break.  
  
---  
  
*📄 Full technical change record, with per-file rationale and seven logged follow-up items, is committed at `/data/code/summary.md`.*  

## Claude CLI usage

💳 Estimated Claude CLI usage (USD): 5.239071  
