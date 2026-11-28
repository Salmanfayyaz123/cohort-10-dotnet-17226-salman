using System.Security.Claims;
using Api.DTOs.Auth;
using Api.Enums;
using Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = nameof(UserRole.Admin))]   // every endpoint in this controller is Admin-only
public class UsersController(IUserService userService) : ControllerBase
{
    // POST /api/users  (Admin creates an Admin or a User)
    [HttpPost]
    public async Task<ActionResult<UserDto>> Create(CreateUserRequest request)
    {
        // The logged-in admin's Id, read from the JWT
        var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(adminId))
        {
            return Unauthorized();
        }

        var user = await userService.CreateUserAsync(
            request.Name, request.Email, request.Password, request.Role, adminId);

        return StatusCode(StatusCodes.Status201Created, user);
    }
}