using ChatbotApiNet9.Interfaces;
using ChatbotApiNet9.Models;
using Microsoft.AspNetCore.Mvc;

namespace ChatbotApiNet9.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IAuthService authService, ILogger<AuthController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        [HttpPost("register")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid registration request: {Errors}", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                return BadRequest(ModelState);
            }

            try
            {
                _logger.LogInformation("Attempting to register user {Username", request.Username);
                var success = await _authService.RegisterAsync(request.Username, request.Password);

                if (success)
                {
                    _logger.LogInformation("User {Username} registered successfully.", request.Username);
                    return Ok(new { Message = "User registered successfully." });
                }
                else
                {
                    _logger.LogWarning("Registration failed for user {Username}: Username likely already exists.", request.Username);
                    return BadRequest(new { Message = "Registration failed. Username might already exist." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during registration for user {Username}", request.Username);
                return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred during registration.");
            }
        }

        [HttpPost("login")]
        [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid model state for LoginRequest.");
                // Returning 401 for invalid model state in login might be confusing, 
                // but BadRequest could leak info. Let's stick to Unauthorized for login failures.
                return Unauthorized(new LoginResponse { Message = "Invalid login request.", Success = false });
            }

            try
            {
                _logger.LogInformation("Attempting to login user {Username}", request.Username);
                var success = await _authService.LoginAsync(request.Username, request.Password);

                if (success)
                {
                    _logger.LogInformation("User {Username} logged in successfully.", request.Username);
                    // In a real app, generate and return a JWT here
                    return Ok(new LoginResponse { Message = "Login successful.", Success = true });
                }
                else
                {
                    _logger.LogWarning("Login failed for user {Username}: Invalid username or password.", request.Username);
                    return Unauthorized(new LoginResponse { Message = "Invalid username or password.", Success = false });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login for user {Username}", request.Username);
                return StatusCode(StatusCodes.Status500InternalServerError, new LoginResponse { Message = "An unexpected error occurred during login.", Success = false });
            }
        }
    }
}