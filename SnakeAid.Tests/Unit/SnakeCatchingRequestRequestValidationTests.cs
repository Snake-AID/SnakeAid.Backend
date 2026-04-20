using SnakeAid.Core.Requests.SnakeCatchingRequest;
using System.ComponentModel.DataAnnotations;

namespace SnakeAid.Tests.Unit;

public class SnakeCatchingRequestRequestValidationTests
{
    [Theory]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("Valid Address", true)]
    public void CreateRequest_Address_ShouldValidateRequired(string? address, bool expectedValid)
    {
        var request = ValidCreateRequest();
        request.Address = address!;

        var isValid = TryValidate(request, out var results);

        Assert.Equal(expectedValid, isValid);
        if (!expectedValid)
        {
            Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateSnakeCatchingRequestRequest.Address)));
        }
    }

    [Fact]
    public void CreateRequest_Address_ShouldRejectLengthAbove1000()
    {
        var request = ValidCreateRequest();
        request.Address = new string('A', 1001);

        var isValid = TryValidate(request, out var results);

        Assert.False(isValid);
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateSnakeCatchingRequestRequest.Address)));
    }

    [Theory]
    [InlineData(-180, true)]
    [InlineData(180, true)]
    [InlineData(-181, false)]
    [InlineData(181, false)]
    [InlineData(0, true)]
    public void CreateRequest_Lng_ShouldValidateRange(double lng, bool expectedValid)
    {
        var request = ValidCreateRequest();
        request.Lng = lng;

        var isValid = TryValidate(request, out var _);

        Assert.Equal(expectedValid, isValid);
    }

    [Theory]
    [InlineData(-90, true)]
    [InlineData(90, true)]
    [InlineData(-91, false)]
    [InlineData(91, false)]
    [InlineData(10.762622, true)]
    public void CreateRequest_Lat_ShouldValidateRange(double lat, bool expectedValid)
    {
        var request = ValidCreateRequest();
        request.Lat = lat;

        var isValid = TryValidate(request, out var _);

        Assert.Equal(expectedValid, isValid);
    }

    [Fact]
    public void CreateRequest_AdditionalDetails_ShouldRejectLengthAbove2000()
    {
        var request = ValidCreateRequest();
        request.AdditionalDetails = new string('D', 2001);

        var isValid = TryValidate(request, out var results);

        Assert.False(isValid);
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateSnakeCatchingRequestRequest.AdditionalDetails)));
    }

    [Fact]
    public void CreateRequest_Notes_ShouldRejectLengthAbove1000()
    {
        var request = ValidCreateRequest();
        request.Notes = new string('N', 1001);

        var isValid = TryValidate(request, out var results);

        Assert.False(isValid);
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateSnakeCatchingRequestRequest.Notes)));
    }

    [Theory]
    [InlineData(1, true)]
    [InlineData(100, true)]
    [InlineData(0, false)]
    [InlineData(101, false)]
    public void SnakeSpeciesItem_Quantity_ShouldValidateRange(int quantity, bool expectedValid)
    {
        var item = new SnakeSpeciesRequestItem
        {
            SnakeSpeciesId = 1,
            Quantity = quantity
        };

        var isValid = TryValidate(item, out var results);

        Assert.Equal(expectedValid, isValid);
        if (!expectedValid)
        {
            Assert.Contains(results, r => r.MemberNames.Contains(nameof(SnakeSpeciesRequestItem.Quantity)));
        }
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("   ", false)]
    [InlineData("Need to cancel", true)]
    public void CancelRequest_Reason_ShouldValidateRequired(string? reason, bool expectedValid)
    {
        var request = new CancelSnakeCatchingRequestRequest { Reason = reason! };

        var isValid = TryValidate(request, out var results);

        Assert.Equal(expectedValid, isValid);
        if (!expectedValid)
        {
            Assert.Contains(results, r => r.MemberNames.Contains(nameof(CancelSnakeCatchingRequestRequest.Reason)));
        }
    }

    [Fact]
    public void CancelRequest_Reason_ShouldRejectLengthAbove500()
    {
        var request = new CancelSnakeCatchingRequestRequest { Reason = new string('R', 501) };

        var isValid = TryValidate(request, out var results);

        Assert.False(isValid);
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CancelSnakeCatchingRequestRequest.Reason)));
    }

    [Fact]
    public void AssignRequest_RescuerId_HasNoDataAnnotations_EmptyGuidIsModelValid()
    {
        var request = new AssignSnakeCatchingRequestRequest { rescuerId = Guid.Empty };

        var isValid = TryValidate(request, out var _);

        Assert.True(isValid);
    }

    private static CreateSnakeCatchingRequestRequest ValidCreateRequest()
    {
        return new CreateSnakeCatchingRequestRequest
        {
            Address = "123 Main St",
            Lng = 106.660172,
            Lat = 10.762622,
            AdditionalDetails = "Snake near the gate",
            Notes = "Urgent"
        };
    }

    private static bool TryValidate(object instance, out List<ValidationResult> results)
    {
        var context = new ValidationContext(instance);
        results = new List<ValidationResult>();
        return Validator.TryValidateObject(instance, context, results, validateAllProperties: true);
    }
}
