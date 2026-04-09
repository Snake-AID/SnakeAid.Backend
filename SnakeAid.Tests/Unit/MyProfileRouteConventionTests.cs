using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Routing;
using SnakeAid.Api.Controllers;

namespace SnakeAid.Tests.Unit;

public class MyProfileRouteConventionTests
{
    [Theory]
    [InlineData("GetMemberProfile", "GET", "members/me/profile", "User")]
    [InlineData("UpdateMemberProfile", "PUT", "members/me/profile", "User")]
    [InlineData("GetExpertProfile", "GET", "experts/me/profile", "Expert")]
    [InlineData("UpdateExpertProfile", "PUT", "experts/me/profile", "Expert")]
    [InlineData("GetRescuerProfile", "GET", "rescuers/me/profile", "Rescuer")]
    [InlineData("UpdateRescuerProfile", "PUT", "rescuers/me/profile", "Rescuer")]
    public void MyProfileRoutes_ShouldKeepApprovedContract(
        string methodName,
        string httpMethod,
        string routeTemplate,
        string role)
    {
        var method = typeof(MyProfileController).GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public);
        Assert.NotNull(method);

        var routeAttribute = method!.GetCustomAttributes()
            .OfType<HttpMethodAttribute>()
            .Single();
        var authorizeAttribute = method.GetCustomAttributes()
            .OfType<AuthorizeAttribute>()
            .Single();

        Assert.Equal(routeTemplate, routeAttribute.Template);
        Assert.Equal(httpMethod, routeAttribute.HttpMethods.Single());
        Assert.Equal(role, authorizeAttribute.Roles);
    }
}
