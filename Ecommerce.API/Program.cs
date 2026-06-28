using Asp.Versioning;
using Ecommerce.Application.Validators.Auth;
using Ecommerce.Domain.Entities;
using Ecommerce.Infrastructure;
using Ecommerce.Infrastructure.Identity;
using Ecommerce.Infrastructure.Middleware;
using Ecommerce.Infrastructure.Persistence;
using FluentValidation;
using FluentValidation.AspNetCore;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using System.Text;
using System.Text.Json;
using System.Threading.RateLimiting;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Npgsql;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
/*
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));*/


builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

//Authentication configuration  
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var key = Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!);

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,

            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(key)
        };
    });


builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.SuppressModelStateInvalidFilter = true;
});


// Rate Limiting configuration
builder.Services.AddRateLimiter(options =>
{
    // Global API rate limit (per IP)
    options.AddPolicy("ApiPolicy", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 100,                // 100 requests
                Window = TimeSpan.FromMinutes(1), // per minute
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 10
            }));

    // Strict login protection
    options.AddPolicy("AuthPolicy", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));

    // Payment endpoints protection
    options.AddPolicy("PaymentPolicy", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1)
            }));

    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.StatusCode = 429;

        await context.HttpContext.Response.WriteAsync(
            "Too many requests. Please try again later.",
            cancellationToken);
    };
});

// OpenTelemetry request trace configuration
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource =>
        resource.AddService("Ecommerce.Api"))
    .WithTracing(tracing =>
    {
        tracing
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddEntityFrameworkCoreInstrumentation()
            .AddConsoleExporter();
    });

// Hangfire asynchtonous job processing configuration
builder.Services.AddHangfire(config =>
    config.SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
          .UseSimpleAssemblyNameTypeSerializer()
          .UseRecommendedSerializerSettings()
          .UsePostgreSqlStorage(
              builder.Configuration.GetConnectionString("DefaultConnection"),
              new PostgreSqlStorageOptions
              {
                  QueuePollInterval = TimeSpan.FromSeconds(15),
                  InvisibilityTimeout = TimeSpan.FromMinutes(5),
                  DistributedLockTimeout = TimeSpan.FromMinutes(5)
              }));

builder.Services.AddHangfireServer();

//API Versioning configuration

builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
    options.ApiVersionReader = new UrlSegmentApiVersionReader();
})
.AddApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File(
        "logs/log-.txt",
        rollingInterval: RollingInterval.Day)
    .CreateLogger();


builder.Services.AddHealthChecks()
    .AddNpgSql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        name: "postgres",
        tags: new[] { "db", "ready" });

    /*.AddRedis(
        builder.Configuration["Redis:ConnectionString"],
        name: "redis",
        tags: new[] { "cache", "ready" });*/

builder.Host.UseSerilog();


builder.Services.AddAuthorization();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
builder.Services.AddHostedService<PaymentReconciliationService>();
builder.Services.AddHostedService<OutboxProcessor>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy
            .WithOrigins(
                "http://localhost:3000", // React local
                "http://localhost:5173", // Vite local
                "https://yourfrontend.com" // Production frontend
            )
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});


builder.Services.AddControllers();

builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssembly(
    typeof(RegisterRequestValidator).Assembly);


// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
//builder.Services.AddSwaggerGen();


builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Ecommerce API",
        Version = "v1"
    });

    //Add JWT Authentication
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter JWT token like this: Bearer {your token}"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});


var app = builder.Build();

await AdminSeeder.SeedAsync(app.Services);

// Configure the HTTP request pipeline.
var port = Environment.GetEnvironmentVariable("PORT") ?? "10000";
app.Urls.Add($"http://0.0.0.0:{port}");

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Ecommerce API V1");
    // Optional:
    // c.RoutePrefix = string.Empty;
});

app.UseSerilogRequestLogging();

app.UseHttpsRedirection();

app.UseMiddleware<PaystackWebhookMiddleware>();
app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseRateLimiter();

app.UseCors("Frontend");

app.UseAuthentication();
app.UseAuthorization();

app.UseHangfireDashboard("/jobs");

app.MapControllers();

app.MapHealthChecks("/health");

app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        var result = new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                duration = e.Value.Duration.ToString()
            })
        };

        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(JsonSerializer.Serialize(result));
    }
});



using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider
        .GetRequiredService<ILogger<Program>>();

    var db = scope.ServiceProvider
        .GetRequiredService<AppDbContext>();

    try
    {
       
        var historyTableExists = await db.Database
            .SqlQueryRaw<bool>(@"
                SELECT EXISTS (
                    SELECT FROM information_schema.tables
                    WHERE table_schema = 'public'
                    AND table_name = '__EFMigrationsHistory'
                ) AS ""Value""
            ")
            .FirstOrDefaultAsync();

        if (!historyTableExists)
        {
            logger.LogWarning(
                "No __EFMigrationsHistory table found. " +
                "Assuming existing schema and registering all migrations as applied.");

            await db.Database.ExecuteSqlRawAsync(@"
                CREATE TABLE IF NOT EXISTS ""__EFMigrationsHistory"" (
                    ""MigrationId"" character varying(150) NOT NULL,
                    ""ProductVersion"" character varying(32) NOT NULL,
                    CONSTRAINT ""PK___EFMigrationsHistory""
                        PRIMARY KEY (""MigrationId"")
                );
            ");

            var allMigrations = db.Database.GetMigrations().ToList();

            foreach (var migration in allMigrations)
            {
                await db.Database.ExecuteSqlRawAsync($@"
                    INSERT INTO ""__EFMigrationsHistory""
                        (""MigrationId"", ""ProductVersion"")
                    VALUES ('{migration}', '9.0.0')
                    ON CONFLICT DO NOTHING;
                ");

                logger.LogInformation(
                    "Registered existing migration: {Migration}",
                    migration);
            }
        }

        // Apply any pending migrations
        var pending = (await db.Database.GetPendingMigrationsAsync()).ToList();

        if (pending.Any())
        {
            logger.LogInformation(
                "Applying {Count} pending migration(s): {Migrations}",
                pending.Count,
                string.Join(", ", pending));

            await db.Database.MigrateAsync();

            logger.LogInformation("Migrations applied successfully.");
        }
        else
        {
            logger.LogInformation("Database is up to date. No pending migrations.");
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while applying database migrations.");
        throw; // Crash app so the bad deploy is visible
    }
}

app.Run();
