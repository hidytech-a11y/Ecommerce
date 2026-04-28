using Ecommerce.Application.Common.Security;
using Ecommerce.Application.Interfaces;
using Ecommerce.Domain.Entities;
using Ecommerce.Domain.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Ecommerce.Infrastructure.Identity;

public static class AdminSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();

        var userRepository = scope.ServiceProvider
            .GetRequiredService<IUserRepository>();

        var passwordHasher = scope.ServiceProvider
            .GetRequiredService<IPasswordHasher>();

        var config = scope.ServiceProvider
            .GetRequiredService<IConfiguration>();

        var adminEmail = config["Admin:Email"];
        var adminPassword = config["Admin:Password"];

        var exists = await userRepository.ExistsByEmailAsync(adminEmail);

        if (exists) return;

        var hashedPassword = passwordHasher.Hash(adminPassword);

        var admin = new User(
            adminEmail,
            hashedPassword,
            UserRole.Admin);

        await userRepository.AddAsync(admin);
        await userRepository.SaveChangesAsync();
    }
}