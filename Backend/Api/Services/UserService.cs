using Api.DTOs.Auth;
using Api.Enums;
using Api.Middleware;
using Api.Models;
using Microsoft.AspNetCore.Identity;

namespace Api.Services;

public class UserService(
    UserManager<AppUser> userManager,
    ILogger<UserService> logger) : IUserService
{
    public async Task<UserDto> CreateUserAsync(
        string name, string email, string password, UserRole role, string? createdById)
    {
        // 1. Email must be unique
        var existingUser = await userManager.FindByEmailAsync(email);
        if (existingUser is not null)
        {
            throw new ConflictException($"A user with email '{email}' already exists.");
        }

        var user = new AppUser
        {
            Name = name,
            Email = email,
            UserName = email,
            CreatedById = createdById
        };

        // 2. Identity hashes the password and saves the user
        var createResult = await userManager.CreateAsync(user, password);
        if (!createResult.Succeeded)
        {
            // e.g. "Passwords must have at least one uppercase ('A'-'Z')."
            var errors = string.Join(" ", createResult.Errors.Select(e => e.Description));
            throw new BadRequestException(errors);
        }

        // 3. Assign the role
        var roleName = role.ToString();
        var roleResult = await userManager.AddToRoleAsync(user, roleName);
        if (!roleResult.Succeeded)
        {
            // Don't leave a user without a role
            await userManager.DeleteAsync(user);
            throw new InvalidOperationException($"Failed to assign role '{roleName}' to new user.");
        }

        logger.LogInformation("User {UserId} ({Email}) created with role {Role} by {CreatedBy}",
            user.Id, email, roleName, createdById ?? "self-registration");

        return new UserDto
        {
            Id = user.Id,
            Name = user.Name,
            Email = email,
            Roles = new List<string> { roleName }
        };
    }
}