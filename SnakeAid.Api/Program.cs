using Mapster;
using MapsterMapper;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Scrutor;
using Serilog;
using Serilog.Ui.Core.Extensions;
using Serilog.Ui.SqliteDataProvider.Extensions;
using Serilog.Ui.Web.Extensions;
using SnakeAid.Core.Mappings;
using SnakeAid.Core.Middlewares;
using SnakeAid.Api.DI;
using SnakeAid.Repository.Data;
using SQLitePCL;
using Swashbuckle.AspNetCore.SwaggerUI;
using System.Text.Json.Serialization;

namespace SnakeAid.Api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .CreateBootstrapLogger();

            try
            {
                var builder = WebApplication.CreateBuilder(args);

                Batteries_V2.Init();

                var sqliteLogPath = Path.Combine(builder.Environment.ContentRootPath, "logs", "logs.db");
                Directory.CreateDirectory(Path.GetDirectoryName(sqliteLogPath)!);
                var hasSerilogConfig = builder.Configuration.GetSection("Serilog").Exists();
                if (hasSerilogConfig)
                {
                    foreach (var sink in builder.Configuration.GetSection("Serilog:WriteTo").GetChildren())
                    {
                        var sinkName = sink["Name"];
                        if (!string.Equals(sinkName, "SQLite", StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        builder.Configuration[$"Serilog:WriteTo:{sink.Key}:Args:sqliteDbPath"] = sqliteLogPath;
                        break; // Only set for the first SQLite sink found
                    }
                }

                builder.Host.UseSerilog((context, services, loggerConfiguration) =>
                {
                    if (hasSerilogConfig)
                    {
                        loggerConfiguration.ReadFrom.Configuration(context.Configuration);
                    }
                    else
                    {
                        loggerConfiguration
                            .Enrich.FromLogContext()
                            .WriteTo.Console();
                    }

                    loggerConfiguration.ReadFrom.Services(services);
                });

                builder.Services.AddDbContext<SnakeAidDbContext>(options =>
                {
                    options.UseNpgsql(builder.Configuration.GetConnectionString("SupabaseConnection"),
                        sqlOptions =>
                        {
                            sqlOptions.EnableRetryOnFailure(
                                5,
                                TimeSpan.FromSeconds(30),
                                null);
                        });
                });

                // Add IUnitOfWork and UnitOfWork
                builder.Services.AddScoped<SnakeAid.Repository.Interfaces.IUnitOfWork<SnakeAidDbContext>, SnakeAid.Repository.Implements.UnitOfWork<SnakeAidDbContext>>();

                // Register Mapster
                var config = TypeAdapterConfig.GlobalSettings;
                MapsterConfig.RegisterMappings();
                builder.Services.AddSingleton(config);
                builder.Services.AddScoped<IMapper, ServiceMapper>();

                // Register OtpUtil
                builder.Services.AddScoped<SnakeAid.Core.Utils.OtpUtil>();

                // Register Email services
                builder.Services.AddHttpClient(); // For ResendEmailSender
                builder.Services.AddScoped<SnakeAid.Service.Implements.Email.Providers.ResendEmailSender>();
                builder.Services.AddScoped<SnakeAid.Service.Implements.Email.Providers.SmtpEmailSender>();
                builder.Services.AddScoped<SnakeAid.Service.Implements.Email.Providers.EmailProviderService>();
                builder.Services.AddScoped<SnakeAid.Service.Implements.Email.EmailTemplateService>();

                builder.Services.AddServices();

                // Register services using Scrutor
                builder.Services.Scan(scan => scan
                    .FromAssemblies(
                        typeof(Program).Assembly,                               // SnakeAid.Api
                        typeof(SnakeAid.Core.Domains.BaseEntity).Assembly,     // SnakeAid.Core
                        typeof(SnakeAid.Service.Interfaces.IAuthService).Assembly,  // SnakeAid.Service
                        typeof(SnakeAid.Repository.Interfaces.IGenericRepository<>).Assembly) // SnakeAid.Repository
                    .AddClasses(classes => classes.Where(type => type.Name.EndsWith("Service") || type.Name.EndsWith("Repository")))
                    .AsImplementedInterfaces()
                    .WithScopedLifetime());

                builder.Services.AddMemoryCache();

                builder.Services.AddCors(options =>
                {
                    options.AddPolicy("AllowAll", policy =>
                    {
                        policy.AllowAnyOrigin()
                            .AllowAnyMethod()
                            .AllowAnyHeader();
                    });
                });

                builder.Services.AddHttpContextAccessor();

                builder.Services.AddControllers().AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                });

                builder.Services.AddControllers();

                builder.Services.AddAuthenticateAuthor(builder.Configuration);

                // Add Swagger with JWT support
                builder.Services.AddSwagger();

                // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
                builder.Services.AddEndpointsApiExplorer();
                builder.Services.AddSwaggerGen();

                builder.Services.AddSerilogUi(options =>
                {
                    options.UseSqliteServer(sqliteOptions => sqliteOptions
                        .WithConnectionString($"Data Source={sqliteLogPath};Cache=Shared")
                        .WithTable("Logs")
                        .WithCustomProviderName("SnakeAid Serilogs"));
                });


                // Bind Kestrel to all network interfaces
                builder.WebHost.ConfigureKestrel((context, options) =>
                {
                    if (context.HostingEnvironment.IsDevelopment())
                    {
                        // Check if running in container
                        var isContainer = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true";
                        
                        if (isContainer)
                        {
                            // Docker container: HTTP only
                            options.ListenAnyIP(8080);
                        }
                        else
                        {
                            // Local dev: both HTTP and HTTPS
                            options.ListenAnyIP(5009);
                            options.ListenLocalhost(7026, listenOptions => listenOptions.UseHttps());
                        }
                    }
                    else
                    {
                        // Production: HTTP only (HTTPS termination at reverse proxy)
                        options.ListenAnyIP(8080);
                    }
                });

                builder.Services.Configure<ApiBehaviorOptions>(options => { options.SuppressModelStateInvalidFilter = true; });

                // // Firebase
                // try
                // {
                //     // Use fixed path for Firebase credentials - works for both development and production
                //     var credentialsPath = File.Exists("/app/firebase-service-account.json")
                //         ? "/app/firebase-service-account.json"
                //         : "firebase-service-account.json";

                //     var firebaseApp = FirebaseApp.Create(new AppOptions
                //     {
                //         Credential = GoogleCredential.FromFile(credentialsPath)
                //     });

                //     builder.Services.AddSingleton(firebaseApp);
                // }
                // catch (Exception ex)
                // {
                //     throw new FileNotFoundException("Failed to initialize Firebase", ex);
                // }

                builder.Services.Configure<RouteOptions>(options =>
                {
                    options.LowercaseUrls = true; // Forces lowercase routes
                });

                // Cấu hình để nhận IP từ header X-Forwarded-For nếu chạy sau proxy
                builder.Services.Configure<ForwardedHeadersOptions>(options =>
                {
                    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
                });

                var app = builder.Build();

                // Thêm middleware xử lý IP từ proxy
                app.UseForwardedHeaders();

                app.UseCors("AllowAll");



                // Handle authorization responses (401/403) before authentication
                app.UseAuthorizationResponseHandler();

                // handle other api response  middleware
                app.UseApiExceptionHandler();

                // Configure the HTTP request pipeline.
                if (app.Environment.IsDevelopment())
                {
                    var applyMigration = app.Configuration.GetValue<bool>("ApplyMigrationOnStartup");
                    if (applyMigration)
                    {
                        // app.ApplyMigrations<SnakeAidDbContext>();
                    }
                }
                app.UseSwagger();
                app.UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", "SnakeAid API V1");
                    c.RoutePrefix = string.Empty; // Set Swagger UI at the app's root
                    c.DocumentTitle = "SnakeAid API Hub";
                    c.DocExpansion(DocExpansion.None);
                    c.EnableTryItOutByDefault();
                    c.DisplayRequestDuration();
                    c.EnablePersistAuthorization();
                    c.EnableTryItOutByDefault();
                    c.EnableFilter();
                    c.EnableDeepLinking();
                });

                app.UseSerilogRequestLogging();

                app.UseHttpsRedirection();

                app.UseAuthentication();
                app.UseAuthorization();

                app.UseSerilogUi(options => options.WithRoutePrefix("logs"));

                app.MapControllers();

                app.Run();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Application terminated unexpectedly");
                throw;
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }
    }
}
