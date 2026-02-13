namespace SnakeAid.Core.Settings;

public class PayOsOptions
{
    public string BaseUrl { get; set; } = "https://api-merchant.payos.vn";
    public string ClientId { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string ChecksumKey { get; set; } = string.Empty;
    public string ReturnUrl { get; set; } = string.Empty;
    public string CancelUrl { get; set; } = string.Empty;
    public string WebhookConfirmUrl { get; set; } = string.Empty;
    public string SuccessRedirectUrl { get; set; } = string.Empty;
    public string FailureRedirectUrl { get; set; } = string.Empty;
    public string DescriptionPrefix { get; set; } = "SNAKEAID";
}
