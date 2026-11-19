# Migration Summary: .NET 6.0 → .NET 10.0

## Status
Build: **SUCCESS — 0 errors, 0 warnings**

## Changes Made

### Project File (`PetShopWeb/PetShopWeb.csproj`)
- Added explicit `NuGet.Packaging 6.13.1` and `NuGet.Protocol 6.13.1` package references to override vulnerable transitive dependencies (NU1901: GHSA-g4vj-cjjj-v7hg) pulled in by `Microsoft.VisualStudio.Web.CodeGeneration.Design`.

### `Program.cs`
- Fixed CS8600: Changed `string connectionString = builder.Configuration[...]` to use null-coalescing throw to properly handle nullable return value.
- Fixed CS8604: Added null-forgiving operator (`!`) to `adminRole.Name` in `AddToRoleAsync` call.
- Fixed ASP0014: Replaced `app.UseEndpoints(endpoints => { endpoints.MapControllerRoute(...) })` with top-level `app.MapControllerRoute(...)`.

### `Attributes/ImageExtensionAttribute.cs`
- Fixed CS8600: Changed `string path` to `string? path` to correctly represent the nullable result of `value as string`.

### `Controllers/AccountController.cs`
- Fixed CS8604 (multiple): Added null-forgiving operators (`!`) on `[Required]`-validated properties (`Email`, `Password`) in `Login` and `Register` actions where `ModelState.IsValid` guarantees non-null values.
- Fixed CS8604: Added `!` to `model.Id` in `EditUser` POST, and `editModel.Id` in `EditRole` POST.
- Fixed CS8604: Added `!` to `role.Name` and `user.UserName` throughout role management actions (`EditRole`, `EditUsersInRole`, `ManageUserRoles`).
- Fixed CS8604: Added `!` to `user` in `EditUsersInRole` POST loop where user lookup can theoretically return null.
- Fixed CS8620: Added `!` to `u.RoleName` in `AddToRolesAsync` LINQ projection to satisfy `IEnumerable<string>` parameter.

## Next Steps
None — the solution builds cleanly with zero errors and zero warnings.
