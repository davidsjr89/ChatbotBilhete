using ChatbotApiNet9.Controllers;
using ChatbotApiNet9.Interfaces;
using ChatbotApiNet9.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;

namespace TestChatbotApiNet9.Tests
{
    public class AuthControllerTests
    {
        private readonly Mock<IAuthService> _mockAuthService;
        private readonly Mock<IJwtService> _mockJwtService;
        private readonly Mock<ILogger<AuthController>> _mockLogger;
        private readonly AuthController _controller;

        public AuthControllerTests()
        {
            _mockAuthService = new Mock<IAuthService>();
            _mockJwtService = new Mock<IJwtService>();
            _mockLogger = new Mock<ILogger<AuthController>>();
            _controller = new AuthController(_mockAuthService.Object, _mockJwtService.Object, _mockLogger.Object);
        }

        //Testes para o método Register
        [Fact]
        public async Task Register_WithInvalidModel_ReturnsBadRequest()
        {
            // Arrange
            AddModelError("Username", "Required");
            var request = new RegisterRequest { Username = "", Password = "password" };

            // Act
            var result = await _controller.Register(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var errorResponse = Assert.IsType<ErrorResponse>(badRequestResult.Value);
            Assert.Equal("Invalid registration data.", errorResponse.Message);
        }

        [Fact]
        public async Task Register_WithExistingUsername_ReturnsBadRequest()
        {
            // Arrange
            var request = new RegisterRequest { Username = "existingUser", Password = "password" };
            _mockAuthService.Setup(x => x.RegisterAsync(request.Username, request.Password))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.Register(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var errorResponse = Assert.IsType<ErrorResponse>(badRequestResult.Value);
            Assert.Equal("Registration failed. Username might already exist.", errorResponse.Message);
        }

        //Testes para o método Login
        [Fact]
        public async Task Login_WithInvalidModel_ReturnsUnauthorized()
        {
            // Arrange
            AddModelError("Username", "Required");
            var request = new LoginRequest { Username = "", Password = "password" };

            // Act
            var result = await _controller.Login(request);

            // Assert
            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
            var errorResponse = Assert.IsType<ErrorResponse>(unauthorizedResult.Value);
            Assert.Equal("Invalid login request.", errorResponse.Message);
        }

        [Fact]
        public async Task Login_WithInvalidCredentials_ReturnsUnauthorized()
        {
            // Arrange
            var request = new LoginRequest { Username = "user", Password = "wrongpassword" };
            _mockAuthService.Setup(x => x.LoginAsync(request.Username, request.Password))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.Login(request);

            // Assert
            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
            var errorResponse = Assert.IsType<ErrorResponse>(unauthorizedResult.Value);
            Assert.Equal("Invalid username or password.", errorResponse.Message);
        }

        [Fact]
        public async Task Login_WithValidCredentials_ReturnsTokens()
        {
            // Arrange
            var request = new LoginRequest { Username = "user", Password = "password" };
            var expectedAccessToken = "access_token";
            var expectedRefreshToken = "refresh_token";

            _mockAuthService.Setup(x => x.LoginAsync(request.Username, request.Password))
                .ReturnsAsync(true);
            _mockJwtService.Setup(x => x.GenerateAccessToken(It.IsAny<Claim[]>()))
                .Returns(expectedAccessToken);
            _mockJwtService.Setup(x => x.GenerateRefreshToken())
                .Returns(expectedRefreshToken);

            // Act
            var result = await _controller.Login(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var tokenResponse = Assert.IsType<TokenResponse>(okResult.Value);
            Assert.Equal(expectedAccessToken, tokenResponse.AccessToken);
            Assert.Equal(expectedRefreshToken, tokenResponse.RefreshToken);

            _mockJwtService.Verify(x => x.SaveRefreshTokenAsync(request.Username, expectedRefreshToken), Times.Once);
        }

        [Fact]
        public async Task Login_WhenServiceThrowsException_ReturnsInternalServerError()
        {
            // Arrange
            var request = new LoginRequest { Username = "user", Password = "password" };
            _mockAuthService.Setup(x => x.LoginAsync(request.Username, request.Password))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.Login(request);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusCodeResult.StatusCode);
        }

        // Testes para o método RefreshToken
        [Fact]
        public async Task Refresh_WithInvalidModel_ReturnsBadRequest()
        {
            // Arrange
            AddModelError("AccessToken", "Required");
            var request = new RefreshTokenRequest { AccessToken = "", RefreshToken = "refresh_token" };

            // Act
            var result = await _controller.Refresh(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var errorResponse = Assert.IsType<ErrorResponse>(badRequestResult.Value);
            Assert.Equal("Invalid token refresh request.", errorResponse.Message);
        }

        [Fact]
        public async Task Refresh_WithInvalidAccessToken_ReturnsBadRequest()
        {
            // Arrange
            var request = new RefreshTokenRequest { AccessToken = "invalid_token", RefreshToken = "refresh_token" };
            _mockJwtService.Setup(x => x.GetPrincipalFromExpiredToken(request.AccessToken))
                .Returns((ClaimsPrincipal)null);

            // Act
            var result = await _controller.Refresh(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var errorResponse = Assert.IsType<ErrorResponse>(badRequestResult.Value);
            Assert.Equal("Invalid access token.", errorResponse.Message);
        }

        [Fact]
        public async Task Refresh_WithInvalidRefreshToken_ReturnsUnauthorized()
        {
            // Arrange
            var request = new RefreshTokenRequest { AccessToken = "expired_token", RefreshToken = "invalid_refresh_token" };
            var principal = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
        new Claim(ClaimTypes.Name, "user")
            }));

            _mockJwtService.Setup(x => x.GetPrincipalFromExpiredToken(request.AccessToken))
                .Returns(principal);
            _mockJwtService.Setup(x => x.ValidateRefreshTokenAsync("user", request.RefreshToken))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.Refresh(request);

            // Assert
            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
            var errorResponse = Assert.IsType<ErrorResponse>(unauthorizedResult.Value);
            Assert.Equal("Invalid or expired refresh token.", errorResponse.Message);
        }

        [Fact]
        public async Task Refresh_WithValidTokens_ReturnsNewTokens()
        {
            // Arrange
            var request = new RefreshTokenRequest { AccessToken = "expired_token", RefreshToken = "valid_refresh_token" };
            var newAccessToken = "new_access_token";
            var newRefreshToken = "new_refresh_token";

            var principal = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
        new Claim(ClaimTypes.Name, "user")
            }));

            _mockJwtService.Setup(x => x.GetPrincipalFromExpiredToken(request.AccessToken))
                .Returns(principal);
            _mockJwtService.Setup(x => x.ValidateRefreshTokenAsync("user", request.RefreshToken))
                .ReturnsAsync(true);
            _mockJwtService.Setup(x => x.GenerateAccessToken(It.IsAny<IEnumerable<Claim>>()))
                .Returns(newAccessToken);
            _mockJwtService.Setup(x => x.GenerateRefreshToken())
                .Returns(newRefreshToken);

            // Act
            var result = await _controller.Refresh(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var tokenResponse = Assert.IsType<TokenResponse>(okResult.Value);
            Assert.Equal(newAccessToken, tokenResponse.AccessToken);
            Assert.Equal(newRefreshToken, tokenResponse.RefreshToken);

            _mockJwtService.Verify(x => x.RemoveRefreshTokenAsync("user", request.RefreshToken), Times.Once);
            _mockJwtService.Verify(x => x.SaveRefreshTokenAsync("user", newRefreshToken), Times.Once);
        }

        //Testes para o método Logout
        [Fact]
        public async Task Logout_WithoutUsernameClaim_ReturnsBadRequest()
        {
            // Arrange
            var request = new RefreshTokenRequest { AccessToken = "token", RefreshToken = "refresh_token" };
            var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity()); // No claims
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };

            // Act
            var result = await _controller.Logout(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var errorResponse = Assert.IsType<ErrorResponse>(badRequestResult.Value);
            Assert.Equal("Invalid token: Username missing.", errorResponse.Message);
        }

        // Helper method to simulate model state errors
        private void AddModelError(string key, string errorMessage)
        {
            _controller.ModelState.AddModelError(key, errorMessage);
        }
    }
}
