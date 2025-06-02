using ChatbotApiNet9.Interfaces;
using ChatbotApiNet9.Models;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace ChatbotApiNet9.Services
{
    public class JwtService : IJwtService
    {
        private readonly JwtSettings _jwtSettings;
        private readonly string _refreshTokenFilePath = Path.Combine(Directory.GetCurrentDirectory(), "refresh_tokens.json");
        private List<UserRefreshToken> _refreshTokens = new List<UserRefreshToken>();
        private readonly ILogger<JwtService> _logger;

        // Need IOptions<JwtSettings> injected, requires configuration setup in Program.cs
        // Assuming IOptions is available in the base runtime
        public JwtService(IOptions<JwtSettings> jwtSettingsOptions, ILogger<JwtService> logger)
        {
            _jwtSettings = jwtSettingsOptions.Value;
            _logger = logger;
            LoadRefreshTokensFromFile();
        }

        // --- Refresh Token Storage (Similar to LocalAuthService) ---
        private void LoadRefreshTokensFromFile()
        {
            try
            {
                if (File.Exists(_refreshTokenFilePath))
                {
                    var json = File.ReadAllText(_refreshTokenFilePath);
                    _refreshTokens = JsonSerializer.Deserialize<List<UserRefreshToken>>(json) ?? new List<UserRefreshToken>();
                    _logger.LogInformation("Loaded {TokenCount} refresh tokens from {FilePath}", _refreshTokens.Count, _refreshTokenFilePath);
                }
                else
                {
                    _logger.LogInformation("Refresh token file not found at {FilePath}, starting empty.", _refreshTokenFilePath);
                    _refreshTokens = new List<UserRefreshToken>();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading refresh tokens from {FilePath}", _refreshTokenFilePath);
                _refreshTokens = new List<UserRefreshToken>();
            }
        }

        private async Task SaveRefreshTokensToFileAsync()
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                var json = JsonSerializer.Serialize(_refreshTokens, options);
                await File.WriteAllTextAsync(_refreshTokenFilePath, json);
                _logger.LogInformation("Saved {TokenCount} refresh tokens to {FilePath}", _refreshTokens.Count, _refreshTokenFilePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving refresh tokens to {FilePath}", _refreshTokenFilePath);
            }
        }

        // --- JWT Generation ---
        public string GenerateAccessToken(IEnumerable<Claim> claims)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes),
                Issuer = _jwtSettings.Issuer,
                Audience = _jwtSettings.Audience,
                SigningCredentials = credentials
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        public string GenerateRefreshToken()
        {
            var randomNumber = new byte[64]; // Increased size for more entropy
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomNumber);
                return Convert.ToBase64String(randomNumber);
            }
        }

        // --- JWT Validation (for refresh token rotation) ---
        public ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
        {
            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateAudience = true,
                ValidAudience = _jwtSettings.Audience,
                ValidateIssuer = true,
                ValidIssuer = _jwtSettings.Issuer,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey)),
                ValidateLifetime = false
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            SecurityToken securityToken;
            try
            {
                var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out securityToken);
                var jwtSecurityToken = securityToken as JwtSecurityToken;
                // Check algorithm explicitly
                if (jwtSecurityToken == null || !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
                    throw new SecurityTokenException("Invalid token algorithm");

                return principal;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to validate expired token.");
                return null;
            }
        }

        // --- Refresh Token Management ---
        public async Task SaveRefreshTokenAsync(string username, string refreshToken)
        {
            _refreshTokens.RemoveAll(rt => rt.Username.Equals(username, StringComparison.OrdinalIgnoreCase));

            var newRefreshToken = new UserRefreshToken
            {
                Username = username,
                Token = refreshToken,
                ExpiryDate = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays)
            };
            _refreshTokens.Add(newRefreshToken);
            await SaveRefreshTokensToFileAsync();
            _logger.LogInformation("Saved refresh token for user {Username}", username);
        }

        public Task<bool> ValidateRefreshTokenAsync(string username, string refreshToken)
        {
            var storedToken = _refreshTokens.FirstOrDefault(rt =>
                rt.Username.Equals(username, StringComparison.OrdinalIgnoreCase) &&
                rt.Token == refreshToken);

            if (storedToken == null)
            {
                _logger.LogWarning("Refresh token validation failed: Token not found for user {Username}", username);
                return Task.FromResult(false);
            }

            if (storedToken.ExpiryDate < DateTime.UtcNow)
            {
                _logger.LogWarning("Refresh token validation failed: Token expired for user {Username}", username);
                _refreshTokens.Remove(storedToken); 
                // Clean up expired token
                // Consider saving changes here if desired, but might happen on next save anyway
                // await SaveRefreshTokensToFileAsync(); 
                return Task.FromResult(false);
            }

            _logger.LogInformation("Refresh token validated successfully for user {Username}", username);
            return Task.FromResult(true);
        }

        public async Task RemoveRefreshTokenAsync(string username, string refreshToken)
        {
            var tokenToRemove = _refreshTokens.FirstOrDefault(rt =>
                rt.Username.Equals(username, StringComparison.OrdinalIgnoreCase) &&
                rt.Token == refreshToken);

            if (tokenToRemove != null)
            {
                _refreshTokens.Remove(tokenToRemove);
                await SaveRefreshTokensToFileAsync();
                _logger.LogInformation("Removed refresh token for user {Username}", username);
            }
            else
            {
                _logger.LogWarning("Attempted to remove non-existent refresh token for user {Username}", username);
            }
        }
    }
}