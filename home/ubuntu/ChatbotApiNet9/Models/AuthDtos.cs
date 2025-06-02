namespace ChatbotApiNet9.Models;

// DTO for registration request
public class RegisterRequest
{
    public required string Username { get; set; }
    public required string Password { get; set; }
}

// DTO for login request
public class LoginRequest
{
    public required string Username { get; set; }
    public required string Password { get; set; }
}

// DTO for successful login/refresh response
public class TokenResponse
{
    public required string AccessToken { get; set; }
    public required string RefreshToken { get; set; }
    public bool Success { get; set; } = true;
    public string Message { get; set; } = "Success";
}

// DTO for refresh token request
public class RefreshTokenRequest
{
    public required string AccessToken { get; set; }
    public required string RefreshToken { get; set; }
}

// DTO for generic error response (optional, can use ProblemDetails)
public class ErrorResponse
{
    public required string Message { get; set; }
    public bool Success { get; set; } = false;
}

