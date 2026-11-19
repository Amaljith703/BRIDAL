using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using FluentValidation;
using MakeupArt.Api.Infrastructure;
using MakeupArt.Domain.Commerce;
using MakeupArt.Domain.Common;
using MakeupArt.Domain.Identity;
using MakeupArt.Infrastructure.Ai;
using MakeupArt.Infrastructure.Options;
using MakeupArt.Infrastructure.Payments;
using MakeupArt.Infrastructure.Persistence;
using MakeupArt.Infrastructure.Security;
using MakeupArt.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Serilog;
using Serilog.Events;

// Load developer secrets from the workspace .env before configuration is built.
DotEnv.Load();

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/api-.log", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 30)
    .CreateLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);
    builder.Host.UseSerilog();

    var configuration = builder.Configuration;
    var isDevelopment = builder.Environment.IsDevelopment();

    builder.Services.Configure<JwtOptions>(configuration.GetSection("Jwt"));
    builder.Services.Configure<SeedOptions>(configuration.GetSection("Seed"));
    builder.Services.Configure<PaymentOptions>(configuration.GetSection("Payments"));
    builder.Services.Configure<RazorpayOptions>(configuration.GetSection("Razorpay"));
    builder.Services.Configure<SmtpOptions>(configuration.GetSection("Smtp"));
    builder.Services.Configure<AiOptions>(configuration.GetSection("Ai"));
    builder.Services.Configure<BackupOptions>(configuration.GetSection("Backup"));
    builder.Services.Configure<FrontendOptions>(configuration.GetSection("Frontend"));

    var connectionString = configuration.GetConnectionString("DefaultConnection")
        ?? configuration["CONNECTIONSTRINGS__DEFAULTCONNECTION"];
    if (string.IsNullOrWhiteSpace(connectionString))
        throw new InvalidOperationException("ConnectionStrings:DefaultConnection is not configured. Copy .env.example to .env and set it.");

    var jwtOptions = configuration.GetSection("Jwt").Get<JwtOptions>() ?? new JwtOptions();
    if (string.IsNullOrWhiteSpace(jwtOptions.SigningKey) || Encoding.UTF8.GetByteCount(jwtOptions.SigningKey) < 32)
        throw new InvalidOperationException("Jwt:SigningKey must be at least 32 bytes. Set a long random value in .env.");
    if (jwtOptions.SigningKey.Contains("CHANGE_ME", StringComparison.OrdinalIgnoreCase))
        throw new InvalidOperationException("Jwt:SigningKey is still the placeholder. Generate a strong random secret before starting the API.");

    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseSqlServer(connectionString, sql =>
        {
            sql.EnableRetryOnFailure(4, TimeSpan.FromSeconds(8), null);
            sql.CommandTimeout(90);
        }));

    builder.Services
        .AddIdentityCore<ApplicationUser>(options =>
        {
            options.Password.RequiredLength = 10;
            options.Password.RequireUppercase = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireDigit = true;
            options.Password.RequireNonAlphanumeric = true;
            options.User.RequireUniqueEmail = true;
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.AllowedForNewUsers = true;
            options.SignIn.RequireConfirmedEmail = false;
            options.Tokens.AuthenticatorTokenProvider = TokenOptions.DefaultAuthenticatorProvider;
        })
        .AddRoles<ApplicationRole>()
        .AddEntityFrameworkStores<AppDbContext>()
        .AddSignInManager()
        .AddDefaultTokenProviders();

    builder.Services.AddDataProtection()
        .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(builder.Environment.ContentRootPath, ".keys")))
        .SetApplicationName("MakeupArt");

    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.RequireHttpsMetadata = !isDevelopment;
            options.SaveToken = true;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = jwtOptions.Issuer,
                ValidateAudience = true,
                ValidAudience = jwtOptions.Audience,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromSeconds(30),
                NameClaimType = ClaimTypes.Name,
                RoleClaimType = ClaimTypes.Role
            };
            options.Events = new JwtBearerEvents
            {
                OnChallenge = context =>
                {
                    if (string.IsNullOrEmpty(context.ErrorDescription))
                        context.ErrorDescription = "A valid access token is required for this request.";
                    return Task.CompletedTask;
                }
            };
        });

    builder.Services.AddAuthorization(options =>
    {
        options.AddPolicy("Staff", policy => policy.RequireRole("Admin", "Manager", "Support"));
        options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
        options.AddPolicy("Customer", policy => policy.RequireAuthenticatedUser());
    });

    builder.Services.AddHttpContextAccessor();
    builder.Services.AddMemoryCache();
    builder.Services.AddSingleton<TokenService>();
    builder.Services.AddScoped<IEmailService, EmailService>();
    builder.Services.AddScoped<IMediaStorageService, MediaStorageService>();
    builder.Services.AddScoped<IHtmlSanitizerService, HtmlSanitizerService>();
    builder.Services.AddScoped<IBackupService, BackupService>();
    builder.Services.AddScoped<ICheckoutService, CheckoutService>();
    builder.Services.AddScoped<IAiService, OllamaAiService>();
    builder.Services.AddScoped<DbSeeder>();

    builder.Services.AddScoped<IPaymentGateway, MockPaymentGateway>();
    builder.Services.AddScoped<IPaymentGateway, RazorpayPaymentGateway>();
    builder.Services.AddScoped<IPaymentGateway>(_ => new UnavailablePaymentGateway(PaymentProviderType.Stripe));
    builder.Services.AddScoped<IPaymentGateway>(_ => new UnavailablePaymentGateway(PaymentProviderType.PayPal));
    builder.Services.AddScoped<IPaymentGatewayFactory, PaymentGatewayFactory>();

    builder.Services.AddHttpClient("razorpay", client => client.Timeout = TimeSpan.FromSeconds(30));
    builder.Services.AddHttpClient("ollama", client => client.Timeout = TimeSpan.FromSeconds(150));

    builder.Services.AddCors(options =>
    {
        var origins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                      ?? ["http://localhost:4200", "http://127.0.0.1:4200"];
        options.AddPolicy("frontend", policy => policy
            .WithOrigins(origins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials()
            .WithExposedHeaders("X-Request-Id"));
    });

    builder.Services.AddResponseCompression(options =>
    {
        options.EnableForHttps = true;
        options.Providers.Add<BrotliCompressionProvider>();
        options.Providers.Add<GzipCompressionProvider>();
    });

    builder.Services.AddRateLimiter(options =>
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        options.AddPolicy("auth", context =>
            RateLimitPartition.GetFixedWindowLimiter(
                context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 15,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0,
                    AutoReplenishment = true
                }));
        options.AddPolicy("public-write", context =>
            RateLimitPartition.GetFixedWindowLimiter(
                context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 12,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0,
                    AutoReplenishment = true
                }));
        options.AddPolicy("ai", context =>
            RateLimitPartition.GetFixedWindowLimiter(
                context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 20,
                    Window = TimeSpan.FromMinutes(10),
                    QueueLimit = 0,
                    AutoReplenishment = true
                }));
        options.AddPolicy("checkout", context =>
            RateLimitPartition.GetFixedWindowLimiter(
                context.User.GetUserId()?.ToString() ?? context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 30,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0,
                    AutoReplenishment = true
                }));
    });

    builder.Services.AddControllers()
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        });
    builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
    {
        options.MultipartBodyLengthLimit = 160 * 1024 * 1024;
        options.ValueLengthLimit = 1024 * 1024;
    });
    builder.Services.AddValidatorsFromAssemblyContaining<MakeupArt.Application.Common.ApiException>();
    builder.Services.AddProblemDetails();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "Makeup Art API",
            Version = "v1.0.0.0",
            Description = "Storefront, checkout, CMS and analytics API for Makeup Art."
        });
        options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "Paste the access token returned by /api/auth/login."
        });
        options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference("Bearer", document)] = []
        });
    });

    builder.Services.AddHealthChecks()
        .AddDbContextCheck<AppDbContext>("database");

    builder.Services.Configure<ForwardedHeadersOptions>(options =>
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto);

    var app = builder.Build();

    app.UseForwardedHeaders();
    app.UseSerilogRequestLogging(options =>
    {
        options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
        {
            diagnosticContext.Set("UserId", httpContext.User.GetUserId()?.ToString() ?? "anonymous");
            diagnosticContext.Set("IpAddress", httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown");
        };
    });
    app.UseMiddleware<ApiExceptionMiddleware>();

    if (!isDevelopment)
    {
        app.UseHsts();
        app.UseHttpsRedirection();
    }

    app.Use(async (context, next) =>
    {
        context.Response.Headers["X-Content-Type-Options"] = "nosniff";
        context.Response.Headers["X-Frame-Options"] = "SAMEORIGIN";
        context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
        context.Response.Headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=(self)";
        context.Response.Headers["X-Request-Id"] = context.TraceIdentifier;
        await next();
    });

    app.UseResponseCompression();
    app.UseCors("frontend");
    app.UseRateLimiter();
    app.UseAuthentication();
    app.UseAuthorization();

    app.UseDefaultFiles();
    app.UseStaticFiles(new StaticFileOptions
    {
        OnPrepareResponse = ctx =>
        {
            if (ctx.File.Name.EndsWith(".html", StringComparison.OrdinalIgnoreCase))
                ctx.Context.Response.Headers.CacheControl = "no-cache";
            else if (ctx.Context.Request.Path.StartsWithSegments("/media"))
                ctx.Context.Response.Headers.CacheControl = "public,max-age=604800";
        }
    });

    if (isDevelopment)
    {
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "Makeup Art API v1");
            options.DocumentTitle = "Makeup Art API";
        });
    }

    app.MapControllers();
    app.MapHealthChecks("/health");
    app.MapGet("/api/health", () => Results.Ok(new { status = "ok", version = "1.0.0.0", utc = DateTime.UtcNow }));
    app.MapFallbackToFile("index.html");

    if (isDevelopment)
    {
        using var scope = app.Services.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        try
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await db.Database.MigrateAsync();
            var seeder = scope.ServiceProvider.GetRequiredService<DbSeeder>();
            await seeder.SeedAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Database migration or seeding failed. The API will still start so health endpoints remain available.");
        }
    }

    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Makeup Art API terminated unexpectedly.");
    throw;
}
finally
{
    await Log.CloseAndFlushAsync();
}

public partial class Program;
