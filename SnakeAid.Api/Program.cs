using Mapster;
using MapsterMapper;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Scrutor;
using Serilog;
using Serilog.Ui.Core.Extensions;
using Serilog.Ui.SqliteDataProvider.Extensions;
using Serilog.Ui.Web.Extensions;
using SnakeAid.Core.Mappings;
using SnakeAid.Core.Middlewares;
using SnakeAid.Api.DI;
using SQLitePCL;
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
                if (hasSerilogConfig &&
                    string.Equals(builder.Configuration["Serilog:WriteTo:1:Name"], "SQLite", StringComparison.OrdinalIgnoreCase))
                {
                    builder.Configuration["Serilog:WriteTo:1:Args:sqliteDbPath"] = sqliteLogPath;
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

                // builder.Services.AddDbContext<SnakeAidDbContext>(options =>
                // {
                //     options.UseNpgsql(builder.Configuration.GetConnectionString("SupabaseConnection"),
                //         sqlOptions =>
                //         {
                //             sqlOptions.EnableRetryOnFailure(
                //                 5,
                //                 TimeSpan.FromSeconds(30),
                //                 null);
                //         });
                // });


                // // Add IUnitOfWork and UnitOfWork
                // builder.Services.AddScoped<IUnitOfWork<SnakeAidDbContext>, UnitOfWork<SnakeAidDbContext>>();

                // Register Mapster
                var config = TypeAdapterConfig.GlobalSettings;
                MapsterConfig.RegisterMappings();
                builder.Services.AddSingleton(config);
                builder.Services.AddScoped<IMapper, ServiceMapper>();

                builder.Services.AddServices();

                // Register services using Scrutor
                builder.Services.Scan(scan => scan
                    .FromAssemblies(typeof(Program).Assembly, typeof(SnakeAid.Core.Domains.BaseEntity).Assembly)
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
                        // Local dev: both HTTP and HTTPS
                        options.ListenAnyIP(5009);
                        // options.ListenLocalhost(5009); // HTTP
                        options.ListenLocalhost(7026, listenOptions => listenOptions.UseHttps());
                    }
                    else
                    {
                        // Docker/Production: HTTP only
                        options.ListenAnyIP(5009);
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
