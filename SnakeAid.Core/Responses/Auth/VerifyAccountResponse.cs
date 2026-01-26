namespace SnakeAid.Core.Responses.Auth;

public class VerifyAccountResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public AuthResponse? AuthData { get; set; }
}
