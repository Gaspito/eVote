using Microsoft.AspNetCore.Identity;


/// <summary>
/// Service that seeds the database with admin and test users.
/// </summary>
public static class UserSeeder
{
    /// <summary>
    /// Number of test users to create.
    /// TO DO: make this a config value.
    /// </summary>
    public static int FakeUserPerRoleCount = 3;

    /// <summary>
    /// Creates an admin user account in the database, if none is registered already.
    /// </summary>
    public static async Task SeedAdminUser(IServiceProvider serviceProvider)
    {
        var userManager = serviceProvider.GetRequiredService<UserManager<AppUser>>();

        string email = "admin@admin.com";
        string adminPassword = "Admin123!";

        if (await userManager.FindByEmailAsync(email) == null)
        {
            var user = new AppUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true
            };

            await userManager.CreateAsync(user, adminPassword);
            await userManager.AddToRoleAsync(user, "Admin");

            Console.WriteLine("Seeded Admin user into the database");
        }

    }

    /// <summary>
    /// Creates test users of each role in the database.
    /// </summary>
    public static async Task SeedUsers(IServiceProvider serviceProvider)
    {
        var userManager = serviceProvider.GetRequiredService<UserManager<AppUser>>();
        var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        string[] roles = RoleSeeder.Roles;

        int userId = 0;

        foreach (string role in roles)
        {
            if (role == "Admin") continue; // Skip admin role test users

            for (int i = 1; i <= FakeUserPerRoleCount; i++)
            {
                userId++;
                string email = $"user{userId}@example.com";
                if (await userManager.FindByEmailAsync(email) == null)
                {
                    var user = new AppUser
                    {
                        UserName = email,
                        Email = email,
                        EmailConfirmed = true
                    };

                    await userManager.CreateAsync(user, "Test123!");
                    await userManager.AddToRoleAsync(user, role);

                    Console.WriteLine($"Seeded test user {email} (Role: {role}) into the database");
                }
            }
        }

    }
}