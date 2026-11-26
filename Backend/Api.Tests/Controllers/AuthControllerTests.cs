using Api.Controllers;
using Api.DTOs.Auth;
using Api.Services;
using Api.Tests.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Api.Tests.Controllers;

public class AuthControllerTests
{
    private readonly Mock<IAuthService> _authService = new();
    private readonly AuthController _controller;

    public AuthControllerTests()
    {
        _controller = new AuthController(_authService.Object);
    }

    private static LoginRequest ValidLogin() => new()
    {
        Email = "ali@test.com",
        Password = "Ali@12345"
    };

    [Fact]
    public async Task Login_InvalidCredentials_ReturnsUnauthorized()
    {
        // Arrange: service says login failed
        _authService.Setup(s => s.LoginAsync(It.IsAny<LoginRequest>()))
            .ReturnsAsync((AuthResponse?)null);

        // Act
        var result = await _controller.Login(ValidLogin());

        // Assert
        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status401Unauthorized, unauthorized.StatusCode);
    }

    [Fact]
    public async Task Login_ValidCredentials_ReturnsOkWithToken()
    {
        // Arrange: service returns a successful login
        var response = new AuthResponse
        {
            Token = "fake-token",
            ExpiresAt = DateTime.UtcNow.AddMinutes(60),
            User = new UserDto
            {
                Id = "user-1",
                Name = "Ali",
                Email = "ali@test.com",
                Roles = new List<string> { "Admin" }
            }
        };

        _authService.Setup(s => s.LoginAsync(It.IsAny<LoginRequest>()))
            .ReturnsAsync(response);

        // Act
        var result = await _controller.Login(ValidLogin());

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var body = Assert.IsType<AuthResponse>(ok.Value);
        Assert.Equal("fake-token", body.Token);
        Assert.Equal("ali@test.com", body.User.Email);
    }

    [Fact]
    public void Me_LoggedInUser_ReturnsUserInfoFromToken()
    {
        // Arrange: pretend a user is logged in
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = TestHelpers.CreatePrincipal("user-1", "Ali", "ali@test.com", "Admin")
            }
        };

        // Act
        var result = _controller.Me();

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);

        // The response is an anonymous object, so read its properties by name
        var value = ok.Value;
        Assert.Equal("user-1", value.GetType().GetProperty("id")?.GetValue(value));
        Assert.Equal("ali@test.com", value.GetType().GetProperty("email")?.GetValue(value));

        var roles = value.GetType().GetProperty("roles")?.GetValue(value) as IEnumerable<string>;
        Assert.NotNull(roles);
        Assert.Contains("Admin", roles);
    }
}