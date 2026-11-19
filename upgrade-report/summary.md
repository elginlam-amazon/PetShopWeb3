# 🚀 .NET 6.0 → .NET 10 Upgrade — Executive Report  
  
**Application:** PetShopWeb (ASP.NET Core MVC + EF Core + ASP.NET Core Identity)  
**Branch:** `ec2-jobrole-mwnxvull-ua-claude-upgrade` · **Baseline commit:** `ec96465`  
**Engine:** `DOTNET_UPGRADE_ASSISTANT_WITH_CLAUDE_CLI` · **Iteration:** 2 · **Date:** 2026-11-19  
  
---  
  
## 1. 📋 Executive Summary  
  
PetShopWeb has been upgraded from **.NET 6.0 (out of support since November 2024)** to **.NET 10**, and the upgrade is **complete and green**. Microsoft's .NET Upgrade Assistant performed the mechanical retargeting — target framework, solution metadata and five NuGet package references — then Claude CLI (Opus 5) resolved everything the Upgrade Assistant could not: it drove the build from **23 warnings to 0 errors and 0 warnings** in both Debug and Release, and modernised the container stack, which still pinned .NET 6 base images and would not have run the upgraded app at all.  
  
Because the application was already ASP.NET Core rather than legacy ASP.NET Framework, there was **no System.Web, Global.asax, MVC5, Web API 2, EF6, WCF or Web Forms surface to rewrite** — this was a runtime-and-dependency modernisation, not a re-architecture. That kept risk low: **14 files changed, 64 insertions, 131 deletions** — the upgrade removed roughly twice as much code as it added.  
  
Two latent defects were found and fixed as a direct side effect of the dependency cleanup: a **role-name validation attribute that was silently never running**, and a **reachable `NullReferenceException`** in the user/role administration POST handler. The headline business outcome is that the application moves from an **unsupported runtime receiving no security patches** onto the current LTS-track release, with **four known-vulnerability advisories (`NU1901`) eliminated** from the dependency graph.  
  
> ⚠️ **One pre-existing risk requires a human decision before production deployment** — `Program.cs` unconditionally deletes and recreates the database on every startup, in every environment. This behaviour predates the upgrade and was deliberately left intact. See §6.  
  
---  
  
## 2. 🔧 Application Changes  
  
### Framework & platform  
- ⬆️ **Target framework:** `net6.0` → `net10.0` (in-place upgrade, SDK-style project retained)  
- ✅ **Eliminated `NETSDK1138`** — the baseline build warned 3× that `net6.0` is out of support and will receive no future security updates  
- 🧩 **Solution metadata:** project type GUID updated to the SDK-style GUID (`{FAE04EC0-…}` → `{9A19103F-…}`) in `PetShopWeb.sln`  
  
### NuGet dependencies — all realigned to the .NET 10 runtime major  
| Package | Before | After |  
| --- | --- | --- |  
| `Microsoft.AspNetCore.Identity.EntityFrameworkCore` | 6.0.11 | **10.0.15** |  
| `Microsoft.EntityFrameworkCore.SqlServer` | 7.0.0 | **10.0.15** |  
| `Microsoft.EntityFrameworkCore.Proxies` | 7.0.0 | **10.0.15** |  
| `Microsoft.EntityFrameworkCore.Tools` | 6.0.11 | **10.0.15** |  
| `Microsoft.VisualStudio.Web.CodeGeneration.Design` | 6.0.10 | **removed** 🗑️ |  
  
- 🔍 **Note for the team:** the baseline had *mixed* package majors (Identity on 6.0.11, EF Core on 7.0.0) against a `net6.0` target — a pre-existing version drift. All framework-tied packages are now on a single, consistent `10.0.x` line.  
- 🔒 **`Microsoft.VisualStudio.Web.CodeGeneration.Design` removed** — it was the **sole source of all four `NU1901` vulnerability advisories**, pulling `NuGet.Packaging` / `NuGet.Protocol` 6.12.1 (GHSA-g4vj-cjjj-v7hg) transitively. It is a design-time scaffolding tool (`dotnet aspnet-codegenerator`) with no build or runtime role in this app. No version in the graph offered a non-vulnerable `NuGet.*`, so removal was the only clean fix (KB 27, Step 3: remove over silence). `Microsoft.EntityFrameworkCore.Tools` is retained for `dotnet ef`.  
  
### 🐳 Containerization (previously broken for .NET 10)  
- ⬆️ `Dockerfile`: `sdk:6.0` / `aspnet:6.0` → **`sdk:10.0` / `aspnet:10.0`** — the old images physically cannot build or host a `net10.0` assembly  
- 🔌 **Port moved 80 → 8080** with an explicit `ENV ASPNETCORE_URLS=http://+:8080`; .NET 8 and later default the container port to 8080, so the previous `EXPOSE 80` no longer matched the listening port  
- 🐞 **Fixed a latent build-stage break:** the Dockerfile copied `PetShopWeb/*.csproj` into the working-directory root and then layered `COPY . .` on top, leaving a loose `.csproj` *and* `PetShopWeb.sln` in one directory — `dotnet publish` with no argument is ambiguous there (`MSB1011`). `restore` and `publish` now name the project explicitly.  
- 🔁 `docker-compose.yml`: remapped `3000:80` → `3000:8080`; dropped the dead `3001:433` mapping (`433` was a typo for `443`, and no HTTPS endpoint is configured) and the obsolete top-level `version:` key that Compose v2 warns about  
  
### 🧹 Project file hygiene  
- 🗑️ Removed **44 dead `<Content Remove="C:\Users\PC\.nuget\packages\bootstrap\5.2.2\…">` entries** — absolute Windows developer paths left over from a `bootstrap` NuGet reference that no longer exists (client libraries actually come from `libman.json` into `wwwroot/lib`). They could never match an item on any machine, least of all Linux.  
- 🗑️ Removed a Windows-only `<Folder Include="wwwroot\photos\" />` VS empty-folder hint (the folder has content)  
- 📉 Net effect: the `.csproj` shrank from 66 lines to 16  
  
---  
  
## 3. 🛠️ Tools Used  
  
| Tool | Version | Role & outcome |  
| --- | --- | --- |  
| 🟦 **.NET SDK** | **10.0.404** (6.0.428 / 7.0.410 / 8.0.428 / 9.0.321 also present) | Build, restore and verification toolchain for all gates |  
| 🧰 **Microsoft .NET Upgrade Assistant** | **1.0.518.26217** | Mechanical retargeting, `framework.inplace` mode → **7 succeeded, 0 failed, 349 skipped** |  
| 🤖 **Claude CLI (Claude Code)** | **2.1.266** | Autonomous build-fix, warning remediation, containerization and reporting |  
| 🧠 **Model** | **Claude Opus 5** (`global.anthropic.claude-opus-5`, via Bedrock) | 51 turns · ~6.4 min wall-clock · 27.5K output tokens (10.9K reasoning) · **$2.56** |  
| 📚 **Migration knowledge base** | 36 domain documents | Consulted KB `README` (error-code index), `27` (build integrity / `NU` advisories), `19` (Linux containerization) |  
  
### 🔍 Division of labour — what each tool actually contributed  
- 🧰 **Upgrade Assistant handled the mechanical layer:** TFM bump, solution GUID, and the five package-reference upgrades. Its code transformers (`DefaultTypeMapTransformer`, `DefaultNamespaceMapTransformer`, `DefaultAttributeTypeMapTransformer`, …) ran across **every `.cs` and `.cshtml` file and reported `Skipped` on all of them** — correctly, since there were no legacy API mappings to apply.  
- ⚠️ **Upgrade Assistant could not verify its own work:** it logged **`Msbuild was not found.`** twice and skipped both of its build-validation steps, so it exited "Succeeded" having never compiled the retargeted project. Every build guarantee in this report comes from the subsequent `dotnet build` runs, not from the Upgrade Assistant.  
- 🤖 **Claude CLI covered everything mechanical tooling cannot:** diagnosing an environment fault, triaging 23 warnings into deliberate code decisions, recognising a mis-bound attribute as a silent validation bug, and updating deployment assets the Upgrade Assistant does not touch at all.  
  
---  
  
## 4. 💻 Code Changes  
  
**14 files changed · 64 insertions · 131 deletions**  
  
### 🧯 Environment fault cleared first  
- The first post-retarget build failed with **`MSB3374` — access denied writing `PetShopWeb.MvcApplicationPartsAssemblyInfo.cache`**. Root cause: the pre-upgrade baseline build left `obj/` and `bin/` artifacts owned by `root`, which the non-root build user could not re-timestamp. Cleared the stale output; the retargeted project then compiled, revealing the real 23-warning backlog.  
  
### 🐛 Latent defect #1 — a validation attribute that was never validating  
- `ViewModels/CreateRoleViewModel.cs` imported **`Microsoft.Build.Framework`**, which leaked into the compile set via the scaffolding package. `[Required]` therefore bound to `Microsoft.Build.Framework.RequiredAttribute` — an **MSBuild task-parameter marker, not a validation attribute** — so `RoleName` was **silently never validated**, despite `CreateRole.cshtml` faithfully rendering `asp-validation-for="RoleName"`.  
- ✅ Corrected to `System.ComponentModel.DataAnnotations`. **Behavioural change:** a blank role name is now rejected by model validation with an inline field error, instead of falling through to `RoleManager.CreateAsync` and surfacing as an identity error. This is what the view and the attribute plainly intended.  
  
### 🐛 Latent defect #2 — reachable NullReferenceException  
- `AccountController.EditUsersInRole` (POST) passed the result of `FindByIdAsync(model[i].UserId)` straight into `IsInRoleAsync` with no null check. A posted `UserId` that no longer resolves — e.g. the user was deleted between the GET and the POST — **dereferenced null and threw**.  
- ✅ Added a `user is null` → `continue` guard; `IdentityResult? result` tightened to `IdentityResult` since every path now assigns.  
  
### 🛡️ Nullability remediation — 18 warnings (`CS8604` / `CS8600` / `CS8620`)  
The project has `<Nullable>enable</Nullable>`, but every view-model string was declared `string?` and then handed directly to Identity APIs that require non-null. Rather than scatter `!` suppressions (which hide the problem without fixing it), the **types were corrected at the source**:  
- 📝 Required form fields are now non-nullable with `= string.Empty` initialisers across **7 view models** — `LoginViewModel`, `RegisterViewModel`, `CreateRoleViewModel`, `EditRoleViewModel`, `EditUserViewModel`, `UserRoleViewModel`, `UserRolesViewModel`  
- 📦 `EditRoleViewModel.Users` and `EditUserViewModel.Claims` / `.Roles` became non-nullable `List<string>` with inline initialisers, replacing constructors that did exactly the same thing  
- ✅ **Validation behaviour is unchanged** — these properties already carried `[Required]`, which rejects the empty string by default, so an omitted field fails exactly as it did when it bound to `null`  
- 🔐 In `AccountController`, Identity's genuinely-nullable `IdentityUser.UserName` / `.Email` and `IdentityRole.Name` got real guards: `role is null` checks widened to `role?.Name is null` (a nameless role is unusable by `IsInRoleAsync`, which would have thrown `ArgumentNullException`, and now takes the existing not-found path); nameless roles are skipped in `ManageUserRoles`; nullable names projected onto view models are coalesced to `string.Empty`, matching the previous rendering of null as an empty field  
  
### 🧵 `Program.cs` modernisation  
- 🔀 **`ASP0014`:** `app.UseEndpoints(e => e.MapControllerRoute(…))` → top-level **`app.MapControllerRoute(…)`**  
- 📐 Reordered to the canonical ASP.NET Core pipeline: `UseStaticFiles` → **`UseRouting`** → **`UseAuthentication`** → `UseAuthorization` → endpoints. The previous order placed authentication *before* routing and worked only because auth did not need endpoint metadata.  
- 🚨 **`CS8600`:** `Configuration["ConnectionStrings:DefaultConnection"]` → `GetConnectionString("DefaultConnection")` with a fail-fast `?? throw new InvalidOperationException(…)`. A missing connection string previously produced a null that surfaced much later as an opaque `SqlException` at first database access; it now fails at startup with a named, actionable message.  
- 🏷️ **`CS8604`:** introduced `const string adminRoleName = "admin"`, used for both `new IdentityRole(…)` and `AddToRoleAsync(…)`, instead of dereferencing the nullable `adminRole.Name`  
  
### ✂️ `Attributes/ImageExtensionAttribute.cs`  
- **`CS8600`:** collapsed an `if/else` that assigned `value as string` (a `string?`) into a non-nullable `string` local, into a single `string? path = …` conditional. The existing `path is not null` check already handled the null case — logic is byte-for-byte equivalent.  
  
### ✅ Verification gates — all green  
| Gate | Result |  
| --- | --- |  
| `dotnet build PetShopWeb/PetShopWeb.csproj` (Debug) | ✅ **0 errors, 0 warnings** |  
| `dotnet build PetShopWeb.sln -c Release` | ✅ **0 errors, 0 warnings** |  
| `dotnet restore PetShopWeb.sln` | ✅ no `NU****` / `NETSDK****` warnings |  
| `dotnet test PetShopWeb.sln` | ⚪ no-op, exit 0 — **no test projects exist** (identical to recorded baseline: 0 test cases from 0 report files) |  
| Razor view compilation | ✅ all 23 `.cshtml` views compiled clean as part of the build |  
| Legacy-API sweep | ✅ zero hits for `System.Web.*`, `System.Data.Entity`, `System.Web.Http.*`, `[Index]`, `[AllowHtml]`, `PreApplicationStartMethod`, `HtmlTextWriter`, `IRegisteredObject`, `CacheDependency` |  
  
> 🧪 **No test was deleted, skipped, disabled or weakened** — the solution has never contained a test project. Build and warning-count evidence is the only automated verification available here, which is precisely why §6 treats manual functional validation as mandatory.  
  
---  
  
## 5. ⏱️ Time Savings Estimate  
  
### Manual effort this upgrade would have required  
| Workstream | Est. manual effort | Basis |  
| --- | --- | --- |  
| 🧰 Retarget + realign 5 packages across mixed 6.0/7.0 majors | 1.0 – 2.0 h | Version-matrix research, restore iteration |  
| 🔍 Triage 23 warnings across 10 files | 3.0 – 4.0 h | Read each diagnostic, decide fix vs. suppress |  
| 🛡️ Nullability design across 7 view models + controller | 2.0 – 3.0 h | Requires a consistent policy decision, not a mechanical edit |  
| 🔒 Trace `NU1901` to its transitive root & decide removal | 1.0 – 1.5 h | Dependency-graph walk + impact assessment |  
| 🐛 Discover the `Microsoft.Build.Framework` validation bug | 2.0 – 8.0 h ⚡ | **Realistically found by accident or never** — the code compiled and looked correct |  
| 🐳 Dockerfile / Compose .NET 10 + port-default migration | 1.0 – 1.5 h | Requires knowing the 8080 default changed in .NET 8 |  
| 🧯 Diagnose `MSB3374` permissions fault | 0.5 h | Misleading error; easy to misread as a project issue |  
| 📄 Write up changes + reviewer notes | 1.0 – 1.5 h | Equivalent of this report |  
| **Total** | **~11.5 – 22 h (1.5 – 3 engineer-days)** | |  
  
### Actual automated effort  
- 🧰 Upgrade Assistant: **< 1 minute**  
- 🤖 Claude CLI: **~6.4 minutes** wall-clock, 51 turns, **$2.56**  
- ⏲️ **Total machine time: well under 10 minutes**  
  
### 📊 Net assessment  
- 💰 **Estimated saving: ~1.5 – 3 engineer-days (≈ 11 – 22 hours) of hands-on work, for ~10 minutes of machine time and $2.56.**  
- 🎯 Highest-leverage gains were **not** the mechanical retargeting (tooling has long handled that) but the **judgement-heavy work**: a coherent nullability policy, correctly attributing four security advisories to one removable design-time package, and spotting a mis-bound attribute that no compiler error or test would ever have surfaced.  
- ⚖️ **Honest caveats on this figure:**  
  - 📏 Estimates are derived from change scope and diagnostic count, **not measured against a control engineer** — treat them as an order-of-magnitude indication, not a metric.  
  - 🚫 **Review, functional validation and deployment time are NOT saved.** With zero automated tests, §6 is unavoidable human work and is excluded from the figures above.  
  - 🏠 This was a *favourable* codebase — already ASP.NET Core, single project, no legacy web stack. A .NET Framework → .NET 10 migration of comparable size would be multiples of this effort for both human and machine.  
  
---  
  
## 6. 🧭 Next Steps  
  
### 🔴 Blocking before production deployment  
1. 🚨 **`Program.cs` destroys and recreates the database on every startup.** `ctx.Database.EnsureDeleted()` followed by `EnsureCreated()` runs **unconditionally, in every environment including Production** — every user, role and animal record is lost on each restart. **Pre-existing behaviour, deliberately left intact** because changing it is a product decision, not an upgrade step. ➡️ Recommended: gate seeding behind `app.Environment.IsDevelopment()` and adopt EF Core migrations (`dotnet ef migrations add`, `Database.Migrate()`) — `Microsoft.EntityFrameworkCore.Tools` is still referenced for exactly this. Note there is currently **no `Migrations/` folder**, so the model has no migration history to build on.  
2. 🔑 **Hard-coded admin password `"Admin123"` in `Program.cs`.** Move to user-secrets (dev) and an environment variable or secret store (test/prod). Not changed because it alters startup requirements.  
3. 🗄️ **Connection string points at a developer LocalDB instance** (`Server=(localdb)\AspNetCoreCourse`) that cannot resolve on Linux or in a container. The commented-out line above it is the container form and **contains a plaintext `sa` password that should not be committed.** ➡️ Supply per environment via `ConnectionStrings__DefaultConnection`, as the compose file already implies.  
  
### 🧪 Validation steps (mandatory — there is no automated test coverage)  
4. ✋ **Manually exercise every changed path**, since the view-model nullability changes touch all authentication and administration forms: register · login · logout · create role · edit role · delete role · edit user · delete user · assign/remove users in role · manage user roles · add animal (image-extension validation) · edit animal · add comment.  
5. ✅ **Confirm the two intended behavioural changes:** (a) creating a role with a **blank name** now shows an inline validation error rather than an identity error; (b) submitting `EditUsersInRole` for a **user deleted mid-session** now skips that row instead of throwing.  
6. 🐳 **Rebuild and smoke-test the container** from the updated `Dockerfile`, verifying the app answers on **port 8080** and that `ConnectionStrings__DefaultConnection` override reaches EF Core.  
7. 🔁 **`docker-compose.yml` still references prebuilt images** (`yarins2000/petshopapp:1.0`, `yarins2000/petshopdb:1.0`) rather than building from this tree, so it **will not pick up this upgrade until those images are rebuilt and republished.** The 8080 remap assumes the republished image is the .NET 10 build. ➡️ Consider switching the `app` service to `build: .`.  
  
### 🟡 Recommended improvements (non-blocking)  
8. 🧪 **Introduce a test project** — the single largest gap. `dotnet test` is a no-op and the baseline recorded 0 test cases, so this upgrade has **no automated regression safety net**. Highest-value first tests: `CreateRoleViewModel` validation (the bug fixed in §4), the `EditUsersInRole` deleted-user guard, and `ImageExtensionAttribute` across `IFormFile`, `string` and `null` inputs.  
9. 🐞 **Admin-seed guard checks the wrong value.** `ctx.Users.Any(u => u.UserName == "Admin")` can never match — the user created two lines below is `"admin@admin.com"`. Harmless **only** because item 1 wipes the database first; it becomes a duplicate-user error the moment `EnsureDeleted` is removed. The `CreateAsync` result is also assigned and never inspected, so a failed admin creation is silent and the following `AddToRoleAsync` runs against an unsaved user. Needs a decision on the intended seed identity.  
10. 🔇 **`EditUsersInRole` (POST) ignores failed `IdentityResult`s** — when `Succeeded` is false the loop falls through and still redirects to `EditRole`, reporting failure to the user as success. The nullability fix did not alter this control flow. Needs a decision on the desired error surface.  
11. 📦 **`Microsoft.EntityFrameworkCore.Tools` ships EF Design plus its MSBuild/Roslyn dependencies into the publish output** (~15 extra assemblies, including `Microsoft.Build.Framework` and `Microsoft.CodeAnalysis.*`). This is the official template reference block and causes no warnings, but it enlarges the runtime image and its attack surface. If `dotnet ef` is not needed from the published app, move it to a dedicated tooling project.  
12. 🗂️ **`package.json` is present but unused** — client libraries are restored through `libman.json`, and the two manifests list overlapping-but-divergent versions (`jquery-validation-unobtrusive-bootstrap` appears only in `package.json`). Remove one to prevent drift.  
13. 🪟 **`launchSettings.json` retains an `IIS Express` profile and `iisSettings`** — harmless and Windows-only, but dead weight for Linux/container development.  
14. 📈 **Consider adopting .NET 10 opt-in features** now that the runtime allows them: `MapStaticAssets` for fingerprinted static assets (deferred here because `UseStaticFiles` works, raises no warnings, and DB-stored image paths were lower-risk left alone), and structured logging/health-check endpoints for container orchestration.  

## Claude CLI usage

💳 Estimated Claude CLI usage (USD): 5.090898  
