
using Api.DTOs.Auth;
using Api.Enums;
using Api.Models;
using Microsoft.AspNetCore.Identity;

namespace Api.Services;

public class AuthService(
    UserManager<AppUser> userManager,
    ITokenService tokenService,
     IUserService userService,
    ILogger<AuthService> logger) : IAuthService
{
    public async Task<AuthResponse?> LoginAsync(LoginRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is null)
        {
            logger.LogWarning("Login failed: no user with email {Email}", request.Email);
            return null;
        }

        if (!user.IsActive)
        {
            logger.LogWarning("Login failed: user {UserId} is deactivated", user.Id);
            return null;
        }

        var passwordValid = await userManager.CheckPasswordAsync(user, request.Password);
        if (!passwordValid)
        {
            logger.LogWarning("Login failed: wrong password for user {UserId}", user.Id);
            return null;
        }

        var roles = await userManager.GetRolesAsync(user);
        var (token, expiresAt) = tokenService.CreateToken(user, roles);

        logger.LogInformation("User {UserId} logged in", user.Id);

        return new AuthResponse
        {
            Token = token,
            ExpiresAt = expiresAt,
            User = new UserDto
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email ?? string.Empty,
                Roles = roles
            }
        };
    }

      public async Task<UserDto> RegisterAsync(RegisterRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Public sign-up is ALWAYS the User role. Nobody can make themselves an Admin.
        return await userService.CreateUserAsync(
            request.Name, request.Email, request.Password, UserRole.User, createdById: null);
    }
}
