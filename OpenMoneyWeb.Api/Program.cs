using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using OpenMoneyWeb.Api.Services;
using OpenMoneyWeb.Data;
using OpenMoneyWeb.Data.Repositories;

var builder = WebApplication.CreateBuilder(args);

// Cloud Run sends a PORT env var; fall back to default Kestrel port for local dev
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(port))
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

// Database — connection string can be overridden via ConnectionStrings__DefaultConnection env var
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("DefaultConnection not configured.");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

// Repositories & services
builder.Services.AddScoped<AccountRepository>();
builder.Services.AddScoped<InvestmentRepository>();
builder.Services.AddScoped<TransactionRepository>();
builder.Services.AddScoped<PortfolioService>();
builder.Services.AddScoped<ReportsService>();

builder.Services.AddControllers()
    .AddJsonOptions(opts =>
        opts.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// Allow Vite dev server to call the API during local development
builder.Services.AddCors(opts =>
    opts.AddPolicy("DevCors", p =>
        p.WithOrigins("http://localhost:5173").AllowAnyMethod().AllowAnyHeader()));

var app = builder.Build();

if (args.Contains("--migrate"))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
    return;
}

// Auto-apply migrations on startup (skipped in Testing environment)
if (!app.Environment.IsEnvironment("Testing"))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

if (app.Environment.IsDevelopment())
    app.UseCors("DevCors");

app.UseStaticFiles();       // serves React build output from wwwroot/
app.MapControllers();
app.MapFallbackToFile("index.html");    // SPA client-side routing fallback

app.Run();

public partial class Program { }
