using Microsoft.EntityFrameworkCore;
using ProductApi.Models;

namespace ProductApi.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        var product = modelBuilder.Entity<Product>();

        product.ToTable("PRODUCTS");

        product.HasKey(p => p.Id)
            .HasName("PK_PRODUCTS");

        product.Property(p => p.Id)
            .HasColumnName("ID")
            .ValueGeneratedOnAdd();

        product.Property(p => p.Name)
            .HasColumnName("NAME")
            .HasMaxLength(120)
            .IsUnicode(false)
            .IsRequired();

        product.Property(p => p.Price)
            .HasColumnName("PRICE")
            .HasPrecision(18, 2)
            .IsRequired();
    }
}
