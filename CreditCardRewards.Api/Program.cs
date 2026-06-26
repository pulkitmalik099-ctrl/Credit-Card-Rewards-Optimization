using System.Text;
using Serilog;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using FluentValidation;
using FluentValidation.AspNetCore;
using CreditCardRewards.Data.Context;
using CreditCardRewards.Core.Interfaces;
using CreditCardRewards.Core.Services;
using CreditCardRewards.DataRefresh.Interfaces;
using CreditCardRewards.DataRefresh.Services;
using CreditCardRewards.Api.Services;
using CreditCardRewards.Api.Validators;
using Microsoft.AspNetCore.Http.Features;

var builder = WebApplication.CreateBuilder(args);

// Serilog
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

// Database
builder.Services.AddDbContext<AppDbContext>(options =>
{
    if (builder.Configuration.GetValue<bool>("UseInMemoryDatabase"))
        options.UseInMemoryDatabase("CreditCardRewardsDev");
    else if (builder.Configuration.GetValue<bool>("UseSqlite"))
        options.UseSqlite(builder.Configuration.GetConnectionString("SqliteConnection") ?? "Data Source=rewards.db");
    else
        options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"));
});

// JWT Auth
var jwtSecret = builder.Configuration["Jwt:Secret"] ?? "dev-secret-change-in-production-min32chars!!";
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "CreditCardRewardsApi",
            ValidAudience = builder.Configuration["Jwt:Audience"] ?? "CreditCardRewardsApp",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret))
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddSingleton<JwtService>();

// Core services
builder.Services.AddScoped<IRewardCalculationService, RewardCalculationService>();
builder.Services.AddScoped<ITransactionRecommendationService, TransactionRecommendationService>();
builder.Services.AddScoped<ISpendTrackingService, SpendTrackingService>();
builder.Services.AddScoped<ICardOfferRefreshService, CardOfferRefreshService>();
builder.Services.AddScoped<ICardLookupService, CardLookupService>();
builder.Services.AddSingleton<IStatementParserService, StatementParserService>();
builder.Services.AddHostedService<StatementFolderWatcher>();

// Allow large PDF uploads
builder.Services.Configure<FormOptions>(o => o.MultipartBodyLengthLimit = 20 * 1024 * 1024);

// FluentValidation
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<CreateCreditCardRequestValidator>();

// Controllers + Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header
    });
    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseDefaultFiles();
app.UseStaticFiles();
app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Apply DB migrations
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    if (db.Database.IsRelational())
        db.Database.Migrate();
}

app.Run();
