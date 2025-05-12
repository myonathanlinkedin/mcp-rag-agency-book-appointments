using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

internal class IdentityDbInitializer : DbInitializer
{
    private readonly UserManager<User> userManager;
    private readonly RoleManager<IdentityRole> roleManager;
    private readonly ILogger<IdentityDbInitializer> logger;

    public IdentityDbInitializer(
        IdentityDbContext db,
        UserManager<User> userManager,
        RoleManager<IdentityRole> roleManager,
        ILogger<IdentityDbInitializer> logger)
        : base(db)
    {
        this.userManager = userManager;
        this.roleManager = roleManager;
        this.logger = logger;
    }

    public override void Initialize()
    {
        base.Initialize();
        SeedAdministrator();
    }

    private void SeedAdministrator()
        => Task.Run(async () =>
        {
            try
            {
                var rolesToCreate = new[]
                {
                    CommonModelConstants.Role.Administrator,
                    CommonModelConstants.Role.Prompter,
                    CommonModelConstants.Role.Agency
                };

                foreach (var roleName in rolesToCreate)
                {
                    if (await roleManager.FindByNameAsync(roleName) == null)
                    {
                        var roleResult = await roleManager.CreateAsync(new IdentityRole(roleName));

                        if (!roleResult.Succeeded)
                        {
                            throw new InvalidOperationException($"Failed to create role: {roleName}. Errors: {string.Join(", ", roleResult.Errors.Select(e => e.Description))}");
                        }
                    }
                }

                var adminEmail = "admin@localhost";
                var existingAdminUser = await userManager.FindByEmailAsync(adminEmail);

                if (existingAdminUser == null)
                {
                    var adminUser = new User(adminEmail);
                    var userResult = await userManager.CreateAsync(adminUser, "Admin01");

                    if (!userResult.Succeeded)
                    {
                        throw new InvalidOperationException($"Failed to create administrator user. Errors: {string.Join(", ", userResult.Errors.Select(e => e.Description))}");
                    }

                    var roleAssignResult = await userManager.AddToRoleAsync(adminUser, CommonModelConstants.Role.Administrator);

                    if (!roleAssignResult.Succeeded)
                    {
                        throw new InvalidOperationException($"Failed to assign Administrator role. Errors: {string.Join(", ", roleAssignResult.Errors.Select(e => e.Description))}");
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in SeedAdministrator: {Message}", ex.Message);
            }

        }).GetAwaiter().GetResult();

}
