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
using SnakeAid.Core.Requests.SnakeCatchingRequest;
using SnakeAid.Core.Responses.SnakeCatchingRequest;
using SnakeAid.Service.Interfaces;
using System.Security.Claims;

namespace SnakeAid.Tests.Unit;

public class SnakeCatchingRequestControllerTests
{
    [Fact]
    public void Controller_ShouldUseAuthorizeAndRouteAttributes()
    {
        var controllerType = typeof(SnakeCatchingRequestController);

        var authorize = Assert.Single(controllerType.GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true).Cast<AuthorizeAttribute>());
        var route = Assert.Single(controllerType.GetCustomAttributes(typeof(RouteAttribute), inherit: true).Cast<RouteAttribute>());

        Assert.Equal("api/snakecatching/requests", route.Template);
        Assert.Null(authorize.Roles);
    }

    [Fact]
    public void GetActiveSnakeCatchingRequests_ShouldRestrictToOperatorAndAdmin()
    {
        var method = typeof(SnakeCatchingRequestController).GetMethod(nameof(SnakeCatchingRequestController.GetActiveSnakeCatchingRequests));

        Assert.NotNull(method);
        var authorize = Assert.Single(method!.GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true).Cast<AuthorizeAttribute>());
        Assert.Equal("Operator,Admin", authorize.Roles);
    }

    [Fact]
    public async Task GetAllSnakeCatchingRequests_ShouldReturnSuccessEnvelopeAndMessage()
    {
        var expected = new List<ListSnakeCatchingRequestResponse>
        {
            new()
            {
                Id = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                Address = "Test Address",
                AdditionalDetails = "Test details",
                Status = RequestStatus.Pending
            }
        };

        var service = new Mock<ISnakeCatchingRequestService>();
        service.Setup(s => s.GetAllRequestAsync(It.IsAny<GetAllSnakeCatchingRequestsQuery>())).ReturnsAsync(expected);

        var controller = CreateController(service.Object);

        var result = await controller.GetAllSnakeCatchingRequests(new GetAllSnakeCatchingRequestsQuery());

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<List<ListSnakeCatchingRequestResponse>>>(ok.Value);

        Assert.True(response.IsSuccess);
        Assert.Equal(StatusCodes.Status200OK, response.StatusCode);
        Assert.Equal("Retrieved 1 snake catching request(s) successfully.", response.Message);
        Assert.NotNull(response.Data);
        Assert.Single(response.Data!);
    }

    [Fact]
    public async Task GetActiveSnakeCatchingRequests_ShouldParseValidStatusesCsv()
    {
        IEnumerable<RequestStatus>? capturedStatuses = null;

        var expected = new PagedData<OperatorSnakeCatchingRequestSummaryResponse>
        {
            Items =
            [
                new OperatorSnakeCatchingRequestSummaryResponse
                {
                    Id = Guid.NewGuid(),
                    Status = RequestStatus.Pending,
                    Address = "A",
                    Priority = RequestPriority.Normal
                }
            ],
            Meta = new PaginationMeta
            {
                CurrentPage = 1,
                PageSize = 20,
                TotalItems = 1,
                TotalPages = 1
            }
        };

        var service = new Mock<ISnakeCatchingRequestService>();
        service.Setup(s => s.GetActiveRequestsAsync(It.IsAny<IEnumerable<RequestStatus>?>(), It.IsAny<DateTimeOffset?>(), It.IsAny<DateTimeOffset?>(), It.IsAny<int>(), It.IsAny<int>()))
            .Callback<IEnumerable<RequestStatus>?, DateTimeOffset?, DateTimeOffset?, int, int>((statuses, _, _, _, _) => capturedStatuses = statuses)
            .ReturnsAsync(expected);

        var controller = CreateController(service.Object);

        var result = await controller.GetActiveSnakeCatchingRequests("pending,confirmed,INVALID", null, null, 1, 20);

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<PagedData<OperatorSnakeCatchingRequestSummaryResponse>>>(ok.Value);

        Assert.True(response.IsSuccess);
        Assert.Equal("Active snake catching requests retrieved successfully.", response.Message);
        Assert.NotNull(capturedStatuses);
        Assert.Equal(new[] { RequestStatus.Pending, RequestStatus.Confirmed }, capturedStatuses!.ToArray());
    }

    [Fact]
    public async Task GetActiveSnakeCatchingRequests_ShouldPassNullStatusesWhenCsvInvalidOrEmpty()
    {
        IEnumerable<RequestStatus>? capturedStatuses = new[] { RequestStatus.Pending };

        var expected = new PagedData<OperatorSnakeCatchingRequestSummaryResponse>
        {
            Items = Array.Empty<OperatorSnakeCatchingRequestSummaryResponse>(),
            Meta = new PaginationMeta { CurrentPage = 1, PageSize = 50, TotalItems = 0, TotalPages = 0 }
        };

        var service = new Mock<ISnakeCatchingRequestService>();
        service.Setup(s => s.GetActiveRequestsAsync(It.IsAny<IEnumerable<RequestStatus>?>(), It.IsAny<DateTimeOffset?>(), It.IsAny<DateTimeOffset?>(), It.IsAny<int>(), It.IsAny<int>()))
            .Callback<IEnumerable<RequestStatus>?, DateTimeOffset?, DateTimeOffset?, int, int>((statuses, _, _, _, _) => capturedStatuses = statuses)
            .ReturnsAsync(expected);

        var controller = CreateController(service.Object);

        await controller.GetActiveSnakeCatchingRequests("   ,unknown", null, null, 1, 50);

        Assert.Null(capturedStatuses);
    }

    [Fact]
    public async Task CreateSnakeCatchingRequest_ShouldUseCurrentUserAndReturnSuccessMessage()
    {
        var userId = Guid.NewGuid();
        var request = new CreateSnakeCatchingRequestRequest
        {
            Address = "123 Street",
            Lng = 106.7,
            Lat = 10.7,
            AdditionalDetails = "Snake seen in yard"
        };

        var expected = new CreateSnakeCatchingRequestResponse
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Address = request.Address,
            AdditionalDetails = request.AdditionalDetails,
            Status = RequestStatus.Pending
        };

        var service = new Mock<ISnakeCatchingRequestService>();
        service.Setup(s => s.CreateSnakeCatchingRequestAsync(userId, request)).ReturnsAsync(expected);

        var controller = CreateController(service.Object, userId);

        var result = await controller.CreateSnakeCatchingRequest(request);

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<CreateSnakeCatchingRequestResponse>>(ok.Value);

        Assert.True(response.IsSuccess);
        Assert.Equal("Snake catching request created successfully! Our team will review and assign a rescuer soon.", response.Message);
        Assert.NotNull(response.Data);
        Assert.Equal(userId, response.Data!.UserId);

        service.Verify(s => s.CreateSnakeCatchingRequestAsync(userId, request), Times.Once);
    }

    [Fact]
    public async Task CreateSnakeCatchingRequest_ShouldThrowUnauthorizedException_WhenNoUserIdClaim()
    {
        var service = new Mock<ISnakeCatchingRequestService>();
        var controller = CreateController(service.Object, userId: null);

        var request = new CreateSnakeCatchingRequestRequest
        {
            Address = "A",
            Lng = 106,
            Lat = 10,
            AdditionalDetails = "D"
        };

        var ex = await Assert.ThrowsAsync<UnauthorizedException>(() => controller.CreateSnakeCatchingRequest(request));
        Assert.Equal("User ID not found in token", ex.Message);

        service.Verify(s => s.CreateSnakeCatchingRequestAsync(It.IsAny<Guid>(), It.IsAny<CreateSnakeCatchingRequestRequest>()), Times.Never);
    }

    [Fact]
    public async Task ConfirmSnakeCatchingRequest_ShouldReturnSuccessMessage()
    {
        var requestId = Guid.NewGuid();
        var expected = new CreateSnakeCatchingRequestResponse
        {
            Id = requestId,
            UserId = Guid.NewGuid(),
            Address = "A",
            AdditionalDetails = "D",
            Status = RequestStatus.Confirmed
        };

        var service = new Mock<ISnakeCatchingRequestService>();
        service.Setup(s => s.ConfirmSnakeCatchingRequestAsync(requestId)).ReturnsAsync(expected);

        var controller = CreateController(service.Object);

        var result = await controller.ConfirmSnakeCatchingRequest(requestId);

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<CreateSnakeCatchingRequestResponse>>(ok.Value);

        Assert.True(response.IsSuccess);
        Assert.Equal("Snake catching request confirmed successfully!", response.Message);
        Assert.Equal(RequestStatus.Confirmed, response.Data!.Status);
    }

    [Fact]
    public async Task AssignSnakeCatchingRequest_ShouldReturnSuccessMessage()
    {
        var requestId = Guid.NewGuid();
        var assignRequest = new AssignSnakeCatchingRequestRequest { rescuerId = Guid.NewGuid() };

        var expected = new CreateSnakeCatchingRequestResponse
        {
            Id = requestId,
            UserId = Guid.NewGuid(),
            Address = "A",
            AdditionalDetails = "D",
            Status = RequestStatus.Assigned,
            AssignedRescuerId = assignRequest.rescuerId
        };

        var service = new Mock<ISnakeCatchingRequestService>();
        service.Setup(s => s.AssignSnakeCatchingRequestAsync(requestId, assignRequest)).ReturnsAsync(expected);

        var controller = CreateController(service.Object);

        var result = await controller.AssignSnakeCatchingRequest(requestId, assignRequest);

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<CreateSnakeCatchingRequestResponse>>(ok.Value);

        Assert.True(response.IsSuccess);
        Assert.Equal("Snake catching request assigned successfully! Mission created.", response.Message);
        Assert.Equal(assignRequest.rescuerId, response.Data!.AssignedRescuerId);
    }

    [Fact]
    public async Task GetSnakeCatchingRequestDetail_ShouldReturnSuccessMessage()
    {
        var requestId = Guid.NewGuid();
        var expected = new DetailSnakeCatchingRequestResponse
        {
            Id = requestId,
            UserId = Guid.NewGuid(),
            Address = "Detail Address",
            AdditionalDetails = "Detail info",
            Status = RequestStatus.Pending
        };

        var service = new Mock<ISnakeCatchingRequestService>();
        service.Setup(s => s.GetDetailAsync(requestId)).ReturnsAsync(expected);

        var controller = CreateController(service.Object);

        var result = await controller.GetSnakeCatchingRequestDetail(requestId);

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<DetailSnakeCatchingRequestResponse>>(ok.Value);

        Assert.True(response.IsSuccess);
        Assert.Equal("Snake catching request details retrieved successfully.", response.Message);
        Assert.Equal(requestId, response.Data!.Id);
    }

    [Fact]
    public async Task CancelSnakeCatchingRequest_ShouldUseCurrentUserAndReturnSuccessMessage()
    {
        var userId = Guid.NewGuid();
        var userRole = "Member";
        var requestId = Guid.NewGuid();
        var request = new CancelSnakeCatchingRequestRequest { Reason = "Changed plan" };

        var expected = new DetailSnakeCatchingRequestResponse
        {
            Id = requestId,
            UserId = userId,
            Address = "A",
            AdditionalDetails = "D",
            Status = RequestStatus.Cancelled,
            CancellationReason = request.Reason
        };

        var service = new Mock<ISnakeCatchingRequestService>();
        service.Setup(s => s.CancelSnakeCatchingRequestAsync(userId, userRole, requestId, request)).ReturnsAsync(expected);

        var controller = CreateController(service.Object, userId);

        var result = await controller.CancelSnakeCatchingRequest(requestId, request);

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<DetailSnakeCatchingRequestResponse>>(ok.Value);

        Assert.True(response.IsSuccess);
        Assert.Equal("Snake catching request cancelled successfully.", response.Message);
        Assert.Equal(RequestStatus.Cancelled, response.Data!.Status);

        service.Verify(s => s.CancelSnakeCatchingRequestAsync(userId, userRole, requestId, request), Times.Once);
    }

    [Fact]
    public async Task CancelSnakeCatchingRequest_ShouldThrowUnauthorizedException_WhenNoUserIdClaim()
    {
        var service = new Mock<ISnakeCatchingRequestService>();
        var controller = CreateController(service.Object, userId: null);

        var ex = await Assert.ThrowsAsync<UnauthorizedException>(() => controller.CancelSnakeCatchingRequest(Guid.NewGuid(), new CancelSnakeCatchingRequestRequest { Reason = "R" }));

        Assert.Equal("User ID not found in token", ex.Message);
        service.Verify(s => s.CancelSnakeCatchingRequestAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancelSnakeCatchingRequestRequest>()), Times.Never);
    }

    [Theory]
    [InlineData("get-all", "bad-request")]
    [InlineData("get-active", "not-found")]
    [InlineData("create", "unexpected")]
    [InlineData("confirm", "bad-request")]
    [InlineData("assign", "not-found")]
    [InlineData("detail", "bad-request")]
    [InlineData("cancel", "unexpected")]
    public async Task Actions_ShouldPropagateServiceExceptions(string actionName, string exceptionType)
    {
        var requestId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        Exception ex = exceptionType switch
        {
            "bad-request" => new BadRequestException("bad request"),
            "not-found" => new NotFoundException("not found"),
            _ => new Exception("unexpected")
        };

        var service = new Mock<ISnakeCatchingRequestService>();

        switch (actionName)
        {
            case "get-all":
                service.Setup(s => s.GetAllRequestAsync(It.IsAny<GetAllSnakeCatchingRequestsQuery>())).ThrowsAsync(ex);
                break;
            case "get-active":
                service.Setup(s => s.GetActiveRequestsAsync(It.IsAny<IEnumerable<RequestStatus>?>(), It.IsAny<DateTimeOffset?>(), It.IsAny<DateTimeOffset?>(), It.IsAny<int>(), It.IsAny<int>())).ThrowsAsync(ex);
                break;
            case "create":
                service.Setup(s => s.CreateSnakeCatchingRequestAsync(It.IsAny<Guid>(), It.IsAny<CreateSnakeCatchingRequestRequest>())).ThrowsAsync(ex);
                break;
            case "confirm":
                service.Setup(s => s.ConfirmSnakeCatchingRequestAsync(It.IsAny<Guid>())).ThrowsAsync(ex);
                break;
            case "assign":
                service.Setup(s => s.AssignSnakeCatchingRequestAsync(It.IsAny<Guid>(), It.IsAny<AssignSnakeCatchingRequestRequest>())).ThrowsAsync(ex);
                break;
            case "detail":
                service.Setup(s => s.GetDetailAsync(It.IsAny<Guid>())).ThrowsAsync(ex);
                break;
            case "cancel":
                service.Setup(s => s.CancelSnakeCatchingRequestAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancelSnakeCatchingRequestRequest>())).ThrowsAsync(ex);
                break;
        }

        var controller = CreateController(service.Object, userId);

        Task action = actionName switch
        {
            "get-all" => controller.GetAllSnakeCatchingRequests(new GetAllSnakeCatchingRequestsQuery()),
            "get-active" => controller.GetActiveSnakeCatchingRequests(),
            "create" => controller.CreateSnakeCatchingRequest(new CreateSnakeCatchingRequestRequest { Address = "A", Lng = 106, Lat = 10, AdditionalDetails = "D" }),
            "confirm" => controller.ConfirmSnakeCatchingRequest(requestId),
            "assign" => controller.AssignSnakeCatchingRequest(requestId, new AssignSnakeCatchingRequestRequest { rescuerId = Guid.NewGuid() }),
            "detail" => controller.GetSnakeCatchingRequestDetail(requestId),
            _ => controller.CancelSnakeCatchingRequest(requestId, new CancelSnakeCatchingRequestRequest { Reason = "R" })
        };

        var thrown = await Assert.ThrowsAsync(ex.GetType(), () => action);
        Assert.Equal(ex.Message, thrown.Message);
    }

    private static SnakeCatchingRequestController CreateController(ISnakeCatchingRequestService service, Guid? userId = null)
    {
        ClaimsPrincipal principal;

        if (userId.HasValue)
        {
            principal = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, userId.Value.ToString()),
                new Claim(ClaimTypes.Role, "Member")
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

        return new SnakeCatchingRequestController(
            NullLogger<SnakeCatchingRequestController>.Instance,
            httpContextAccessor,
            mapper.Object,
            service);
    }
}
