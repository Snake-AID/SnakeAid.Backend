# Nuget Toys Tracker

Track implementation and configuration work for packages already referenced in CPM and csproj files.
Goal: mirror EzyFix behavior and config from `D:\SourceCode\EZYFIX\EzyFix.aspnet`.

## Legend
- [ ] Not started
- [~] In progress
- [x] Done

## Data seeding (Repository)
- [ ] AutoBogus/Bogus: port seeding entrypoint and runner pipeline (EzyFixSeeder -> SeedRunner) ([EzyFixSeeder](D:\SourceCode\EZYFIX\EzyFix.aspnet\EzyFix.Data\Seed\EzyFixSeeder.cs)) ([SeedRunner](D:\SourceCode\EZYFIX\EzyFix.aspnet\EzyFix.Data\Seed\SeederCore\SeedRunner.cs))
- [ ] AutoBogus/Bogus: implement SeedOptions validation, environment guard, and skip-if-users-exist logic ([SeedRunner](D:\SourceCode\EZYFIX\EzyFix.aspnet\EzyFix.Data\Seed\SeederCore\SeedRunner.cs))
- [ ] AutoBogus/Bogus: configure AutoFaker depth + deterministic seed + locale Faker ([FakerConfig](D:\SourceCode\EZYFIX\EzyFix.aspnet\EzyFix.Data\Seed\SeederCore\FakerConfig.cs))
- [ ] AutoBogus/Bogus: match module order (RoleSeeder -> AdminSeeder -> SupporterSeeder -> DefaultAccountSeeder -> CatalogSeeder -> VoucherSeeder -> UserSeeder -> TechnicianSeeder -> AppointmentSeeder -> PaymentSeeder -> VoucherUsageSeeder -> WalletTransactionSeeder -> ReviewSeeder -> DisputeSeeder -> PayoutSeeder) ([SeedRunner](D:\SourceCode\EZYFIX\EzyFix.aspnet\EzyFix.Data\Seed\SeederCore\SeedRunner.cs))
- [ ] AutoBogus/Bogus: add API flags `--seed` and `--seed-force` with default SeedOptions values (RandomSeed=2025, Locale=en, counts/ratios) ([Program](D:\SourceCode\EZYFIX\EzyFix.aspnet\EzyFix.API\Program.cs))

## Minimal API (API)
- [ ] Carter: register `services.AddCarter()` ([Program](D:\SourceCode\EZYFIX\EzyFix.aspnet\EzyFix.API\Program.cs))
- [ ] Carter: map `app.MapCarter()` ([Program](D:\SourceCode\EZYFIX\EzyFix.aspnet\EzyFix.API\Program.cs))
- [ ] Carter: implement `CarterModule` routes (example) ([PasswordToolsModule](D:\SourceCode\EZYFIX\EzyFix.aspnet\EzyFix.API\Modules\PasswordToolsModule.cs))

## Uploads (Service)
- [ ] CloudinaryDotNet: add Cloudinary settings (CloudName, ApiKey, ApiSecret) in appsettings ([appsettings.json](D:\SourceCode\EZYFIX\EzyFix.aspnet\EzyFix.API\appsettings.json))
- [ ] CloudinaryDotNet: validate settings on startup and build Cloudinary client ([CloudinaryService](D:\SourceCode\EZYFIX\EzyFix.aspnet\EzyFix.Service\Services\Implements\CloudinaryService.cs))
- [ ] CloudinaryDotNet: implement upload logic (extension + size limits, SecureUrl checks, `UserId` claim) ([CloudinaryService](D:\SourceCode\EZYFIX\EzyFix.aspnet\EzyFix.Service\Services\Implements\CloudinaryService.cs))
- [ ] CloudinaryDotNet: register service via Scrutor scanning in API ([Program](D:\SourceCode\EZYFIX\EzyFix.aspnet\EzyFix.API\Program.cs))

## HTTP client wrapper (Service)
- [ ] Refit/Refit.HttpClientFactory: add LocationIq config (BaseUrl, ApiKeys, timeouts, Orchestrator) in appsettings ([appsettings.json](D:\SourceCode\EZYFIX\EzyFix.aspnet\EzyFix.API\appsettings.json))
- [ ] Refit/Refit.HttpClientFactory: define API interface(s) with Refit attributes ([LocationIqApi](D:\SourceCode\EZYFIX\EzyFix.aspnet\EzyFix.Service\Services\LocationIq\Api\LocationIqApi.cs))
- [ ] Refit/Refit.HttpClientFactory: register AddRefitClient with base URL, timeout, headers ([LocationIqServiceCollectionExtensions](D:\SourceCode\EZYFIX\EzyFix.aspnet\EzyFix.Service\Extension\LocationIqServiceCollectionExtensions.cs))
- [ ] Refit/Refit.HttpClientFactory: call `services.AddLocationIqOrchestrator(configuration)` ([Program](D:\SourceCode\EZYFIX\EzyFix.aspnet\EzyFix.API\Program.cs))

## Email (Service)
- [ ] MailKit/MimeKit/NETCore.MailKit: add EmailSettings config and provider selection (Resend/SMTP) in appsettings ([appsettings.json](D:\SourceCode\EZYFIX\EzyFix.aspnet\EzyFix.API\appsettings.json))
- [ ] MailKit/MimeKit/NETCore.MailKit: implement provider selection and fallback strategy ([EmailProviderService](D:\SourceCode\EZYFIX\EzyFix.aspnet\EzyFix.Service\Services\Implements\Email\Providers\EmailProviderService.cs))
- [ ] NETCore.MailKit: implement SMTP sender (EzyFix uses System.Net.Mail) ([SmtpEmailSender](D:\SourceCode\EZYFIX\EzyFix.aspnet\EzyFix.Service\Services\Implements\Email\Providers\SmtpEmailSender.cs))
- [ ] MailKit/MimeKit: implement Resend sender for HTTP provider fallback ([ResendEmailSender](D:\SourceCode\EZYFIX\EzyFix.aspnet\EzyFix.Service\Services\Implements\Email\Providers\ResendEmailSender.cs))
- [ ] RazorLight: render templates from file system with memory cache ([EmailTemplateService](D:\SourceCode\EZYFIX\EzyFix.aspnet\EzyFix.Service\Services\Implements\Email\EmailTemplateService.cs))
- [ ] PreMailer.Net: inline CSS for HTML templates before send ([EmailTemplateService](D:\SourceCode\EZYFIX\EzyFix.aspnet\EzyFix.Service\Services\Implements\Email\EmailTemplateService.cs))
- [ ] RazorLight: ensure templates are copied to output for runtime rendering ([EzyFix.BLL.csproj](D:\SourceCode\EZYFIX\EzyFix.aspnet\EzyFix.Service\EzyFix.BLL.csproj))
- [ ] NETCore.MailKit: register email services via Scrutor and add `services.AddHttpClient()` ([Program](D:\SourceCode\EZYFIX\EzyFix.aspnet\EzyFix.API\Program.cs))
- [ ] MailKit/MimeKit/NETCore.MailKit: note EzyFix references packages but SMTP uses System.Net.Mail (no MailKit usage) ([SmtpEmailSender](D:\SourceCode\EZYFIX\EzyFix.aspnet\EzyFix.Service\Services\Implements\Email\Providers\SmtpEmailSender.cs))

## Payment QR (Service)
- [ ] VietQRHelper: implement bank directory cache from `BankApp.BanksObject` ([BankDirectoryService](D:\SourceCode\EZYFIX\EzyFix.aspnet\EzyFix.Service\Services\Implements\BankDirectoryService.cs))
- [ ] VietQRHelper: implement adapter using `QRPay.InitVietQR(...).Build()` with fallback payload ([VietQrAdapter](D:\SourceCode\EZYFIX\EzyFix.aspnet\EzyFix.Service\Services\Implements\VietQrAdapter.cs))
- [ ] VietQRHelper: optional QR image base64 generation uses QRCoder (not in CPM) ([VietQrAdapter](D:\SourceCode\EZYFIX\EzyFix.aspnet\EzyFix.Service\Services\Implements\VietQrAdapter.cs))

## Logging (API)
- [x] Serilog.Settings.Configuration: add Serilog section in appsettings (Using/MinimumLevel/Enrich/WriteTo) ([appsettings.json](D:\SourceCode\EZYFIX\EzyFix.aspnet\EzyFix.API\appsettings.json))
- [x] Serilog.AspNetCore + Serilog.Settings.Configuration: configure bootstrap logger + `builder.Host.UseSerilog(...ReadFrom.Configuration...)` ([Program](D:\SourceCode\EZYFIX\EzyFix.aspnet\EzyFix.API\Program.cs))
- [x] SQLitePCLRaw.bundle_e_sqlite3: call `SQLitePCL.Batteries_V2.Init()` before Serilog uses SQLite ([Program](D:\SourceCode\EZYFIX\EzyFix.aspnet\EzyFix.API\Program.cs))
- [x] Serilog.Sinks.SQLite: set sqlite log path `logs/logs.db` and override `Serilog:WriteTo:1:Args:sqliteDbPath` ([Program](D:\SourceCode\EZYFIX\EzyFix.aspnet\EzyFix.API\Program.cs))
- [x] Serilog.Sinks.Console: configure console output template in Serilog config ([Program](D:\SourceCode\EZYFIX\EzyFix.aspnet\EzyFix.API\Program.cs))
- [x] Serilog.Enrichers.Environment + Serilog.Enrichers.Span: configure enrichers in Serilog config (WithMachineName/WithEnvironmentName/WithSpan) ([Program](D:\SourceCode\EZYFIX\EzyFix.aspnet\EzyFix.API\Program.cs))
- [x] Serilog.UI + Serilog.UI.SqliteProvider: register Serilog UI and map `/logs` ([Program](D:\SourceCode\EZYFIX\EzyFix.aspnet\EzyFix.API\Program.cs))
- [ ] (optional) Serilog.Sinks.SQLite + Serilog.UI: run `EnsureSqliteLogSchema` to add/backfill `Message` column when reusing an existing `logs.db` or Serilog.UI errors; skip for fresh log DB ([EnsureSqliteLogSchema](D:\SourceCode\EZYFIX\EzyFix.aspnet\EzyFix.API\Program.cs))
- [x] Serilog.Sinks.Grafana.Loki: add Loki sink config and labels (not in EzyFix config) ([Program](D:\SourceCode\EZYFIX\EzyFix.aspnet\EzyFix.API\Program.cs))

## Dependency scanning (API)
- [ ] Scrutor: mirror scan patterns for services, unit of work, utils, and email services ([Program](D:\SourceCode\EZYFIX\EzyFix.aspnet\EzyFix.API\Program.cs))
