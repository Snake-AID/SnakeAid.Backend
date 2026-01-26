# Domain-Driven Design Migration - Usage Guide

## Development Workflow

### Prerequisites
- .NET 8.0 SDK
- PostgreSQL with PostGIS extension
- Redis (for caching)
- Visual Studio 2022 or VS Code

### Setup Development Environment

#### 1. Clone and Setup
```bash
git clone <repository-url>
cd SnakeAid.Backend
dotnet restore
```

#### 2. Database Setup
```sql
-- PostgreSQL with PostGIS
CREATE DATABASE SnakeAidDDD;
CREATE EXTENSION postgis;

-- Connection string in appsettings.json
"ConnectionStrings": {
  "DefaultConnection": "Host=localhost;Database=SnakeAidDDD;Username=postgres;Password=your_password"
}
```

#### 3. Run Migrations
```bash
# Emergency Rescue Context
dotnet ef database update --context EmergencyRescueDbContext

# Snake Catching Context  
dotnet ef database update --context SnakeCatchingDbContext

# Other contexts...
```

---

## Creating New Bounded Context

### Step 1: Create Project Structure
```bash
mkdir -p "src/Contexts/NewContext"
cd "src/Contexts/NewContext"

# Domain Layer
dotnet new classlib -n SnakeAid.NewContext.Domain
mkdir -p SnakeAid.NewContext.Domain/{Entities,ValueObjects,Events,Services,Repositories}

# Application Layer
dotnet new classlib -n SnakeAid.NewContext.Application  
mkdir -p SnakeAid.NewContext.Application/{Commands,Queries,DTOs,Mappings,EventHandlers,Services}
mkdir -p SnakeAid.NewContext.Application/DTOs/{Requests,Responses}

# Infrastructure Layer
dotnet new classlib -n SnakeAid.NewContext.Infrastructure
mkdir -p SnakeAid.NewContext.Infrastructure/{Persistence,Services,EventBus}
mkdir -p SnakeAid.NewContext.Infrastructure/Persistence/{Repositories,Configurations}

# API Layer (Optional - có thể dùng chung API Gateway)
dotnet new webapi -n SnakeAid.NewContext.API
mkdir -p SnakeAid.NewContext.API/{Controllers,Middleware,Filters}
```

## 📂 Folder Naming Conventions

### Domain Layer Folders:
- **`Entities/`** - Domain entities và aggregate roots (thay vì `Domains/`)
- **`ValueObjects/`** - Immutable value objects 
- **`Events/`** - Domain events
- **`Services/`** - Domain service interfaces
- **`Repositories/`** - Repository interfaces (chỉ interfaces)

### Application Layer Folders:
- **`Commands/`** - Write operations theo CQRS pattern
- **`Queries/`** - Read operations theo CQRS pattern  
- **`DTOs/Requests/`** - Input DTOs (thay thế cho `DomainRequestHub`)
- **`DTOs/Responses/`** - Output DTOs (thay thế cho `DomainResponseHub`) 
- **`Mappings/`** - AutoMapper profiles
- **`EventHandlers/`** - Domain event handlers
- **`Services/`** - Application services (orchestration)

### Infrastructure Layer Folders:
- **`Persistence/`** - Database concerns (DbContext, migrations)
- **`Persistence/Repositories/`** - Repository implementations
- **`Persistence/Configurations/`** - EF Core configurations
- **`Services/`** - External service implementations
- **`EventBus/`** - Message bus implementations

### API Layer Folders:
- **`Controllers/`** - HTTP endpoints
- **`Middleware/`** - Custom middleware
- **`Filters/`** - Action filters

### Step 2: Reference SharedKernel
```xml
<!-- In each project file -->
<ProjectReference Include="..\..\SnakeAid.SharedKernel\SnakeAid.SharedKernel.csproj" />
```

### Step 3: Create Aggregate Root
```csharp
// Example: NewAggregate.cs
using SnakeAid.SharedKernel.Domain;

namespace SnakeAid.NewContext.Domain.Aggregates.NewAggregate;

public class NewAggregate : AggregateRoot<Guid>
{
    // Properties
    public string Name { get; private set; }
    public AggregateStatus Status { get; private set; }
    
    // Constructor
    private NewAggregate() { }
    
    // Factory method
    public static NewAggregate Create(string name)
    {
        var aggregate = new NewAggregate
        {
            Id = Guid.NewGuid(),
            Name = name,
            Status = AggregateStatus.Active,
            CreatedAt = DateTime.UtcNow
        };
        
        aggregate.AddDomainEvent(new NewAggregateCreatedEvent(aggregate.Id, name));
        return aggregate;
    }
    
    // Business methods
    public void UpdateName(string newName)
    {
        if (string.IsNullOrWhiteSpace(newName))
            throw new DomainException("Name cannot be empty");
            
        var oldName = Name;
        Name = newName;
        UpdatedAt = DateTime.UtcNow;
        
        AddDomainEvent(new NewAggregateUpdatedEvent(Id, oldName, newName));
    }
}
```

---

## Working with Domain Events

### 1. Creating Domain Events
```csharp
// filepath: src/Contexts/NewContext/SnakeAid.NewContext.Domain/Events/NewAggregateCreatedEvent.cs
using SnakeAid.SharedKernel.Domain;

namespace SnakeAid.NewContext.Domain.Events;

public record NewAggregateCreatedEvent(
    Guid AggregateId,
    string Name
) : DomainEvent;
```

### 2. Event Handlers
```csharp
// filepath: src/Contexts/NewContext/SnakeAid.NewContext.Application/EventHandlers/NewAggregateCreatedEventHandler.cs
using MediatR;
using SnakeAid.NewContext.Domain.Events;

namespace SnakeAid.NewContext.Application.EventHandlers;

public class NewAggregateCreatedEventHandler : INotificationHandler<NewAggregateCreatedEvent>
{
    private readonly ILogger<NewAggregateCreatedEventHandler> _logger;

    public NewAggregateCreatedEventHandler(ILogger<NewAggregateCreatedEventHandler> logger)
    {
        _logger = logger;
    }

    public Task Handle(NewAggregateCreatedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("New aggregate created: {AggregateId} - {Name}", 
            notification.AggregateId, notification.Name);
            
        // Additional logic here (send notification, update read model, etc.)
        
        return Task.CompletedTask;
    }
}
```

### 3. Cross-Context Events
```csharp
// When EmergencyCase is created, notify other contexts
public class EmergencyCaseCreatedEventHandler : INotificationHandler<EmergencyCaseCreatedEvent>
{
    private readonly ILocationTrackingService _locationService;
    private readonly INotificationService _notificationService;

    public async Task Handle(EmergencyCaseCreatedEvent notification, CancellationToken cancellationToken)
    {
        // Start location tracking
        await _locationService.StartTrackingAsync(notification.EmergencyId, SessionType.Emergency);
        
        // Send push notifications to nearby rescuers
        await _notificationService.NotifyNearbyRescuersAsync(
            notification.Latitude, 
            notification.Longitude,
            notification.Severity);
    }
}
```

---

## CQRS Implementation

### 1. Command Pattern
```csharp
// Command
public record UpdateEmergencyStatusCommand(
    Guid EmergencyId,
    EmergencyStatus NewStatus,
    string? Notes
) : IRequest<bool>;

// Handler
public class UpdateEmergencyStatusHandler : IRequestHandler<UpdateEmergencyStatusCommand, bool>
{
    private readonly IEmergencyCaseRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateEmergencyStatusHandler(
        IEmergencyCaseRepository repository,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(UpdateEmergencyStatusCommand request, CancellationToken cancellationToken)
    {
        var emergencyCase = await _repository.GetByIdAsync(request.EmergencyId, cancellationToken);
        
        if (emergencyCase == null)
            return false;

        // Business logic through domain methods
        switch (request.NewStatus)
        {
            case EmergencyStatus.Completed:
                emergencyCase.CompleteEmergency(request.Notes ?? "Completed");
                break;
            case EmergencyStatus.Cancelled:
                emergencyCase.CancelEmergency(request.Notes ?? "Cancelled");
                break;
            default:
                throw new DomainException($"Cannot directly set status to {request.NewStatus}");
        }

        await _repository.UpdateAsync(emergencyCase, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}
```

### 2. Query Pattern
```csharp
// Query
public record GetEmergencyDashboardQuery(
    Guid RescuerId,
    int PageSize = 10,
    int PageNumber = 1
) : IRequest<EmergencyDashboardResponse>;

// Response DTO (Read Model)
public class EmergencyDashboardResponse
{
    public List<EmergencySummary> ActiveEmergencies { get; set; } = new();
    public List<EmergencySummary> MyAssignedEmergencies { get; set; } = new();
    public EmergencyStatistics Statistics { get; set; } = new();
    public int TotalPages { get; set; }
}

public class EmergencySummary
{
    public Guid Id { get; set; }
    public string VictimName { get; set; } = "";
    public string Location { get; set; } = "";
    public SeverityLevel Severity { get; set; }
    public EmergencyStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public double? DistanceKm { get; set; }
}

// Handler với optimized read
public class GetEmergencyDashboardHandler : IRequestHandler<GetEmergencyDashboardQuery, EmergencyDashboardResponse>
{
    private readonly IEmergencyReadModelService _readModelService;

    public GetEmergencyDashboardHandler(IEmergencyReadModelService readModelService)
    {
        _readModelService = readModelService;
    }

    public async Task<EmergencyDashboardResponse> Handle(
        GetEmergencyDashboardQuery request, 
        CancellationToken cancellationToken)
    {
        return await _readModelService.GetDashboardAsync(
            request.RescuerId,
            request.PageSize,
            request.PageNumber,
            cancellationToken);
    }
}
```

---

## Repository Patterns

### 1. Generic Repository
```csharp
// filepath: src/SnakeAid.SharedKernel/Infrastructure/EfRepository.cs
using Microsoft.EntityFrameworkCore;
using SnakeAid.SharedKernel.Domain;

namespace SnakeAid.SharedKernel.Infrastructure;

public abstract class EfRepository<TEntity, TId, TContext> : IRepository<TEntity, TId>
    where TEntity : AggregateRoot<TId>
    where TId : IEquatable<TId>
    where TContext : DbContext
{
    protected readonly TContext Context;
    protected readonly DbSet<TEntity> DbSet;

    protected EfRepository(TContext context)
    {
        Context = context;
        DbSet = context.Set<TEntity>();
    }

    public virtual async Task<TEntity?> GetByIdAsync(TId id, CancellationToken cancellationToken = default)
    {
        return await DbSet.FindAsync(new object[] { id }, cancellationToken);
    }

    public virtual async Task AddAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        await DbSet.AddAsync(entity, cancellationToken);
    }

    public virtual Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        DbSet.Update(entity);
        return Task.CompletedTask;
    }

    public virtual Task DeleteAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        DbSet.Remove(entity);
        return Task.CompletedTask;
    }
}
```

### 2. Specific Repository with Business Methods
```csharp
// Interface
public interface IEmergencyCaseRepository : IRepository<EmergencyCase, Guid>
{
    Task<List<EmergencyCase>> GetActiveEmergenciesAsync(CancellationToken cancellationToken = default);
    Task<List<EmergencyCase>> GetNearbyEmergenciesAsync(double latitude, double longitude, double radiusKm, CancellationToken cancellationToken = default);
    Task<EmergencyCase?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);
}

// Implementation
public class EmergencyCaseRepository : EfRepository<EmergencyCase, Guid, EmergencyRescueDbContext>, IEmergencyCaseRepository
{
    public EmergencyCaseRepository(EmergencyRescueDbContext context) : base(context) { }

    public async Task<List<EmergencyCase>> GetActiveEmergenciesAsync(CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(e => e.Status == EmergencyStatus.Pending || e.Status == EmergencyStatus.InProgress)
            .OrderByDescending(e => e.Severity)
            .ThenBy(e => e.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<EmergencyCase>> GetNearbyEmergenciesAsync(
        double latitude, 
        double longitude, 
        double radiusKm, 
        CancellationToken cancellationToken = default)
    {
        // PostGIS spatial query
        var point = new NetTopologySuite.Geometries.Point(longitude, latitude) { SRID = 4326 };
        
        return await DbSet
            .Where(e => e.Status == EmergencyStatus.Pending)
            .Where(e => e.Location.LocationCoordinates.Distance(point) <= radiusKm * 1000) // Convert to meters
            .OrderBy(e => e.Location.LocationCoordinates.Distance(point))
            .ToListAsync(cancellationToken);
    }

    public async Task<EmergencyCase?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(e => e.RescuerRequests)
            .Include(e => e.RescueMission)
            .AsSplitQuery()
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }
}
```

---

## Unit of Work Pattern

### 1. UnitOfWork Implementation
```csharp
// filepath: src/Contexts/EmergencyRescue/SnakeAid.EmergencyRescue.Infrastructure/Persistence/EmergencyRescueUnitOfWork.cs
using SnakeAid.SharedKernel.Domain;
using SnakeAid.SharedKernel.Infrastructure;

namespace SnakeAid.EmergencyRescue.Infrastructure.Persistence;

public class EmergencyRescueUnitOfWork : IUnitOfWork
{
    private readonly EmergencyRescueDbContext _context;
    private readonly IDomainEventDispatcher _eventDispatcher;

    public EmergencyRescueUnitOfWork(
        EmergencyRescueDbContext context,
        IDomainEventDispatcher eventDispatcher)
    {
        _context = context;
        _eventDispatcher = eventDispatcher;
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Dispatch domain events before saving
        await _eventDispatcher.DispatchEventsAsync(_context, cancellationToken);
        
        // Save changes
        return await _context.SaveChangesAsync(cancellationToken);
    }
}
```

---

## Testing Strategies

### 1. Domain Unit Tests
```csharp
// filepath: tests/EmergencyRescue.UnitTests/Domain/EmergencyCaseTests.cs
using SnakeAid.EmergencyRescue.Domain.Aggregates.EmergencyCase;
using SnakeAid.EmergencyRescue.Domain.Events;

namespace EmergencyRescue.UnitTests.Domain;

public class EmergencyCaseTests
{
    [Test]
    public void Create_ShouldCreateEmergencyCase_WithValidData()
    {
        // Arrange
        var victim = new VictimInfo("John Doe", "0123456789", "Snake bite on leg");
        var location = new EmergencyLocation(10.762622, 106.660172, "District 1, HCMC");
        var severity = SeverityLevel.High;

        // Act
        var emergencyCase = EmergencyCase.Create(victim, location, severity);

        // Assert
        emergencyCase.Should().NotBeNull();
        emergencyCase.Id.Should().NotBeEmpty();
        emergencyCase.Status.Should().Be(EmergencyStatus.Pending);
        emergencyCase.Severity.Should().Be(SeverityLevel.High);
        
        // Domain event should be raised
        var domainEvent = emergencyCase.DomainEvents.OfType<EmergencyCaseCreatedEvent>().FirstOrDefault();
        domainEvent.Should().NotBeNull();
        domainEvent!.EmergencyId.Should().Be(emergencyCase.Id);
    }

    [Test]
    public void AssignRescuer_ShouldThrowException_WhenAlreadyAssigned()
    {
        // Arrange
        var emergencyCase = CreateValidEmergencyCase();
        var rescuerId1 = Guid.NewGuid();
        var rescuerId2 = Guid.NewGuid();
        
        emergencyCase.AssignRescuer(rescuerId1, "Rescuer 1");

        // Act & Assert
        var action = () => emergencyCase.AssignRescuer(rescuerId2, "Rescuer 2");
        action.Should().Throw<DomainException>()
            .WithMessage("Rescuer already assigned");
    }

    private EmergencyCase CreateValidEmergencyCase()
    {
        var victim = new VictimInfo("Test Victim", "0123456789");
        var location = new EmergencyLocation(10.762622, 106.660172, "Test Address");
        return EmergencyCase.Create(victim, location, SeverityLevel.Medium);
    }
}
```

### 2. Application Layer Integration Tests
```csharp
// filepath: tests/EmergencyRescue.IntegrationTests/Application/CreateEmergencyCaseHandlerTests.cs
using Microsoft.EntityFrameworkCore;
using SnakeAid.EmergencyRescue.Application.Commands.CreateEmergencyCase;

namespace EmergencyRescue.IntegrationTests.Application;

public class CreateEmergencyCaseHandlerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public CreateEmergencyCaseHandlerTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Test]
    public async Task Handle_ShouldCreateEmergencyCase_WithValidCommand()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<IRequestHandler<CreateEmergencyCaseCommand, Guid>>();
        var context = scope.ServiceProvider.GetRequiredService<EmergencyRescueDbContext>();

        var command = new CreateEmergencyCaseCommand(
            "John Doe",
            "0123456789",
            "Snake bite on leg",
            "Swelling and pain",
            10.762622,
            106.660172,
            "District 1, HCMC",
            "Near Ben Thanh Market",
            null);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeEmpty();

        var emergencyCase = await context.EmergencyCases.FindAsync(result);
        emergencyCase.Should().NotBeNull();
        emergencyCase!.Victim.Name.Should().Be("John Doe");
        emergencyCase.Status.Should().Be(EmergencyStatus.Pending);
    }
}
```

### 3. API Integration Tests
```csharp
// filepath: tests/SnakeAid.API.IntegrationTests/Controllers/EmergencyControllerTests.cs
public class EmergencyControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public EmergencyControllerTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Test]
    public async Task CreateEmergency_ShouldReturn201_WithValidRequest()
    {
        // Arrange
        var request = new CreateEmergencyRequest(
            "John Doe",
            "0123456789",
            "Snake bite on leg",
            "Swelling and pain",
            10.762622,
            106.660172,
            "District 1, HCMC",
            "Near Ben Thanh Market",
            null);

        // Act
        var response = await _client.PostAsJsonAsync("/api/emergency", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        
        var emergencyId = await response.Content.ReadFromJsonAsync<Guid>();
        emergencyId.Should().NotBeEmpty();
    }
}
```

---

## Performance Optimization

### 1. Database Optimizations
```csharp
// EF Core Configuration
public class EmergencyCaseConfiguration : IEntityTypeConfiguration<EmergencyCase>
{
    public void Configure(EntityTypeBuilder<EmergencyCase> builder)
    {
        builder.HasKey(e => e.Id);
        
        // Indexes for common queries
        builder.HasIndex(e => e.Status);
        builder.HasIndex(e => new { e.Status, e.CreatedAt });
        builder.HasIndex(e => new { e.Status, e.Severity });
        
        // Spatial index for location queries
        builder.Property(e => e.Location.LocationCoordinates)
            .HasColumnType("geometry(Point, 4326)");
        builder.HasIndex(e => e.Location.LocationCoordinates)
            .HasMethod("gist");

        // Value object mapping
        builder.OwnsOne(e => e.Victim, victim =>
        {
            victim.Property(v => v.Name).HasMaxLength(200);
            victim.Property(v => v.PhoneNumber).HasMaxLength(20);
        });

        // Collection mapping
        builder.HasMany(e => e.RescuerRequests)
            .WithOne()
            .HasForeignKey("EmergencyCaseId")
            .OnDelete(DeleteBehavior.Cascade);
    }
}
```

### 2. Caching Strategy
```csharp
// filepath: src/SnakeAid.SharedKernel/Infrastructure/CachedRepository.cs
public class CachedEmergencyCaseRepository : IEmergencyCaseRepository
{
    private readonly IEmergencyCaseRepository _repository;
    private readonly IMemoryCache _cache;
    private readonly TimeSpan _cacheExpiry = TimeSpan.FromMinutes(10);

    public CachedEmergencyCaseRepository(
        IEmergencyCaseRepository repository,
        IMemoryCache cache)
    {
        _repository = repository;
        _cache = cache;
    }

    public async Task<EmergencyCase?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"emergency-{id}";
        
        if (_cache.TryGetValue(cacheKey, out EmergencyCase? cached))
            return cached;

        var emergency = await _repository.GetByIdAsync(id, cancellationToken);
        
        if (emergency != null)
        {
            _cache.Set(cacheKey, emergency, _cacheExpiry);
        }

        return emergency;
    }

    // Invalidate cache on updates
    public async Task UpdateAsync(EmergencyCase entity, CancellationToken cancellationToken = default)
    {
        await _repository.UpdateAsync(entity, cancellationToken);
        _cache.Remove($"emergency-{entity.Id}");
    }
}
```

---

## Deployment & DevOps

### 1. Docker Setup
```dockerfile
# Dockerfile for each context
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

COPY bin/Release/net8.0/publish/ .

ENTRYPOINT ["dotnet", "SnakeAid.EmergencyRescue.API.dll"]
```

### 2. Kubernetes Deployment
```yaml
# k8s/emergency-rescue-deployment.yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: emergency-rescue-api
spec:
  replicas: 3
  selector:
    matchLabels:
      app: emergency-rescue-api
  template:
    metadata:
      labels:
        app: emergency-rescue-api
    spec:
      containers:
      - name: emergency-rescue-api
        image: snakeaid/emergency-rescue-api:latest
        ports:
        - containerPort: 80
        env:
        - name: ConnectionStrings__DefaultConnection
          valueFrom:
            secretKeyRef:
              name: database-secret
              key: connection-string
```

---

**Next**: [Agent Prompt](ddd-migration.prompt.md)