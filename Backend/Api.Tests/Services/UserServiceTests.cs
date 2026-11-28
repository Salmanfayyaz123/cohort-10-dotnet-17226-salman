using Api.Enums;
using Api.Middleware;
using Api.Models;
using Api.Services;
using Api.Tests.Helpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Api.Tests.Services;

public class UserServiceTests
{
    private readonly Mock<UserManager<AppUser>> _userManager = TestHelpers.CreateUserManagerMock();
    private readonly UserService _userService;

    public UserServiceTests()
    {
        _userService = new UserService(_userManager.Object, NullLogger<UserService>.Instance);
    }

    [Fact]
    public async Task CreateUserAsync_EmailAlreadyExists_ThrowsConflictException()
    {
        // Arrange
        _userManager.Setup(m => m.FindByEmailAsync("ali@test.com"))
            .ReturnsAsync(new AppUser { Email = "ali@test.com" });

        // Act + Assert
        await Assert.ThrowsAsync<ConflictException>(() =>
            _userService.CreateUserAsync("Ali", "ali@test.com", "Ali@12345", UserRole.User, null));

        _userManager.Verify(m => m.CreateAsync(It.IsAny<AppUser>(), It.IsAny<string>()), Times.Never);
    }
        [Fact]
    public async Task CreateUserAsync_AdminCreatesAdmin_SavesCreatorAndAssignsAdminRole()
    {
        // Arrange
        _userManager.Setup(m => m.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync((AppUser?)null);
        _userManager.Setup(m => m.CreateAsync(It.IsAny<AppUser>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);
        _userManager.Setup(m => m.AddToRoleAsync(It.IsAny<AppUser>(), "Admin"))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _userService.CreateUserAsync(
            "Ali", "ali@test.com", "Ali@12345", UserRole.Admin, createdById: "admin-1");

        // Assert
        Assert.Contains("Admin", result.Roles);

        _userManager.Verify(m => m.CreateAsync(
            It.Is<AppUser>(u => u.CreatedById == "admin-1"), "Ali@12345"), Times.Once);
        _userManager.Verify(m => m.AddToRoleAsync(It.IsAny<AppUser>(), "Admin"), Times.Once);
    }

    [Fact]
    public async Task CreateUserAsync_WeakPassword_ThrowsBadRequestWithIdentityMessage()
    {
        // Arrange
        _userManager.Setup(m => m.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync((AppUser?)null);
        _userManager.Setup(m => m.CreateAsync(It.IsAny<AppUser>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError
            {
                Description = "Passwords must have at least one uppercase ('A'-'Z')."
            }));

        // Act
        var exception = await Assert.ThrowsAsync<BadRequestException>(() =>
            _userService.CreateUserAsync("Ali", "ali@test.com", "weakpass1", UserRole.User, null));

        // Assert
        Assert.Contains("uppercase", exception.Message);
    }

    [Fact]
    public async Task CreateUserAsync_ValidData_CreatesUserWithRole()
    {
        // Arrange
        _userManager.Setup(m => m.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync((AppUser?)null);
        _userManager.Setup(m => m.CreateAsync(It.IsAny<AppUser>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);
        _userManager.Setup(m => m.AddToRoleAsync(It.IsAny<AppUser>(), "User"))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _userService.CreateUserAsync(
            "Sara", "sara@test.com", "Sara@12345", UserRole.User, createdById: null);

        // Assert
        Assert.Equal("Sara", result.Name);
        Assert.Equal("sara@test.com", result.Email);
        Assert.Contains("User", result.Roles);

        // Check exactly what was saved
        _userManager.Verify(m => m.CreateAsync(
            It.Is<AppUser>(u => u.Email == "sara@test.com"
                                && u.UserName == "sara@test.com"
                                && u.CreatedById == null),
            "Sara@12345"), Times.Once);
    }

    [Fact]
    public async Task CreateUserAsync_RoleAssignmentFails_DeletesUserAndThrows()
    {
        // Arrange
        _userManager.Setup(m => m.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync((AppUser?)null);
        _userManager.Setup(m => m.CreateAsync(It.IsAny<AppUser>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);
        _userManager.Setup(m => m.AddToRoleAsync(It.IsAny<AppUser>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Role not found" }));
        _userManager.Setup(m => m.DeleteAsync(It.IsAny<AppUser>()))
            .ReturnsAsync(IdentityResult.Success);

        // Act + Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _userService.CreateUserAsync("Sara", "sara@test.com", "Sara@12345", UserRole.User, null));

        _userManager.Verify(m => m.DeleteAsync(It.IsAny<AppUser>()), Times.Once);
    }
}