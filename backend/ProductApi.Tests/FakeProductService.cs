using ProductApi.DTOs;
using ProductApi.Services;

namespace ProductApi.Tests;

public sealed class FakeProductService : IProductService
{
    private readonly Lock _lock = new();
    private readonly List<ProductResponse> _products = [];
    private long _nextId = 1;

    public Exception? Failure { get; set; }

    public void Reset()
    {
        lock (_lock)
        {
            _products.Clear();
            _nextId = 1;
            Failure = null;
        }
    }

    public void Seed(params ProductResponse[] products)
    {
        lock (_lock)
        {
            _products.AddRange(products);
            _nextId = products.Length == 0 ? 1 : products.Max(product => product.Id) + 1;
        }
    }

    public Task<IReadOnlyList<ProductResponse>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfConfigured();

        lock (_lock)
        {
            IReadOnlyList<ProductResponse> products = _products
                .OrderBy(product => product.Id)
                .ToList();

            return Task.FromResult(products);
        }
    }

    public Task<ProductResponse?> GetByIdAsync(
        long id,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfConfigured();

        lock (_lock)
        {
            var product = _products.SingleOrDefault(product => product.Id == id);
            return Task.FromResult(product);
        }
    }

    public Task<ProductResponse> CreateAsync(
        CreateProductRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfConfigured();

        lock (_lock)
        {
            var product = new ProductResponse(_nextId++, request.Name.Trim(), request.Price);
            _products.Add(product);

            return Task.FromResult(product);
        }
    }

    public Task<bool> UpdateAsync(
        long id,
        UpdateProductRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfConfigured();

        lock (_lock)
        {
            var index = _products.FindIndex(product => product.Id == id);

            if (index < 0)
            {
                return Task.FromResult(false);
            }

            _products[index] = new ProductResponse(id, request.Name.Trim(), request.Price);
            return Task.FromResult(true);
        }
    }

    public Task<bool> DeleteAsync(
        long id,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfConfigured();

        lock (_lock)
        {
            var removed = _products.RemoveAll(product => product.Id == id) == 1;
            return Task.FromResult(removed);
        }
    }

    private void ThrowIfConfigured()
    {
        if (Failure is not null)
        {
            throw Failure;
        }
    }
}
