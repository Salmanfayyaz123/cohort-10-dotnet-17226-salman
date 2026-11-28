using System.Security.Claims;
using Api.Controllers;
using Api.DTOs.Auth;
using Api.Enums;
using Api.Services;
using Api.Tests.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Api.Tests.Controllers;

public class UsersControllerTests
{
    private readonly Mock<IUserService> _userService = new();
    private readonly UsersController _controller;

    public UsersControllerTests()
    {
        _controller = new UsersController(_userService.Object);
    }

    private static CreateUserRequest ValidRequest() => new()
    {
        Name = "Ali",
        Email = "ali@test.com",
        Password = "Ali@12345",
        Role = UserRole.Admin
    };

    private void SetLoggedInUser(ClaimsPrincipal user)
    {
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };
    }

    [Fact]
    public async Task Create_NoUserIdInToken_ReturnsUnauthorized()
    {
        // Arrange: a "logged in" user with no Id claim
        SetLoggedInUser(new ClaimsPrincipal(new ClaimsIdentity()));

        // Act
        var result = await _controller.Create(ValidRequest());

        // Assert
        Assert.IsType<UnauthorizedResult>(result.Result);
        _userService.Verify(
            s => s.CreateUserAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<UserRole>(), It.IsAny<string?>()),
            Times.Never);
    }

    [Fact]
    public async Task Create_AdminLoggedIn_Returns201AndPassesAdminIdAndRole()
    {
        // Arrange
        SetLoggedInUser(TestHelpers.CreatePrincipal("admin-1", "System Admin", "admin@taskmanager.com", "Admin"));

        _userService
            .Setup(s => s.CreateUserAsync("Ali", "ali@test.com", "Ali@12345", UserRole.Admin, "admin-1"))
            .ReturnsAsync(new UserDto
            {
                Id = "user-3",
                Name = "Ali",
                Email = "ali@test.com",
                Roles = new List<string> { "Admin" }
            });

        // Act
        var result = await _controller.Create(ValidRequest());

        // Assert
        var created = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status201Created, created.StatusCode);

        var body = Assert.IsType<UserDto>(created.Value);
        Assert.Contains("Admin", body.Roles);

        // The admin's Id from the token must be saved as CreatedById
        _userService.Verify(
            s => s.CreateUserAsync("Ali", "ali@test.com", "Ali@12345", UserRole.Admin, "admin-1"),
            Times.Once);
    }
}