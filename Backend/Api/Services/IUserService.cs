
using Api.DTOs.Auth;
using Api.Enums;

namespace Api.Services;

public interface IUserService
{
Task<UserDto> CreateUserAsync(string name, string email, string password, UserRole role, string? createdById);
}
