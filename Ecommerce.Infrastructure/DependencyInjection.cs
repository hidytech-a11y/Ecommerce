using Ecommerce.Application.Common.Interfaces;
using Ecommerce.Application.Common.Security;
using Ecommerce.Application.Interfaces;
using Ecommerce.Application.Services;
using Ecommerce.Infrastructure.Caching;
using Ecommerce.Infrastructure.Identity;
using Ecommerce.Infrastructure.Payments;
using Ecommerce.Infrastructure.Repositories;
using Ecommerce.Infrastructure.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace Ecommerce.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Security
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddScoped<IPasswordHasher, PasswordHasherAdapter>();

        // Repositories
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IOrderRepository, OrderRepository>();

        // Payment
        services.AddHttpClient<IPaystackClient, PaystackClient>();
        services.AddScoped<IPaymentService, PaymentService>();

        // Redis Connection
        services.AddSingleton<IConnectionMultiplexer>(sp =>
        {
            var connectionString = configuration["Redis:ConnectionString"];
            return ConnectionMultiplexer.Connect(connectionString);
        });

        // Cache Service
        services.AddScoped<ICacheService, RedisCacheService>();

        // Outbox pattern
        services.AddScoped<IOutboxRepository, OutboxRepository>();

        return services;
    }
}