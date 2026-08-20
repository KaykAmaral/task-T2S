using Microsoft.EntityFrameworkCore;
using ProductApi.Data;
using ProductApi.Services;

const string FrontendCorsPolicy = "Frontend";

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddProblemDetails();

var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>();

if (allowedOrigins is null || allowedOrigins.Length == 0)
{
    throw new InvalidOperationException(
        "At least one CORS origin must be configured in 'Cors:AllowedOrigins'.");
}

builder.Services.AddCors(options =>
{
    options.AddPolicy(FrontendCorsPolicy, policy =>
    {
        policy.WithOrigins(allowedOrigins)
            .WithMethods("GET", "POST", "PUT", "DELETE")
            .AllowAnyHeader();
    });
});

var oracleConnectionString = builder.Configuration.GetConnectionString("Oracle");

if (string.IsNullOrWhiteSpace(oracleConnectionString))
{
    throw new InvalidOperationException(
        "Connection string 'Oracle' was not configured. " +
        "Set the ConnectionStrings__Oracle environment variable.");
}

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseOracle(oracleConnectionString));

builder.Services.AddScoped<IProductService, ProductService>();

var app = builder.Build();

// Configure the HTTP request pipeline.

if (builder.Configuration.GetValue<bool>("Database:ApplyMigrationsOnStartup"))
{
    await using var scope = app.Services.CreateAsyncScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await dbContext.Database.MigrateAsync();
}

app.UseExceptionHandler();
app.UseStatusCodePages();

app.UseHttpsRedirection();

app.UseCors(FrontendCorsPolicy);

app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program;
