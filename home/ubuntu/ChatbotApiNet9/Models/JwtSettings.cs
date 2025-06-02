namespace ChatbotApiNet9.Models;

public class JwtSettings
{
    public string SecretKey { get; set; } = "THIS_IS_A_DEFAULT_SECRET_KEY_REPLACE_ME_1234567890!@#$%^";
    public string Issuer { get; set; } = "ChatbotApi";
    public string Audience { get; set; } = "ChatbotFrontend";
    public int AccessTokenExpirationMinutes { get; set; } = 15;
    public int RefreshTokenExpirationDays { get; set; } = 7;
}

public class UserRefreshToken
{
    public required string Username { get; set; }
    public required string Token { get; set; }
    public DateTime ExpiryDate { get; set; }
}
