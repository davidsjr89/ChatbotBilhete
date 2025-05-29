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

// DTO for login response (could include a token in the future)
public class LoginResponse
{
    public required string Message { get; set; }
    public bool Success { get; set; }
    // public string? Token { get; set; } // Example for future JWT implementation
}

