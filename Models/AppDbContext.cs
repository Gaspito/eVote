using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using VotingApp.Web.Models;

/// <summary>
/// Main Database Context (Entity Framework) for the Web App
/// </summary>
public class AppDbContext : IdentityDbContext<AppUser>
{
    /// <summary>
    /// Set of Vote Tokens (ballots)
    /// </summary>
    public DbSet<VoteToken> VoteTokens { get; set; }

    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Fix for Identity Manager tables using invalid value for some db backends
        modelBuilder.Model.GetEntityTypes()
            .SelectMany(t => t.GetProperties())
            .Where(p => p.GetColumnType() == "nvarchar(max)")
            .ToList()
            .ForEach(p => p.SetColumnType("TEXT"));
    }
    
}