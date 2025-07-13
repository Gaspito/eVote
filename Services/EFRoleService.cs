using System.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using VotingApp.Web.Models;


/// <summary>
/// Entity Framework only implementation of the Role Switching service
/// </summary>
public class EFRoleService : IRoleService
{
    public AppDbContext _dbContext;
    public UserManager<AppUser> _userManager;

    public EFRoleService(AppDbContext dbContext, UserManager<AppUser> userManager)
    {
        _dbContext = dbContext;
        _userManager = userManager;
    }

    public async Task<VoteRequestStatus> SwitchToCandidate(string userEmail)
    {
        // Use of Serialization lock to minimize race conditions on db
        using (var transaction = await _dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable))
        {
            // Find the user by email
            var voter = await _userManager.FindByEmailAsync(userEmail);
            if (voter == null)
                return new VoteRequestStatus(false, "User Not Found");
            var voterId = voter.Id;

            // Check if the user has any pending votes
            var hasAnyVotes = await _dbContext.VoteTokens
                .AnyAsync(i => i.VoterId == voterId);

            if (hasAnyVotes)
                return new VoteRequestStatus(false, "Cannot switch a user with cast votes to a candidate");

            // Change roles
            await _userManager.RemoveFromRoleAsync(voter, "Voter");
            await _userManager.AddToRoleAsync(voter, "Candidate");

            // Reset the vote count cache of the user
            voter.VoteCount = 0;

            await _dbContext.SaveChangesAsync();

            await transaction.CommitAsync();
        }
        return new VoteRequestStatus(true, "Role changed"); ;
    }

    public async Task<VoteRequestStatus> SwitchToVoter(string userEmail)
    {
        // Use of Serialization lock to minimize race conditions on db
        using (var transaction = await _dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable))
        {
            // Get the user by email (voter = main user in request)
            var voter = await _userManager.FindByEmailAsync(userEmail);
            if (voter == null)
                return new VoteRequestStatus(false, "User Not Found");
            var voterId = voter.Id;

            // Check if the user has any pending votes for them
            var hasAnyVotes = await _dbContext.VoteTokens
                .AnyAsync(i => i.CandidateId == voterId);

            if (hasAnyVotes)
                return new VoteRequestStatus(false, "Cannot change a candidate to a voter if their vote count is not 0"); ;

            // Change roles
            await _userManager.RemoveFromRoleAsync(voter, "Candidate");
            await _userManager.AddToRoleAsync(voter, "Voter");

            // reset the vote count cache of the user
            voter.VoteCount = 0;

            await _dbContext.SaveChangesAsync();

            await transaction.CommitAsync();
        }
        return new VoteRequestStatus(true, "Role changed"); ;
    }
}