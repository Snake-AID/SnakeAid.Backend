using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

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
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = configuration["Jwt:Issuer"],
                    ValidAudience = configuration["Jwt:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(configuration["Jwt:SecretKey"] ?? throw new InvalidOperationException("JWT Secret Key is not configured")))
                };
                options.Authority = "https://snakeaid.jp.auth0.com/";

                options.Audience = "https://snakeaid.com/api";

                options.RequireHttpsMetadata = true;
            });

        // services.AddAuthorizationBuilder()
        //     .AddPolicy("Admin", policy => policy.RequireClaim(ClaimTypes.Role, UserRole.Admin.ToString()))
        //     .AddPolicy("User", policy => policy.RequireClaim(ClaimTypes.Role, UserRole.User.ToString()))
        //     .AddPolicy("Guest", policy => policy.RequireClaim(ClaimTypes.Role, UserRole.Guest.ToString()));
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