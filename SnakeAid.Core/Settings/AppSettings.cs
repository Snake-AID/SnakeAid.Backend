namespace SnakeAid.Core.Settings;

public class JwtSettings
{
    public string SecretKey { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public int AccessTokenExpirationMinutes { get; set; } = 15;
    public int RefreshTokenExpirationDays { get; set; } = 7;
}

public class EmailSettings
{
    public string SmtpServer { get; set; } = "smtp.gmail.com";
    public int SmtpPort { get; set; } = 587;
    public string FromEmail { get; set; } = "lvhhoangg1@gmail.com";
    public string FromName { get; set; } = "lvhhoangg1@gmail.com";
    public string Username { get; set; } = "lvhhoangg1@gmail.com";
    public string Password { get; set; } = "ojlx ohfr qxwd llxx";
}

public class FirebaseCloudMessagingSettings
{
    public string ServerKey { get; set; } = string.Empty;
    public string SenderId { get; set; } = string.Empty;
}