
using Api.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
namespace Api.Data;

public class AppDBContext : IdentityDbContext<AppUser>
{
    // constructor that takes in the options and passes it to the base class
    public AppDBContext(DbContextOptions<AppDBContext> options) : base(options)
    {
    }

     protected override void OnModelCreating(ModelBuilder builder)
    {
        // Must be called first: creates all the Identity tables (Users, Roles, UserRoles...)
        base.OnModelCreating(builder);
    }
}
