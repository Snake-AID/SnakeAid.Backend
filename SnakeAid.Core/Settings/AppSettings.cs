namespace SnakeAid.Core.Settings;

public class JwtSettings
{
    public string SecretKey { get; set; } = string.Empty;
    public string RefreshSecretKey { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public int AccessTokenExpirationMinutes { get; set; } = 60;
    public int RefreshTokenExpirationDays { get; set; } = 30;
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

public class CloudinarySettings
{
    public string CloudName { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string ApiSecret { get; set; } = string.Empty;
    public string BaseFolder { get; set; } = "snakeaid";
}

public class SnakeAISettings
{
    public string BaseUrl { get; set; } = "http://localhost:8000";
    public int ImageSize { get; set; } = 640;
    public float IouThreshold { get; set; } = 0.5f;
    public int TopK { get; set; } = 100;
    public bool SaveImage { get; set; } = false;
    public float Confidence { get; set; } = 0.25f;
    public int TimeoutSeconds { get; set; } = 30;
} 