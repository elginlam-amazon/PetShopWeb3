using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using PetShopWeb.Data;

namespace PetShopWeb.Tests;

public class PetShopWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = Guid.NewGuid().ToString();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseContentRoot(GetApplicationContentRoot());

        builder.ConfigureServices(services =>
        {
            // EF Core 8+ registers the per-context options configuration as
            // IDbContextOptionsConfiguration<TContext>. Remove that (and the
            // legacy DbContextOptions<T> descriptor) so no SQL Server traces remain.
            var optionsConfigType = typeof(IDbContextOptionsConfiguration<PetShopContext>);
            foreach (var d in services.Where(d => d.ServiceType == optionsConfigType).ToList())
                services.Remove(d);

            var optionsDescriptor = services.SingleOrDefault(d =>
                d.ServiceType == typeof(DbContextOptions<PetShopContext>));
            if (optionsDescriptor is not null)
                services.Remove(optionsDescriptor);

            // Replace with InMemory provider using a unique database per factory instance.
            services.AddDbContext<PetShopContext>(options =>
                options.UseLazyLoadingProxies().UseInMemoryDatabase(_dbName));
        });
    }

    private static string GetApplicationContentRoot()
    {
        var testAssemblyDir = Path.GetDirectoryName(typeof(PetShopWebApplicationFactory).Assembly.Location)!;
        var dir = new DirectoryInfo(testAssemblyDir);
        while (dir != null && !dir.GetFiles("PetShopWeb.sln").Any())
            dir = dir.Parent;

        if (dir is null)
            throw new DirectoryNotFoundException("Could not locate PetShopWeb solution directory.");

        return Path.Combine(dir.FullName, "PetShopWeb");
    }
}
