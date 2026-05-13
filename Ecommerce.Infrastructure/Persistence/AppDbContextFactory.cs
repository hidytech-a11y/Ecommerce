using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Ecommerce.Infrastructure.Persistence;

public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        // Step 1 — Print current directory so we know exactly where EF is running from
        var currentDir = Directory.GetCurrentDirectory();
        Console.WriteLine($"[AppDbContextFactory] Current directory: {currentDir}");

        // Step 2 — Use absolute path directly (no guessing)
        // Point this EXACTLY to your Ecommerce.API folder
        var basePath = @"C:\Users\HIDYGRAFIX\source\repos\Ecommerce\Ecommerce.API";

        Console.WriteLine($"[AppDbContextFactory] Looking for appsettings.json in: {basePath}");

        // Step 3 — Confirm the file exists before trying to load it
        var appSettingsPath = Path.Combine(basePath, "appsettings.json");

        if (!File.Exists(appSettingsPath))
            throw new FileNotFoundException(
                $"appsettings.json not found at: {appSettingsPath}. " +
                "Please verify the file exists at that exact location.");

        // Step 4 — Build configuration
        var configuration = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
            .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: false)
            .AddEnvironmentVariables()
            .Build();

        // Step 5 — Get connection string
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        Console.WriteLine($"[AppDbContextFactory] Connection string found: {!string.IsNullOrWhiteSpace(connectionString)}");

        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException(
                $"Connection string 'DefaultConnection' is empty or missing in: {appSettingsPath}");

        // Step 6 — Build and return DbContext
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseSqlServer(connectionString);

        return new AppDbContext(optionsBuilder.Options);
    }
}