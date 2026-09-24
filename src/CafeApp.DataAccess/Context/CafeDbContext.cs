using CafeApp.DataAccess.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CafeApp.DataAccess.Contexts;

public class CafeDbContext : IdentityDbContext<AppUser, AppRole, Guid>
{
    public CafeDbContext(DbContextOptions<CafeDbContext> options) : base(options)
    {
    }

    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<RestaurantTable> Tables => Set<RestaurantTable>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<DailyZReport> DailyZReports => Set<DailyZReport>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Category>().HasQueryFilter(c => !c.IsDeleted);
        builder.Entity<Product>().HasQueryFilter(p => !p.IsDeleted);
        builder.Entity<RestaurantTable>().HasQueryFilter(t => !t.IsDeleted);
        builder.Entity<Order>().HasQueryFilter(o => !o.IsDeleted);
        builder.Entity<OrderItem>().HasQueryFilter(oi => !oi.IsDeleted);
        builder.Entity<DailyZReport>().HasQueryFilter(z => !z.IsDeleted);

        builder.Entity<RestaurantTable>()
            .HasIndex(t => t.QrToken)
            .IsUnique();

        builder.Entity<Product>().Property(p => p.UnitPrice).HasPrecision(18, 2);
        builder.Entity<Product>().Property(p => p.CostPrice).HasPrecision(18, 2);
        builder.Entity<Order>().Property(o => o.TotalAmount).HasPrecision(18, 2);
        builder.Entity<Order>().Property(o => o.TotalCost).HasPrecision(18, 2);
        builder.Entity<OrderItem>().Property(oi => oi.UnitPrice).HasPrecision(18, 2);
        builder.Entity<OrderItem>().Property(oi => oi.CostPrice).HasPrecision(18, 2);
        builder.Entity<DailyZReport>().Property(z => z.TotalRevenue).HasPrecision(18, 2);
        builder.Entity<DailyZReport>().Property(z => z.TotalCost).HasPrecision(18, 2);
        builder.Entity<DailyZReport>().Property(z => z.NetProfit).HasPrecision(18, 2);
    }
}