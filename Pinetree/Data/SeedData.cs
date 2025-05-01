using Microsoft.AspNetCore.Identity;
using Pinetree.Shared;

namespace Pinetree.Data
{
    public static class SeedData
    {

        public static async Task Initialize(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
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
