using System.Linq.Expressions;
using Microsoft.Extensions.Logging;
using Moq;
using SnakeAid.Core.Constants;
using SnakeAid.Core.Domains;
using SnakeAid.Core.Exceptions;
using SnakeAid.Core.Requests.Notification;
using SnakeAid.Core.Requests.RescueMission;
using SnakeAid.Core.Responses.RescueMission;
using SnakeAid.Core.Services;
using SnakeAid.Repository.Data;
using SnakeAid.Repository.Interfaces;
using SnakeAid.Service.Implements;
using SnakeAid.Service.Interfaces;

namespace SnakeAid.Tests.Unit;

public class SnakeRescueMissionServiceTests
{
    private readonly Mock<IUnitOfWork<SnakeAidDbContext>> _unitOfWorkMock = new();
    private readonly Mock<IGenericRepository<SnakebiteIncident>> _incidentRepoMock = new();
    private readonly Mock<IGenericRepository<RescueMission>> _missionRepoMock = new();
    private readonly Mock<IGenericRepository<RescuerProfile>> _rescuerRepoMock = new();
    private readonly Mock<IGenericRepository<ReportMedia>> _reportMediaRepoMock = new();
    private readonly Mock<IGenericRepository<TreatmentFacility>> _hospitalRepoMock = new();

    private readonly Mock<ILogger<SnakeRescueMissionService>> _loggerMock = new();
    private readonly Mock<ISystemSettingService> _settingServiceMock = new();
    private readonly Mock<ILocationIqService> _locationIqServiceMock = new();
    private readonly Mock<IMissionNotificationService> _missionNotificationMock = new();
    private readonly Mock<IOperatorRealtimeNotificationService> _operatorRealtimeMock = new();

    private readonly SnakeRescueMissionService _service;

    public SnakeRescueMissionServiceTests()
    {
        _unitOfWorkMock.Setup(u => u.GetRepository<SnakebiteIncident>()).Returns(_incidentRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.GetRepository<RescueMission>()).Returns(_missionRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.GetRepository<RescuerProfile>()).Returns(_rescuerRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.GetRepository<ReportMedia>()).Returns(_reportMediaRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.GetRepository<TreatmentFacility>()).Returns(_hospitalRepoMock.Object);

        _unitOfWorkMock
            .Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<Task<RescueMission>>>()))
            .Returns((Func<Task<RescueMission>> operation) => operation());

        _unitOfWorkMock
            .Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<Task<HospitalTransferPricingResponse>>>()))
            .Returns((Func<Task<HospitalTransferPricingResponse>> operation) => operation());

        _settingServiceMock
            .Setup(s => s.GetSetting(SystemSettingKeys.RescueDefaultPrice, It.IsAny<decimal>()))
            .Returns(500000m);

        _settingServiceMock
            .Setup(s => s.GetSetting(SystemSettingKeys.PricingCenterLatitude, It.IsAny<double>()))
            .Returns(10.8391267);

        _settingServiceMock
            .Setup(s => s.GetSetting(SystemSettingKeys.PricingCenterLongitude, It.IsAny<double>()))
            .Returns(106.8413534);

        _settingServiceMock
            .Setup(s => s.GetSetting(SystemSettingKeys.RescuePricePerKmDefault, It.IsAny<decimal>()))
            .Returns(5000m);

        _locationIqServiceMock
            .Setup(l => l.CalculateDistanceAndPriceAsync(It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<decimal>()))
            .ReturnsAsync((10d, 50000m));

        _service = new SnakeRescueMissionService(
            _unitOfWorkMock.Object,
            _loggerMock.Object,
            _settingServiceMock.Object,
            _locationIqServiceMock.Object,
            _missionNotificationMock.Object,
            _operatorRealtimeMock.Object);
    }

    [Fact]
    public async Task CreateMissionAsync_ShouldThrowNotFound_WhenIncidentNotFound()
    {
        // Arrange
        _incidentRepoMock
            .Setup(r => r.FirstOrDefaultAsync(
                It.IsAny<Expression<Func<SnakebiteIncident, bool>>>(),
                It.IsAny<Func<IQueryable<SnakebiteIncident>, IOrderedQueryable<SnakebiteIncident>>>(),
                It.IsAny<Func<IQueryable<SnakebiteIncident>, IQueryable<SnakebiteIncident>>>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((SnakebiteIncident?)null);

        // Act + Assert
        await Assert.ThrowsAsync<NotFoundException>(() =>
            _service.CreateMissionAsync(Guid.NewGuid(), Guid.NewGuid()));
    }

    [Fact]
    public async Task CreateMissionAsync_ShouldThrowBadRequest_WhenIncidentStatusIsInvalid()
    {
        // Arrange
        var incident = new SnakebiteIncident
        {
            Id = Guid.NewGuid(),
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
            _service.CreateMissionAsync(incident.Id, Guid.NewGuid()));
    }

    [Fact]
    public async Task UpdateMissionStatusAsync_ShouldThrowBadRequest_WhenTransitionIsInvalid()
    {
        // Arrange
        var mission = new RescueMission
        {
            Id = Guid.NewGuid(),
            IncidentId = Guid.NewGuid(),
            RescuerId = Guid.NewGuid(),
            Status = RescueMissionStatus.Preparing,
            Price = 100000m
        };

        _missionRepoMock
            .Setup(r => r.FirstOrDefaultAsync(
                It.IsAny<Expression<Func<RescueMission, bool>>>(),
                It.IsAny<Func<IQueryable<RescueMission>, IOrderedQueryable<RescueMission>>>(),
                It.IsAny<Func<IQueryable<RescueMission>, IQueryable<RescueMission>>>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(mission);

        // Act + Assert
        await Assert.ThrowsAsync<BadRequestException>(() =>
            _service.UpdateMissionStatusAsync(mission.Id, RescueMissionStatus.RescuerArrived));
    }

    [Fact]
    public async Task UpdateMissionStatusAsync_ShouldSetEnRoute_AndNotifyMissionStarted()
    {
        // Arrange
        var missionId = Guid.NewGuid();
        var incidentId = Guid.NewGuid();
        var rescuerId = Guid.NewGuid();
        var memberId = Guid.NewGuid();

        var missionEntity = new RescueMission
        {
            Id = missionId,
            IncidentId = incidentId,
            RescuerId = rescuerId,
            Status = RescueMissionStatus.Preparing,
            Price = 100000m
        };

        var missionSnapshot = new RescueMission
        {
            Id = missionId,
            IncidentId = incidentId,
            RescuerId = rescuerId,
            Status = RescueMissionStatus.EnRoute,
            Price = 100000m,
            Incident = new SnakebiteIncident
            {
                Id = incidentId,
                UserId = memberId,
                User = new MemberProfile
                {
                    Account = new Account { Id = memberId, FullName = "Member" }
                }
            },
            Rescuer = new RescuerProfile
            {
                AccountId = rescuerId,
                Account = new Account { Id = rescuerId, FullName = "Rescuer A" }
            }
        };

        _missionRepoMock
            .SetupSequence(r => r.FirstOrDefaultAsync(
                It.IsAny<Expression<Func<RescueMission, bool>>>(),
                It.IsAny<Func<IQueryable<RescueMission>, IOrderedQueryable<RescueMission>>>(),
                It.IsAny<Func<IQueryable<RescueMission>, IQueryable<RescueMission>>>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(missionEntity)
            .ReturnsAsync(missionSnapshot);

        // Act
        await _service.UpdateMissionStatusAsync(missionId, RescueMissionStatus.EnRoute);

        // Assert
        Assert.Equal(RescueMissionStatus.EnRoute, missionEntity.Status);
        Assert.NotNull(missionEntity.StartedAt);

        _missionRepoMock.Verify(r => r.Update(missionEntity), Times.Once);
        _missionNotificationMock.Verify(n => n.NotifyMissionStartedAsync(
            incidentId,
            memberId,
            rescuerId,
            It.Is<MissionStartedNotificationPayload>(p => p.Status == RescueMissionStatus.EnRoute.ToString())),
            Times.Once);
    }

    [Fact]
    public async Task CompleteMissionAsync_ShouldThrowBadRequest_WhenEvidenceListIsEmpty()
    {
        // Arrange
        var mission = new RescueMission
        {
            Id = Guid.NewGuid(),
            IncidentId = Guid.NewGuid(),
            RescuerId = Guid.NewGuid(),
            Status = RescueMissionStatus.RescuerArrived,
            Price = 100000m
        };

        _missionRepoMock
            .Setup(r => r.FirstOrDefaultAsync(
                It.IsAny<Expression<Func<RescueMission, bool>>>(),
                It.IsAny<Func<IQueryable<RescueMission>, IOrderedQueryable<RescueMission>>>(),
                It.IsAny<Func<IQueryable<RescueMission>, IQueryable<RescueMission>>>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(mission);

        // Act + Assert
        await Assert.ThrowsAsync<BadRequestException>(() =>
            _service.CompleteMissionAsync(mission.Id, new List<Guid>(), "done"));
    }

    [Fact]
    public async Task CompleteMissionAsync_ShouldCompleteMission_UpdateIncident_AndNotify()
    {
        // Arrange
        var missionId = Guid.NewGuid();
        var incidentId = Guid.NewGuid();
        var rescuerId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var evidenceId1 = Guid.NewGuid();
        var evidenceId2 = Guid.NewGuid();
        var notes = "Mission completed with evidence";

        var missionEntity = new RescueMission
        {
            Id = missionId,
            IncidentId = incidentId,
            RescuerId = rescuerId,
            Status = RescueMissionStatus.RescuerArrived,
            Price = 100000m
        };

        var missionSnapshot = new RescueMission
        {
            Id = missionId,
            IncidentId = incidentId,
            RescuerId = rescuerId,
            Status = RescueMissionStatus.MissionCompleted,
            Price = 100000m,
            ActualCost = 150000m,
            Incident = new SnakebiteIncident
            {
                Id = incidentId,
                UserId = memberId,
                User = new MemberProfile
                {
                    Account = new Account { Id = memberId, FullName = "Member" }
                }
            },
            Rescuer = new RescuerProfile
            {
                AccountId = rescuerId,
                Account = new Account { Id = rescuerId, FullName = "Rescuer B" }
            }
        };

        var incident = new SnakebiteIncident
        {
            Id = incidentId,
            UserId = memberId,
            Status = SnakebiteIncidentStatus.Assigned
        };

        var evidenceList = new List<ReportMedia>
        {
            new()
            {
                Id = evidenceId1,
                ReferenceId = missionId,
                ReferenceType = MediaReferenceType.RescueMission,
                Purpose = MediaPurpose.Evidence,
                FileName = "evidence-1.jpg",
                MediaUrl = "https://example.com/evidence-1.jpg",
                ContentType = "image/jpeg"
            },
            new()
            {
                Id = evidenceId2,
                ReferenceId = missionId,
                ReferenceType = MediaReferenceType.RescueMission,
                Purpose = MediaPurpose.Evidence,
                FileName = "evidence-2.jpg",
                MediaUrl = "https://example.com/evidence-2.jpg",
                ContentType = "image/jpeg"
            }
        };

        _missionRepoMock
            .SetupSequence(r => r.FirstOrDefaultAsync(
                It.IsAny<Expression<Func<RescueMission, bool>>>(),
                It.IsAny<Func<IQueryable<RescueMission>, IOrderedQueryable<RescueMission>>>(),
                It.IsAny<Func<IQueryable<RescueMission>, IQueryable<RescueMission>>>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(missionEntity)
            .ReturnsAsync(missionSnapshot);

        _reportMediaRepoMock
            .Setup(r => r.GetListAsync(
                It.IsAny<Expression<Func<ReportMedia, bool>>>(),
                It.IsAny<Func<IQueryable<ReportMedia>, IOrderedQueryable<ReportMedia>>>(),
                It.IsAny<Func<IQueryable<ReportMedia>, IQueryable<ReportMedia>>>(),
                It.IsAny<int?>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(evidenceList);

        _incidentRepoMock
            .Setup(r => r.FirstOrDefaultAsync(
                It.IsAny<Expression<Func<SnakebiteIncident, bool>>>(),
                It.IsAny<Func<IQueryable<SnakebiteIncident>, IOrderedQueryable<SnakebiteIncident>>>(),
                It.IsAny<Func<IQueryable<SnakebiteIncident>, IQueryable<SnakebiteIncident>>>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(incident);

        // Act
        await _service.CompleteMissionAsync(missionId, new List<Guid> { evidenceId1, evidenceId2 }, notes);

        // Assert
        Assert.Equal(RescueMissionStatus.MissionCompleted, missionEntity.Status);
        Assert.NotNull(missionEntity.CompletedAt);
        Assert.Equal(notes, missionEntity.Notes);

        Assert.Equal(SnakebiteIncidentStatus.Finished, incident.Status);

        _missionRepoMock.Verify(r => r.Update(missionEntity), Times.Once);
        _incidentRepoMock.Verify(r => r.Update(incident), Times.Once);

        _missionNotificationMock.Verify(n => n.NotifyMissionCompletedAsync(
            incidentId,
            memberId,
            rescuerId,
            It.Is<MissionCompletedNotificationPayload>(p => p.MissionId == missionId)),
            Times.Once);

        _operatorRealtimeMock.Verify(n => n.NotifyIncidentCompletedAsync(incidentId, rescuerId), Times.Once);
    }

    [Fact]
    public async Task ReportHospitalTransferAsync_ShouldThrowBadRequest_WhenMissionStatusIsNotArrived()
    {
        // Arrange
        var missionId = Guid.NewGuid();
        var rescuerId = Guid.NewGuid();

        var mission = new RescueMission
        {
            Id = missionId,
            IncidentId = Guid.NewGuid(),
            RescuerId = rescuerId,
            Status = RescueMissionStatus.EnRoute,
            Price = 100000m,
            Incident = new SnakebiteIncident { Id = Guid.NewGuid(), UserId = Guid.NewGuid() }
        };

        _missionRepoMock
            .Setup(r => r.FirstOrDefaultAsync(
                It.IsAny<Expression<Func<RescueMission, bool>>>(),
                It.IsAny<Func<IQueryable<RescueMission>, IOrderedQueryable<RescueMission>>>(),
                It.IsAny<Func<IQueryable<RescueMission>, IQueryable<RescueMission>>>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(mission);

        var request = new ReportHospitalTransferRequest
        {
            HospitalId = 1,
            Notes = "Need transfer"
        };

        // Act + Assert
        await Assert.ThrowsAsync<BadRequestException>(() =>
            _service.ReportHospitalTransferAsync(missionId, rescuerId, request));
    }
}
