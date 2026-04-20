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

    private static ConsultationsController BuildController(IConsultationService service, Guid userId)
    {
        var mapper = new Mock<IMapper>();
        var httpContextAccessor = new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                [
                    new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                    new Claim(ClaimTypes.Role, "User")
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
