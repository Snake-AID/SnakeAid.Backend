using System.Linq.Expressions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using SnakeAid.Core.Domains;
using SnakeAid.Core.Exceptions;
using SnakeAid.Core.Requests.SnakebiteIncident;
using SnakeAid.Core.Responses.SnakebiteIncident;
using SnakeAid.Repository.Data;
using SnakeAid.Repository.Interfaces;
using SnakeAid.Service.Implements;
using SnakeAid.Service.Interfaces;

namespace SnakeAid.Tests.Unit;

public class SnakebiteIncidentServiceTests
{
    private readonly Mock<IUnitOfWork<SnakeAidDbContext>> _unitOfWorkMock = new();
    private readonly Mock<IGenericRepository<SnakebiteIncident>> _incidentRepoMock = new();
    private readonly Mock<IGenericRepository<RescueMission>> _missionRepoMock = new();
    private readonly Mock<IGenericRepository<RescuerRequest>> _requestRepoMock = new();
    private readonly Mock<IGenericRepository<RescuerProfile>> _rescuerRepoMock = new();
    private readonly Mock<ILogger<SnakebiteIncidentService>> _loggerMock = new();
    private readonly Mock<IConfiguration> _configurationMock = new();
    private readonly Mock<IOperatorRealtimeNotificationService> _operatorRealtimeMock = new();
    private readonly Mock<IRescueNotificationService> _rescueNotificationMock = new();
    private readonly Mock<IMissionNotificationService> _missionNotificationMock = new();
    private readonly Mock<ISnakeRescueMissionService> _missionServiceMock = new();
    private readonly Mock<INotificationQueueService> _notificationQueueMock = new();

    private readonly SnakebiteIncidentService _service;

    public SnakebiteIncidentServiceTests()
    {
        _unitOfWorkMock.Setup(u => u.GetRepository<SnakebiteIncident>()).Returns(_incidentRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.GetRepository<RescueMission>()).Returns(_missionRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.GetRepository<RescuerRequest>()).Returns(_requestRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.GetRepository<RescuerProfile>()).Returns(_rescuerRepoMock.Object);

        _unitOfWorkMock
            .Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<Task<CreateIncidentResponse>>>()))
            .Returns((Func<Task<CreateIncidentResponse>> operation) => operation());

        _unitOfWorkMock
            .Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<Task<RejectRescueResponse>>>()))
            .Returns((Func<Task<RejectRescueResponse>> operation) => operation());

        _unitOfWorkMock
            .Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<Task<(RejectRescueResponse response, SnakebiteIncident incident)>>>()))
            .Returns((Func<Task<(RejectRescueResponse response, SnakebiteIncident incident)>> operation) => operation());

        _service = new SnakebiteIncidentService(
            _unitOfWorkMock.Object,
            _loggerMock.Object,
            _configurationMock.Object,
            _operatorRealtimeMock.Object,
            _rescueNotificationMock.Object,
            _missionNotificationMock.Object,
            _missionServiceMock.Object,
            _notificationQueueMock.Object);
    }

    [Fact]
    public async Task ConfirmIncidentAsync_ShouldClaimAndVerify_AndNotifyOperatorRealtime()
    {
        // Arrange
        var incidentId = Guid.NewGuid();
        var operatorId = Guid.NewGuid();

        var incident = new SnakebiteIncident
        {
            Id = incidentId,
            Status = SnakebiteIncidentStatus.Pending,
            UserId = Guid.NewGuid()
        };

        _incidentRepoMock
            .Setup(r => r.FirstOrDefaultAsync(
                It.IsAny<Expression<Func<SnakebiteIncident, bool>>>(),
                It.IsAny<Func<IQueryable<SnakebiteIncident>, IOrderedQueryable<SnakebiteIncident>>>(),
                It.IsAny<Func<IQueryable<SnakebiteIncident>, IQueryable<SnakebiteIncident>>>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(incident);

        // Act
        var response = await _service.ConfirmIncidentAsync(incidentId, operatorId);

        // Assert
        Assert.Equal(incidentId, response.Id);
        Assert.Equal(operatorId, incident.HandlingOperatorId);
        Assert.Equal(SnakebiteIncidentStatus.Verified, incident.Status);

        _incidentRepoMock.Verify(r => r.Update(incident), Times.Once);
        _operatorRealtimeMock.Verify(n => n.NotifyIncidentClaimedAsync(incidentId, operatorId), Times.Once);
    }

    [Fact]
    public async Task ConfirmIncidentAsync_ShouldThrowConflict_WhenIncidentHandledByAnotherOperator()
    {
        // Arrange
        var incidentId = Guid.NewGuid();
        var operatorId = Guid.NewGuid();

        var incident = new SnakebiteIncident
        {
            Id = incidentId,
            Status = SnakebiteIncidentStatus.Pending,
            UserId = Guid.NewGuid(),
            HandlingOperatorId = Guid.NewGuid()
        };

        _incidentRepoMock
            .Setup(r => r.FirstOrDefaultAsync(
                It.IsAny<Expression<Func<SnakebiteIncident, bool>>>(),
                It.IsAny<Func<IQueryable<SnakebiteIncident>, IOrderedQueryable<SnakebiteIncident>>>(),
                It.IsAny<Func<IQueryable<SnakebiteIncident>, IQueryable<SnakebiteIncident>>>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(incident);

        // Act + Assert
        await Assert.ThrowsAsync<ConflictException>(() => _service.ConfirmIncidentAsync(incidentId, operatorId));

        _incidentRepoMock.Verify(r => r.Update(It.IsAny<SnakebiteIncident>()), Times.Never);
        _operatorRealtimeMock.Verify(n => n.NotifyIncidentClaimedAsync(It.IsAny<Guid>(), It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task DeclineDispatchRequestAsync_ShouldSetDeclinedAndNotify()
    {
        // Arrange
        var requestId = Guid.NewGuid();
        var rescuerId = Guid.NewGuid();
        var incidentId = Guid.NewGuid();

        var incident = new SnakebiteIncident
        {
            Id = incidentId,
            Status = SnakebiteIncidentStatus.Verified,
            UserId = Guid.NewGuid()
        };

        var request = new RescuerRequest
        {
            Id = requestId,
            RescuerId = rescuerId,
            IncidentId = incidentId,
            Status = RescueRequestStatus.Pending,
            Incident = incident
        };

        var rescuerProfile = new RescuerProfile
        {
            AccountId = rescuerId,
            IsAvailable = false
        };

        _requestRepoMock
            .Setup(r => r.FirstOrDefaultAsync(
                It.IsAny<Expression<Func<RescuerRequest, bool>>>(),
                It.IsAny<Func<IQueryable<RescuerRequest>, IOrderedQueryable<RescuerRequest>>>(),
                It.IsAny<Func<IQueryable<RescuerRequest>, IQueryable<RescuerRequest>>>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(request);

        _rescuerRepoMock
            .Setup(r => r.FirstOrDefaultAsync(
                It.IsAny<Expression<Func<RescuerProfile, bool>>>(),
                It.IsAny<Func<IQueryable<RescuerProfile>, IOrderedQueryable<RescuerProfile>>>(),
                It.IsAny<Func<IQueryable<RescuerProfile>, IQueryable<RescuerProfile>>>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(rescuerProfile);

        var declineReason = "Busy at this moment";

        // Act
        var response = await _service.DeclineDispatchRequestAsync(requestId, rescuerId, declineReason);

        // Assert
        Assert.Equal(requestId, response.RequestId);
        Assert.Equal(RescueRequestStatus.Declined, request.Status);
        Assert.True(rescuerProfile.IsAvailable);

        _requestRepoMock.Verify(r => r.Update(request), Times.Once);
        _incidentRepoMock.Verify(r => r.Update(incident), Times.Once);
        _rescuerRepoMock.Verify(r => r.Update(rescuerProfile), Times.Once);

        _rescueNotificationMock.Verify(n =>
            n.NotifyRescuerDeclinedAsync(
                rescuerId.ToString(),
                It.Is<RejectRescueResponse>(x => x.RequestId == requestId)),
            Times.Once);

        _operatorRealtimeMock.Verify(n =>
            n.NotifyRescuerDeclinedAsync(incidentId, rescuerId, declineReason),
            Times.Once);
    }

    [Fact]
    public async Task CancelIncidentAsync_ShouldThrowBadRequest_WhenIncidentAlreadyFinalized()
    {
        // Arrange
        var incidentId = Guid.NewGuid();

        var incident = new SnakebiteIncident
        {
            Id = incidentId,
            Status = SnakebiteIncidentStatus.Completed,
            UserId = Guid.NewGuid()
        };

        _incidentRepoMock
            .Setup(r => r.FirstOrDefaultAsync(
                It.IsAny<Expression<Func<SnakebiteIncident, bool>>>(),
                It.IsAny<Func<IQueryable<SnakebiteIncident>, IOrderedQueryable<SnakebiteIncident>>>(),
                It.IsAny<Func<IQueryable<SnakebiteIncident>, IQueryable<SnakebiteIncident>>>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(incident);

        // Act + Assert
        await Assert.ThrowsAsync<BadRequestException>(() =>
            _service.CancelIncidentAsync(incidentId, new CancelIncidentRequest { Reason = "No longer needed" }));

        _operatorRealtimeMock.Verify(n => n.NotifyIncidentCancelledAsync(It.IsAny<Guid>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task CancelIncidentAsync_ShouldCancelPreparingMission_AndNotifyMissionChannel()
    {
        // Arrange
        var incidentId = Guid.NewGuid();
        var rescuerId = Guid.NewGuid();
        var reason = "Member cancelled request";

        var incident = new SnakebiteIncident
        {
            Id = incidentId,
            Status = SnakebiteIncidentStatus.Assigned,
            UserId = Guid.NewGuid(),
            AssignedRescuerId = rescuerId,
            AssignedAt = DateTime.UtcNow,
            DispatchedAt = DateTime.UtcNow
        };

        var activeMission = new RescueMission
        {
            Id = Guid.NewGuid(),
            IncidentId = incidentId,
            RescuerId = rescuerId,
            Status = RescueMissionStatus.Preparing,
            Price = 100000m,
            Rescuer = new RescuerProfile
            {
                AccountId = rescuerId,
                IsAvailable = false
            }
        };

        _incidentRepoMock
            .Setup(r => r.FirstOrDefaultAsync(
                It.IsAny<Expression<Func<SnakebiteIncident, bool>>>(),
                It.IsAny<Func<IQueryable<SnakebiteIncident>, IOrderedQueryable<SnakebiteIncident>>>(),
                It.IsAny<Func<IQueryable<SnakebiteIncident>, IQueryable<SnakebiteIncident>>>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(incident);

        _missionRepoMock
            .SetupSequence(r => r.FirstOrDefaultAsync(
                It.IsAny<Expression<Func<RescueMission, bool>>>(),
                It.IsAny<Func<IQueryable<RescueMission>, IOrderedQueryable<RescueMission>>>(),
                It.IsAny<Func<IQueryable<RescueMission>, IQueryable<RescueMission>>>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((RescueMission?)null)
            .ReturnsAsync(activeMission);

        // Act
        var response = await _service.CancelIncidentAsync(incidentId, new CancelIncidentRequest { Reason = reason });

        // Assert
        Assert.Equal(incidentId, response.Id);

        Assert.Equal(RescueMissionStatus.Cancelled, activeMission.Status);
        Assert.Equal(reason, activeMission.CancellationReason);

        Assert.Equal(SnakebiteIncidentStatus.Cancelled, incident.Status);
        Assert.Null(incident.AssignedRescuerId);
        Assert.Null(incident.AssignedAt);
        Assert.Null(incident.DispatchedAt);
        Assert.True(activeMission.Rescuer.IsAvailable);

        _missionRepoMock.Verify(r => r.Update(activeMission), Times.Once);
        _incidentRepoMock.Verify(r => r.Update(incident), Times.Once);
        _rescuerRepoMock.Verify(r => r.Update(activeMission.Rescuer), Times.Once);

        _operatorRealtimeMock.Verify(n => n.NotifyIncidentCancelledAsync(incidentId, reason), Times.Once);
        _missionNotificationMock.Verify(n => n.NotifyMissionCancelledAsync(incidentId, rescuerId, reason), Times.Once);
        _rescueNotificationMock.Verify(n => n.NotifyRequestCancelledAsync(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task CancelIncidentAsync_ShouldCancelPendingRequests_AndNotifyEachRescuer()
    {
        // Arrange
        var incidentId = Guid.NewGuid();
        var reason = "No longer needed";

        var incident = new SnakebiteIncident
        {
            Id = incidentId,
            Status = SnakebiteIncidentStatus.Verified,
            UserId = Guid.NewGuid(),
            AssignedRescuerId = Guid.NewGuid(),
            AssignedAt = DateTime.UtcNow,
            DispatchedAt = DateTime.UtcNow
        };

        var pendingRequests = new List<RescuerRequest>
        {
            new()
            {
                Id = Guid.NewGuid(),
                IncidentId = incidentId,
                RescuerId = Guid.NewGuid(),
                Status = RescueRequestStatus.Pending
            },
            new()
            {
                Id = Guid.NewGuid(),
                IncidentId = incidentId,
                RescuerId = Guid.NewGuid(),
                Status = RescueRequestStatus.Pending
            }
        };

        _incidentRepoMock
            .Setup(r => r.FirstOrDefaultAsync(
                It.IsAny<Expression<Func<SnakebiteIncident, bool>>>(),
                It.IsAny<Func<IQueryable<SnakebiteIncident>, IOrderedQueryable<SnakebiteIncident>>>(),
                It.IsAny<Func<IQueryable<SnakebiteIncident>, IQueryable<SnakebiteIncident>>>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(incident);

        _missionRepoMock
            .SetupSequence(r => r.FirstOrDefaultAsync(
                It.IsAny<Expression<Func<RescueMission, bool>>>(),
                It.IsAny<Func<IQueryable<RescueMission>, IOrderedQueryable<RescueMission>>>(),
                It.IsAny<Func<IQueryable<RescueMission>, IQueryable<RescueMission>>>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((RescueMission?)null)
            .ReturnsAsync((RescueMission?)null);

        _requestRepoMock
            .Setup(r => r.GetListAsync(
                It.IsAny<Expression<Func<RescuerRequest, bool>>>(),
                It.IsAny<Func<IQueryable<RescuerRequest>, IOrderedQueryable<RescuerRequest>>>(),
                It.IsAny<Func<IQueryable<RescuerRequest>, IQueryable<RescuerRequest>>>(),
                It.IsAny<int?>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(pendingRequests);

        // Act
        var response = await _service.CancelIncidentAsync(incidentId, new CancelIncidentRequest { Reason = reason });

        // Assert
        Assert.Equal(incidentId, response.Id);
        Assert.Equal(SnakebiteIncidentStatus.Cancelled, incident.Status);
        Assert.Null(incident.AssignedRescuerId);
        Assert.Null(incident.AssignedAt);
        Assert.Null(incident.DispatchedAt);

        foreach (var request in pendingRequests)
        {
            Assert.Equal(RescueRequestStatus.Cancelled, request.Status);
            Assert.Equal(reason, request.DeclineReason);
            Assert.NotNull(request.ResponseAt);
        }

        _requestRepoMock.Verify(r => r.Update(It.IsAny<RescuerRequest>()), Times.Exactly(pendingRequests.Count));
        _incidentRepoMock.Verify(r => r.Update(incident), Times.Once);

        _operatorRealtimeMock.Verify(n => n.NotifyIncidentCancelledAsync(incidentId, reason), Times.Once);

        foreach (var request in pendingRequests)
        {
            _rescueNotificationMock.Verify(n =>
                n.NotifyRequestCancelledAsync(request.RescuerId.ToString(), request.Id, reason),
                Times.Once);
        }

        _missionNotificationMock.Verify(n => n.NotifyMissionCancelledAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task CreateIncidentAsync_ShouldThrowBadRequest_WhenRequestIsNull()
    {
        // Act + Assert
        await Assert.ThrowsAsync<BadRequestException>(() =>
            _service.CreateIncidentAsync(null!, Guid.NewGuid()));
    }
}
