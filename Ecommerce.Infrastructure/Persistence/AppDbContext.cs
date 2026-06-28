using Ecommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;

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
            .HasIndex(p => p.Name);

        builder.Entity<Product>()
            .HasIndex(p => p.CategoryId);

        builder.Entity<Product>()
            .HasIndex(p => p.Price);

        builder.Entity<Product>()
            .Property(p => p.Price)
            .HasPrecision(18, 2);

        builder.Entity<Product>()
            .Property<uint>("xmin")
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();

        builder.Entity<Product>(e =>
        {
            e.Property(p => p.FrontImageUrl).HasMaxLength(2048);
            e.Property(p => p.BackImageUrl).HasMaxLength(2048);
            e.Property(p => p.SideImageUrl).HasMaxLength(2048);
            e.Property(p => p.FrontImagePublicId).HasMaxLength(512);
            e.Property(p => p.BackImagePublicId).HasMaxLength(512);
            e.Property(p => p.SideImagePublicId).HasMaxLength(512);
        });

        builder.Entity<Product>()
            .Property(p => p.Slug)
            .HasMaxLength(250)
            .IsRequired();

        builder.Entity<Product>()
            .HasIndex(p => p.Slug)
            .IsUnique();

        builder.Entity<Order>()
            .HasMany(o => o.Items)
            .WithOne()
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Order>()
            .Property(o => o.TotalAmount)
            .HasPrecision(18, 2);

        builder.Entity<OrderItem>()
            .Property(o => o.OriginalPriceSnapshot)
            .HasPrecision(18, 2);

        builder.Entity<OrderItem>()
            .Property(o => o.FinalPriceSnapshot)
            .HasPrecision(18, 2);

        builder.Entity<Discount>()
            .HasIndex(d => d.ProductId);

        builder.Entity<Discount>()
            .Property(d => d.Value)
            .HasPrecision(18, 2);

        builder.Entity<Order>()
            .Property(o => o.CancellationReason)
            .HasMaxLength(500);

        builder.Entity<Cart>(b =>
        {
            b.HasKey(c => c.Id);

            b.Property(c => c.Id)
             .ValueGeneratedNever();

            b.HasIndex(c => c.UserId)
             .IsUnique();

            b.HasMany(c => c.Items)
             .WithOne()
             .HasForeignKey("CartId")
             .OnDelete(DeleteBehavior.Cascade);

            b.Navigation(c => c.Items)
             .UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        builder.Entity<CartItem>(b =>
        {
            b.HasKey(i => i.Id);

            b.Property(i => i.Id)
             .ValueGeneratedNever();

            b.HasIndex("CartId", nameof(CartItem.ProductId))
             .IsUnique();
        });

        base.OnModelCreating(builder);
    }
}