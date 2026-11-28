using System;
using System.ComponentModel.DataAnnotations;
using Api.Enums;

namespace Api.DTOs.Auth;

public class CreateUserRequest
{
[Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required, EmailAddress, MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required, MinLength(8)]
    public string Password { get; set; } = string.Empty;

    // Admin chooses: "Admin" or "User"
    [Required, EnumDataType(typeof(UserRole))]
    public UserRole Role { get; set; } = UserRole.User;
}
