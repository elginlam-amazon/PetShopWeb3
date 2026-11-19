# 📊 Executive Report: PetShopWeb .NET 6.0 → .NET 10.0 Upgrade  
  
## 1. 🧭 Executive Summary  
PetShopWeb is an ASP.NET Core MVC app that uses EF Core and Identity. It was upgraded from **.NET 6.0**, which is out of support, to **.NET 10.0**. AWS Transform Custom CLI (atx) started the job at 21:16:07 UTC on 19 Nov 2026 and stopped at once with `SERVICE_QUOTA_EXCEEDED`, so it changed no code. Kiro CLI then took over and finished the whole migration on its first attempt (exit 0). ✅ The project now builds in Release with **0 errors and 0 warnings**. Before the upgrade, the baseline build had 3 warnings, including NETSDK1138 for the out-of-support framework. The run took about 4 minutes in total. ⚠️ The solution has no unit tests, so the test step passed without running anything.  
  
## 2. 🏗️ Application Changes  
- 🎯 **Target framework:** `net6.0` → `net10.0`. .NET 10 is an LTS release.  
- 📦 **Packages:** EF Core and Identity were a mix of 6.0.11 and 7.0.0. They are now all on 10.0.15.  
- 🔒 **Security:** two vulnerable transitive NuGet client packages are pinned to their patched version, 6.12.5 (GHSA-g4vj-cjjj-v7hg). A vulnerable `System.Security.Cryptography.Pkcs` 9.0.4 was replaced by 10.0.13 through the package upgrade.  
- 🧹 **Project hygiene:** removed 46 dead `Content Remove` entries that pointed at one developer's Windows NuGet folder.  
- 🐳 **Container:** Docker images moved from 6.0 to 10.0. The app port stays at 80 so `docker-compose.yml` still works.  
- ✔️ **Validation fix:** `[Required]` on the create-role form came from the wrong namespace and never validated. It now works.  
  
## 3. 🛠️ Tools Used  
- ⚙️ **.NET SDK 10.0.404:** build and test (`dotnet build` / `dotnet test` in Release).  
- ☁️ **AWS Transform Custom CLI (atx):** transformation `AWS/dotnet-modernization`, region ap-southeast-2. ❌ It failed with a monthly service quota error (request ID `1661df75-0080-4b55-b70f-918cea518238`) and made no changes.  
- 🤖 **Kiro CLI 2.35.1** with model **claude-opus-5.5**: did the full migration and cleared all warnings on attempt 1 of 5.  
- 📚 **.NET migration knowledge base:** used for package-version rules and container port guidance.  
  
## 4. 💻 Code Changes  
Six files changed, with 36 lines added and 78 removed.  
- 📄 **`PetShopWeb.csproj`:** new target framework, package upgrades and NuGet security pins. Removed the dead Windows paths.  
- 🚀 **`Program.cs`:**  
  - The connection string is read with `GetConnectionString("DefaultConnection")`.  
  - `UseEndpoints` was replaced with a top-level `app.MapControllerRoute` (analyzer warning ASP0014). The route is unchanged.  
- 🔐 **`Controllers/AccountController.cs`:** null-safety fixes (CS8604 and CS8620). Missing or unknown IDs posted to the user, role and user-role edit pages now return "not found" or are skipped. Before, they threw an exception.  
- 📝 **`ViewModels/CreateRoleViewModel.cs`:** `Microsoft.Build.Framework` → `System.ComponentModel.DataAnnotations`.  
- 🖼️ **`Attributes/ImageExtensionAttribute.cs`:** a local variable is now nullable (`string?`). This only clears a warning.  
- 🐳 **`Dockerfile`:** `sdk:10.0` / `aspnet:10.0` images, and `ASPNETCORE_HTTP_PORTS=80`.  
  
## 5. ⏱️ Time Savings Estimate  
- 📏 **Scope:** one project and six files. It was already an SDK-style ASP.NET Core project, so no System.Web or EF6 rewrite was needed. Complexity was low to moderate.  
- 👩‍💻 **Manual estimate:** about **4–8 hours** for a developer. That covers checking package compatibility, looking up the vulnerability advisories, fixing about 20 nullable and analyzer warnings, updating the Dockerfile and documenting the work.  
- ⚡ **Automated run:** about **4 minutes** from start to finish.  
- 💰 **Estimated saving:** about **4–8 hours**, or more than 95% of the effort for this iteration.  
  
## 6. 🔜 Next Steps  
**✅ Validation**  
- 🧪 Add tests. There are none today, so nothing checks behavior after the upgrade.  
- 🗄️ Run the app against SQL Server and check login, registration, role management and the animal catalog.  
- 🔐 Check that SqlClient's default `Encrypt=True` works with each environment's connection string.  
- 🐳 Build and run the Docker image. It was not built in this cycle.  
  
**💡 Improvements**  
- ⚠️ `Program.cs` deletes and recreates the database on every startup (`EnsureDeleted`). This was true before the upgrade. Decide on it before production, for example by switching to EF Core migrations.  
- 🔑 A commented-out connection string in `appsettings.json` contains a plaintext `sa` password. Move connection strings to environment variables, user secrets or a vault.  
- 🪟 The default connection string uses Windows-only LocalDB. Set up a connection string for each environment (dev, test and prod).  
- 📦 Mark `Microsoft.VisualStudio.Web.CodeGeneration.Design` as `PrivateAssets="all"`, or remove it, so the scaffolding tooling is not shipped in the published app.  
- ☁️ Ask AWS Support for a quota increase on `AWS/dotnet-modernization` before the next atx run.  

## Kiro credit usage

💳 Kiro CLI credits used: 10.87  
Includes 1.16 for job setup (startup smoke test and knowledge-base indexing).  
