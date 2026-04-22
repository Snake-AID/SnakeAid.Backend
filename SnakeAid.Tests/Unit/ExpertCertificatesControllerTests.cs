using MapsterMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SnakeAid.Api.Controllers;
using SnakeAid.Core.Meta;
using SnakeAid.Core.Requests.ExpertCertificate;
using SnakeAid.Core.Responses.ExpertCertificate;
using SnakeAid.Service.Interfaces;
using System.Security.Claims;

namespace SnakeAid.Tests.Unit;

public class ExpertCertificatesControllerTests
{
    [Fact]
    public void ExpertController_ShouldUseExpertRouteAndAuthorization()
    {
        var controllerType = typeof(ExpertCertificatesController);

        var authorize = Assert.Single(controllerType.GetCustomAttributes(typeof(AuthorizeAttribute), true).Cast<AuthorizeAttribute>());
        var route = Assert.Single(controllerType.GetCustomAttributes(typeof(RouteAttribute), true).Cast<RouteAttribute>());

        Assert.Equal("Expert", authorize.Roles);
        Assert.Equal("api/experts/me/certificates", route.Template);
    }

    [Fact]
    public void AdminController_ShouldUseAdminRouteAndAuthorization()
    {
        var controllerType = typeof(AdminExpertCertificatesController);

        var authorize = Assert.Single(controllerType.GetCustomAttributes(typeof(AuthorizeAttribute), true).Cast<AuthorizeAttribute>());
        var route = Assert.Single(controllerType.GetCustomAttributes(typeof(RouteAttribute), true).Cast<RouteAttribute>());

        Assert.Equal("Admin", authorize.Roles);
        Assert.Equal("api/admin/expert/certificates", route.Template);
    }

    [Fact]
    public async Task ExpertCreate_ShouldReturnCreatedEnvelope()
    {
        var expertId = Guid.NewGuid();
        var expected = new ExpertCertificateResponse
        {
            Id = Guid.NewGuid(),
            ExpertId = expertId,
            CertificateName = "Clinical Toxicology",
            IssuingOrganization = "SnakeAid",
            IssueDate = DateTime.UtcNow,
            VerificationStatus = SnakeAid.Core.Domains.VerificationStatus.Pending
        };

        var service = new Mock<IExpertCertificateService>();
        service.Setup(s => s.CreateMyAsync(expertId, It.IsAny<CreateExpertCertificateRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var controller = new ExpertCertificatesController(
            NullLogger<ExpertCertificatesController>.Instance,
            BuildHttpContextAccessor(expertId, "Expert"),
            Mock.Of<IMapper>(),
            service.Object);

        var result = await controller.Create(new CreateExpertCertificateRequest
        {
            CertificateName = "Clinical Toxicology",
            IssuingOrganization = "SnakeAid",
            IssueDate = DateTime.UtcNow,
            ReportMediaIds = [Guid.NewGuid()]
        });

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status201Created, objectResult.StatusCode);
        var payload = Assert.IsType<ApiResponse<ExpertCertificateResponse>>(objectResult.Value);
        Assert.True(payload.IsSuccess);
        Assert.NotNull(payload.Data);
        Assert.Equal(expected.Id, payload.Data!.Id);
    }

    private static HttpContextAccessor BuildHttpContextAccessor(Guid userId, string role)
    {
        return new HttpContextAccessor
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
    }
}
