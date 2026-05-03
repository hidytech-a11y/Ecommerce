using Ecommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace Ecommerce.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<Cart> Carts => Set<Cart>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<Discount> Discounts => Set<Discount>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.Entity<User>()
            .HasIndex(x => x.Email)
            .IsUnique();

        builder.Entity<Product>()
            .Property(p => p.RowVersion)
            .IsRowVersion();

        builder.Entity<Order>()
            .HasMany(o => o.Items)
            .WithOne()
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Discount>()
            .HasIndex(d => d.ProductId);

        builder.Entity<Product>()
        .HasIndex(p => p.Name);

        builder.Entity<Product>()
            .HasIndex(p => p.CategoryId);

        builder.Entity<Product>()
            .HasIndex(p => p.Price);

        base.OnModelCreating(builder);

        builder.Entity<Product>()
            .Property(p => p.Price)
            .HasPrecision(18, 2);

        builder.Entity<Product>()
            .Property(p => p.RowVersion)
            .IsRowVersion()
            .IsConcurrencyToken()
            .IsRequired(false);

        builder.Entity<Discount>()
            .Property(d => d.Value)
            .HasPrecision(18, 2);

        builder.Entity<Order>()
            .Property(o => o.TotalAmount)
            .HasPrecision(18, 2);

        builder.Entity<OrderItem>()
            .Property(o => o.OriginalPriceSnapshot)
            .HasPrecision(18, 2);

        builder.Entity<OrderItem>()
            .Property(o => o.FinalPriceSnapshot)
            .HasPrecision(18, 2);

        builder.Entity<Cart>(builder =>
        {
            builder.HasKey(c => c.Id);

            builder.Property(c => c.Id)
                   .ValueGeneratedNever();

            builder.HasIndex(c => c.UserId)
                   .IsUnique();

            builder.HasMany(c => c.Items)
                   .WithOne()
                   .HasForeignKey("CartId")
                   .OnDelete(DeleteBehavior.Cascade);

            builder.Navigation(c => c.Items)
                   .UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        builder.Entity<CartItem>(builder =>
        {
            builder.HasKey(i => i.Id);

            builder.Property(i => i.Id)
                   .ValueGeneratedNever();

            builder.HasIndex("CartId", nameof(CartItem.ProductId))
                   .IsUnique();
        });
    }
}
