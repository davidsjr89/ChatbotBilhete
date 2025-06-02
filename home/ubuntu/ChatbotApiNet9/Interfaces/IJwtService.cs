using System.Security.Claims;

namespace ChatbotApiNet9.Interfaces;

public interface IJwtService
{
    string GenerateAccessToken(IEnumerable<Claim> claims);
    string GenerateRefreshToken();
    ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
    Task<bool> ValidateRefreshTokenAsync(string username, string refreshToken);
    Task SaveRefreshTokenAsync(string username, string refreshToken);
    Task RemoveRefreshTokenAsync(string username, string refreshToken);
}