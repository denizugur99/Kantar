using Kantarv2.Entities;
using Microsoft.AspNetCore.Identity;

namespace Kantarv2.Services
{
    public class RoleSeeder
    {
        private readonly RoleManager<IdentityRole<int>> _roleManager;
        private readonly ILogger<RoleSeeder> _logger;

        public RoleSeeder(RoleManager<IdentityRole<int>> roleManager, ILogger<RoleSeeder> logger)
        {
            _roleManager = roleManager;
            _logger = logger;
        }

        public async Task SeedRolesAsync()
        {
            string[] roleNames = { "SuperAdmin", "Admin", "User" };

            foreach (var roleName in roleNames)
            {
                var roleExists = await _roleManager.RoleExistsAsync(roleName);
                if (!roleExists)
                {
                    var role = new IdentityRole<int>(roleName);
                    var result = await _roleManager.CreateAsync(role);

                    if (result.Succeeded)
                    {
                        _logger.LogInformation("Role {RoleName} created successfully.", roleName);
                    }
                    else
                    {
                        _logger.LogError("Failed to create role {RoleName}. Errors: {Errors}",
                            roleName, string.Join(", ", result.Errors.Select(e => e.Description)));
                    }
                }
                else
                {
                    _logger.LogInformation("Role {RoleName} already exists.", roleName);
                }
            }
        }
    }
}
