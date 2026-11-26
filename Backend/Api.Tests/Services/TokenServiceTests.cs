using System.Security.Claims;
using Api.Models;
using Api.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Api.Tests.Services;

public class TokenServiceTests
{
    // Fake settings instead of appsettings.json / user-secrets
    private static IConfiguration CreateConfig(string? key = "this-is-a-very-long-test-signing-key-123456")
    {
        var settings = new Dictionary<string, string?>
        {
            ["Jwt:Key"] = key,
            ["Jwt:Issuer"] = "TestIssuer",
            ["Jwt:Audience"] = "TestAudience",
            ["Jwt:ExpiryMinutes"] = "30"
        };

        return new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
    }

    private static AppUser CreateUser() => new()
    {
        Id = "user-1",
        Name = "Ali",
        Email = "ali@test.com"
    };

    [Fact]
    public void CreateToken_ValidUser_ReturnsTokenWithCorrectClaims()
    {
        // Arrange
        var service = new TokenService(CreateConfig());

        // Act
        var (token, expiresAt) = service.CreateToken(CreateUser(), new[] { "Admin" });

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(token));

        var jwt = new JsonWebTokenHandler().ReadJsonWebToken(token);
        Assert.Equal("TestIssuer", jwt.Issuer);
        Assert.Contains("TestAudience", jwt.Audiences);
        Assert.Equal("user-1", jwt.Claims.First(c => c.Type == ClaimTypes.NameIdentifier).Value);
        Assert.Equal("ali@test.com", jwt.Claims.First(c => c.Type == ClaimTypes.Email).Value);
        Assert.Contains(jwt.Claims, c => c.Type == ClaimTypes.Role && c.Value == "Admin");

        Assert.InRange(expiresAt, DateTime.UtcNow.AddMinutes(29), DateTime.UtcNow.AddMinutes(31));
    }

    [Fact]
    public void CreateToken_MissingKey_ThrowsInvalidOperationException()
    {
        // Arrange
        var service = new TokenService(CreateConfig(key: null));

        // Act + Assert
        Assert.Throws<InvalidOperationException>(
            () => service.CreateToken(CreateUser(), Array.Empty<string>()));
    }
}