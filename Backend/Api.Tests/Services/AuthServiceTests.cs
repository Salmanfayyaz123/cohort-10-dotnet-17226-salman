using Api.DTOs.Auth;
using Api.Enums;
using Api.Models;
using Api.Services;
using Api.Tests.Helpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Api.Tests.Services;

public class AuthServiceTests
{
    private readonly Mock<UserManager<AppUser>> _userManager = TestHelpers.CreateUserManagerMock();
    private readonly Mock<ITokenService> _tokenService = new();
    private readonly AuthService _authService;
    private readonly Mock<IUserService> _userService = new();

    public AuthServiceTests()
    {
        // NullLogger = a logger that does nothing (we're not testing logging here)
        _authService = new AuthService(
            _userManager.Object,
            _tokenService.Object,
                _userService.Object,
            NullLogger<AuthService>.Instance);
    }

    private static LoginRequest ValidLogin() => new()
    {
        Email = "ali@test.com",
        Password = "Ali@12345"
    };

    private static AppUser ActiveUser() => new()
    {
        Id = "user-1",
        Name = "Ali",
        Email = "ali@test.com",
        IsActive = true
    };
        [Fact]
    public async Task RegisterAsync_AlwaysCreatesUserWithUserRole()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Name = "Sara",
            Email = "sara@test.com",
            Password = "Sara@12345"
        };

        _userService
            .Setup(s => s.CreateUserAsync("Sara", "sara@test.com", "Sara@12345", UserRole.User, null))
            .ReturnsAsync(new UserDto
            {
                Id = "user-2",
                Name = "Sara",
                Email = "sara@test.com",
                Roles = new List<string> { "User" }
            });

        // Act
        var result = await _authService.RegisterAsync(request);

        // Assert
        Assert.Equal("user-2", result.Id);
        Assert.Contains("User", result.Roles);

        // Proves sign-up can never create an Admin
        _userService.Verify(
            s => s.CreateUserAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), UserRole.User, null),
            Times.Once);
    }

    [Fact]
    public async Task LoginAsync_UnknownEmail_ReturnsNull()
    {
        // Arrange: no user with this email
        _userManager.Setup(m => m.FindByEmailAsync("ali@test.com"))
            .ReturnsAsync((AppUser?)null);

        // Act
        var result = await _authService.LoginAsync(ValidLogin());

        // Assert
        Assert.Null(result);
        _tokenService.Verify(
            t => t.CreateToken(It.IsAny<AppUser>(), It.IsAny<IEnumerable<string>>()),
            Times.Never);
    }

    [Fact]
    public async Task LoginAsync_InactiveUser_ReturnsNull_WithoutCheckingPassword()
    {
        // Arrange: user exists but is deactivated
        var user = ActiveUser();
        user.IsActive = false;
        _userManager.Setup(m => m.FindByEmailAsync("ali@test.com")).ReturnsAsync(user);

        // Act
        var result = await _authService.LoginAsync(ValidLogin());

        // Assert
        Assert.Null(result);
        _userManager.Verify(
            m => m.CheckPasswordAsync(It.IsAny<AppUser>(), It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task LoginAsync_WrongPassword_ReturnsNull()
    {
        // Arrange: user exists, password check fails
        var user = ActiveUser();
        _userManager.Setup(m => m.FindByEmailAsync("ali@test.com")).ReturnsAsync(user);
        _userManager.Setup(m => m.CheckPasswordAsync(user, "Ali@12345")).ReturnsAsync(false);

        // Act
        var result = await _authService.LoginAsync(ValidLogin());

        // Assert
        Assert.Null(result);
        _tokenService.Verify(
            t => t.CreateToken(It.IsAny<AppUser>(), It.IsAny<IEnumerable<string>>()),
            Times.Never);
    }

    [Fact]
    public async Task LoginAsync_ValidCredentials_ReturnsTokenAndUser()
    {
        // Arrange: everything correct
        var user = ActiveUser();
        IList<string> roles = new List<string> { "Admin" };
        var expiresAt = DateTime.UtcNow.AddMinutes(60);

        _userManager.Setup(m => m.FindByEmailAsync("ali@test.com")).ReturnsAsync(user);
        _userManager.Setup(m => m.CheckPasswordAsync(user, "Ali@12345")).ReturnsAsync(true);
        _userManager.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(roles);
        _tokenService.Setup(t => t.CreateToken(user, roles)).Returns(("fake-token", expiresAt));

        // Act
        var result = await _authService.LoginAsync(ValidLogin());

        // Assert
        Assert.NotNull(result);
        Assert.Equal("fake-token", result.Token);
        Assert.Equal(expiresAt, result.ExpiresAt);
        Assert.Equal("user-1", result.User.Id);
        Assert.Equal("Ali", result.User.Name);
        Assert.Equal("ali@test.com", result.User.Email);
        Assert.Contains("Admin", result.User.Roles);
    }
}