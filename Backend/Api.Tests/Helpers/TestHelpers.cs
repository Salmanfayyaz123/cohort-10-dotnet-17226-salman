using System.Security.Claims;
using Api.Models;
using Microsoft.AspNetCore.Identity;
using Moq;

namespace Api.Tests.Helpers;

public static class TestHelpers
{
    // UserManager has no interface, so we mock the class itself.
    // Its constructor needs 9 arguments; only the store matters for tests.
    public static Mock<UserManager<AppUser>> CreateUserManagerMock()
    {
        var store = new Mock<IUserStore<AppUser>>();
        return new Mock<UserManager<AppUser>>(
            store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
    }

        // A fake logged-in user (what the JWT would normally provide)
    public static ClaimsPrincipal CreatePrincipal(string userId, string name, string email, params string[] roles)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId),
            new(ClaimTypes.Name, name),
            new(ClaimTypes.Email, email)
        };
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        return new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
    }
}