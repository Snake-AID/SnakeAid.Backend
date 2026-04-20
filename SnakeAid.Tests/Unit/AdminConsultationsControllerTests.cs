using MapsterMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SnakeAid.Api.Controllers;
using SnakeAid.Core.Meta;
using SnakeAid.Core.Requests.Consultation;
using SnakeAid.Core.Responses.Consultation;
using SnakeAid.Service.Interfaces;
using System.Security.Claims;

namespace SnakeAid.Tests.Unit;

public class AdminConsultationsControllerTests
{
    [Fact]
    public void Controller_ShouldUseAdminRouteAndAuthorization()
    {
        var controllerType = typeof(AdminConsultationsController);

        var authorize = Assert.Single(controllerType.GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true).Cast<AuthorizeAttribute>());
        var route = Assert.Single(controllerType.GetCustomAttributes(typeof(RouteAttribute), inherit: true).Cast<RouteAttribute>());

        Assert.Equal("Admin", authorize.Roles);
        Assert.Equal("api/admin/consultations", route.Template);
    }

    [Fact]
    public void GetAllConsultations_ActionShouldExposeHttpGetAndPagingResponseMetadata()
    {
        var method = typeof(AdminConsultationsController).GetMethod(nameof(AdminConsultationsController.GetAllConsultations));

        Assert.NotNull(method);
        Assert.Single(method!.GetCustomAttributes(typeof(HttpGetAttribute), inherit: true).Cast<HttpGetAttribute>());

        var produces = Assert.Single(method.GetCustomAttributes(typeof(ProducesResponseTypeAttribute), inherit: true).Cast<ProducesResponseTypeAttribute>());
        Assert.Equal(StatusCodes.Status200OK, produces.StatusCode);
        Assert.Equal(typeof(ApiResponse<PagingResponse<AdminConsultationResponse>>), produces.Type);
    }

    [Fact]
    public void GetConsultationById_ActionShouldExposeGuidRouteAndResponseMetadata()
    {
        var method = typeof(AdminConsultationsController).GetMethod(nameof(AdminConsultationsController.GetConsultationById));

        Assert.NotNull(method);

        var httpGet = Assert.Single(method!.GetCustomAttributes(typeof(HttpGetAttribute), inherit: true).Cast<HttpGetAttribute>());
        Assert.Equal("{consultationId:guid}", httpGet.Template);

        var produces = Assert.Single(method.GetCustomAttributes(typeof(ProducesResponseTypeAttribute), inherit: true).Cast<ProducesResponseTypeAttribute>());
        Assert.Equal(StatusCodes.Status200OK, produces.StatusCode);
        Assert.Equal(typeof(ApiResponse<AdminConsultationResponse>), produces.Type);
    }

    [Fact]
    public async Task GetAllConsultations_ShouldReturnSuccessEnvelope()
    {
        var expected = new PagingResponse<AdminConsultationResponse>
        {
            Items =
            [
                new AdminConsultationResponse
                {
                    ConsultationId = Guid.NewGuid(),
                    Type = "Emergency",
                    Status = "Completed",
                    UserId = Guid.NewGuid(),
                    ExpertId = Guid.NewGuid(),
                    StartTime = DateTime.UtcNow
                }
            ],
            Meta = new PaginationMeta
            {
                CurrentPage = 1,
                PageSize = 10,
                TotalItems = 1,
                TotalPages = 1
            }
        };

        var service = new Mock<IConsultationService>();
        service.Setup(s => s.GetAllConsultationsForAdminAsync(It.IsAny<AdminConsultationsQueryRequest>()))
            .ReturnsAsync(expected);

        var mapper = new Mock<IMapper>();
        var httpContextAccessor = new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                [
                    new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
                    new Claim(ClaimTypes.Role, "Admin")
                ], "Test"))
            }
        };

        var controller = new AdminConsultationsController(
            service.Object,
            NullLogger<AdminConsultationsController>.Instance,
            httpContextAccessor,
            mapper.Object);

        var result = await controller.GetAllConsultations(new AdminConsultationsQueryRequest
        {
            PageNumber = 1,
            PageSize = 10
        });

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<PagingResponse<AdminConsultationResponse>>>(ok.Value);

        Assert.True(response.IsSuccess);
        Assert.NotNull(response.Data);
        Assert.Single(response.Data!.Items);
        Assert.Equal(1, response.Data.Meta.TotalItems);
    }

    [Fact]
    public async Task GetConsultationById_ShouldReturnSuccessEnvelope()
    {
        var consultationId = Guid.NewGuid();
        var expected = new AdminConsultationResponse
        {
            ConsultationId = consultationId,
            Type = "Scheduled",
            Status = "Completed",
            UserId = Guid.NewGuid(),
            ExpertId = Guid.NewGuid(),
            BookingId = Guid.NewGuid(),
            BookingStatus = "Completed",
            StartTime = DateTime.UtcNow
        };

        var service = new Mock<IConsultationService>();
        service.Setup(s => s.GetConsultationByIdForAdminAsync(consultationId))
            .ReturnsAsync(expected);

        var mapper = new Mock<IMapper>();
        var httpContextAccessor = new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                [
                    new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
                    new Claim(ClaimTypes.Role, "Admin")
                ], "Test"))
            }
        };

        var controller = new AdminConsultationsController(
            service.Object,
            NullLogger<AdminConsultationsController>.Instance,
            httpContextAccessor,
            mapper.Object);

        var result = await controller.GetConsultationById(consultationId);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<AdminConsultationResponse>>(ok.Value);

        Assert.True(response.IsSuccess);
        Assert.NotNull(response.Data);
        Assert.Equal(consultationId, response.Data!.ConsultationId);
        Assert.Equal("Completed", response.Data.BookingStatus);
    }
}
