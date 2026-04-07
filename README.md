[![Jenkins CICD Status](https://jenkins.duykhiem.id.vn/buildStatus/icon?job=SnakeAid/main&subject=Jenkins%20CI/CD)](https://jenkins.duykhiem.id.vn/job/SnakeAid/job/main/lastBuild/pipeline-overview/) [![Docker Image](https://img.shields.io/docker/v/thekhiem7/snakeaid-api/latest?&logo=docker)](https://hub.docker.com/r/thekhiem7/snakeaid-api) [![Version](https://img.shields.io/github/v/tag/Snake-AID/SnakeAid.Backend)](https://github.com/Snake-AID/SnakeAid.Backend/releases) ![Server Status](https://img.shields.io/website?url=https://dev.snakeaid.tech/health&label=Server%20Status)

# SnakeAid.Backend

**SnakeAid** (Nền tảng hỗ trợ sơ cứu & cứu hộ rắn cắn ứng dụng AI) is a comprehensive backend API infrastructure powering the SnakeAid platform. Designed and built with ASP.NET Core 8, it connects users with experts, rescuers, and community resources related to snake identification, capturing missions, and emergency first aid response.

## Core Functional Modules

The backend supports a multi-actor ecosystem defined by four primary modules:

### 1. Emergency Response & Incident Tracking (Patient)

- **SOS Setup**: Integrates directly with GPS to provide real-time location sharing and emergency dialing (e.g., configuring webhook alerts or SMS).
- **First Aid Guidance**: Serves step-by-step instructions and warnings against prohibited actions (`SymptomConfig`, `SnakebiteIncident`).
- **AI-Powered Identification**: Endpoints connecting to AI models to analyze images of snakes/bites to determine species, toxicity, and severity (`SnakeAIRecognitionResult.cs`).
- **Facility Locator**: Geospatial queries using PostGIS (`NetTopologySuite`) to find nearby antivenom treatment centers.

### 2. Snake Real-Time Rescue Missions (Rescuer)

- **Live Mission Allocation**: Routes rescue requests to nearby rescuers based on proximity (e.g., 5km, 10km radius) and expertise.
- **Tracking & Environment Tracking**: Domains for `CatchingEnvironment` and `CatchingMissionDetail` to record specific capture scenarios and ensure rescuer safety.
- **SignalR Hub Notifications**: Real-time `/rescuer-hub` and `/mission-hub` communication matching victims to respondents and streaming location updates.
- **Rescue Fee Management**: Handles transactional status via the PayOS gateway once a mission completes.

### 3. Expert Consultation & Verification (Expert)

- **Online Booking System**: API endpoints to schedule video or text consultations with verified snake experts.
- **Transactional Integrity**: Uses strict database transactions to lock time slots and prevent double-booking.
- **Payment Gateway Flow**: Generates a secure PayOS checkout link, keeping funds in escrow pending completion, and processes webhooks to `Confirm` or `Cancel` bookings interactively.
- **Species Verification**: Expert portals to manually verify or correct AI-identified snake instances.

### 4. Administrative Operations & Community (Admin)

- **Data Management**: Aggregates reporting metrics for community safety, manages treatment facility data, and handles user role permissions.
- **Financial Reconciliation**: Tracks total platform revenue, processes rescue and consultation splits, and calculates insurance payouts and net income.

---

## Technologies & Architecture

This project strictly adheres to **Clean Architecture / Onion Architecture** principles with distinct layers:

- **SnakeAid.Core**: Domain models, Entities, Enums, DTOs (Requests/Responses), mappings, and exceptions.
- **SnakeAid.Repository**: Data access layer via **Entity Framework Core**, implementing the Generic Repository Pattern and `IUnitOfWork`.
- **SnakeAid.Service**: Business logic encapsulation interacting with Repositories.
- **SnakeAid.Api**: ASP.NET Core Controllers, Swagger integrations, SignalR Hubs, and Razor Pages UI.

### Key Tools & Integrations

- **Framework**: .NET 8 SDK
- **Database**: PostgreSQL hosted on **Supabase** (with Npgsql & NetTopologySuite for spatial data).
- **ORM**: Entity Framework Core.
- **Mapping**: [Mapster](https://github.com/MapsterMapper/Mapster) for DTO to Entity conversions.
- **Real-Time Communication**: SignalR (`/chat-hub`, `/rescuer-hub`, `/mission-hub`).
- **Payment Gateway**: [PayOS](https://payos.vn/) integration (`SnakeAid.Service.Services.PayOs`).
- **Configuration Management**: Doppler Cloud.
- **Logging & Monitoring**: Serilog with SQLite storage, Health Checks `/health`.
- **Email Providers**: Resend & SMTP configured via factory pattern.

### Transactional Webhook Example (PayOS)

```csharp
[HttpPost("payment-callback")]
public async Task<IActionResult> HandleCallback([FromBody] PayOsWebhookData webhook)
{
    await ExecuteInTransactionAsync(async () => {
        var booking = await Context.Bookings.FindAsync(webhook.OrderCode);

        if (webhook.Success) {
            booking.Status = BookingStatus.Confirmed;
        } else {
            booking.Status = BookingStatus.Cancelled;
            await RollbackAsync();
        }
    });
    return Ok();
}
```

---

## Getting Started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- PostgreSQL Database (with PostGIS extensions for spatial queries)
- Docker (Optional)

### Environment Configuration

1. Clone the repository.
2. Create `.env` or properly duplicate `appsettings.Example.json` into `appsettings.json`.
3. Provide essential external keys:
   - `SupabaseConnection`
   - `PayOS` integration keys (`ClientId`, `ApiKey`, `ChecksumKey`)
   - `DopplerCloud` (Optional based on local execution)

### Running Locally

```bash
# Navigate to API project
cd SnakeAid.Api

# Restore workloads
dotnet restore

# Run API (Listens on ports 8080/8081 locally)
dotnet run
```

**Access Swagger UI:** [http://localhost:8080](http://localhost:8080) / [https://localhost:8081](https://localhost:8081)

---

## Project Structure

```text
SnakeAid.Backend/
├── SnakeAid.Api/         # Controllers, Middlewares, SignalR Hubs, Webhooks
├── SnakeAid.Core/        # Interfaces, DTOs, Entities, Constants, Enums
├── SnakeAid.Repository/  # AppDbContext, Migrations, Repositories, Seeds
├── SnakeAid.Service/     # Business Layer, Third-party service integrations
├── File configurations   # Dockerfile, Jenkinsfile, docker-compose.yml
└── README.md
```

## License & Contributions

Maintain standard pull-request policies. Verify code formatting and CI/CD pipelines before integration.
