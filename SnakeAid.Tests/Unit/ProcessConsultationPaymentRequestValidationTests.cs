using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using SnakeAid.Core.Requests.Consultation;

namespace SnakeAid.Tests.Unit;

public class ProcessConsultationPaymentRequestValidationTests
{
    [Fact]
    public void DeserializeWithoutPaymentMethod_ShouldLeavePaymentMethodNull()
    {
        var request = JsonSerializer.Deserialize<ProcessConsultationPaymentRequest>("{}");

        Assert.NotNull(request);
        Assert.Null(request!.PaymentMethod);
    }

    [Fact]
    public void ValidateWithoutPaymentMethod_ShouldFailRequiredValidation()
    {
        var request = new ProcessConsultationPaymentRequest();
        var validationResults = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(
            request,
            new ValidationContext(request),
            validationResults,
            validateAllProperties: true);

        Assert.False(isValid);
        Assert.Contains(validationResults, result =>
            result.MemberNames.Contains(nameof(ProcessConsultationPaymentRequest.PaymentMethod)));
    }
}
