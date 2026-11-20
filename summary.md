# .NET 6.0 → .NET 10.0 transformation — PetShopWeb

Date: 2026-11-20
Project: `./PetShopWeb/PetShopWeb.csproj`

## Result

| Check | Outcome |
| --- | --- |
| `dotnet build PetShopWeb.sln` (Debug) | **0 errors, 0 warnings** |
| `dotnet build PetShopWeb/PetShopWeb.csproj -c Release` | **0 errors, 0 warnings** |
| `dotnet publish -c Release` | succeeds |
| `dotnet list package --vulnerable --include-transitive` | no vulnerable packages |
| Unit tests | none exist in the solution (see Next steps) |

The app was already ASP.NET Core (SDK-style project, `Program.cs`, EF Core, ASP.NET Core
Identity), so no `System.Web`, `System.Data.Entity`, `System.Web.Http`, `Global.asax`,
`Web.config`, WCF, Web Forms, System.Drawing or legacy test-stack migration applied. The
pre-build cleanup sweep (KB 00) found no `[Index]`, `[AllowHtml]`,
`[assembly: PreApplicationStartMethod]`, `CacheDependency`, `IRegisteredObject` or
`HtmlTextWriter` usage, and no `System.Web.*` / `System.Data.Entity` references anywhere in
the source tree.

## Changes

### Target framework and project file (`PetShopWeb/PetShopWeb.csproj`)

- `TargetFramework` `net6.0` → `net10.0`.
- Framework-tied packages aligned to the `10.0.x` band (KB README "Target framework"):
  `Microsoft.AspNetCore.Identity.EntityFrameworkCore`, `Microsoft.EntityFrameworkCore.Proxies`,
  `Microsoft.EntityFrameworkCore.SqlServer`, `Microsoft.EntityFrameworkCore.Tools` → `10.0.15`.
  Previously these were split across 6.0.11 / 7.0.0, i.e. EF Core 7 runtime with EF Core 6
  tooling and Identity stores.
- Removed `Microsoft.VisualStudio.Web.CodeGeneration.Design`. It is a design-time scaffolding
  package (`dotnet aspnet-codegenerator`), is not needed to build or run the app, and was the
  only source of the four `NU1901` advisories (transitive `NuGet.Packaging` /
  `NuGet.Protocol` 6.12.1 — GHSA-g4vj-cjjj-v7hg). Per KB 27 Step 3 the advisory is cleared by
  migrating off the package rather than pinning it.
- Removed 44 dead `<Content Remove="C:\Users\PC\.nuget\packages\bootstrap\5.2.2\...">` items.
  They pointed at a Windows developer's local NuGet cache (absolute paths that cannot resolve
  on any other machine, and on Linux in particular) for a `bootstrap` package the project no
  longer references. Bootstrap is vendored under `wwwroot/lib/bootstrap`, so there is no
  duplicate-Content (`NETSDK1022`) risk from removing them.
- Removed the `<Folder Include="wwwroot\photos\" />` placeholder — the folder has content and
  is picked up by the Web SDK.

### Solution file (`PetShopWeb.sln`)

- Project type GUID updated from the legacy C# GUID `{FAE04EC0-…}` to the SDK-style C# GUID
  `{9A19103F-…}` (KB 01).

### `Program.cs`

- `app.UseEndpoints(endpoints => endpoints.MapControllerRoute(…))` → top-level
  `app.MapControllerRoute("default", "{controller=Home}/{action=HomePage}/{id?}")`. This clears
  `ASP0014` and is the supported form on .NET 10. The route template is unchanged.
- Middleware order corrected to `UseStaticFiles` → `UseRouting` → `UseAuthentication` →
  `UseAuthorization` (it was `UseStaticFiles` → `UseAuthentication` → `UseRouting` →
  `UseAuthorization`). **Behavioural note:** with endpoint routing, authentication must run
  after `UseRouting` for endpoint-selected authentication/authorization metadata to be honoured.
  Cookie sign-in behaviour for this app is unchanged; the fix makes per-endpoint
  `[Authorize]` evaluation correct rather than order-dependent.
- Connection string read through `Configuration.GetConnectionString("DefaultConnection")` with
  an explicit `InvalidOperationException` when absent, instead of the raw indexer assigned to a
  non-nullable `string` (cleared `CS8600`). The configuration key is the same, so the Docker
  `ConnectionStrings__DefaultConnection` environment-variable override (KB 19 Step 4) still works.
- Admin role name hoisted into a `const string adminRoleName = "admin"` and used for both
  `roleManager.CreateAsync` and `userManager.AddToRoleAsync`, replacing the nullable
  `adminRole.Name` (cleared `CS8604`). Seeded role/user values are unchanged.
- Dropped the unused `var result =` capture on the admin-user `CreateAsync` call.

### `Controllers/AccountController.cs`

Nullable-reference warnings (`CS8604`, `CS8620`) resolved without changing any control flow,
except where the old code would have thrown on a null id:

- `Login` / `Register`: `loginmodel.Email!`, `loginmodel.Password!`, `registermodel.Password!` —
  these are inside `if (ModelState.IsValid)` and the properties carry `[Required]`.
- `EditUser` (POST) and `EditRole` (POST): `model.Id is null ? null : await FindByIdAsync(model.Id)`.
  A post without an id now takes the existing "not found" path (`NotFound()` /
  `View(nameof(RolesManager))`) instead of throwing `ArgumentNullException`.
- `EditUsersInRole` (POST): entries whose `UserId` is null, or whose user no longer exists, are
  skipped (`continue`) instead of dereferencing null; `IdentityResult? result` tightened to
  `IdentityResult` since every assigned branch now returns a value. Also removed a stray blank
  line inside the `if`/`else if` chain.
- `IdentityRole.Name` / `IdentityUser.UserName` dereferences marked `!` (persisted Identity rows
  always carry these) in `EditRole`, `EditUsersInRole`, `ManageUserRoles`.
- `AddToRolesAsync(..., Select(u => u.RoleName!))` for the `IEnumerable<string?>` → `IEnumerable<string>`
  variance warning.
- Removed the unused `using System.Data;`.

### `ViewModels/CreateRoleViewModel.cs` — functional bug fix

`RoleName` was annotated with `[Required]` resolved from **`Microsoft.Build.Framework`**
(MSBuild's task-parameter attribute), not `System.ComponentModel.DataAnnotations`. That
attribute is invisible to MVC model validation, so `RoleName` was never actually validated and
the "Create role" form accepted an empty name. The `using` now points at
`System.ComponentModel.DataAnnotations`, which also removes the last compile-time dependency on
the scaffolding package that was dropped above. **Behavioural note:** empty role names are now
rejected by `ModelState.IsValid` and the form redisplays with a validation message.

### `Attributes/ImageExtensionAttribute.cs`

- `string path` → `string? path` so `value as string` no longer trips `CS8600`; the `is not null`
  guard below it already handled the null case.
- `ToLower()` → `ToLowerInvariant()` for the extension comparison, so file-extension validation
  does not depend on the container's current culture (relevant now that the app runs on Linux).

### Container and docs

- `Dockerfile`: `sdk:6.0` / `aspnet:6.0` → `sdk:10.0` / `aspnet:10.0` (KB 19 Step 2); added
  `ENV ASPNETCORE_URLS=http://+:8080` and `EXPOSE 8080`, because .NET 8+ container images run as
  a non-root user and no longer listen on port 80 by default. Removed the `EXPOSE 433`
  (a typo for 443, with no certificate configured in the image).
  Restore/publish now name the project explicitly (`dotnet restore PetShopWeb/PetShopWeb.csproj`,
  `dotnet publish PetShopWeb/PetShopWeb.csproj`) and the csproj is staged into a `PetShopWeb/`
  subdirectory. The previous version copied the csproj to the build root and then ran a bare
  `dotnet publish`, which after `COPY . .` left `PetShopWeb.sln`, `PetShopWeb/PetShopWeb.csproj`
  and the stray root copy all visible to MSBuild.
- `docker-compose.yml`: app port mapping `3000:80` → `3000:8080` to match the new container
  port; dropped the dead `3001:433` mapping and the obsolete `version:` key. `localhost:3000`
  in the README is unchanged.
- `README.md`: "ASP.NET Core 6 MVC web app" → "ASP.NET Core 10".

### Unused `using` cleanup (no behaviour change)

`Data/PetShopContext.cs` (`Microsoft.EntityFrameworkCore.Design`, `Microsoft.AspNetCore.Identity`),
`Repositories/AccountRepository/{AccountRepository,IAccountRepository}.cs`
(`System.Security.Claims`), `ViewModels/EditRoleViewModel.cs` (`System.Security`).

## Next steps

Non-blocking items for human review. None of these prevent the build.

1. **No test project exists.** The solution contains only `PetShopWeb`, so there is nothing to
   run `dotnet test` against and no regression net under the changes above. Nothing was deleted
   or skipped. Worth adding a test project (xUnit per KB 24) covering at minimum: the
   `ImageExtensionAttribute` validation matrix, `AccountController.EditUsersInRole` /
   `ManageUserRoles` role-assignment logic, and `CreateRoleViewModel` validation (which was
   silently broken before this pass — see above).
2. **`Program.cs` calls `ctx.Database.EnsureDeleted()` on every startup.** This drops and
   recreates the database, destroying all users, roles and comments on each process start. It
   is pre-existing behaviour and was deliberately preserved, but it is unsafe outside a demo:
   in dev/test/prod it should be replaced with EF Core migrations (`ctx.Database.Migrate()`), or
   at least guarded by `app.Environment.IsDevelopment()`. The project has no `Migrations/`
   folder, so moving to `Migrate()` requires generating an initial migration — a product
   decision, so it was left alone.
3. **`appsettings.json` default connection string is Windows-only**
   (`Server=(localdb)\AspNetCoreCourse`), with the container connection string sitting next to
   it as a JSON comment. On Linux/Docker the app depends entirely on the
   `ConnectionStrings__DefaultConnection` environment override, which `docker-compose.yml` does
   not currently set (the published `yarins2000/petshopapp:1.0` image presumably baked it in).
   Left as-is to avoid changing deployment configuration; recommend moving the localdb value to
   `appsettings.Development.json` and setting the real value per environment.
4. **Seeding quirk preserved:** the admin-user guard is `!ctx.Users.Any(u => u.UserName == "Admin")`
   while the user created is `admin@admin.com`, so the guard can never match an existing row.
   Harmless today because `EnsureDeleted()` empties the database first (item 2), but the two
   should be reconciled when seeding is reworked.
5. **`UseStaticFiles()` retained instead of .NET 10's `MapStaticAssets()`.** The newer API adds
   build-time fingerprinting and compression, but changes asset URLs and requires every
   `<link>`/`<script>` in the views to go through the `@Assets[...]` helper. Deferred as an
   opt-in optimisation rather than a migration requirement.
6. **`Properties/launchSettings.json` keeps the `IIS Express` profile.** It is dev-only tooling
   and inert on Linux; remove it if the team no longer develops on Windows.
7. **Front-end dependency manifests are stale but untouched:** `libman.json` lists
   `bootstrap-toggle@2.2.2` with no corresponding `wwwroot/lib/bootstrap-toggle/` directory, and
   `package.json` lists `jquery-validation-unobtrusive-bootstrap` which no view references. Client
   asset restore is outside the .NET build, so neither affects compilation.
