using MapsterMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SnakeAid.Api.Controllers;
using SnakeAid.Core.Domains;
using SnakeAid.Core.Exceptions;
using SnakeAid.Core.Meta;
using SnakeAid.Core.Requests.SnakeCatchingMission;
using SnakeAid.Core.Responses.SnakeCatchingMission;
using SnakeAid.Service.Interfaces;
using System.Security.Claims;

namespace SnakeAid.Tests.Unit;

public class SnakeCatchingMissionControllerTests
{
    [Fact]
    public void Controller_ShouldUseAuthorizeAndRouteAttributes()
    {
        var controllerType = typeof(SnakeCatchingMissionController);

        var authorize = Assert.Single(controllerType.GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true).Cast<AuthorizeAttribute>());
        var route = Assert.Single(controllerType.GetCustomAttributes(typeof(RouteAttribute), inherit: true).Cast<RouteAttribute>());

        Assert.Equal("api/snakecatching/missions", route.Template);
        Assert.Null(authorize.Roles);
    }

    [Theory]
    [InlineData(nameof(SnakeCatchingMissionController.StartMission), "{missionId}/start")]
    [InlineData(nameof(SnakeCatchingMissionController.MarkAsArrived), "{missionId}/arrived")]
    [InlineData(nameof(SnakeCatchingMissionController.CompleteMission), "{missionId}/complete")]
    [InlineData(nameof(SnakeCatchingMissionController.AbortMission), "{missionId}/abort")]
    public void Actions_ShouldUseExpectedHttpPatchRoute(string actionName, string template)
    {
        var method = typeof(SnakeCatchingMissionController).GetMethod(actionName);

        Assert.NotNull(method);
        var patch = Assert.Single(method!.GetCustomAttributes(typeof(HttpPatchAttribute), inherit: true).Cast<HttpPatchAttribute>());
        Assert.Equal(template, patch.Template);
    }

    [Fact]
    public async Task StartMission_ShouldUseCurrentUserAndReturnSuccessEnvelope()
    {
        var rescuerId = Guid.NewGuid();
        var missionId = Guid.NewGuid();
        var request = new UpdateMissionStatusRequest { Notes = "Heading out" };
        var expected = CreateMissionResponse(missionId, rescuerId, CatchingMissionStatus.EnRoute, request.Notes);

        var service = new Mock<ISnakeCatchingMissionService>();
        service.Setup(s => s.StartMissionAsync(rescuerId, missionId, request)).ReturnsAsync(expected);

        var controller = CreateController(service.Object, rescuerId);

        var result = await controller.StartMission(missionId, request);

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<SnakeCatchingMissionDetailResponse>>(ok.Value);

        Assert.True(response.IsSuccess);
        Assert.Equal(StatusCodes.Status200OK, response.StatusCode);
        Assert.Equal("Mission started! En route to location.", response.Message);
        Assert.NotNull(response.Data);
        Assert.Equal(CatchingMissionStatus.EnRoute, response.Data!.Status);

        service.Verify(s => s.StartMissionAsync(rescuerId, missionId, request), Times.Once);
    }

    [Fact]
    public async Task MarkAsArrived_ShouldUseCurrentUserAndReturnSuccessEnvelope()
    {
        var rescuerId = Guid.NewGuid();
        var missionId = Guid.NewGuid();
        var request = new UpdateMissionStatusRequest { Notes = "Arrived at point" };
        var expected = CreateMissionResponse(missionId, rescuerId, CatchingMissionStatus.Arrived, request.Notes);

        var service = new Mock<ISnakeCatchingMissionService>();
        service.Setup(s => s.MarkAsArrivedAsync(rescuerId, missionId, request)).ReturnsAsync(expected);

        var controller = CreateController(service.Object, rescuerId);

        var result = await controller.MarkAsArrived(missionId, request);

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<SnakeCatchingMissionDetailResponse>>(ok.Value);

        Assert.True(response.IsSuccess);
        Assert.Equal("Mission marked as arrived!", response.Message);
        Assert.Equal(CatchingMissionStatus.Arrived, response.Data!.Status);

        service.Verify(s => s.MarkAsArrivedAsync(rescuerId, missionId, request), Times.Once);
    }

    [Fact]
    public async Task CompleteMission_ShouldUseCurrentUserAndReturnSuccessEnvelope()
    {
        var rescuerId = Guid.NewGuid();
        var missionId = Guid.NewGuid();
        var request = new UpdateMissionStatusRequest { Notes = "Completed with evidence", CatchingEnvironmentId = 2 };
        var expected = CreateMissionResponse(missionId, rescuerId, CatchingMissionStatus.MissionCompleted, request.Notes);

        var service = new Mock<ISnakeCatchingMissionService>();
        service.Setup(s => s.CompleteMissionAsync(rescuerId, missionId, request)).ReturnsAsync(expected);

        var controller = CreateController(service.Object, rescuerId);

        var result = await controller.CompleteMission(missionId, request);

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<SnakeCatchingMissionDetailResponse>>(ok.Value);

        Assert.True(response.IsSuccess);
        Assert.Equal("Mission completed successfully! Request marked as completed.", response.Message);
        Assert.Equal(CatchingMissionStatus.MissionCompleted, response.Data!.Status);

        service.Verify(s => s.CompleteMissionAsync(rescuerId, missionId, request), Times.Once);
    }

    [Fact]
    public async Task AbortMission_ShouldUseCurrentUserAndReturnSuccessEnvelope()
    {
        var rescuerId = Guid.NewGuid();
        var missionId = Guid.NewGuid();
        var request = new AbortSnakeCatchingMissionRequest { Reason = "Unsafe condition" };
        var expected = CreateMissionResponse(missionId, rescuerId, CatchingMissionStatus.MissionAborted, cancellationReason: request.Reason);

        var service = new Mock<ISnakeCatchingMissionService>();
        service.Setup(s => s.AbortMissionAsync(rescuerId, missionId, request)).ReturnsAsync(expected);

        var controller = CreateController(service.Object, rescuerId);

        var result = await controller.AbortMission(missionId, request);

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<SnakeCatchingMissionDetailResponse>>(ok.Value);

        Assert.True(response.IsSuccess);
        Assert.Equal("Mission aborted successfully.", response.Message);
        Assert.Equal(CatchingMissionStatus.MissionAborted, response.Data!.Status);
        Assert.Equal(request.Reason, response.Data.CancellationReason);

        service.Verify(s => s.AbortMissionAsync(rescuerId, missionId, request), Times.Once);
    }

    [Theory]
    [InlineData("start")]
    [InlineData("arrived")]
    [InlineData("complete")]
    [InlineData("abort")]
    public async Task Actions_ShouldThrowUnauthorizedException_WhenNoUserIdClaim(string actionName)
    {
        var missionId = Guid.NewGuid();
        var service = new Mock<ISnakeCatchingMissionService>();
        var controller = CreateController(service.Object, userId: null);

        Task action = actionName switch
        {
            "start" => controller.StartMission(missionId, new UpdateMissionStatusRequest()),
            "arrived" => controller.MarkAsArrived(missionId, new UpdateMissionStatusRequest()),
            "complete" => controller.CompleteMission(missionId, new UpdateMissionStatusRequest()),
            _ => controller.AbortMission(missionId, new AbortSnakeCatchingMissionRequest { Reason = "R" })
        };

        var ex = await Assert.ThrowsAsync<UnauthorizedException>(() => action);
        Assert.Equal("User ID not found in token", ex.Message);

        service.Verify(s => s.StartMissionAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<UpdateMissionStatusRequest>()), Times.Never);
        service.Verify(s => s.MarkAsArrivedAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<UpdateMissionStatusRequest>()), Times.Never);
        service.Verify(s => s.CompleteMissionAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<UpdateMissionStatusRequest>()), Times.Never);
        service.Verify(s => s.AbortMissionAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<AbortSnakeCatchingMissionRequest>()), Times.Never);
    }

    [Theory]
    [InlineData("start", "bad-request")]
    [InlineData("arrived", "not-found")]
    [InlineData("complete", "unexpected")]
    [InlineData("abort", "bad-request")]
    public async Task Actions_ShouldPropagateServiceExceptions(string actionName, string exceptionType)
    {
        var missionId = Guid.NewGuid();
        var rescuerId = Guid.NewGuid();

        Exception ex = exceptionType switch
        {
            "bad-request" => new BadRequestException("bad request"),
            "not-found" => new NotFoundException("not found"),
            _ => new Exception("unexpected")
        };

        var service = new Mock<ISnakeCatchingMissionService>();

        switch (actionName)
        {
            case "start":
                service.Setup(s => s.StartMissionAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<UpdateMissionStatusRequest>())).ThrowsAsync(ex);
                break;
            case "arrived":
                service.Setup(s => s.MarkAsArrivedAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<UpdateMissionStatusRequest>())).ThrowsAsync(ex);
                break;
            case "complete":
                service.Setup(s => s.CompleteMissionAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<UpdateMissionStatusRequest>())).ThrowsAsync(ex);
                break;
            case "abort":
                service.Setup(s => s.AbortMissionAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<AbortSnakeCatchingMissionRequest>())).ThrowsAsync(ex);
                break;
        }

        var controller = CreateController(service.Object, rescuerId);

        Task action = actionName switch
        {
            "start" => controller.StartMission(missionId, new UpdateMissionStatusRequest()),
            "arrived" => controller.MarkAsArrived(missionId, new UpdateMissionStatusRequest()),
            "complete" => controller.CompleteMission(missionId, new UpdateMissionStatusRequest()),
            _ => controller.AbortMission(missionId, new AbortSnakeCatchingMissionRequest { Reason = "R" })
        };

        var thrown = await Assert.ThrowsAsync(ex.GetType(), () => action);
        Assert.Equal(ex.Message, thrown.Message);
    }

    private static SnakeCatchingMissionDetailResponse CreateMissionResponse(
        Guid missionId,
        Guid rescuerId,
        CatchingMissionStatus status,
        string? notes = null,
        string? cancellationReason = null)
    {
        return new SnakeCatchingMissionDetailResponse
        {
            Id = missionId,
            RescuerId = rescuerId,
            SnakeCatchingRequestId = Guid.NewGuid(),
            Status = status,
            Notes = notes,
            CancellationReason = cancellationReason,
            CreatedAt = DateTime.UtcNow
        };
    }

    private static SnakeCatchingMissionController CreateController(ISnakeCatchingMissionService service, Guid? userId = null)
    {
        ClaimsPrincipal principal;

        if (userId.HasValue)
        {
            principal = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, userId.Value.ToString()),
                new Claim(ClaimTypes.Role, "Rescuer")
            ], "Test"));
        }
        else
        {
            principal = new ClaimsPrincipal(new ClaimsIdentity());
        }

        var httpContextAccessor = new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };

        var mapper = new Mock<IMapper>();

        return new SnakeCatchingMissionController(
            NullLogger<SnakeCatchingMissionController>.Instance,
            httpContextAccessor,
            mapper.Object,
            service);
    }
}
