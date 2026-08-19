using Microsoft.EntityFrameworkCore;
using ProductApi.Data;
using ProductApi.DTOs;
using ProductApi.Models;

namespace ProductApi.Services;

public sealed class ProductService(AppDbContext context) : IProductService
{
    public async Task<IReadOnlyList<ProductResponse>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return await context.Products
            .AsNoTracking()
            .OrderBy(product => product.Id)
            .Select(product => new ProductResponse(product.Id, product.Name, product.Price))
            .ToListAsync(cancellationToken);
    }

    public async Task<ProductResponse?> GetByIdAsync(
        long id,
        CancellationToken cancellationToken = default)
    {
        return await context.Products
            .AsNoTracking()
            .Where(product => product.Id == id)
            .Select(product => new ProductResponse(product.Id, product.Name, product.Price))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<ProductResponse> CreateAsync(
        CreateProductRequest request,
        CancellationToken cancellationToken = default)
    {
        var product = new Product
        {
            Name = request.Name.Trim(),
            Price = request.Price
        };

        context.Products.Add(product);
        await context.SaveChangesAsync(cancellationToken);

        return ToResponse(product);
    }

    public async Task<bool> UpdateAsync(
        long id,
        UpdateProductRequest request,
        CancellationToken cancellationToken = default)
    {
        var product = await context.Products
            .SingleOrDefaultAsync(product => product.Id == id, cancellationToken);

        if (product is null)
        {
            return false;
        }

        product.Name = request.Name.Trim();
        product.Price = request.Price;

        await context.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<bool> DeleteAsync(
        long id,
        CancellationToken cancellationToken = default)
    {
        var product = await context.Products
            .SingleOrDefaultAsync(product => product.Id == id, cancellationToken);

        if (product is null)
        {
            return false;
        }

        context.Products.Remove(product);
        await context.SaveChangesAsync(cancellationToken);

        return true;
    }

    private static ProductResponse ToResponse(Product product)
    {
        return new ProductResponse(product.Id, product.Name, product.Price);
    }
}
