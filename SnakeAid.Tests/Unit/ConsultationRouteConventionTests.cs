using System.Reflection;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using SnakeAid.Api.Controllers;

namespace SnakeAid.Tests.Unit;

public class ConsultationRouteConventionTests
{
    [Theory]
    [InlineData(typeof(ConsultationBookingsController), "GetMyBookings", "/api/users/me/consultation-bookings")]
    [InlineData(typeof(ConsultationBookingsController), "GetExpertBookings", "/api/experts/me/consultation-bookings")]
    [InlineData(typeof(ConsultationPaymentsController), "PayScheduledBooking", "/api/consultation-bookings/{bookingId:guid}/payments")]
    [InlineData(typeof(ConsultationPaymentsController), "PayEmergencyRequest", "/api/consultations/emergency-requests/{requestId:guid}/payments")]
    [InlineData(typeof(ConsultationsController), "CreateEmergencyConsultationRequest", "emergency-requests")]
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
