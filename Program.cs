using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddDefaultIdentity<AppUser>(options =>
    options.SignIn.RequireConfirmedAccount = false
)
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<AppDbContext>();


var config = builder.Configuration;

// Setup appropriate backend services for votes & roles
var voteBackendType = config.GetValue<string>("ServiceConfig:VoteBackend");

if (voteBackendType == "RabbitMq")
{
    // RabbitMQ infrastructure
    builder.Services.AddSingleton<IRabbitMqConnection, RabbitMqConnection>();
    builder.Services.AddSingleton<IMessagePublisher, RabbitMqPublisher>();

    // Hosted consumer
    builder.Services.AddHostedService<VoteQueueConsumer>();

    builder.Services.AddScoped<IVoteService, RMQVoteService>();
    builder.Services.AddScoped<IRoleService, RMQRoleService>();
}
else if (voteBackendType == "EF")
{
    builder.Services.AddScoped<IVoteService, EFVoteService>();
    builder.Services.AddScoped<IRoleService, EFRoleService>();
}
else
{
    throw new Exception($"Unknown backend type for vote service: '{voteBackendType}'");
}


builder.Services.AddRazorPages();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapRazorPages();

// Seed database with roles, users, test users
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();

    var services = scope.ServiceProvider;
    // Seed Db with roles
    await RoleSeeder.SeedRoles(services);
    // Seed Db with 1 Admin user
    await UserSeeder.SeedAdminUser(services);

    // Optional:
    // Seed Db with fake users for each role
    // TO DO: use a launch config to toggle this
    await UserSeeder.SeedUsers(services);
}

app.Run();
