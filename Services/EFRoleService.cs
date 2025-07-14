using System.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using VotingApp.Web.Models;


/// <summary>
/// Entity Framework only implementation of the Role Switching service
/// </summary>
public class EFRoleService : IRoleService
{
    private readonly ILogger<EFRoleService> _logger;

    public AppDbContext _dbContext;
    public UserManager<AppUser> _userManager;

    public EFRoleService(ILogger<EFRoleService> logger, AppDbContext dbContext, UserManager<AppUser> userManager)
    {
        _logger = logger;
        _dbContext = dbContext;
        _userManager = userManager;
    }

    public async Task<VoteRequestStatus> SwitchToCandidate(string userEmail)
    {
        // Use of Serialization lock to minimize race conditions on db
        using (var transaction = await _dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable))
        {
            _logger.LogInformation($"Switching user {userEmail} to Candidate");

            // Find the user by email
            var voter = await _userManager.FindByEmailAsync(userEmail);
            if (voter == null)
            {
                _logger.LogError($"User {userEmail} not Found");
                return new VoteRequestStatus(false, "User Not Found");
            }
            var voterId = voter.Id;

            // Check if the user has any pending votes
            var hasAnyVotes = await _dbContext.VoteTokens
                .AnyAsync(i => i.VoterId == voterId);

            if (hasAnyVotes)
            {
                _logger.LogError($"User {userEmail} cannot switch to candidate as their vote count is not 0");
                return new VoteRequestStatus(false, "Cannot switch a user with cast votes to a candidate");
            }

            // Change roles
            await _userManager.RemoveFromRoleAsync(voter, "Voter");
            await _userManager.AddToRoleAsync(voter, "Candidate");

            // Reset the vote count cache of the user
            voter.VoteCount = 0;

            await _dbContext.SaveChangesAsync();

            await transaction.CommitAsync();

            _logger.LogInformation($"User {userEmail} is now a Candidate");
        }
        return new VoteRequestStatus(true, "Role changed"); ;
    }

    public async Task<VoteRequestStatus> SwitchToVoter(string userEmail)
    {
        // Use of Serialization lock to minimize race conditions on db
        using (var transaction = await _dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable))
        {
            _logger.LogInformation($"Switching user {userEmail} to Voter");

            // Get the user by email (voter = main user in request)
            var voter = await _userManager.FindByEmailAsync(userEmail);
            if (voter == null)
            {
                _logger.LogError($"User {userEmail} not found");
                return new VoteRequestStatus(false, "User Not Found");
            }
            var voterId = voter.Id;

            // Check if the user has any pending votes for them
            var hasAnyVotes = await _dbContext.VoteTokens
                .AnyAsync(i => i.CandidateId == voterId);

            if (hasAnyVotes)
            {
                _logger.LogError($"Cannot change user {userEmail} to Voter as their vote count is not 0");
                return new VoteRequestStatus(false, "Cannot change a candidate to a voter if their vote count is not 0"); ;
            }

            // Change roles
            await _userManager.RemoveFromRoleAsync(voter, "Candidate");
            await _userManager.AddToRoleAsync(voter, "Voter");

            // reset the vote count cache of the user
            voter.VoteCount = 0;

            await _dbContext.SaveChangesAsync();

            await transaction.CommitAsync();

            _logger.LogInformation($"User {userEmail} is now a Voter");
        }
        return new VoteRequestStatus(true, "Role changed"); ;
    }
}