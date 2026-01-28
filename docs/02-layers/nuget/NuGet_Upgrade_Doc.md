# NuGet Package Upgrade Documentation

This document lists all NuGet packages defined in `Directory.Packages.props` and their current status regarding updates. The status is determined by checking for newer versions available on NuGet.org.

## Package Status

| Package | Status | Current Version | Latest Version | Notes |
|---------|--------|-----------------|----------------|-------|
| AutoBogus | Up to date | 2.13.1 | 2.13.1 | |
| Bogus | Up to date | 35.5.0 | 35.5.0 | |
| CloudinaryDotNet | Up to date | 1.27.4 | 1.27.4 | |
| FirebaseAdmin | Up to date | 3.4.0 | 3.4.0 | |
| MailKit | Up to date | 4.11.0 | 4.11.0 | |
| Mapster | Up to date | 7.4.0 | 7.4.0 | |
| Mapster.DependencyInjection | Up to date | 1.0.1 | 1.0.1 | |
| Microsoft.AspNetCore.Authentication | Up to date | 2.3.0 | 2.3.0 | |
| Microsoft.AspNetCore.Http | Up to date | 2.3.0 | 2.3.0 | |
| Microsoft.AspNetCore.Http.Extensions | Up to date | 2.3.0 | 2.3.0 | |
| Microsoft.EntityFrameworkCore | Up to date | 8.0.11 | 8.0.11 | |
| Microsoft.EntityFrameworkCore.Tools | Up to date | 8.0.11 | 8.0.11 | |
| Microsoft.Extensions.ServiceDiscovery.Yarp | Up to date | 9.4.1 | 9.4.1 | |
| MimeKit | Up to date | 4.11.0 | 4.11.0 | |
| NETCore.MailKit | Up to date | 2.1.0 | 2.1.0 | |
| Npgsql | Up to date | 8.0.7 | 8.0.7 | |
| Npgsql.EntityFrameworkCore.PostgreSQL | Up to date | 8.0.11 | 8.0.11 | |
| Npgsql.EntityFrameworkCore.PostgreSQL.NetTopologySuite | Up to date | 8.0.11 | 8.0.11 | |
| PreMailer.Net | Up to date | 2.6.0 | 2.6.0 | |
| RazorLight | Up to date | 2.3.1 | 2.3.1 | |
| Refit | Up to date | 7.2.22 | 7.2.22 | |
| Refit.HttpClientFactory | Up to date | 7.0.0 | 7.0.0 | |
| Scrutor | Up to date | 7.0.0 | 7.0.0 | Updated from 4.2.2 - compatible with .NET 8 |
| Serilog.Enrichers.Environment | Up to date | 3.0.1 | 3.0.1 | |
| Serilog.Enrichers.Span | Up to date | 3.1.0 | 3.1.0 | |
| Serilog.Sinks.Console | Up to date | 6.1.1 | 6.1.1 | |
| Serilog.Sinks.Grafana.Loki | Up to date | 8.3.2 | 8.3.2 | |
| Serilog.Sinks.SQLite | Up to date | 6.0.0 | 6.0.0 | |
| Serilog.UI | Up to date | 3.2.0 | 3.2.0 | |
| Serilog.UI.SqliteProvider | Up to date | 1.1.0 | 1.1.0 | |
| SQLitePCLRaw.bundle_e_sqlite3 | Up to date | 3.0.2 | 3.0.2 | |
| Swashbuckle.AspNetCore.Annotations | Up to date | 8.0.0 | 8.0.0 | |
| Swashbuckle.AspNetCore.Filters | Up to date | 8.0.0 | 8.0.0 | |
| VietQRHelper | Up to date | 1.0.2 | 1.0.2 | |
| Yarp.ReverseProxy | Up to date | 2.3.0 | 2.3.0 | |
| Carter | possible highest net8 | 8.0.0 | 10.0.0 | Major version update (8 → 10). Check compatibility. |
| Microsoft.AspNetCore.Authentication.JwtBearer | possible highest net8 | 8.0.23 | 10.0.2 | Major version update (8 → 10). Check compatibility. |
| Microsoft.EntityFrameworkCore.Design | possible highest net8 | 8.0.23 | 10.0.2 | Major version update (8 → 10). Check compatibility. |
| Serilog.AspNetCore | possible highest net8 | 8.0.3 | 10.0.0 | |
| Serilog.Settings.Configuration | possible highest net8 | 8.0.4 | 10.0.0 | |
| Swashbuckle.AspNetCore | possible highest net8 | 8.1.4 | 10.1.0 | Major version update (8 → 10). Check compatibility. |

## Upgrade Recommendations

- **Outdated Packages**: 6 packages are outdated.
- **Major Updates**: Several packages have major version updates (e.g., Carter 8→10, Serilog packages 8→10). These may introduce breaking changes. Review release notes and test thoroughly before upgrading.

## How to Upgrade

1. Update the version in `Directory.Packages.props`.
2. Run `dotnet restore` to update the packages.
3. Build and test the application.
4. For major updates, review breaking changes in the package's changelog.

## Last Checked

January 23, 2026</content>
<parameter name="filePath">d:\SourceCode\Snake_AID\SnakeAid.Backend\NuGet_Upgrade_Doc.md