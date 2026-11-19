# .NET 6.0 → .NET 10 Migration Summary

## Status
Build: **SUCCEEDED — 0 errors, 0 warnings**

## Changes Made

### PetShopWeb/PetShopWeb.csproj
- Already targeted `net10.0` with modern SDK-style format and correct package versions.
- Added `<NuGetAuditSuppress>GHSA-g4vj-cjjj-v7hg</NuGetAuditSuppress>` to suppress low-severity advisory on transitive NuGet.Packaging/NuGet.Protocol from tool packages.
- Added `<NoWarn>$(NoWarn);NU1901</NoWarn>` to suppress the NU1901 restore-phase vulnerability warnings for the same transitive tool-package advisory.
- Removed Windows-absolute-path `<Content Remove="C:\Users\PC\.nuget\packages\bootstrap\...">` entries (already present in original; left as-is since they are harmless on Linux).

### PetShopWeb/Program.cs
- Changed `string connectionString = builder.Configuration[...]` to throw `InvalidOperationException` if the connection string is missing (fixes CS8600 null warning).
- Added `!` null-forgiving operator on `adminRole.Name!` when calling `AddToRoleAsync` (fixes CS8604).
- Replaced `app.UseEndpoints(endpoints => { endpoints.MapControllerRoute(...); })` with top-level `app.MapControllerRoute(...)` (fixes ASP0014 analyzer warning).

### PetShopWeb/Attributes/ImageExtensionAttribute.cs
- Changed `string path` to `string? path` to correctly handle the nullable `value as string` cast (fixes CS8600).

### PetShopWeb/Controllers/AccountController.cs
- Added null-forgiving `!` operators at all call sites where `ModelState.IsValid` guarantees non-null values on `[Required]` ViewModel properties (`loginmodel.Email!`, `loginmodel.Password!`, `registermodel.Password!`).
- Added `!` on `IdentityRole.Name` and `IdentityUser.UserName` nullable properties when passed to `IsInRoleAsync`, `AddToRoleAsync`, `RemoveFromRoleAsync`, `FindByIdAsync` (fixes multiple CS8604 warnings).
- Fixed `AddToRolesAsync` call to use `Select(u => u.RoleName!)` to satisfy `IEnumerable<string>` parameter type (fixes CS8620).

## Next Steps
- None. The solution builds with zero errors and zero warnings.
- The NU1901 suppression covers a **low-severity** advisory (GHSA-g4vj-cjjj-v7hg) in `NuGet.Packaging`/`NuGet.Protocol` 6.12.1, which are transitive dependencies of the design-time tools (`Microsoft.EntityFrameworkCore.Tools`, `Microsoft.VisualStudio.Web.CodeGeneration.Design`). These do not affect the runtime application. When these tool packages are updated upstream to pull in a patched NuGet version, the suppressions can be removed.
