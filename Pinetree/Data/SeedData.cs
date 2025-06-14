using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Pinetree.Shared;

namespace Pinetree.Data
{
    public static class SeedData
    {
        public static async Task Initialize(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

            // Create roles if they don't exist
            await CreateRolesAsync(roleManager);

            // Create admin user if it doesn't exist
            await CreateAdminUserAsync(userManager, configuration);

            // Create test user (existing functionality)
            await CreateTestUserAsync(userManager);
        }

        private static async Task CreateRolesAsync(RoleManager<IdentityRole> roleManager)
        {
            foreach (var roleName in Roles.RoleNames)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }
        }

        private static async Task CreateAdminUserAsync(UserManager<ApplicationUser> userManager, IConfiguration configuration)
        {
            var adminEmail = configuration.GetConnectionString("AdminUserEmail") ?? "";
            var adminPassword = configuration.GetConnectionString("AdminUserPassword") ?? "";
            var adminUserName = configuration.GetConnectionString("AdminUserUserName") ?? "";

            var adminUser = await userManager.FindByEmailAsync(adminEmail);
            if (adminUser != null) return;

            var passwordValidators = userManager.PasswordValidators.ToList();
            try
            {
                userManager.PasswordValidators.Clear();

                adminUser = new ApplicationUser
                {
                    UserName = adminUserName,
                    Email = adminEmail,
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(adminUser, adminPassword);
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, Roles.Admin);
                }
                else
                {
                    throw new Exception($"Failed to create admin user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                }
            }
            finally
            {
                foreach (var validator in passwordValidators)
                {
                    userManager.PasswordValidators.Add(validator);
                }
            }
        }

        private static async Task CreateTestUserAsync(UserManager<ApplicationUser> userManager)
        {
            var testUser = await userManager.FindByNameAsync("test");
            if (testUser != null) return;

            var passwordValidators = userManager.PasswordValidators.ToList();
            try
            {
                userManager.PasswordValidators.Clear();

                testUser = new ApplicationUser
                {
                    UserName = "test",
                    Email = "test@example.com",
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(testUser, "test");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(testUser, Roles.Free);
                }
                else
                {
                    throw new Exception($"Failed to create test user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                }
            }
            finally
            {
                foreach (var validator in passwordValidators)
                {
                    userManager.PasswordValidators.Add(validator);
                }
            }
        }
    }
}
