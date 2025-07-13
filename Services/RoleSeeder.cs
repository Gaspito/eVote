using Microsoft.AspNetCore.Identity;

/// <summary>
/// Singleton class to seed the database with roles
/// </summary>
public static class RoleSeeder
{
    /// <summary>
    /// Possible roles to create.
    /// </summary>
    public static string[] Roles = { "Voter", "Candidate", "Admin" };

    /// <summary>
    /// Seeds the database with all possible roles.
    /// </summary>
    public static async Task SeedRoles(IServiceProvider serviceProvider)
    {
        var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        string[] roles = Roles;

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }
    }
}