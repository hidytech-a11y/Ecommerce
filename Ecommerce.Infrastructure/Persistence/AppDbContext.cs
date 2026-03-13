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
    }
}