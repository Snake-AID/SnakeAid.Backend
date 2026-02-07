using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Polly;
using Polly.Extensions.Http;
using Refit;
using SnakeAid.Core.Domains;
using SnakeAid.Core.Settings;
using SnakeAid.Repository.Data;
using SnakeAid.Service.Implements;
using SnakeAid.Service.Interfaces;
using System.Security.Claims;
using System.Text;

using Serilog;
using Doppler.Extensions.Configuration; // Ensure this is available

namespace SnakeAid.Api.DI;

public static class DependencyInjection
{
    #region service he thong

    #region AppSettings
    public static WebApplicationBuilder AddAppsettings(this WebApplicationBuilder builder)
    {
        var dopplerAccessToken = LoadDopplerToken();

        var configStrategy = new ConfigControl
        {
            UseDoppler = Environment.GetEnvironmentVariable("SNAKEAID_CONF_USE_DOPPLER") != "false",
            UseAppSettings = Environment.GetEnvironmentVariable("SNAKEAID_CONF_USE_APPSETTINGS") != "false",
            Priority = Environment.GetEnvironmentVariable("SNAKEAID_CONF_PRIORITY")?.ToUpper() ?? "DOPPLER"
        };

        ManageAppSettings(builder, configStrategy);
        ManageDoppler(builder, configStrategy, dopplerAccessToken);
        ValidateConfiguration(configStrategy);

        return builder;
    }

    private static string? LoadDopplerToken()
    {
        var token = Environment.GetEnvironmentVariable("DOPPLER_TOKEN");
        if (!string.IsNullOrEmpty(token)) return token;

        token = Environment.GetEnvironmentVariable("DOPPLER_TOKEN", EnvironmentVariableTarget.User);
        if (string.IsNullOrEmpty(token)) return null;

        Environment.SetEnvironmentVariable("DOPPLER_TOKEN", token, EnvironmentVariableTarget.Process);

        // Propagate other user env vars
        foreach (System.Collections.DictionaryEntry userEnvVar in Environment.GetEnvironmentVariables(EnvironmentVariableTarget.User))
        {
            if (userEnvVar.Key is string key && userEnvVar.Value is string value && key != "DOPPLER_TOKEN")
            {
                Environment.SetEnvironmentVariable(key, value, EnvironmentVariableTarget.Process);
            }
        }

        return token;
    }

    private static void ManageAppSettings(WebApplicationBuilder builder, ConfigControl strategy)
    {
        if (strategy.UseAppSettings)
        {
            Serilog.Log.Information("✅ Configuration Source: AppSettings (ENABLED)");
            return;
        }

        var jsonSourcesToDiscard = builder.Configuration.Sources
            .Where(source => source is Microsoft.Extensions.Configuration.Json.JsonConfigurationSource)
            .ToList();

        foreach (var source in jsonSourcesToDiscard)
        {
            builder.Configuration.Sources.Remove(source);
        }
        Serilog.Log.Information("🚫 Configuration Source: AppSettings (DISABLED via Env Var)");
    }

    private static void ManageDoppler(WebApplicationBuilder builder, ConfigControl strategy, string? token)
    {
        if (!strategy.UseDoppler)
        {
            Serilog.Log.Information("🚫 Configuration Source: Doppler (DISABLED via Env Var)");
            return;
        }

        if (string.IsNullOrEmpty(token))
        {
            Serilog.Log.Warning("⚠️ Doppler enabled but DOPPLER_TOKEN is missing.");
            return;
        }

        builder.Configuration.AddDoppler(token);

        if (strategy.Priority == "APPSETTINGS" && strategy.UseAppSettings)
        {
            var jsonSourcesToReorder = builder.Configuration.Sources
                .Where(source => source is Microsoft.Extensions.Configuration.Json.JsonConfigurationSource)
                .ToList();

            foreach (var source in jsonSourcesToReorder)
            {
                builder.Configuration.Sources.Remove(source);
                builder.Configuration.Sources.Add(source);
            }
            Serilog.Log.Information("✅ Configuration Source: Doppler (ENABLED, Priority: LOW)");
        }
        else
        {
            Serilog.Log.Information("✅ Configuration Source: Doppler (ENABLED, Priority: HIGH)");
        }
    }

    private static void ValidateConfiguration(ConfigControl strategy)
    {
        if (!strategy.UseAppSettings && !strategy.UseDoppler)
        {
            throw new Core.Exceptions.ConfigurationException("❌ Critical Error: All configuration sources (Doppler and AppSettings) are DISABLED.");
        }
    }

    #endregion

    #region Services
    public static IServiceCollection AddServices(this IServiceCollection services, IConfiguration configuration)
    {
        var cloudinarySection = configuration.GetSection("Cloudinary");
        services.Configure<CloudinarySettings>(cloudinarySection);

        if (cloudinarySection.Exists())
        {
            var cloudinarySettings = cloudinarySection.Get<CloudinarySettings>();
            if (cloudinarySettings is null ||
                string.IsNullOrWhiteSpace(cloudinarySettings.CloudName) ||
                string.IsNullOrWhiteSpace(cloudinarySettings.ApiKey) ||
                string.IsNullOrWhiteSpace(cloudinarySettings.ApiSecret))
            {
                throw new InvalidOperationException("Cloudinary settings are not configured properly.");
            }
        }

        // Configure SnakeAI Settings (Type-safe)
        var snakeAISettings = configuration.GetSection("SnakeAI").Get<SnakeAISettings>();
        if (snakeAISettings is null)
        {
            throw new InvalidOperationException("SnakeAI settings are not configured properly.");
        }
        services.AddSingleton(snakeAISettings);

        // Register SnakeAI Service with Refit and Polly
        services
            .AddRefitClient<ISnakeAIApi>()
            .ConfigureHttpClient(c =>
            {
                c.BaseAddress = new Uri(snakeAISettings.BaseUrl);
                c.Timeout = TimeSpan.FromSeconds(snakeAISettings.TimeoutSeconds);
            })
            .AddPolicyHandler(GetRetryPolicy())
            .AddPolicyHandler(GetCircuitBreakerPolicy());

        services.AddScoped<ISnakeAIService, SnakeAIService>();

        return services;
    }

    private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .WaitAndRetryAsync(3, retryAttempt =>
                TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
    }

    private static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(5, TimeSpan.FromSeconds(30));
    }

    #endregion

    #endregion

    #region authen vs author

    public static IServiceCollection AddAuthenticateAuthor(this IServiceCollection services,
        IConfiguration configuration)
    {
        // Bind JwtSettings from configuration
        services.Configure<JwtSettings>(configuration.GetSection("Jwt"));

        // Add Identity Core services (without roles management)
        services.AddIdentityCore<Account>(options =>
            {
                // Password settings
                var passwordSection = configuration.GetSection("IdentityOptions:Password");
                options.Password.RequiredLength = passwordSection.GetValue<int>("RequiredLength", 8);
                options.Password.RequireDigit = passwordSection.GetValue<bool>("RequireDigit", true);
                options.Password.RequireLowercase = passwordSection.GetValue<bool>("RequireLowercase", true);
                options.Password.RequireUppercase = passwordSection.GetValue<bool>("RequireUppercase", true);
                options.Password.RequireNonAlphanumeric = passwordSection.GetValue<bool>("RequireNonAlphanumeric", false);

                // Lockout settings
                var lockoutSection = configuration.GetSection("IdentityOptions:Lockout");
                options.Lockout.MaxFailedAccessAttempts = lockoutSection.GetValue<int>("MaxFailedAccessAttempts", 5);
                options.Lockout.DefaultLockoutTimeSpan = lockoutSection.GetValue<TimeSpan>("DefaultLockoutTimeSpan", TimeSpan.FromMinutes(15));
                options.Lockout.AllowedForNewUsers = lockoutSection.GetValue<bool>("AllowedForNewUsers", true);

                // SignIn settings
                var signInSection = configuration.GetSection("IdentityOptions:SignIn");
                options.SignIn.RequireConfirmedEmail = signInSection.GetValue<bool>("RequireConfirmedEmail", false);

                // User settings
                options.User.RequireUniqueEmail = true;
            })
            .AddSignInManager()
            .AddEntityFrameworkStores<SnakeAidDbContext>()
            .AddDefaultTokenProviders();

        // Validate JWT settings
        var jwtSettings = configuration.GetSection("Jwt").Get<JwtSettings>();
        if (jwtSettings == null || string.IsNullOrWhiteSpace(jwtSettings.SecretKey))
        {
            throw new InvalidOperationException("JWT settings are not configured properly.");
        }

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey));

        // Add JWT Authentication
        services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtSettings.Issuer,
                    ValidAudience = jwtSettings.Audience,
                    IssuerSigningKey = signingKey,
                    RoleClaimType = ClaimTypes.Role,
                    NameClaimType = ClaimTypes.NameIdentifier
                };
                options.RequireHttpsMetadata = true;
                options.SaveToken = true;

                // Auto-add "Bearer " prefix if missing
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
                        if (!string.IsNullOrEmpty(authHeader))
                        {
                            // If token doesn't start with "Bearer ", add it
                            if (!authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                            {
                                context.Token = authHeader;
                            }
                        }
                        return Task.CompletedTask;
                    }
                };
            });

        services.AddAuthorization();

        return services;
    }

    #endregion

    #region swagger

    public static IServiceCollection AddSwagger(this IServiceCollection services)
    {
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "SnakeAid.API",
                Version = "v1",
                Description = "A SnakeAid Project"
            });
            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Description =
                    "JWT Authorization header using the Bearer scheme. Example: \"Bearer {token}\"",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.ApiKey,
                Scheme = "Bearer"
            });
            c.AddSecurityRequirement(new OpenApiSecurityRequirement
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
                    new string[] { }
                }
            });

            c.EnableAnnotations();

            // c.AddServer(new OpenApiServer
            // {
            //     Url = "http://localhost:5009",
            //     Description = "Development"
            // });

            // c.AddServer(new OpenApiServer
            // {
            //     Url = "http://localhost:8080",
            //     Description = "Docker Container"
            // });

            // c.AddServer(new OpenApiServer
            // {
            //     Url = "https://localhost:7026",
            //     Description = "Local HTTPS"
            // });

            // c.AddServer(new OpenApiServer
            // {
            //     Url = "https://snakeaid.com.vn",
            //     Description = "Production Phake"
            // });
        });
        return services;
    }

    #endregion
}
