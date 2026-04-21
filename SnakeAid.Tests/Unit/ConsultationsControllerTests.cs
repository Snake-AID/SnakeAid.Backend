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

public class ConsultationsControllerTests
{
    [Fact]
    public void GetMessageHistory_ActionShouldExposeRouteAndResponseMetadata()
    {
        var method = typeof(ConsultationsController).GetMethod(nameof(ConsultationsController.GetMessageHistory));

        Assert.NotNull(method);

        var httpGet = Assert.Single(method!.GetCustomAttributes(typeof(HttpGetAttribute), inherit: true).Cast<HttpGetAttribute>());
        Assert.Equal("{consultationId:guid}/messages-history", httpGet.Template);

        Assert.Empty(method.GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true).Cast<AuthorizeAttribute>());

        var produces = Assert.Single(method.GetCustomAttributes(typeof(ProducesResponseTypeAttribute), inherit: true).Cast<ProducesResponseTypeAttribute>());
        Assert.Equal(StatusCodes.Status200OK, produces.StatusCode);
        Assert.Equal(typeof(ApiResponse<PagingResponse<ConsultationMessageHistoryItemResponse>>), produces.Type);
    }

    [Fact]
    public async Task GetMessageHistory_ShouldReturnSuccessEnvelope()
    {
        var consultationId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        var expected = new PagingResponse<ConsultationMessageHistoryItemResponse>
        {
            Items =
            [
                new ConsultationMessageHistoryItemResponse
                {
                    Id = Guid.NewGuid(),
                    ConsultationId = consultationId,
                    SenderId = actorId,
                    Content = "hello",
                    SentAt = DateTime.UtcNow
                }
            ],
            Meta = new PaginationMeta
            {
                CurrentPage = 1,
                PageSize = 50,
                TotalItems = 1,
                TotalPages = 1
            }
        };

        var service = new Mock<IConsultationService>();
        service.Setup(s => s.GetConsultationMessageHistoryAsync(
                consultationId,
                actorId,
                false,
                It.IsAny<ConsultationMessageHistoryQueryRequest>()))
            .ReturnsAsync(expected);

        var controller = BuildController(service.Object, actorId);

        var result = await controller.GetMessageHistory(
            consultationId,
            new ConsultationMessageHistoryQueryRequest { PageNumber = 1, PageSize = 50 });

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<PagingResponse<ConsultationMessageHistoryItemResponse>>>(ok.Value);

        Assert.True(response.IsSuccess);
        Assert.NotNull(response.Data);
        Assert.Single(response.Data!.Items);
        Assert.Equal(1, response.Data.Meta.CurrentPage);
    }

    [Fact]
    public void ReportExpertAbsent_ActionShouldExposeUserRouteAndResponseMetadata()
    {
        var method = typeof(ConsultationsController).GetMethod(nameof(ConsultationsController.ReportExpertAbsent));

        Assert.NotNull(method);

        var httpPost = Assert.Single(method!.GetCustomAttributes(typeof(HttpPostAttribute), inherit: true).Cast<HttpPostAttribute>());
        Assert.Equal("{consultationId:guid}/expert-absent-report", httpPost.Template);

        var authorize = Assert.Single(method.GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true).Cast<AuthorizeAttribute>());
        Assert.Equal("User", authorize.Roles);

        var produces = Assert.Single(method.GetCustomAttributes(typeof(ProducesResponseTypeAttribute), inherit: true).Cast<ProducesResponseTypeAttribute>());
        Assert.Equal(StatusCodes.Status200OK, produces.StatusCode);
        Assert.Equal(typeof(ApiResponse<MyConsultationResponse>), produces.Type);
    }

    [Fact]
    public async Task ReportExpertAbsent_ShouldReturnSuccessEnvelope()
    {
        var consultationId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var expected = new MyConsultationResponse
        {
            ConsultationId = consultationId,
            Type = "Scheduled",
            Status = "ExpertAbsent",
            ExpertId = Guid.NewGuid(),
            CustomerReport = "Expert did not join the room.",
            CustomerReportSubmittedAt = DateTime.UtcNow,
            StartTime = DateTime.UtcNow.AddMinutes(-5)
        };

        var service = new Mock<IConsultationService>();
        service.Setup(s => s.ReportExpertAbsentAsync(
                consultationId,
                memberId,
                It.IsAny<ReportExpertAbsentRequest>()))
            .ReturnsAsync(expected);

        var controller = BuildController(service.Object, memberId);

        var result = await controller.ReportExpertAbsent(
            consultationId,
            new ReportExpertAbsentRequest { CustomerReport = "Expert did not join the room." });

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<MyConsultationResponse>>(ok.Value);

        Assert.True(response.IsSuccess);
        Assert.NotNull(response.Data);
        Assert.Equal("ExpertAbsent", response.Data!.Status);
        Assert.Equal("Expert did not join the room.", response.Data.CustomerReport);
        Assert.NotNull(response.Data.CustomerReportSubmittedAt);
    }

    private static ConsultationsController BuildController(IConsultationService service, Guid userId, string role = "User")
    {
        var mapper = new Mock<IMapper>();
        var httpContextAccessor = new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                [
                    new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                    new Claim(ClaimTypes.Role, role)
                ], "Test"))
            }
        };

        return new ConsultationsController(
            NullLogger<ConsultationsController>.Instance,
            httpContextAccessor,
            mapper.Object,
            service);
    }
}
