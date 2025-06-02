using ChatbotApiNet9.Interfaces;
using ChatbotApiNet9.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;

namespace ChatbotApiNet9.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IJwtService _jwtService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, IJwtService jwtService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _jwtService = jwtService;
        _logger = logger;
    }

    [HttpPost("register")]
    [AllowAnonymous] // Registration should be public
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Invalid model state for RegisterRequest.");
            return BadRequest(new ErrorResponse { Message = "Invalid registration data." });
        }

        try
        {
            _logger.LogInformation("Attempting to register user {Username}", request.Username);
            var success = await _authService.RegisterAsync(request.Username, request.Password);

            if (success)
            {
                _logger.LogInformation("User {Username} registered successfully.", request.Username);
                // Don't return tokens on registration, user should login separately
                return Ok(new { Message = "User registered successfully. Please log in." });
            }
            else
            {
                _logger.LogWarning("Registration failed for user {Username}: Username likely already exists.", request.Username);
                return BadRequest(new ErrorResponse { Message = "Registration failed. Username might already exist." });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during registration for user {Username}", request.Username);
            return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponse { Message = "An unexpected error occurred during registration." });
        }
    }

    [HttpPost("login")]
    [AllowAnonymous] // Login should be public
    [ProducesResponseType(typeof(TokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Invalid model state for LoginRequest.");
            return Unauthorized(new ErrorResponse { Message = "Invalid login request." });
        }

        try
        {
            _logger.LogInformation("Attempting to login user {Username}", request.Username);
            var loginSuccess = await _authService.LoginAsync(request.Username, request.Password);

            if (loginSuccess)
            {
                _logger.LogInformation("User {Username} logged in successfully. Generating tokens.", request.Username);

                // --- Generate JWT Tokens --- 
                var claims = new[]
                {
                    new Claim(JwtRegisteredClaimNames.Sub, request.Username), // Subject = username
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()), // Unique token ID
                    // Add other claims as needed (e.g., roles)
                    new Claim(ClaimTypes.Name, request.Username) // Standard claim for username
                };

                var accessToken = _jwtService.GenerateAccessToken(claims);
                var refreshToken = _jwtService.GenerateRefreshToken();

                // Save the refresh token associated with the user
                await _jwtService.SaveRefreshTokenAsync(request.Username, refreshToken);

                return Ok(new TokenResponse { AccessToken = accessToken, RefreshToken = refreshToken });
            }
            else
            {
                _logger.LogWarning("Login failed for user {Username}: Invalid username or password.", request.Username);
                return Unauthorized(new ErrorResponse { Message = "Invalid username or password." });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login for user {Username}", request.Username);
            return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponse { Message = "An unexpected error occurred during login." });
        }
    }

    [HttpPost("refresh")]
    [AllowAnonymous] // Refresh endpoint needs to be accessible without a valid *access* token
    [ProducesResponseType(typeof(TokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new ErrorResponse { Message = "Invalid token refresh request." });
        }

        var principal = _jwtService.GetPrincipalFromExpiredToken(request.AccessToken);
        if (principal?.Identity?.Name == null) // Use ClaimTypes.Name or JwtRegisteredClaimNames.Sub
        {
            _logger.LogWarning("Refresh token failed: Invalid access token principal.");
            return BadRequest(new ErrorResponse { Message = "Invalid access token." });
        }

        var username = principal.Identity.Name;
        var savedRefreshTokenValid = await _jwtService.ValidateRefreshTokenAsync(username, request.RefreshToken);

        if (!savedRefreshTokenValid)
        {
            _logger.LogWarning("Refresh token failed: Invalid or expired refresh token for user {Username}.", username);
            // Optional: Consider removing all refresh tokens for the user if an invalid one is presented (security measure)
            // await _jwtService.RemoveAllRefreshTokensForUserAsync(username);
            return Unauthorized(new ErrorResponse { Message = "Invalid or expired refresh token." });
        }

        // Generate new tokens
        _logger.LogInformation("Generating new tokens for user {Username} via refresh token.", username);
        var newAccessToken = _jwtService.GenerateAccessToken(principal.Claims); // Re-use claims from expired token
        var newRefreshToken = _jwtService.GenerateRefreshToken();

        // Remove the old refresh token and save the new one (Token Rotation)
        await _jwtService.RemoveRefreshTokenAsync(username, request.RefreshToken);
        await _jwtService.SaveRefreshTokenAsync(username, newRefreshToken);

        return Ok(new TokenResponse { AccessToken = newAccessToken, RefreshToken = newRefreshToken });
    }

    [HttpPost("logout")]
    [Authorize] // User must be authenticated to logout
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Logout([FromBody] RefreshTokenRequest request) // Requires refresh token to invalidate it
    {
        // Log para diagnóstico
        Console.WriteLine($"ModelState.IsValid: {ModelState.IsValid}");
        Console.WriteLine($"ModelState errors: {string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage))}");

        if (!ModelState.IsValid)
        {
            return BadRequest(new ErrorResponse { Message = "Invalid logout request." });
        }

        // Extract username from the validated access token (present due to [Authorize])
        var username = User.Identity?.Name;
        if (string.IsNullOrEmpty(username))
        {
            _logger.LogWarning("Logout failed: Username claim not found in token.");
            return BadRequest(new ErrorResponse { Message = "Invalid token: Username missing." });
        }

        _logger.LogInformation("Attempting to logout user {Username} by removing refresh token.", username);
        await _jwtService.RemoveRefreshTokenAsync(username, request.RefreshToken);

        return Ok(new { Message = "Logout successful." });
    }
}

