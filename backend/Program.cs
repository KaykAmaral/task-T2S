using Microsoft.EntityFrameworkCore;
using ProductApi.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

var oracleConnectionString = builder.Configuration.GetConnectionString("Oracle");

if (string.IsNullOrWhiteSpace(oracleConnectionString))
{
    throw new InvalidOperationException(
        "Connection string 'Oracle' was not configured. " +
        "Set the ConnectionStrings__Oracle environment variable.");
}

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseOracle(oracleConnectionString));

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
