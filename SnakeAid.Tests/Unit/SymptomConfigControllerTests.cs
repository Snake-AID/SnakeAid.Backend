using Microsoft.AspNetCore.Authorization;
using SnakeAid.Api.Controllers;

namespace SnakeAid.Tests.Unit;

public class SymptomConfigControllerTests
{
    [Theory]
    [InlineData(nameof(SymptomConfigController.CreateSymptomConfig))]
    [InlineData(nameof(SymptomConfigController.GetSymptomConfigById))]
    [InlineData(nameof(SymptomConfigController.FilterSymptomConfigs))]
    [InlineData(nameof(SymptomConfigController.GetAllSymptomConfig))]
    [InlineData(nameof(SymptomConfigController.UpdateSymptomConfig))]
    [InlineData(nameof(SymptomConfigController.DeleteSymptomConfig))]
    public void AdminActions_ShouldRequireAdminRole(string actionName)
    {
        var method = typeof(SymptomConfigController).GetMethods()
            .Single(method => method.Name == actionName);

        var authorize = Assert.Single(method.GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true)
            .Cast<AuthorizeAttribute>());

        Assert.Equal("Admin", authorize.Roles);
    }

    [Theory]
    [InlineData(nameof(SymptomConfigController.GetSymptomConfigsGrouped))]
    [InlineData(nameof(SymptomConfigController.GetSymptomConfigsGroupedForUI))]
    public void ActiveGroupedReadActions_ShouldRemainPublic(string actionName)
    {
        var method = typeof(SymptomConfigController).GetMethods()
            .Single(method => method.Name == actionName);

        Assert.Empty(method.GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true)
            .Cast<AuthorizeAttribute>());
    }
}
