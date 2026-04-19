using System.Reflection;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using NetTopologySuite;
using NetTopologySuite.Geometries;
using SnakeAid.Core.Constants;
using SnakeAid.Core.Domains;
using SnakeAid.Core.Meta;
using SnakeAid.Core.Requests.Notification;
using SnakeAid.Core.Requests.RescueMission;
using SnakeAid.Core.Requests.SnakebiteIncident;
using SnakeAid.Core.Responses.RescueMission;
using SnakeAid.Core.Responses.SnakebiteIncident;
using SnakeAid.Core.Services;
using SnakeAid.Repository.Data;
using SnakeAid.Repository.Implements;
using SnakeAid.Service.Implements;
using SnakeAid.Service.Interfaces;
using SnakeAid.Service.Services.LocationIq.Models;

namespace SnakeAid.Tests.Integration;

public class SnakebiteAndMissionFlowIntegrationTests
{
    [Fact]
    public async Task CompleteMissionAsync_ShouldPersistState_AndSendNotifications()
    {
        var memberId = Guid.NewGuid();
        var rescuerId = Guid.NewGuid();
        var incidentId = Guid.NewGuid();
        var missionId = Guid.NewGuid();

        await using var db = CreateDbContext();
        await SeedMemberAndRescuerAsync(db, memberId, rescuerId);

        db.SnakebiteIncidents.Add(new SnakebiteIncident
        {
            Id = incidentId,
            UserId = memberId,
            Status = SnakebiteIncidentStatus.Assigned,
            LocationCoordinates = CreatePoint(106.70098, 10.77689),
            Address = "District 1"
        });

        db.RescueMissions.Add(new RescueMission
        {
            Id = missionId,
            IncidentId = incidentId,
            RescuerId = rescuerId,
            Status = RescueMissionStatus.RescuerArrived,
            Price = 500_000m
        });

        var evidence1 = Guid.NewGuid();
        var evidence2 = Guid.NewGuid();

        db.ReportMedias.AddRange(
            new ReportMedia
            {
                Id = evidence1,
                ReferenceId = missionId,
                ReferenceType = MediaReferenceType.RescueMission,
                Purpose = MediaPurpose.Evidence,
                FileName = "evidence-1.jpg",
                MediaUrl = "https://cdn.local/evidence-1.jpg",
                ContentType = "image/jpeg",
                FileSize = 1024
            },
            new ReportMedia
            {
                Id = evidence2,
                ReferenceId = missionId,
                ReferenceType = MediaReferenceType.RescueMission,
                Purpose = MediaPurpose.Evidence,
                FileName = "evidence-2.jpg",
                MediaUrl = "https://cdn.local/evidence-2.jpg",
                ContentType = "image/jpeg",
                FileSize = 2048
            });

        await db.SaveChangesAsync();

        var missionNotifications = new RecordingMissionNotificationService();
        var operatorNotifications = new RecordingOperatorRealtimeNotificationService();

        var service = new SnakeRescueMissionService(
            new UnitOfWork<SnakeAidDbContext>(db),
            NullLogger<SnakeRescueMissionService>.Instance,
            new TestSystemSettingService(),
            new TestLocationIqService(),
            missionNotifications,
            operatorNotifications);

        await service.CompleteMissionAsync(missionId, new List<Guid> { evidence1, evidence2 }, "done");

        var savedMission = await db.RescueMissions.FirstAsync(x => x.Id == missionId);
        var savedIncident = await db.SnakebiteIncidents.FirstAsync(x => x.Id == incidentId);

        Assert.Equal(RescueMissionStatus.MissionCompleted, savedMission.Status);
        Assert.NotNull(savedMission.CompletedAt);
        Assert.Equal("done", savedMission.Notes);
        Assert.Equal(SnakebiteIncidentStatus.Finished, savedIncident.Status);

        Assert.Single(missionNotifications.CompletedCalls);
        Assert.Equal(incidentId, missionNotifications.CompletedCalls[0].IncidentId);
        Assert.Equal(rescuerId, missionNotifications.CompletedCalls[0].RescuerId);

        Assert.Single(operatorNotifications.IncidentCompletedCalls);
        Assert.Equal((incidentId, rescuerId), operatorNotifications.IncidentCompletedCalls[0]);
    }

    [Fact]
    public async Task CancelIncidentAsync_WithPreparingMission_ShouldCancelMission_AndFreeRescuer()
    {
        var memberId = Guid.NewGuid();
        var rescuerId = Guid.NewGuid();
        var operatorId = Guid.NewGuid();
        var incidentId = Guid.NewGuid();
        var missionId = Guid.NewGuid();

        await using var db = CreateDbContext();
        await SeedMemberAndRescuerAsync(db, memberId, rescuerId, rescuerAvailable: false);
        await SeedOperatorAsync(db, operatorId);

        db.SnakebiteIncidents.Add(new SnakebiteIncident
        {
            Id = incidentId,
            UserId = memberId,
            Status = SnakebiteIncidentStatus.Assigned,
            AssignedRescuerId = rescuerId,
            AssignedAt = DateTime.UtcNow,
            DispatchedAt = DateTime.UtcNow,
            HandlingOperatorId = operatorId,
            LocationCoordinates = CreatePoint(106.6999, 10.7701),
            Address = "District 3"
        });

        db.RescueMissions.Add(new RescueMission
        {
            Id = missionId,
            IncidentId = incidentId,
            RescuerId = rescuerId,
            Status = RescueMissionStatus.Preparing,
            Price = 500_000m
        });

        await db.SaveChangesAsync();

        var operatorNotifications = new RecordingOperatorRealtimeNotificationService();
        var rescueNotifications = new RecordingRescueNotificationService();
        var missionNotifications = new RecordingMissionNotificationService();

        var incidentService = CreateIncidentService(db, operatorNotifications, rescueNotifications, missionNotifications);

        var response = await incidentService.CancelIncidentAsync(incidentId, new CancelIncidentRequest { Reason = "Member cancelled" });

        var savedIncident = await db.SnakebiteIncidents.FirstAsync(x => x.Id == incidentId);
        var savedMission = await db.RescueMissions.FirstAsync(x => x.Id == missionId);
        var savedRescuer = await db.RescuerProfiles.FirstAsync(x => x.AccountId == rescuerId);

        Assert.Equal(incidentId, response.Id);
        Assert.Equal(SnakebiteIncidentStatus.Cancelled, savedIncident.Status);
        Assert.Null(savedIncident.AssignedRescuerId);
        Assert.Equal(RescueMissionStatus.Cancelled, savedMission.Status);
        Assert.Equal("Member cancelled", savedMission.CancellationReason);
        Assert.True(savedRescuer.IsAvailable);

        Assert.Single(operatorNotifications.IncidentCancelledCalls);
        Assert.Single(missionNotifications.CancelledCalls);
        Assert.Empty(rescueNotifications.RequestCancelledCalls);
    }

    [Fact]
    public async Task CancelIncidentAsync_WithPendingRequests_ShouldCancelEachRequest_AndNotifyRescuers()
    {
        var memberId = Guid.NewGuid();
        var operatorId = Guid.NewGuid();
        var incidentId = Guid.NewGuid();
        var rescuer1 = Guid.NewGuid();
        var rescuer2 = Guid.NewGuid();

        await using var db = CreateDbContext();
        await SeedMemberAndRescuerAsync(db, memberId, rescuer1);
        await SeedRescuerAsync(db, rescuer2);
        await SeedOperatorAsync(db, operatorId);

        db.SnakebiteIncidents.Add(new SnakebiteIncident
        {
            Id = incidentId,
            UserId = memberId,
            Status = SnakebiteIncidentStatus.Verified,
            HandlingOperatorId = operatorId,
            LocationCoordinates = CreatePoint(106.6888, 10.7777),
            Address = "District 5"
        });

        var request1 = new RescuerRequest
        {
            Id = Guid.NewGuid(),
            IncidentId = incidentId,
            RescuerId = rescuer1,
            OperatorId = operatorId,
            Status = RescueRequestStatus.Pending,
            DispatchedAt = DateTime.UtcNow
        };

        var request2 = new RescuerRequest
        {
            Id = Guid.NewGuid(),
            IncidentId = incidentId,
            RescuerId = rescuer2,
            OperatorId = operatorId,
            Status = RescueRequestStatus.Pending,
            DispatchedAt = DateTime.UtcNow
        };

        db.RescuerRequests.AddRange(request1, request2);
        await db.SaveChangesAsync();

        var operatorNotifications = new RecordingOperatorRealtimeNotificationService();
        var rescueNotifications = new RecordingRescueNotificationService();
        var missionNotifications = new RecordingMissionNotificationService();

        var incidentService = CreateIncidentService(db, operatorNotifications, rescueNotifications, missionNotifications);

        await incidentService.CancelIncidentAsync(incidentId, new CancelIncidentRequest { Reason = "No longer needed" });

        var savedIncident = await db.SnakebiteIncidents.FirstAsync(x => x.Id == incidentId);
        var savedRequests = await db.RescuerRequests
            .Where(x => x.IncidentId == incidentId)
            .OrderBy(x => x.CreatedAt)
            .ToListAsync();

        Assert.Equal(SnakebiteIncidentStatus.Cancelled, savedIncident.Status);
        Assert.Equal(2, savedRequests.Count);
        Assert.All(savedRequests, x => Assert.Equal(RescueRequestStatus.Cancelled, x.Status));
        Assert.All(savedRequests, x => Assert.Equal("No longer needed", x.DeclineReason));
        Assert.All(savedRequests, x => Assert.NotNull(x.ResponseAt));

        Assert.Single(operatorNotifications.IncidentCancelledCalls);
        Assert.Equal(2, rescueNotifications.RequestCancelledCalls.Count);
        Assert.Empty(missionNotifications.CancelledCalls);
    }

    #region Admin Paging Tests

    [Fact]
    public async Task GetAdminIncidentsAsync_WithoutFilters_ShouldReturnAllIncidents()
    {
        var memberId1 = Guid.NewGuid();
        var memberId2 = Guid.NewGuid();
        var rescuerId = Guid.NewGuid();

        await using var db = CreateDbContext();
        await SeedMemberAsync(db, memberId1);
        await SeedMemberAsync(db, memberId2);
        await SeedRescuerAsync(db, rescuerId);

        // Create 3 incidents with different statuses
        var incident1 = Guid.NewGuid();
        var incident2 = Guid.NewGuid();
        var incident3 = Guid.NewGuid();

        db.SnakebiteIncidents.AddRange(
            new SnakebiteIncident
            {
                Id = incident1,
                UserId = memberId1,
                Status = SnakebiteIncidentStatus.Verified,
                LocationCoordinates = CreatePoint(106.70098, 10.77689),
                Address = "District 1"
            },
            new SnakebiteIncident
            {
                Id = incident2,
                UserId = memberId1,
                Status = SnakebiteIncidentStatus.Assigned,
                LocationCoordinates = CreatePoint(106.70098, 10.77689),
                Address = "District 2"
            },
            new SnakebiteIncident
            {
                Id = incident3,
                UserId = memberId2,
                Status = SnakebiteIncidentStatus.Finished,
                LocationCoordinates = CreatePoint(106.70098, 10.77689),
                Address = "District 3"
            });

        await db.SaveChangesAsync();

        var incidentService = new SnakebiteIncidentService(
            new UnitOfWork<SnakeAidDbContext>(db),
            NullLogger<SnakebiteIncidentService>.Instance,
            new ConfigurationBuilder().Build(),
            new RecordingOperatorRealtimeNotificationService(),
            new RecordingRescueNotificationService(),
            new RecordingMissionNotificationService(),
            new NoopSnakeRescueMissionService());

        var result = await incidentService.GetAdminIncidentsAsync(
            statuses: null,
            since: null,
            until: null,
            page: 1,
            pageSize: 50);

        Assert.NotNull(result);
        Assert.Equal(3, result.Items.Count());
        Assert.Equal(1, result.Meta.CurrentPage);
        Assert.Equal(50, result.Meta.PageSize);
        Assert.Equal(3, result.Meta.TotalItems);
    }

    [Fact]
    public async Task GetAdminIncidentsAsync_WithStatusFilter_ShouldReturnOnlyMatchingIncidents()
    {
        var memberId = Guid.NewGuid();

        await using var db = CreateDbContext();
        await SeedMemberAsync(db, memberId);

        var incident1 = Guid.NewGuid();
        var incident2 = Guid.NewGuid();
        var incident3 = Guid.NewGuid();

        db.SnakebiteIncidents.AddRange(
            new SnakebiteIncident
            {
                Id = incident1,
                UserId = memberId,
                Status = SnakebiteIncidentStatus.Verified,
                LocationCoordinates = CreatePoint(106.70098, 10.77689),
                Address = "District 1"
            },
            new SnakebiteIncident
            {
                Id = incident2,
                UserId = memberId,
                Status = SnakebiteIncidentStatus.Assigned,
                LocationCoordinates = CreatePoint(106.70098, 10.77689),
                Address = "District 2"
            },
            new SnakebiteIncident
            {
                Id = incident3,
                UserId = memberId,
                Status = SnakebiteIncidentStatus.Finished,
                LocationCoordinates = CreatePoint(106.70098, 10.77689),
                Address = "District 3"
            });

        await db.SaveChangesAsync();

        var incidentService = new SnakebiteIncidentService(
            new UnitOfWork<SnakeAidDbContext>(db),
            NullLogger<SnakebiteIncidentService>.Instance,
            new ConfigurationBuilder().Build(),
            new RecordingOperatorRealtimeNotificationService(),
            new RecordingRescueNotificationService(),
            new RecordingMissionNotificationService(),
            new NoopSnakeRescueMissionService());

        var result = await incidentService.GetAdminIncidentsAsync(
            statuses: new[] { SnakebiteIncidentStatus.Verified, SnakebiteIncidentStatus.Assigned },
            since: null,
            until: null,
            page: 1,
            pageSize: 50);

        var itemsList = result.Items.ToList();
        Assert.Equal(2, itemsList.Count);
        Assert.All(result.Items, item =>
            Assert.True(item.Status == SnakebiteIncidentStatus.Verified || item.Status == SnakebiteIncidentStatus.Assigned));
    }

    [Fact]
    public async Task GetAdminIncidentsAsync_WithPagination_ShouldReturnCorrectPage()
    {
        var memberId = Guid.NewGuid();

        await using var db = CreateDbContext();
        await SeedMemberAsync(db, memberId);

        // Create 5 incidents
        for (int i = 0; i < 5; i++)
        {
            db.SnakebiteIncidents.Add(new SnakebiteIncident
            {
                Id = Guid.NewGuid(),
                UserId = memberId,
                Status = SnakebiteIncidentStatus.Verified,
                LocationCoordinates = CreatePoint(106.70098, 10.77689),
                Address = $"District {i}"
            });
        }

        await db.SaveChangesAsync();

        var incidentService = new SnakebiteIncidentService(
            new UnitOfWork<SnakeAidDbContext>(db),
            NullLogger<SnakebiteIncidentService>.Instance,
            new ConfigurationBuilder().Build(),
            new RecordingOperatorRealtimeNotificationService(),
            new RecordingRescueNotificationService(),
            new RecordingMissionNotificationService(),
            new NoopSnakeRescueMissionService());

        var page1 = await incidentService.GetAdminIncidentsAsync(
            statuses: null,
            since: null,
            until: null,
            page: 1,
            pageSize: 2);

        var page2 = await incidentService.GetAdminIncidentsAsync(
            statuses: null,
            since: null,
            until: null,
            page: 2,
            pageSize: 2);

        Assert.Equal(2, page1.Items.Count());
        Assert.Equal(2, page2.Items.Count());
        Assert.Equal(5, page1.Meta.TotalItems);
        Assert.Equal(1, page1.Meta.CurrentPage);
        Assert.Equal(2, page2.Meta.CurrentPage);
    }

    [Fact]
    public async Task GetAdminMissionListAsync_WithoutFilters_ShouldReturnAllMissions()
    {
        var memberId = Guid.NewGuid();
        var rescuer1 = Guid.NewGuid();
        var rescuer2 = Guid.NewGuid();

        await using var db = CreateDbContext();
        await SeedMemberAsync(db, memberId);
        await SeedRescuerAsync(db, rescuer1);
        await SeedRescuerAsync(db, rescuer2);

        var incident1 = Guid.NewGuid();
        var incident2 = Guid.NewGuid();

        db.SnakebiteIncidents.AddRange(
            new SnakebiteIncident
            {
                Id = incident1,
                UserId = memberId,
                Status = SnakebiteIncidentStatus.Verified,
                LocationCoordinates = CreatePoint(106.70098, 10.77689),
                Address = "District 1"
            },
            new SnakebiteIncident
            {
                Id = incident2,
                UserId = memberId,
                Status = SnakebiteIncidentStatus.Assigned,
                LocationCoordinates = CreatePoint(106.70098, 10.77689),
                Address = "District 2"
            });

        var mission1 = Guid.NewGuid();
        var mission2 = Guid.NewGuid();
        var mission3 = Guid.NewGuid();

        db.RescueMissions.AddRange(
            new RescueMission
            {
                Id = mission1,
                IncidentId = incident1,
                RescuerId = rescuer1,
                Status = RescueMissionStatus.Preparing,
                Price = 500_000m
            },
            new RescueMission
            {
                Id = mission2,
                IncidentId = incident1,
                RescuerId = rescuer2,
                Status = RescueMissionStatus.EnRoute,
                Price = 600_000m
            },
            new RescueMission
            {
                Id = mission3,
                IncidentId = incident2,
                RescuerId = rescuer1,
                Status = RescueMissionStatus.MissionCompleted,
                Price = 700_000m
            });

        await db.SaveChangesAsync();

        var missionService = new SnakeRescueMissionService(
            new UnitOfWork<SnakeAidDbContext>(db),
            NullLogger<SnakeRescueMissionService>.Instance,
            new TestSystemSettingService(),
            new TestLocationIqService(),
            new RecordingMissionNotificationService(),
            new RecordingOperatorRealtimeNotificationService());

        var result = await missionService.GetAdminMissionListAsync(
            statuses: null,
            since: null,
            until: null,
            page: 1,
            pageSize: 50);

        Assert.NotNull(result);
        Assert.Equal(3, result.Items.Count());
        Assert.Equal(1, result.Meta.CurrentPage);
        Assert.Equal(50, result.Meta.PageSize);
        Assert.Equal(3, result.Meta.TotalItems);
    }

    [Fact]
    public async Task GetAdminMissionListAsync_WithStatusFilter_ShouldReturnOnlyMatchingMissions()
    {
        var memberId = Guid.NewGuid();
        var rescuer1 = Guid.NewGuid();
        var rescuer2 = Guid.NewGuid();

        await using var db = CreateDbContext();
        await SeedMemberAsync(db, memberId);
        await SeedRescuerAsync(db, rescuer1);
        await SeedRescuerAsync(db, rescuer2);

        var incident1 = Guid.NewGuid();

        db.SnakebiteIncidents.Add(new SnakebiteIncident
        {
            Id = incident1,
            UserId = memberId,
            Status = SnakebiteIncidentStatus.Verified,
            LocationCoordinates = CreatePoint(106.70098, 10.77689),
            Address = "District 1"
        });

        var mission1 = Guid.NewGuid();
        var mission2 = Guid.NewGuid();
        var mission3 = Guid.NewGuid();

        db.RescueMissions.AddRange(
            new RescueMission
            {
                Id = mission1,
                IncidentId = incident1,
                RescuerId = rescuer1,
                Status = RescueMissionStatus.Preparing,
                Price = 500_000m
            },
            new RescueMission
            {
                Id = mission2,
                IncidentId = incident1,
                RescuerId = rescuer2,
                Status = RescueMissionStatus.EnRoute,
                Price = 600_000m
            },
            new RescueMission
            {
                Id = mission3,
                IncidentId = incident1,
                RescuerId = rescuer1,
                Status = RescueMissionStatus.MissionCompleted,
                Price = 700_000m
            });

        await db.SaveChangesAsync();

        var missionService = new SnakeRescueMissionService(
            new UnitOfWork<SnakeAidDbContext>(db),
            NullLogger<SnakeRescueMissionService>.Instance,
            new TestSystemSettingService(),
            new TestLocationIqService(),
            new RecordingMissionNotificationService(),
            new RecordingOperatorRealtimeNotificationService());

        var result = await missionService.GetAdminMissionListAsync(
            statuses: new[] { RescueMissionStatus.Preparing, RescueMissionStatus.EnRoute },
            since: null,
            until: null,
            page: 1,
            pageSize: 50);

        var itemsList = result.Items.ToList();
        Assert.Equal(2, itemsList.Count);
        Assert.All(itemsList, item =>
            Assert.True(item.Status == RescueMissionStatus.Preparing || item.Status == RescueMissionStatus.EnRoute));
    }


    [Fact]
    public async Task GetAdminMissionListAsync_WithPagination_ShouldReturnCorrectPage()
    {
        var memberId = Guid.NewGuid();
        var rescuer = Guid.NewGuid();

        await using var db = CreateDbContext();
        await SeedMemberAsync(db, memberId);
        await SeedRescuerAsync(db, rescuer);

        var incident = Guid.NewGuid();

        db.SnakebiteIncidents.Add(new SnakebiteIncident
        {
            Id = incident,
            UserId = memberId,
            Status = SnakebiteIncidentStatus.Verified,
            LocationCoordinates = CreatePoint(106.70098, 10.77689),
            Address = "District 1"
        });

        // Create 5 missions
        for (int i = 0; i < 5; i++)
        {
            db.RescueMissions.Add(new RescueMission
            {
                Id = Guid.NewGuid(),
                IncidentId = incident,
                RescuerId = rescuer,
                Status = RescueMissionStatus.Preparing,
                Price = 500_000m + (i * 100_000m)
            });
        }

        await db.SaveChangesAsync();

        var missionService = new SnakeRescueMissionService(
            new UnitOfWork<SnakeAidDbContext>(db),
            NullLogger<SnakeRescueMissionService>.Instance,
            new TestSystemSettingService(),
            new TestLocationIqService(),
            new RecordingMissionNotificationService(),
            new RecordingOperatorRealtimeNotificationService());

        var page1 = await missionService.GetAdminMissionListAsync(
            statuses: null,
            since: null,
            until: null,
            page: 1,
            pageSize: 2);

        var page2 = await missionService.GetAdminMissionListAsync(
            statuses: null,
            since: null,
            until: null,
            page: 2,
            pageSize: 2);

        Assert.Equal(2, page1.Items.Count());
        Assert.Equal(2, page2.Items.Count());
        Assert.Equal(5, page1.Meta.TotalItems);
        Assert.Equal(1, page1.Meta.CurrentPage);
        Assert.Equal(2, page2.Meta.CurrentPage);
    }

    [Fact]
    public async Task GetAdminMissionListAsync_ShouldHaveCorrectResponseStructure()
    {
        var memberId = Guid.NewGuid();
        var rescuer = Guid.NewGuid();

        await using var db = CreateDbContext();
        await SeedMemberAsync(db, memberId);
        await SeedRescuerAsync(db, rescuer);

        var incident = Guid.NewGuid();

        db.SnakebiteIncidents.Add(new SnakebiteIncident
        {
            Id = incident,
            UserId = memberId,
            Status = SnakebiteIncidentStatus.Verified,
            LocationCoordinates = CreatePoint(106.70098, 10.77689),
            Address = "Test Address"
        });

        var missionId = Guid.NewGuid();

        db.RescueMissions.Add(new RescueMission
        {
            Id = missionId,
            IncidentId = incident,
            RescuerId = rescuer,
            Status = RescueMissionStatus.MissionCompleted,
            Price = 500_000m,
            ActualCost = 450_000m,
            StartedAt = DateTime.UtcNow.AddHours(-2),
            ArrivedAt = DateTime.UtcNow.AddHours(-1),
            CompletedAt = DateTime.UtcNow
        });

        await db.SaveChangesAsync();

        var missionService = new SnakeRescueMissionService(
            new UnitOfWork<SnakeAidDbContext>(db),
            NullLogger<SnakeRescueMissionService>.Instance,
            new TestSystemSettingService(),
            new TestLocationIqService(),
            new RecordingMissionNotificationService(),
            new RecordingOperatorRealtimeNotificationService());

        var result = await missionService.GetAdminMissionListAsync(
            statuses: null,
            since: null,
            until: null,
            page: 1,
            pageSize: 50);

        var itemsList = result.Items.ToList();
        Assert.Single(itemsList);
        var mission = itemsList[0];

        Assert.Equal(missionId, mission.Id);
        Assert.Equal(incident, mission.IncidentId);
        Assert.Equal(rescuer, mission.RescuerId);
        Assert.Equal(RescueMissionStatus.MissionCompleted, mission.Status);
        Assert.Equal(500_000m, mission.Price);
        Assert.Equal(450_000m, mission.ActualCost);
        Assert.Equal(SnakebiteIncidentStatus.Verified, mission.IncidentStatus);
        Assert.Equal("Test Address", mission.IncidentAddress);
        Assert.NotNull(mission.RescuerName);
    }

    #endregion

    private static SnakebiteIncidentService CreateIncidentService(
        SnakeAidDbContext db,
        RecordingOperatorRealtimeNotificationService operatorNotifications,
        RecordingRescueNotificationService rescueNotifications,
        RecordingMissionNotificationService missionNotifications)
    {
        return new SnakebiteIncidentService(
            new UnitOfWork<SnakeAidDbContext>(db),
            NullLogger<SnakebiteIncidentService>.Instance,
            new ConfigurationBuilder().Build(),
            operatorNotifications,
            rescueNotifications,
            missionNotifications,
            new NoopSnakeRescueMissionService());
    }

    private static SnakeAidDbContext CreateDbContext()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<SnakeAidDbContext>()
            .UseSqlite(connection)
            .Options;

        var context = new SnakebiteMissionSqliteDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    private static Point CreatePoint(double longitude, double latitude)
    {
        var geometryFactory = NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);
        return geometryFactory.CreatePoint(new Coordinate(longitude, latitude));
    }

    private static async Task SeedMemberAndRescuerAsync(
        SnakeAidDbContext db,
        Guid memberId,
        Guid rescuerId,
        bool rescuerAvailable = true)
    {
        await SeedMemberAsync(db, memberId);
        await SeedRescuerAsync(db, rescuerId, rescuerAvailable);
    }

    private static async Task SeedMemberAsync(SnakeAidDbContext db, Guid memberId)
    {
        db.Users.Add(new Account
        {
            Id = memberId,
            UserName = $"member.{memberId:N}",
            NormalizedUserName = $"MEMBER.{memberId:N}",
            Email = $"member.{memberId:N}@example.com",
            NormalizedEmail = $"MEMBER.{memberId:N}@EXAMPLE.COM",
            FullName = "Member User",
            IsActive = true,
            Role = AccountRole.User
        });

        db.MemberProfiles.Add(new MemberProfile
        {
            AccountId = memberId,
            Rating = 0,
            RatingCount = 0,
            EmergencyContacts = new List<string>()
        });

        await db.SaveChangesAsync();
    }

    private static async Task SeedRescuerAsync(SnakeAidDbContext db, Guid rescuerId, bool available = true)
    {
        db.Users.Add(new Account
        {
            Id = rescuerId,
            UserName = $"rescuer.{rescuerId:N}",
            NormalizedUserName = $"RESCUER.{rescuerId:N}",
            Email = $"rescuer.{rescuerId:N}@example.com",
            NormalizedEmail = $"RESCUER.{rescuerId:N}@EXAMPLE.COM",
            FullName = "Rescuer User",
            IsActive = true,
            Role = AccountRole.Rescuer
        });

        db.RescuerProfiles.Add(new RescuerProfile
        {
            AccountId = rescuerId,
            IsOnline = true,
            IsAvailable = available,
            Type = RescuerType.Emergency,
            Rating = 0,
            RatingCount = 0,
            TotalMissions = 0,
            CompletedMissions = 0
        });

        await db.SaveChangesAsync();
    }

    private static async Task SeedOperatorAsync(SnakeAidDbContext db, Guid operatorId)
    {
        db.Users.Add(new Account
        {
            Id = operatorId,
            UserName = $"operator.{operatorId:N}",
            NormalizedUserName = $"OPERATOR.{operatorId:N}",
            Email = $"operator.{operatorId:N}@example.com",
            NormalizedEmail = $"OPERATOR.{operatorId:N}@EXAMPLE.COM",
            FullName = "Operator User",
            IsActive = true,
            Role = AccountRole.Operator
        });

        await db.SaveChangesAsync();
    }

    private sealed class SnakebiteMissionSqliteDbContext : SnakeAidDbContext
    {
        public SnakebiteMissionSqliteDbContext(DbContextOptions<SnakeAidDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            var keep = new HashSet<Type>
            {
                typeof(Account),
                typeof(MemberProfile),
                typeof(RescuerProfile),
                typeof(SnakebiteIncident),
                typeof(RescueMission),
                typeof(RescuerRequest),
                typeof(ReportMedia)
            };

            var dbSetEntityTypes = typeof(SnakeAidDbContext)
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.PropertyType.IsGenericType && p.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>))
                .Select(p => p.PropertyType.GetGenericArguments()[0])
                .Distinct();

            foreach (var type in dbSetEntityTypes)
            {
                if (!keep.Contains(type))
                {
                    modelBuilder.Ignore(type);
                }
            }

            modelBuilder.Entity<Account>(entity => entity.HasKey(x => x.Id));

            modelBuilder.Entity<MemberProfile>(entity =>
            {
                entity.HasKey(x => x.AccountId);
                entity.Property(x => x.EmergencyContacts).HasConversion(
                    v => string.Join("|", v),
                    v => string.IsNullOrEmpty(v) ? new List<string>() : v.Split('|', StringSplitOptions.RemoveEmptyEntries).ToList());
                entity.HasOne(x => x.Account)
                    .WithOne()
                    .HasForeignKey<MemberProfile>(x => x.AccountId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<RescuerProfile>(entity =>
            {
                entity.HasKey(x => x.AccountId);
                entity.Ignore(x => x.LastLocation);
                entity.HasOne(x => x.Account)
                    .WithOne()
                    .HasForeignKey<RescuerProfile>(x => x.AccountId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<SnakebiteIncident>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Ignore(x => x.LocationCoordinates);
                entity.Ignore(x => x.SymptomsReport);
                entity.Ignore(x => x.FilterAnswers);
                entity.Ignore(x => x.IdentifiedSnakeSpecies);
                entity.Ignore(x => x.AIRecognitionResult);
                entity.Property(x => x.Version)
                    .IsConcurrencyToken()
                    .ValueGeneratedNever()
                    .HasDefaultValue(0u);

                entity.HasOne(x => x.User)
                    .WithMany(x => x.SnakebiteIncidents)
                    .HasForeignKey(x => x.UserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.AssignedRescuer)
                    .WithMany()
                    .HasForeignKey(x => x.AssignedRescuerId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(x => x.HandlingOperator)
                    .WithMany()
                    .HasForeignKey(x => x.HandlingOperatorId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<RescueMission>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.HasOne(x => x.Incident)
                    .WithMany(x => x.Missions)
                    .HasForeignKey(x => x.IncidentId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(x => x.Rescuer)
                    .WithMany(x => x.Missions)
                    .HasForeignKey(x => x.RescuerId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.Ignore(x => x.Hospital);
            });

            modelBuilder.Entity<RescuerRequest>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.HasOne(x => x.Incident)
                    .WithMany(x => x.DispatchRequests)
                    .HasForeignKey(x => x.IncidentId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(x => x.Rescuer)
                    .WithMany(x => x.RescuerRequests)
                    .HasForeignKey(x => x.RescuerId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.Operator)
                    .WithMany()
                    .HasForeignKey(x => x.OperatorId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<ReportMedia>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Ignore(x => x.AIRecognitionResults);
            });
        }
    }

    private sealed class RecordingOperatorRealtimeNotificationService : IOperatorRealtimeNotificationService
    {
        public List<(Guid IncidentId, string? Reason)> IncidentCancelledCalls { get; } = [];
        public List<(Guid IncidentId, Guid RescuerId)> IncidentCompletedCalls { get; } = [];

        public Task NotifyNewIncidentCreatedAsync(Guid incidentId, Guid memberId, double latitude, double longitude, string? address) => Task.CompletedTask;
        public Task NotifyIncidentClaimedAsync(Guid incidentId, Guid operatorId) => Task.CompletedTask;

        public Task NotifyIncidentCancelledAsync(Guid incidentId, string? reason)
        {
            IncidentCancelledCalls.Add((incidentId, reason));
            return Task.CompletedTask;
        }

        public Task NotifyIncidentFalseAlarmAsync(Guid incidentId, Guid operatorId, string? reason) => Task.CompletedTask;
        public Task NotifyIncidentNoAnswerAsync(Guid incidentId, Guid operatorId, string? reason, bool continueCalling) => Task.CompletedTask;
        public Task NotifyDispatchRequestedAsync(Guid incidentId, Guid rescuerId, Guid operatorId) => Task.CompletedTask;
        public Task NotifyRescuerDispatchedAsync(Guid incidentId, Guid rescuerId) => Task.CompletedTask;
        public Task NotifyRescuerDeclinedAsync(Guid incidentId, Guid rescuerId, string? reason) => Task.CompletedTask;
        public Task NotifyRescuerAbortedAsync(Guid incidentId, Guid rescuerId, Guid? operatorId, string? reason) => Task.CompletedTask;

        public Task NotifyIncidentCompletedAsync(Guid incidentId, Guid rescuerId)
        {
            IncidentCompletedCalls.Add((incidentId, rescuerId));
            return Task.CompletedTask;
        }
    }

    private sealed class RecordingRescueNotificationService : IRescueNotificationService
    {
        public List<(string RescuerId, Guid RequestId)> RequestCancelledCalls { get; } = [];

        public bool IsRescuerConnected(string rescuerId) => true;
        public Task NotifyDispatchRequestedAsync(string rescuerId, DispatchRequestNotificationPayload requestData) => Task.CompletedTask;
        public Task NotifyRescuerAcceptedAsync(string rescuerId, AcceptRescueResponse acceptedData) => Task.CompletedTask;
        public Task NotifyRescuerDeclinedAsync(string rescuerId, RejectRescueResponse declinedData) => Task.CompletedTask;

        public Task NotifyRequestCancelledAsync(string rescuerId, Guid requestId, string? cancelReason = null)
        {
            RequestCancelledCalls.Add((rescuerId, requestId));
            return Task.CompletedTask;
        }

        public Task NotifyRequestExpiredAsync(string rescuerId, Guid requestId) => Task.CompletedTask;
        public Task ForceDisconnectRescuerAsync(string rescuerId, string reason) => Task.CompletedTask;
    }

    private sealed class RecordingMissionNotificationService : IMissionNotificationService
    {
        public List<(Guid IncidentId, Guid RescuerId, string Reason)> CancelledCalls { get; } = [];
        public List<(Guid IncidentId, Guid RescuerId)> CompletedCalls { get; } = [];

        public Task NotifyRescuerAcceptedAsync(Guid incidentId, Guid memberUserId, AcceptRescueResponse rescuerInfo) => Task.CompletedTask;
        public Task NotifyMissionStartedAsync(Guid incidentId, Guid memberUserId, Guid rescuerUserId, MissionStartedNotificationPayload missionInfo) => Task.CompletedTask;
        public Task NotifyRescuerArrivedAsync(Guid incidentId, Guid memberUserId, Guid rescuerUserId, string? rescuerName = null) => Task.CompletedTask;

        public Task NotifyMissionCompletedAsync(Guid incidentId, Guid memberUserId, Guid rescuerUserId, MissionCompletedNotificationPayload result)
        {
            CompletedCalls.Add((incidentId, rescuerUserId));
            return Task.CompletedTask;
        }

        public Task NotifyMissionCancelledAsync(Guid incidentId, Guid rescuerUserId, string reason)
        {
            CancelledCalls.Add((incidentId, rescuerUserId, reason));
            return Task.CompletedTask;
        }

        public Task NotifyMissionAbortedAsync(Guid incidentId, Guid memberUserId, Guid rescuerUserId, string? rescuerName, string reason) => Task.CompletedTask;
    }

    private sealed class TestSystemSettingService : ISystemSettingService
    {
        public Task LoadSettingsAsync() => Task.CompletedTask;
        public T? GetSetting<T>(string key) => default;

        public T GetSetting<T>(string key, T defaultValue)
        {
            if (key == SystemSettingKeys.RescueDefaultPrice && typeof(T) == typeof(decimal))
            {
                return (T)(object)500000m;
            }

            if (key == SystemSettingKeys.RescuePricePerKmDefault && typeof(T) == typeof(decimal))
            {
                return (T)(object)5000m;
            }

            if (key == SystemSettingKeys.PricingCenterLatitude && typeof(T) == typeof(double))
            {
                return (T)(object)10.8391267;
            }

            if (key == SystemSettingKeys.PricingCenterLongitude && typeof(T) == typeof(double))
            {
                return (T)(object)106.8413534;
            }

            return defaultValue;
        }

        public Task<SystemSetting?> GetByKeyAsync(string key) => Task.FromResult<SystemSetting?>(null);
        public Task<IReadOnlyCollection<SystemSetting>> GetAllAsync() => Task.FromResult<IReadOnlyCollection<SystemSetting>>(Array.Empty<SystemSetting>());
        public Task<SystemSetting> UpsertAsync(string key, string value, SettingValueType valueType, string? description = null) => throw new NotImplementedException();
        public Task RefreshSettingAsync(string key) => Task.CompletedTask;
        public Task RefreshAllSettingsAsync() => Task.CompletedTask;
    }

    private sealed class TestLocationIqService : ILocationIqService
    {
        public Task<double> CalculateDistanceAsync(double sourceLng, double sourceLat, double destLng, double destLat)
            => Task.FromResult(10d);

        public decimal CalculatePrice(double distanceInKm, decimal pricePerKilometer)
            => 50_000m;

        public Task<(double distanceInKm, decimal priceInVnd)> CalculateDistanceAndPriceAsync(
            double sourceLng,
            double sourceLat,
            double destLng,
            double destLat,
            decimal pricePerKm)
            => Task.FromResult((10d, 50_000m));
    }

    private sealed class NoopSnakeRescueMissionService : ISnakeRescueMissionService
    {
        public Task<RescueMission> CreateMissionAsync(Guid incidentId, Guid rescuerId) => throw new NotImplementedException();
        public Task UpdateMissionStatusAsync(Guid missionId, RescueMissionStatus status) => throw new NotImplementedException();
        public Task CompleteMissionAsync(Guid missionId, List<Guid> evidenceMediaIds, string? completionNotes) => throw new NotImplementedException();
        public Task UserCancelMissionAsync(Guid missionId, string reason) => throw new NotImplementedException();
        public Task RescuerAbortMissionAsync(Guid missionId, string reason) => throw new NotImplementedException();
        public Task<RescueMission> GetMissionByIdAsync(Guid missionId) => throw new NotImplementedException();
        public Task<DetailRescueMissionResponse> GetMissionDetailAsync(Guid missionId) => throw new NotImplementedException();
        public Task<DetailRescueMissionResponse> GetMissionDetailAsync(Guid missionId, double? rescuerLat, double? rescuerLng) => throw new NotImplementedException();
        public Task<HospitalTransferPricingResponse> ReportHospitalTransferAsync(Guid missionId, Guid rescuerId, ReportHospitalTransferRequest request) => throw new NotImplementedException();
        public Task<PagedData<AdminRescueMissionSummaryResponse>> GetAdminMissionListAsync(IEnumerable<RescueMissionStatus>? statuses, DateTimeOffset? since, DateTimeOffset? until, int page, int pageSize) => throw new NotImplementedException();
    }
}
