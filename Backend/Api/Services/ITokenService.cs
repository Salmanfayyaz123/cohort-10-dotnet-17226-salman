using System;
using Api.Models;

namespace Api.Services;

public interface ITokenService
{
 (string Token, DateTime ExpiresAt) CreateToken(AppUser user, IEnumerable<string> roles);
}
