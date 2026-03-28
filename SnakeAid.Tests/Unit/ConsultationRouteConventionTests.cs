using System.Reflection;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using SnakeAid.Api.Controllers;

namespace SnakeAid.Tests.Unit;

public class ConsultationRouteConventionTests
{
    [Theory]
    [InlineData(typeof(ConsultationBookingsController), "GetMyBookings", "/api/users/me/consultations/scheduled")]
    [InlineData(typeof(ConsultationBookingsController), "GetExpertBookings", "/api/experts/me/consultations/scheduled")]
    [InlineData(typeof(ConsultationPaymentsController), "PayScheduledBooking", "/api/consultations/scheduled/{bookingId:guid}/payments")]
    [InlineData(typeof(ConsultationPaymentsController), "PayEmergencyRequest", "/api/consultations/instant/{requestId:guid}/payments")]
    [InlineData(typeof(ConsultationsController), "CreateEmergencyConsultationRequest", "instant")]
    public void ConsultationRoutes_ShouldMatchApprovedConvention(Type controllerType, string methodName, string expectedTemplate)
    {
        var method = controllerType.GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public);
        Assert.NotNull(method);

        var routeAttribute = method!.GetCustomAttributes()
            .OfType<HttpMethodAttribute>()
            .Single();

        Assert.Equal(expectedTemplate, routeAttribute.Template);
    }
}
