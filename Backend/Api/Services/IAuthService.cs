using System;
using Api.DTOs.Auth;

namespace Api.Services;

public interface IAuthService
{
Task<AuthResponse?> LoginAsync(LoginRequest request);
}
