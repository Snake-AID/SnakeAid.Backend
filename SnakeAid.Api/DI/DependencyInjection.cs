using System.Text;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using SnakeAid.Core.Domains;
using SnakeAid.Core.Settings;
using SnakeAid.Repository.Data;

namespace SnakeAid.Api.DI;

public static class DependencyInjection
{
    #region service he thong

    public static IServiceCollection AddServices(this IServiceCollection services)
    {


        return services;
    }

    #endregion

    #region authen vs author

    public static IServiceCollection AddAuthenticateAuthor(this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<JwtSettings>(configuration.GetSection("Jwt"));

        services.AddIdentityCore<Account>(options =>
            configuration.GetSection("IdentityOptions").Bind(options))
            .AddRoles<IdentityRole<Guid>>()
            .AddSignInManager()
            .AddEntityFrameworkStores<SnakeAidDbContext>()
            .AddDefaultTokenProviders();

        var jwtSettings = configuration.GetSection("Jwt").Get<JwtSettings>();
        if (jwtSettings == null || string.IsNullOrWhiteSpace(jwtSettings.SecretKey))
        {
            throw new InvalidOperationException("JWT settings are not configured.");
        }

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey));

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

            c.AddServer(new OpenApiServer
            {
                Url = "http://localhost:5009",
                Description = "Development"
            });

            // c.AddServer(new OpenApiServer
            // {
            //     Url = "http://localhost:8386",
            //     Description = "Development Docker"
            // });

            c.AddServer(new OpenApiServer
            {
                Url = "https://localhost:7026",
                Description = "Local HTTPS"
            });

            // c.AddServer(new OpenApiServer
            // {
            //     Url = "https://snakeaid.com.vn",
            //     Description = "Production Phake"
            // });

            // c.AddServer(new OpenApiServer
            // {
            //     Url = "https://tgx.lch.id.vn",
            //     Description = "Production Real"
            // });
        });
        return services;
    }

    #endregion
}
