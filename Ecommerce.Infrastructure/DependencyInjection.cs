using Ecommerce.Application.Common.Interfaces;
using Ecommerce.Application.Common.Security;
using Ecommerce.Application.Interfaces;
using Ecommerce.Application.Services;
using Ecommerce.Infrastructure.Caching;
using Ecommerce.Infrastructure.Email;
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
        services.AddScoped<IDiscountRepository, DiscountRepository>();

        //Cart Service
        services.AddScoped<ICartRepository, CartRepository>();
        services.AddScoped<ICartService, CartService>();

        // AuthService
        services.AddScoped<IAuthService, AuthService>();

        // ProductService
        services.AddScoped<IProductService, ProductService>();

        // OrderService
        services.AddScoped<IOrderService, OrderService>();

        // Transaction Service
        //services.AddScoped<ITransaction, Transaction>();

        // Payment
        services.AddHttpClient<IPaystackClient, PaystackClient>();
        services.AddScoped<IPaymentService, PaymentService>();

        // Redis Connection
        //services.AddSingleton<IConnectionMultiplexer>(sp =>
        //{
        //    try
        //    {
        //        return ConnectionMultiplexer.Connect("localhost:6379,abortConnect=false");
        //    }
        //    catch
        //    {
        //        return null!;
        //    }
        //});

        //services.AddSingleton<IConnectionMultiplexer>(sp =>
        //{
        //    var connectionString = configuration["Redis:ConnectionString"];
        //    return ConnectionMultiplexer.Connect(connectionString);
        //});

        // Cache Service
        //services.AddScoped<ICacheService, RedisCacheService>();
        services.AddMemoryCache();
        services.AddScoped<ICacheService, MemoryCacheService>();

        // Outbox pattern
        services.AddScoped<IOutboxRepository, OutboxRepository>();

        //Email Service
        services.AddScoped<IEmailService, SmtpEmailService>();

        return services;
    }
}