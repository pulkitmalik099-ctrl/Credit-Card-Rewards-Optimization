using Serilog;
using Microsoft.EntityFrameworkCore;
using CreditCardRewards.Data.Context;
using CreditCardRewards.Core.Interfaces;
using CreditCardRewards.Core.Services;
using CreditCardRewards.DataRefresh.Interfaces;
using CreditCardRewards.DataRefresh.Services;
using Microsoft.AspNetCore.Http.Features;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
builder.Host.UseSerilog((context, config) =>
{
    config
        .MinimumLevel.Debug()
        .WriteTo.Console()
        .WriteTo.File(
            path: "logs/app-.txt",
            rollingInterval: RollingInterval.Day,
            outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}");
});

// Add services
builder.Services.AddDbContext<AppDbContext>(options =>
{
    if (builder.Configuration.GetValue<bool>("UseInMemoryDatabase"))
    {
        options.UseInMemoryDatabase("CreditCardRewardsDev");
    }
    else if (builder.Configuration.GetValue<bool>("UseSqlite"))
    {
        options.UseSqlite(builder.Configuration.GetConnectionString("SqliteConnection") ?? "Data Source=rewards.db");
    }
    else
    {
        options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"));
    }
});

// Core services DI
builder.Services.AddScoped<IRewardCalculationService, RewardCalculationService>();
builder.Services.AddScoped<ITransactionRecommendationService, TransactionRecommendationService>();
builder.Services.AddScoped<ISpendTrackingService, SpendTrackingService>();
builder.Services.AddScoped<ICardOfferRefreshService, CardOfferRefreshService>();
builder.Services.AddScoped<ICardLookupService, CardLookupService>();
builder.Services.AddSingleton<IStatementParserService, StatementParserService>();
builder.Services.AddHostedService<StatementFolderWatcher>();

// Allow large file uploads for statement PDFs
builder.Services.Configure<FormOptions>(o => o.MultipartBodyLengthLimit = 20 * 1024 * 1024);

// Add controllers
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
        builder
            .AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader());
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseDefaultFiles();
app.UseStaticFiles();
app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseAuthorization();
app.MapControllers();

// Automatically apply migrations at startup for relational databases
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    if (db.Database.IsRelational())
    {
        db.Database.Migrate();
    }
}

app.Run();
