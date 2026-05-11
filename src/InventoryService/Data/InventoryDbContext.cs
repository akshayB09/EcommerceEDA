using InventoryService.Models;
using Microsoft.EntityFrameworkCore;

namespace InventoryService.Data;

public class InventoryDbContext(DbContextOptions<InventoryDbContext> options) : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Seed some products so the demo works out of the box
        modelBuilder.Entity<Product>().HasData(
            new Product { Id = "PROD-1", Name = "Laptop", Stock = 10 },
            new Product { Id = "PROD-2", Name = "Mouse", Stock = 50 },
            new Product { Id = "PROD-3", Name = "Keyboard", Stock = 0 }  // intentionally out of stock
        );
    }
}
