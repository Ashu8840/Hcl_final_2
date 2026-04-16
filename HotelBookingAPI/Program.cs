using System.Data.Common;
using System.Net.Sockets;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using HotelBookingAPI.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// 1. Register DbContext with MySQL connection + safe fallback
var mySqlConnectionString = builder.Configuration.GetConnectionString("DefaultConnection");

if (string.IsNullOrWhiteSpace(mySqlConnectionString))
{
    throw new InvalidOperationException("Connection string 'DefaultConnection' is missing.");
}

var hasMySqlEndpoint = TryGetMySqlHostAndPort(mySqlConnectionString, out var mySqlHost, out var mySqlPort);
var canReachMySql = hasMySqlEndpoint && CanReachTcpHost(mySqlHost, mySqlPort, TimeSpan.FromSeconds(5));
var useSqliteFallback = !canReachMySql;

if (useSqliteFallback)
{
    var sqlitePath = Path.Combine(builder.Environment.ContentRootPath, "hotelbooking-dev.db");
    var sqliteConnectionString = $"Data Source={sqlitePath}";

    builder.Services.AddDbContext<HotelBookingDbContext>(options =>
        options.UseSqlite(sqliteConnectionString));
}
else
{
    builder.Services.AddDbContext<HotelBookingDbContext>(options =>
        options.UseMySQL(mySqlConnectionString));
}

// 2. Controllers + safer JSON serialization
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });

// 3. JWT authentication + authorization
var jwtKey = builder.Configuration["Jwt:Key"] ?? Environment.GetEnvironmentVariable("JWT__KEY");
if (string.IsNullOrWhiteSpace(jwtKey))
{
    throw new InvalidOperationException("JWT signing key is missing. Set Jwt:Key or JWT__KEY.");
}

var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "HotelBookingAPI";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "HotelBookingUI";

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false;
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });

builder.Services.AddAuthorization();

// 4. Swagger + JWT bearer support in Swagger UI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Hotel Booking API",
        Version = "v1",
        Description = "REST API for Hotel Booking System"
    });

    var jwtSecurityScheme = new OpenApiSecurityScheme
    {
        BearerFormat = "JWT",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        Description = "Enter your JWT token in this format: Bearer {token}",
        Reference = new OpenApiReference
        {
            Id = "Bearer",
            Type = ReferenceType.SecurityScheme
        }
    };

    c.AddSecurityDefinition(jwtSecurityScheme.Reference.Id, jwtSecurityScheme);
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        { jwtSecurityScheme, Array.Empty<string>() }
    });
});

// 5. CORS for Angular dev servers + production frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
        policy.SetIsOriginAllowed(origin =>
              {
                  if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri))
                  {
                      return false;
                  }

                  if (uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase)
                      || uri.Host.Equals("127.0.0.1"))
                  {
                      return true;
                  }

                  if (!uri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase))
                  {
                      return false;
                  }

                  return uri.Host.Equals("hcl-final-2-3crf.vercel.app", StringComparison.OrdinalIgnoreCase)
                         || uri.Host.Equals("hcl-final.vercel.app", StringComparison.OrdinalIgnoreCase);
              })
              .AllowAnyMethod()
              .AllowAnyHeader());
});

// 6. Basic API rate limiting
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = 429;

    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.User.Identity?.Name
                          ?? context.Connection.RemoteIpAddress?.ToString()
                          ?? "anonymous",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 120,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            }));

    options.AddFixedWindowLimiter("auth", limiterOptions =>
    {
        limiterOptions.PermitLimit = 20;
        limiterOptions.Window = TimeSpan.FromMinutes(1);
        limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiterOptions.QueueLimit = 0;
    });
});

var app = builder.Build();

if (useSqliteFallback)
{
    app.Logger.LogWarning(
        "MySQL server {Host}:{Port} is unreachable. Falling back to SQLite for runtime stability.",
        hasMySqlEndpoint ? mySqlHost : "<unknown>",
        hasMySqlEndpoint ? mySqlPort : 3306);
}
else
{
    app.Logger.LogInformation("Database provider configured: MySQL ({Host}:{Port})", mySqlHost, mySqlPort);
}

try
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<HotelBookingDbContext>();
    await DatabaseSeeder.SeedAsync(db, app.Configuration, app.Logger);
}
catch (Exception ex)
{
    app.Logger.LogError(ex, "Database seeding failed. API startup will continue.");
}

// 7. Swagger UI
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Hotel Booking API v1");
    c.RoutePrefix = "swagger";
});

// 8. Middleware pipeline
app.UseCors("AllowAngular");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", () => Results.Ok(new
{
    status = "ok",
    service = "HotelBookingAPI",
    databaseProvider = useSqliteFallback ? "SQLiteFallback" : "MySQL"
}));

app.MapGet("/health", () => Results.Ok(new
{
    status = "healthy",
    databaseProvider = useSqliteFallback ? "SQLiteFallback" : "MySQL"
}));

app.MapControllers();

app.Run();

static bool TryGetMySqlHostAndPort(string connectionString, out string host, out int port)
{
    host = string.Empty;
    port = 3306;

    try
    {
        var connectionBuilder = new DbConnectionStringBuilder
        {
            ConnectionString = connectionString
        };

        if (!TryGetConnectionStringValue(connectionBuilder, out host, "Server", "Host", "Data Source"))
        {
            return false;
        }

        if (TryGetConnectionStringValue(connectionBuilder, out var portValue, "Port")
            && int.TryParse(portValue, out var parsedPort)
            && parsedPort > 0)
        {
            port = parsedPort;
        }

        return !string.IsNullOrWhiteSpace(host);
    }
    catch
    {
        return false;
    }
}

static bool TryGetConnectionStringValue(DbConnectionStringBuilder connectionBuilder, out string value, params string[] keys)
{
    foreach (var key in keys)
    {
        if (!connectionBuilder.TryGetValue(key, out var rawValue) || rawValue is null)
        {
            continue;
        }

        var candidate = rawValue.ToString()?.Trim();
        if (!string.IsNullOrWhiteSpace(candidate))
        {
            value = candidate;
            return true;
        }
    }

    value = string.Empty;
    return false;
}

static bool CanReachTcpHost(string host, int port, TimeSpan timeout)
{
    try
    {
        using var client = new TcpClient();
        var connectTask = client.ConnectAsync(host, port);
        var connectedInTime = connectTask.Wait(timeout);

        return connectedInTime && client.Connected;
    }
    catch
    {
        return false;
    }
}
