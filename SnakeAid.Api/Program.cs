using Mapster;
using MapsterMapper;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Scrutor;
using Serilog;
using Serilog.Ui.Core.Extensions;
using Serilog.Ui.SqliteDataProvider.Extensions;
using Serilog.Ui.Web.Extensions;
using SnakeAid.Core.Mappings;
using SnakeAid.Core.Middlewares;
using SnakeAid.Api.DI;
using SnakeAid.Api.Hubs;
using SnakeAid.Repository.Data;
using SnakeAid.Repository.Seeds;
using SQLitePCL;
using Swashbuckle.AspNetCore.SwaggerUI;
using System.Text.Json.Serialization;
using Doppler.Extensions.Configuration;
using SnakeAid.Service.Interfaces;

namespace SnakeAid.Api
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .CreateBootstrapLogger();

            try
            {
                var builder = WebApplication.CreateBuilder(args);

                builder.AddConfigurationFromDopplerCloud();

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
                        loggerConfiguration.MinimumLevel.Override("Microsoft.Hosting.Lifetime", Serilog.Events.LogEventLevel.Information);
                    }
                    else
                    {
                        loggerConfiguration
                            .Enrich.FromLogContext()
                            .WriteTo.Console()
                            .MinimumLevel.Override("Microsoft.Hosting.Lifetime", Serilog.Events.LogEventLevel.Information);
                    }

                    loggerConfiguration.ReadFrom.Services(services);
                });

                var connectionString = builder.Configuration.GetConnectionString("SupabaseConnection");

                // Fix for Supabase MaxClientsInSessionMode error
                // Redirect to Transaction pooling mode port (6543) and disable prepared statements
                if (!string.IsNullOrEmpty(connectionString) && connectionString.Contains("pooler.supabase.com"))
                {
                    var npgsqlBuilder = new Npgsql.NpgsqlConnectionStringBuilder(connectionString);
                    if (npgsqlBuilder.Port == 5432)
                    {
                        npgsqlBuilder.Port = 6543;
                    }
                    npgsqlBuilder.MaxAutoPrepare = 0;
                    connectionString = npgsqlBuilder.ConnectionString;
                }

                builder.Services.AddDbContext<SnakeAidDbContext>(options =>
                {
                    options.UseNpgsql(connectionString,
                        sqlOptions =>
                        {
                            sqlOptions.UseNetTopologySuite();
                            sqlOptions.EnableRetryOnFailure(
                                5,
                                TimeSpan.FromSeconds(30),
                                null);
                        });
                });

                // Add IUnitOfWork and UnitOfWork
                builder.Services.AddScoped<SnakeAid.Repository.Interfaces.IUnitOfWork<SnakeAidDbContext>, SnakeAid.Repository.Implements.UnitOfWork<SnakeAidDbContext>>();

                // Also register base IUnitOfWork interface for services that don't need generic version
                builder.Services.AddScoped<SnakeAid.Repository.Interfaces.IUnitOfWork>(provider =>
                    provider.GetRequiredService<SnakeAid.Repository.Interfaces.IUnitOfWork<SnakeAidDbContext>>());

                // Register Mapster
                var config = TypeAdapterConfig.GlobalSettings;
                MapsterConfig.RegisterMappings();
                builder.Services.AddSingleton(config);
                builder.Services.AddScoped<IMapper, ServiceMapper>();

                // Register OtpUtil
                builder.Services.AddScoped<SnakeAid.Core.Utils.OtpUtil>();

                // Configure PayOS options
                builder.Services.Configure<SnakeAid.Core.Settings.PayOsOptions>(
                    builder.Configuration.GetSection("PayOS"));

                // Register payment gateway
                builder.Services.AddScoped<SnakeAid.Service.Interfaces.IPaymentGateway, SnakeAid.Service.Services.PayOs.PayOsGateway>();

                // Register Snake Catching Payment Service
                builder.Services.AddScoped<SnakeAid.Service.Interfaces.ISnakeCatchingPaymentService, SnakeAid.Service.Implements.SnakeCatchingPaymentService>();

                // Register Wallet Topup Service
                builder.Services.AddScoped<SnakeAid.Service.Interfaces.IWalletTopupService, SnakeAid.Service.Implements.WalletTopupService>();

                // Register Email services
                builder.Services.AddHttpClient(); // For ResendEmailSender
                builder.Services.AddScoped<SnakeAid.Service.Implements.Email.Providers.ResendEmailSender>();
                builder.Services.AddScoped<SnakeAid.Service.Implements.Email.Providers.SmtpEmailSender>();
                builder.Services.AddScoped<SnakeAid.Service.Implements.Email.Providers.EmailProviderService>();
                builder.Services.AddScoped<SnakeAid.Service.Implements.Email.EmailTemplateService>();

                builder.Services.AddServices(builder.Configuration);

                // Register services using Scrutor (excluding background services)
                builder.Services.Scan(scan => scan
                    .FromAssemblies(
                        typeof(Program).Assembly,                               // SnakeAid.Api
                        typeof(SnakeAid.Core.Domains.BaseEntity).Assembly,     // SnakeAid.Core
                        typeof(SnakeAid.Service.Interfaces.IAuthService).Assembly,  // SnakeAid.Service
                        typeof(SnakeAid.Repository.Interfaces.IGenericRepository<>).Assembly) // SnakeAid.Repository
                    .AddClasses(classes => classes
                        .Where(type => (type.Name.EndsWith("Service") || type.Name.EndsWith("Repository"))
                            && !type.Name.Contains("BackgroundService"))) // Exclude background services
                    .AsImplementedInterfaces()
                    .WithScopedLifetime());

                builder.Services.AddSingleton<SnakeAid.Service.Implements.ConsultationLifecycleBackgroundService>();
                builder.Services.AddSingleton<IHostedService>(provider =>
                    provider.GetRequiredService<SnakeAid.Service.Implements.ConsultationLifecycleBackgroundService>());

                builder.Services.AddMemoryCache();

                // Health checks endpoint
                builder.Services.AddHealthChecks();

                builder.Services.AddCors(options =>
                {
                    options.AddPolicy("AllowAll", policy =>
                    {
                        policy.SetIsOriginAllowed(_ => true)
                            .AllowAnyMethod()
                            .AllowAnyHeader()
                            .AllowCredentials(); // Required for SignalR
                    });

                    // Specific policy for SignalR
                    options.AddPolicy("SignalRCorsPolicy", policy =>
                    {
                        policy.SetIsOriginAllowed(_ => true)
                            .AllowAnyMethod()
                            .AllowAnyHeader()
                            .AllowCredentials()
                            .WithExposedHeaders("Access-Control-Allow-Origin");
                    });
                });

                builder.Services.AddHttpContextAccessor();

                builder.Services.AddControllers().AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                    options.JsonSerializerOptions.Converters.Add(new SnakeAid.Core.Converters.PointJsonConverter());

                    // Handle circular references in JSON serialization
                    options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
                });

                builder.Services.AddControllers();

                // Add Razor Pages for lightweight UI admin pages
                builder.Services.AddRazorPages();

                // Add SignalR
                builder.Services.AddSignalR(options =>
                {
                    options.EnableDetailedErrors = builder.Environment.IsDevelopment();
                    options.KeepAliveInterval = TimeSpan.FromSeconds(15);
                    options.ClientTimeoutInterval = TimeSpan.FromMinutes(2);
                    options.HandshakeTimeout = TimeSpan.FromSeconds(30);
                    options.MaximumReceiveMessageSize = 64 * 1024; // 64KB
                    options.StreamBufferCapacity = 10;
                }).AddJsonProtocol(options =>
                {
                    options.PayloadSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
                });

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
                    // Always listen on port 8080 (HTTP)
                    // This creates consistency across Local, Docker, and Production environments
                    options.ListenAnyIP(8080);

                    // For Local Development, also listen on port 8081 (HTTPS)
                    // This allows debugging secure features (Cookies, OAuth, etc.) locally
                    if (context.HostingEnvironment.IsDevelopment())
                    {
                        options.ListenLocalhost(8081, listenOptions => listenOptions.UseHttps());
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

                    // // // Seed data (mở ra nếu seed lại dữ liệu)
                    // using (var scope = app.Services.CreateScope())
                    // {
                    //     var context = scope.ServiceProvider.GetRequiredService<SnakeAidDbContext>();
                    //     await DataSeeder.SeedAsync(context);
                    // }
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

                // Only use HTTPS redirection in production
                if (!app.Environment.IsDevelopment())
                {
                    app.UseHttpsRedirection();
                }

                app.UseAuthentication();
                app.UseAuthorization();

                app.UseSerilogUi(options => options.WithRoutePrefix("logs"));

                // Map SignalR Hub with specific CORS policy
                app.MapHub<RescuerHub>("/rescuer-hub").RequireCors("SignalRCorsPolicy");

                app.MapHub<MissionHub>("/mission-hub").RequireCors("SignalRCorsPolicy");
                app.MapHub<ExpertHub>("/hubs/expert").RequireCors("SignalRCorsPolicy");
                app.MapHub<ConsultationHub>("/hubs/consultation").RequireCors("SignalRCorsPolicy");

                // Map Razor pages
                app.MapRazorPages();

                app.MapControllers();

                // Health checks endpoint
                app.MapHealthChecks("/health");

                // Test database connection endpoint
                app.MapGet("/api/test/db", async (SnakeAidDbContext dbContext) =>
                {
                    try
                    {
                        var canConnect = await dbContext.Database.CanConnectAsync();
                        if (canConnect)
                        {
                            var accountCount = await dbContext.MemberProfiles.CountAsync();
                            return Results.Ok(new
                            {
                                status = "Connected",
                                message = "Database connection successful",
                                accountCount,
                                timestamp = DateTime.UtcNow
                            });
                        }
                        return Results.Problem("Cannot connect to database");
                    }
                    catch (Exception ex)
                    {
                        return Results.Problem(
                            detail: ex.Message,
                            title: "Database Connection Failed",
                            statusCode: 500
                        );
                    }
                }).WithTags("Diagnostics");

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
