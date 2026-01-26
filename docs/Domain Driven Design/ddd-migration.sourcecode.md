# Domain-Driven Design Migration - Source Code Implementation

## Architecture Overview

```
src/
├── SnakeAid.SharedKernel/              # Common domain building blocks
├── Contexts/
│   ├── EmergencyRescue/               # Core Domain 1
│   │   ├── SnakeAid.EmergencyRescue.Domain/        # Pure Business Logic
│   │   │   ├── Entities/                           # Domain Entities (Aggregate Roots)
│   │   │   ├── ValueObjects/                       # Immutable Value Objects  
│   │   │   ├── Events/                             # Domain Events
│   │   │   ├── Services/                           # Domain Service Interfaces
│   │   │   └── Repositories/                       # Repository Interfaces
│   │   ├── SnakeAid.EmergencyRescue.Application/   # Use Cases & Orchestration
│   │   │   ├── Commands/                           # Write Operations (CQRS)
│   │   │   ├── Queries/                            # Read Operations (CQRS)
│   │   │   ├── DTOs/                               # Data Transfer Objects
│   │   │   │   ├── Requests/                       # Input DTOs (thay DomainRequestHub)
│   │   │   │   └── Responses/                      # Output DTOs (thay DomainResponseHub)
│   │   │   ├── Mappings/                           # AutoMapper Profiles
│   │   │   ├── EventHandlers/                      # Domain Event Handlers
│   │   │   └── Services/                           # Application Services
│   │   ├── SnakeAid.EmergencyRescue.Infrastructure/ # External Concerns
│   │   │   ├── Persistence/                        # Database Layer
│   │   │   ├── Services/                           # External Service Implementations
│   │   │   └── EventBus/                          # Message Bus
│   │   └── SnakeAid.EmergencyRescue.API/          # HTTP Endpoints
│   │       ├── Controllers/                       # REST API Controllers
│   │       ├── Middleware/                        # Cross-cutting concerns
│   │       └── Program.cs
│   ├── SnakeCatching/                 # Core Domain 2  
│   ├── ExpertConsultation/            # Core Domain 3
│   ├── SnakeKnowledge/                # Supporting Domain
│   ├── CommunityAndSocial/            # Supporting Domain
│   ├── Payment/                       # Supporting Domain
│   ├── Identity/                      # Generic Domain
│   └── Communication/                 # Supporting Domain
└── SnakeAid.API/                      # API Gateway (Optional)
```

---

## 1. Shared Kernel Implementation

### Base Domain Classes

#### Entity Base Class
```csharp
// filepath: src/SnakeAid.SharedKernel/Domain/Entity.cs
using System.ComponentModel.DataAnnotations;

namespace SnakeAid.SharedKernel.Domain;

public abstract class Entity<TId> : IEquatable<Entity<TId>>
    where TId : IEquatable<TId>
{
    public TId Id { get; protected set; } = default!;
    
    [Required]
    public DateTime CreatedAt { get; protected set; } = DateTime.UtcNow;
    
    [Required] 
    public DateTime UpdatedAt { get; protected set; } = DateTime.UtcNow;

    protected Entity() { }
    
    protected Entity(TId id)
    {
        Id = id;
    }

    public bool Equals(Entity<TId>? other)
    {
        return other is not null && Id.Equals(other.Id);
    }

    public override bool Equals(object? obj)
    {
        return obj is Entity<TId> entity && Equals(entity);
    }

    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }

    public static bool operator ==(Entity<TId>? left, Entity<TId>? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(Entity<TId>? left, Entity<TId>? right)
    {
        return !Equals(left, right);
    }
}
```

#### Aggregate Root Base Class
```csharp
// filepath: src/SnakeAid.SharedKernel/Domain/AggregateRoot.cs
namespace SnakeAid.SharedKernel.Domain;

public abstract class AggregateRoot<TId> : Entity<TId>
    where TId : IEquatable<TId>
{
    private readonly List<IDomainEvent> _domainEvents = new();

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected AggregateRoot() { }
    
    protected AggregateRoot(TId id) : base(id) { }

    protected void AddDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }

    protected void RemoveDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Remove(domainEvent);
    }
}
```

#### Value Object Base Class
```csharp
// filepath: src/SnakeAid.SharedKernel/Domain/ValueObject.cs
namespace SnakeAid.SharedKernel.Domain;

public abstract class ValueObject : IEquatable<ValueObject>
{
    protected abstract IEnumerable<object> GetEqualityComponents();

    public bool Equals(ValueObject? other)
    {
        return other is not null && 
               GetEqualityComponents().SequenceEqual(other.GetEqualityComponents());
    }

    public override bool Equals(object? obj)
    {
        return obj is ValueObject vo && Equals(vo);
    }

    public override int GetHashCode()
    {
        return GetEqualityComponents()
            .Select(x => x?.GetHashCode() ?? 0)
            .Aggregate((x, y) => x ^ y);
    }

    public static bool operator ==(ValueObject? left, ValueObject? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(ValueObject? left, ValueObject? right)
    {
        return !Equals(left, right);
    }
}
```

#### Domain Event Interface
```csharp
// filepath: src/SnakeAid.SharedKernel/Domain/IDomainEvent.cs  
using MediatR;

namespace SnakeAid.SharedKernel.Domain;

public interface IDomainEvent : INotification
{
    DateTime OccurredOn { get; }
    Guid EventId { get; }
}

public abstract record DomainEvent : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
    public Guid EventId { get; } = Guid.NewGuid();
}
```

### Location Tracking (Shared Kernel)
```csharp
// filepath: src/SnakeAid.SharedKernel/Domain/Location/TrackingSession.cs
using NetTopologySuite.Geometries;

namespace SnakeAid.SharedKernel.Domain.Location;

public class TrackingSession : Entity<Guid>
{
    public Guid SessionId { get; private set; }
    public SessionType SessionType { get; private set; }
    public bool IsActive { get; private set; }
    
    public LocationPoint? MemberLocation { get; private set; }
    public LocationPoint? RescuerLocation { get; private set; }
    
    public DateTime? MemberLastUpdate { get; private set; }
    public DateTime? RescuerLastUpdate { get; private set; }
    
    public double? DistanceMeters { get; private set; }
    public int? EtaMinutes { get; private set; }

    private TrackingSession() { }

    public static TrackingSession Create(Guid sessionId, SessionType sessionType)
    {
        return new TrackingSession
        {
            Id = Guid.NewGuid(),
            SessionId = sessionId,
            SessionType = sessionType,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void UpdateMemberLocation(double latitude, double longitude)
    {
        MemberLocation = new LocationPoint(latitude, longitude);
        MemberLastUpdate = DateTime.UtcNow;
        RecalculateDistance();
    }

    public void UpdateRescuerLocation(double latitude, double longitude)
    {
        RescuerLocation = new LocationPoint(latitude, longitude);
        RescuerLastUpdate = DateTime.UtcNow;
        RecalculateDistance();
    }

    public void EndSession()
    {
        IsActive = false;
    }

    private void RecalculateDistance()
    {
        if (MemberLocation != null && RescuerLocation != null)
        {
            DistanceMeters = CalculateHaversineDistance(MemberLocation, RescuerLocation);
            EtaMinutes = CalculateEta(DistanceMeters.Value);
        }
    }

    private static double CalculateHaversineDistance(LocationPoint from, LocationPoint to)
    {
        const double earthRadius = 6371000; // meters
        var dLat = ToRadians(to.Latitude - from.Latitude);
        var dLon = ToRadians(to.Longitude - from.Longitude);

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRadians(from.Latitude)) * Math.Cos(ToRadians(to.Latitude)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return earthRadius * c;
    }

    private static double ToRadians(double degrees) => degrees * Math.PI / 180;

    private static int CalculateEta(double distanceMeters)
    {
        const double averageSpeedKmh = 30.0;
        var distanceKm = distanceMeters / 1000;
        var hours = distanceKm / averageSpeedKmh;
        return (int)Math.Ceiling(hours * 60);
    }
}

public record LocationPoint(double Latitude, double Longitude)
{
    public DateTime Timestamp { get; } = DateTime.UtcNow;
}

public enum SessionType
{
    Emergency = 0,
    Catching = 1
}
```

### AI Recognition (Shared Kernel)
```csharp
// filepath: src/SnakeAid.SharedKernel/Domain/AI/SnakeAIRecognitionResult.cs
namespace SnakeAid.SharedKernel.Domain.AI;

public class SnakeAIRecognitionResult : Entity<Guid>
{
    public string ImageUrl { get; private set; }
    public Guid? SpeciesId { get; private set; }
    public string? SpeciesName { get; private set; }
    public double ConfidenceScore { get; private set; }
    public bool IsVenomous { get; private set; }
    public string? DangerLevel { get; private set; }
    public List<BoundingBox> DetectedObjects { get; private set; } = new();

    private SnakeAIRecognitionResult() { }

    public static SnakeAIRecognitionResult Create(string imageUrl)
    {
        return new SnakeAIRecognitionResult
        {
            Id = Guid.NewGuid(),
            ImageUrl = imageUrl,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void SetRecognitionResult(
        Guid? speciesId,
        string? speciesName, 
        double confidenceScore,
        bool isVenomous,
        string? dangerLevel)
    {
        SpeciesId = speciesId;
        SpeciesName = speciesName;
        ConfidenceScore = confidenceScore;
        IsVenomous = isVenomous;
        DangerLevel = dangerLevel;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AddDetectedObject(double x, double y, double width, double height, double confidence)
    {
        DetectedObjects.Add(new BoundingBox(x, y, width, height, confidence));
    }
}

public record BoundingBox(double X, double Y, double Width, double Height, double Confidence);
```

---

## 2. Emergency Rescue Context Implementation

### Aggregate Root: EmergencyCase
```csharp
// filepath: src/Contexts/EmergencyRescue/SnakeAid.EmergencyRescue.Domain/Aggregates/EmergencyCase/EmergencyCase.cs
using SnakeAid.SharedKernel.Domain;

namespace SnakeAid.EmergencyRescue.Domain.Aggregates.EmergencyCase;

public class EmergencyCase : AggregateRoot<Guid>
{
    public VictimInfo Victim { get; private set; }
    public EmergencyLocation Location { get; private set; }
    public EmergencyStatus Status { get; private set; }
    public SeverityLevel Severity { get; private set; }
    public Guid? AssignedRescuerId { get; private set; }
    
    // Entities
    private readonly List<RescuerRequest> _rescuerRequests = new();
    public IReadOnlyList<RescuerRequest> RescuerRequests => _rescuerRequests.AsReadOnly();
    
    public RescueMission? RescueMission { get; private set; }
    
    // Value Objects
    public FirstAidGuidance? FirstAidGuidance { get; private set; }
    public HospitalReferral? HospitalReferral { get; private set; }
    
    // Timeline tracking
    private readonly List<EmergencyTimelineEntry> _timeline = new();
    public IReadOnlyList<EmergencyTimelineEntry> Timeline => _timeline.AsReadOnly();

    private EmergencyCase() { }

    public static EmergencyCase Create(
        VictimInfo victim,
        EmergencyLocation location,
        SeverityLevel severity)
    {
        var emergencyCase = new EmergencyCase
        {
            Id = Guid.NewGuid(),
            Victim = victim,
            Location = location,
            Severity = severity,
            Status = EmergencyStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        emergencyCase.AddDomainEvent(new EmergencyCaseCreatedEvent(
            emergencyCase.Id,
            victim.Name,
            location.Latitude,
            location.Longitude,
            severity));

        emergencyCase.AddTimelineEntry("Emergency case created", "System");
        
        return emergencyCase;
    }

    public void RequestRescuers(List<Guid> rescuerIds, int sessionNumber, double radiusKm)
    {
        if (Status != EmergencyStatus.Pending)
            throw new DomainException("Can only request rescuers for pending emergencies");

        foreach (var rescuerId in rescuerIds)
        {
            var request = RescuerRequest.Create(Id, rescuerId, sessionNumber, radiusKm);
            _rescuerRequests.Add(request);
        }

        AddDomainEvent(new RescuersRequestedEvent(Id, rescuerIds, sessionNumber, radiusKm));
        AddTimelineEntry($"Requested {rescuerIds.Count} rescuers in session {sessionNumber}", "System");
    }

    public void AssignRescuer(Guid rescuerId, string rescuerName)
    {
        if (Status != EmergencyStatus.Pending)
            throw new DomainException("Can only assign rescuer to pending cases");

        if (AssignedRescuerId.HasValue)
            throw new DomainException("Rescuer already assigned");

        AssignedRescuerId = rescuerId;
        Status = EmergencyStatus.RescuerAssigned;
        
        // Mark the accepted request
        var acceptedRequest = _rescuerRequests.FirstOrDefault(r => r.RescuerId == rescuerId);
        acceptedRequest?.MarkAsAccepted();

        // Cancel other pending requests
        var pendingRequests = _rescuerRequests.Where(r => r.Status == RescuerRequestStatus.Pending);
        foreach (var request in pendingRequests)
        {
            if (request.RescuerId != rescuerId)
                request.MarkAsCancelled();
        }

        AddDomainEvent(new RescuerAssignedEvent(Id, rescuerId, rescuerName));
        AddTimelineEntry($"Rescuer assigned: {rescuerName}", "System");
    }

    public void ProvideFirstAidGuidance(FirstAidGuidance guidance)
    {
        FirstAidGuidance = guidance;
        AddDomainEvent(new FirstAidGuidanceProvidedEvent(Id, guidance.Instructions));
        AddTimelineEntry("First aid guidance provided", "AI System");
    }

    public void ReferToHospital(HospitalReferral referral)
    {
        HospitalReferral = referral;
        AddDomainEvent(new HospitalReferredEvent(Id, referral.HospitalId, referral.HospitalName));
        AddTimelineEntry($"Referred to hospital: {referral.HospitalName}", "System");
    }

    public void StartRescueMission()
    {
        if (Status != EmergencyStatus.RescuerAssigned)
            throw new DomainException("Cannot start mission without assigned rescuer");

        if (RescueMission != null)
            throw new DomainException("Mission already started");

        RescueMission = Domain.Aggregates.EmergencyCase.RescueMission.Create(Id, AssignedRescuerId!.Value);
        Status = EmergencyStatus.InProgress;

        AddDomainEvent(new RescueMissionStartedEvent(Id, RescueMission.Id));
        AddTimelineEntry("Rescue mission started", "Rescuer");
    }

    public void CompleteEmergency(string outcome, string notes = "")
    {
        if (Status != EmergencyStatus.InProgress)
            throw new DomainException("Can only complete emergency that is in progress");

        Status = EmergencyStatus.Completed;
        RescueMission?.Complete(outcome, notes);

        AddDomainEvent(new EmergencyCompletedEvent(Id, outcome));
        AddTimelineEntry($"Emergency completed: {outcome}", "Rescuer");
    }

    public void CancelEmergency(string reason)
    {
        if (Status == EmergencyStatus.Completed)
            throw new DomainException("Cannot cancel completed emergency");

        Status = EmergencyStatus.Cancelled;
        RescueMission?.Cancel(reason);

        // Cancel all pending rescuer requests
        foreach (var request in _rescuerRequests.Where(r => r.Status == RescuerRequestStatus.Pending))
        {
            request.MarkAsCancelled();
        }

        AddDomainEvent(new EmergencyCancelledEvent(Id, reason));
        AddTimelineEntry($"Emergency cancelled: {reason}", "System");
    }

    public void EscalateSeverity(string reason)
    {
        if (Severity == SeverityLevel.Critical)
            return; // Already at maximum severity

        var oldSeverity = Severity;
        Severity = Severity switch
        {
            SeverityLevel.Low => SeverityLevel.Medium,
            SeverityLevel.Medium => SeverityLevel.High,
            SeverityLevel.High => SeverityLevel.Critical,
            _ => Severity
        };

        AddDomainEvent(new SeverityEscalatedEvent(Id, oldSeverity, Severity, reason));
        AddTimelineEntry($"Severity escalated from {oldSeverity} to {Severity}: {reason}", "AI System");
    }

    private void AddTimelineEntry(string description, string actor)
    {
        _timeline.Add(new EmergencyTimelineEntry(DateTime.UtcNow, description, actor));
    }
}
```

### Value Objects
```csharp
// filepath: src/Contexts/EmergencyRescue/SnakeAid.EmergencyRescue.Domain/ValueObjects/VictimInfo.cs
using SnakeAid.SharedKernel.Domain;

namespace SnakeAid.EmergencyRescue.Domain.ValueObjects;

public class VictimInfo : ValueObject
{
    public string Name { get; }
    public string PhoneNumber { get; }
    public string? BiteDescription { get; }
    public string? Symptoms { get; }

    public VictimInfo(string name, string phoneNumber, string? biteDescription = null, string? symptoms = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be empty", nameof(name));
        if (string.IsNullOrWhiteSpace(phoneNumber))
            throw new ArgumentException("Phone number cannot be empty", nameof(phoneNumber));

        Name = name;
        PhoneNumber = phoneNumber;
        BiteDescription = biteDescription;
        Symptoms = symptoms;
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Name;
        yield return PhoneNumber;
        yield return BiteDescription ?? "";
        yield return Symptoms ?? "";
    }
}

// filepath: src/Contexts/EmergencyRescue/SnakeAid.EmergencyRescue.Domain/ValueObjects/EmergencyLocation.cs
public class EmergencyLocation : ValueObject
{
    public double Latitude { get; }
    public double Longitude { get; }
    public string Address { get; }
    public string? Landmark { get; }

    public EmergencyLocation(double latitude, double longitude, string address, string? landmark = null)
    {
        if (latitude < -90 || latitude > 90)
            throw new ArgumentException("Invalid latitude", nameof(latitude));
        if (longitude < -180 || longitude > 180)
            throw new ArgumentException("Invalid longitude", nameof(longitude));

        Latitude = latitude;
        Longitude = longitude;
        Address = address ?? throw new ArgumentNullException(nameof(address));
        Landmark = landmark;
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Latitude;
        yield return Longitude;
        yield return Address;
        yield return Landmark ?? "";
    }
}

// filepath: src/Contexts/EmergencyRescue/SnakeAid.EmergencyRescue.Domain/ValueObjects/EmergencyStatus.cs
public enum EmergencyStatus
{
    Pending = 0,
    RescuerAssigned = 1,
    InProgress = 2,
    Completed = 3,
    Cancelled = 4
}

public enum SeverityLevel
{
    Low = 0,
    Medium = 1,
    High = 2,
    Critical = 3
}
```

### Domain Events
```csharp
// filepath: src/Contexts/EmergencyRescue/SnakeAid.EmergencyRescue.Domain/Events/EmergencyCaseCreatedEvent.cs
using SnakeAid.SharedKernel.Domain;

namespace SnakeAid.EmergencyRescue.Domain.Events;

public record EmergencyCaseCreatedEvent(
    Guid EmergencyId,
    string VictimName,
    double Latitude,
    double Longitude,
    SeverityLevel Severity
) : DomainEvent;

public record RescuerAssignedEvent(
    Guid EmergencyId,
    Guid RescuerId,
    string RescuerName
) : DomainEvent;

public record FirstAidGuidanceProvidedEvent(
    Guid EmergencyId,
    string Instructions
) : DomainEvent;

public record HospitalReferredEvent(
    Guid EmergencyId,
    Guid HospitalId,
    string HospitalName
) : DomainEvent;

public record EmergencyCompletedEvent(
    Guid EmergencyId,
    string Outcome
) : DomainEvent;
```

### Application Layer - CQRS
```csharp
// filepath: src/Contexts/EmergencyRescue/SnakeAid.EmergencyRescue.Application/Commands/CreateEmergencyCase/CreateEmergencyCaseCommand.cs
using MediatR;

namespace SnakeAid.EmergencyRescue.Application.Commands.CreateEmergencyCase;

public record CreateEmergencyCaseCommand(
    string VictimName,
    string PhoneNumber,
    string? BiteDescription,
    string? Symptoms,
    double Latitude,
    double Longitude,
    string Address,
    string? Landmark,
    string? SnakePhotoUrl
) : IRequest<Guid>;

// filepath: src/Contexts/EmergencyRescue/SnakeAid.EmergencyRescue.Application/Commands/CreateEmergencyCase/CreateEmergencyCaseHandler.cs
using SnakeAid.EmergencyRescue.Domain.Aggregates.EmergencyCase;
using SnakeAid.SharedKernel.Domain.AI;

public class CreateEmergencyCaseHandler : IRequestHandler<CreateEmergencyCaseCommand, Guid>
{
    private readonly IEmergencyCaseRepository _repository;
    private readonly ISnakeIdentificationService _aiService;
    private readonly IFirstAidService _firstAidService;
    private readonly IUnitOfWork _unitOfWork;

    public CreateEmergencyCaseHandler(
        IEmergencyCaseRepository repository,
        ISnakeIdentificationService aiService,
        IFirstAidService firstAidService,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _aiService = aiService;
        _firstAidService = firstAidService;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> Handle(CreateEmergencyCaseCommand request, CancellationToken cancellationToken)
    {
        // 1. AI Snake Identification (if photo provided)
        SnakeAIRecognitionResult? aiResult = null;
        if (!string.IsNullOrEmpty(request.SnakePhotoUrl))
        {
            aiResult = await _aiService.IdentifySnakeAsync(request.SnakePhotoUrl);
        }

        // 2. Determine severity based on AI result
        var severity = DetermineSeverity(aiResult, request.Symptoms);

        // 3. Create domain entities
        var victim = new VictimInfo(
            request.VictimName,
            request.PhoneNumber,
            request.BiteDescription,
            request.Symptoms);

        var location = new EmergencyLocation(
            request.Latitude,
            request.Longitude,
            request.Address,
            request.Landmark);

        // 4. Create aggregate
        var emergencyCase = EmergencyCase.Create(victim, location, severity);

        // 5. Generate first aid guidance
        var guidance = await _firstAidService.GenerateGuidanceAsync(aiResult, severity);
        emergencyCase.ProvideFirstAidGuidance(guidance);

        // 6. Persist
        await _repository.AddAsync(emergencyCase, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return emergencyCase.Id;
    }

    private SeverityLevel DetermineSeverity(SnakeAIRecognitionResult? aiResult, string? symptoms)
    {
        // AI detected venomous snake
        if (aiResult?.IsVenomous == true && aiResult.ConfidenceScore > 0.8)
            return SeverityLevel.Critical;

        // Severe symptoms
        if (symptoms?.Contains("difficulty breathing") == true ||
            symptoms?.Contains("swelling") == true)
            return SeverityLevel.High;

        // Default to medium for any snake bite
        return SeverityLevel.Medium;
    }
}
```

---

## 3. Repository Pattern & Infrastructure

### Repository Interface
```csharp
// filepath: src/SnakeAid.SharedKernel/Domain/IRepository.cs
namespace SnakeAid.SharedKernel.Domain;

public interface IRepository<TEntity, TId>
    where TEntity : AggregateRoot<TId>
    where TId : IEquatable<TId>
{
    Task<TEntity?> GetByIdAsync(TId id, CancellationToken cancellationToken = default);
    Task AddAsync(TEntity entity, CancellationToken cancellationToken = default);
    Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);
    Task DeleteAsync(TEntity entity, CancellationToken cancellationToken = default);
}

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
```

### EF Core Repository Implementation
```csharp
// filepath: src/Contexts/EmergencyRescue/SnakeAid.EmergencyRescue.Infrastructure/Persistence/EmergencyCaseRepository.cs
using Microsoft.EntityFrameworkCore;
using SnakeAid.EmergencyRescue.Domain.Aggregates.EmergencyCase;
using SnakeAid.SharedKernel.Domain;

namespace SnakeAid.EmergencyRescue.Infrastructure.Persistence;

public class EmergencyCaseRepository : IEmergencyCaseRepository
{
    private readonly EmergencyRescueDbContext _context;

    public EmergencyCaseRepository(EmergencyRescueDbContext context)
    {
        _context = context;
    }

    public async Task<EmergencyCase?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.EmergencyCases
            .Include(e => e.RescuerRequests)
            .Include(e => e.RescueMission)
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    public async Task AddAsync(EmergencyCase entity, CancellationToken cancellationToken = default)
    {
        await _context.EmergencyCases.AddAsync(entity, cancellationToken);
    }

    public Task UpdateAsync(EmergencyCase entity, CancellationToken cancellationToken = default)
    {
        _context.EmergencyCases.Update(entity);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(EmergencyCase entity, CancellationToken cancellationToken = default)
    {
        _context.EmergencyCases.Remove(entity);
        return Task.CompletedTask;
    }

    public async Task<List<EmergencyCase>> GetActiveEmergenciesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.EmergencyCases
            .Where(e => e.Status == EmergencyStatus.Pending || e.Status == EmergencyStatus.InProgress)
            .ToListAsync(cancellationToken);
    }
}
```

### Domain Event Publishing
```csharp
// filepath: src/SnakeAid.SharedKernel/Infrastructure/DomainEventDispatcher.cs
using MediatR;
using Microsoft.EntityFrameworkCore;
using SnakeAid.SharedKernel.Domain;

namespace SnakeAid.SharedKernel.Infrastructure;

public class DomainEventDispatcher : IDomainEventDispatcher
{
    private readonly IMediator _mediator;

    public DomainEventDispatcher(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task DispatchEventsAsync<TContext>(TContext context, CancellationToken cancellationToken = default)
        where TContext : DbContext
    {
        var domainEntities = context.ChangeTracker
            .Entries<AggregateRoot<Guid>>()
            .Where(x => x.Entity.DomainEvents.Any())
            .ToList();

        var domainEvents = domainEntities
            .SelectMany(x => x.Entity.DomainEvents)
            .ToList();

        domainEntities.ForEach(entity => entity.Entity.ClearDomainEvents());

        foreach (var domainEvent in domainEvents)
        {
            await _mediator.Publish(domainEvent, cancellationToken);
        }
    }
}
```

---

## 4. API Gateway Integration

### API Controller
```csharp
// filepath: src/SnakeAid.API/Controllers/EmergencyController.cs
using MediatR;
using Microsoft.AspNetCore.Mvc;
using SnakeAid.EmergencyRescue.Application.Commands.CreateEmergencyCase;

namespace SnakeAid.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EmergencyController : ControllerBase
{
    private readonly IMediator _mediator;

    public EmergencyController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<ActionResult<Guid>> CreateEmergency(CreateEmergencyRequest request)
    {
        var command = new CreateEmergencyCaseCommand(
            request.VictimName,
            request.PhoneNumber,
            request.BiteDescription,
            request.Symptoms,
            request.Latitude,
            request.Longitude,
            request.Address,
            request.Landmark,
            request.SnakePhotoUrl);

        var emergencyId = await _mediator.Send(command);

        return CreatedAtAction(nameof(GetEmergency), new { id = emergencyId }, emergencyId);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<EmergencyCaseResponse>> GetEmergency(Guid id)
    {
        var query = new GetEmergencyCaseQuery(id);
        var result = await _mediator.Send(query);

        if (result == null)
            return NotFound();

        return Ok(result);
    }
}

public record CreateEmergencyRequest(
    string VictimName,
    string PhoneNumber,
    string? BiteDescription,
    string? Symptoms,
    double Latitude,
    double Longitude,
    string Address,
    string? Landmark,
    string? SnakePhotoUrl);
```

---

**Next**: [Usage Guide](ddd-migration.usageguide.md)