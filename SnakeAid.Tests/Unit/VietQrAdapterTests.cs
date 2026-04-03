using SnakeAid.Service.Implements;

namespace SnakeAid.Tests.Unit;

public class VietQrAdapterTests
{
    [Fact]
    public void GenerateQr_ShouldBuildVerifiedEmvPayload_AndImage()
    {
        var adapter = new VietQrAdapter();

        var result = adapter.GenerateQr(
            "970416",
            "257678859",
            "Nguyen Van A",
            10000m,
            "Chuyen tien");

        Assert.Equal(
            "00020101021238530010A0000007270123000697041601092576788590208QRIBFTTA53037045405100005802VN62150811Chuyen tien630453E6",
            result.Payload);
        Assert.DoesNotContain("XXXX", result.Payload, StringComparison.Ordinal);
        Assert.Contains("0208QRIBFTTA", result.Payload, StringComparison.Ordinal);
        Assert.EndsWith("630453E6", result.Payload, StringComparison.Ordinal);
        Assert.False(string.IsNullOrWhiteSpace(result.ImageBase64));
    }
}
