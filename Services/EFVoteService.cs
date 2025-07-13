using System.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using VotingApp.Web.Models;

/// <summary>
/// Entity Framework only implementation of the Vote service
/// </summary>
public class EFVoteService : IVoteService
{
    public AppDbContext _dbContext;
    public UserManager<AppUser> _userManager;

    private readonly int _userVoteLimit;

    public EFVoteService(IConfiguration config, AppDbContext dbContext, UserManager<AppUser> userManager)
    {
        _userVoteLimit = config.GetValue<int>("ServiceConfig:CastVoteLimit");
        _dbContext = dbContext;
        _userManager = userManager;
    }

    public async Task<VoteRequestStatus> TryAddVoteAsync(string voterEmail, string candidateEmail)
    {
        using (var transaction = await _dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable))
        {
            // Get the candidate by email
            var candidate = await _dbContext.Users.FirstOrDefaultAsync(i => i.Email == candidateEmail);
            if (candidate == null)
                return new VoteRequestStatus(false, "Candidate Not Found");

            // Check if the candidate is indeed a candidate
            if (!await _userManager.IsInRoleAsync(candidate, "Candidate"))
                return new VoteRequestStatus(false, "Cannot cast a vote to users that are not candidates");

            // Get voter by email
            var voter = await _dbContext.Users.FirstOrDefaultAsync(i => i.Email == voterEmail);
            if (voter == null)
                return new VoteRequestStatus(false, "User Not Found");

            // Check if the voter is indeed a voter
            if (!await _userManager.IsInRoleAsync(voter, "Voter"))
                return new VoteRequestStatus(false, "User is not a Voter");

            // Get the voters' vortes
            var votes = await _dbContext.VoteTokens
                .Where(i => i.VoterId == voter.Id)
                .ToListAsync();

            // Check if the voter has available votes
            if (votes.Count >= _userVoteLimit)
                return new VoteRequestStatus(false, "Vote limit reached");

            // Check if the voter already voted for that candidate
            if (votes.Any(i => i.CandidateId == candidate.Id))
                return new VoteRequestStatus(false, "Cannot vote for a candidate more than once");

            // Add a vote
            var vote = new VoteToken();
            vote.CandidateId = candidate.Id;
            vote.VoterId = voter.Id;

            await _dbContext.VoteTokens.AddAsync(vote);

            // Update the vote count cache per user
            voter.VoteCount++;
            candidate.VoteCount++;

            _dbContext.Update(voter);
            _dbContext.Update(candidate);

            await _dbContext.SaveChangesAsync();

            await transaction.CommitAsync();
        }
        return new VoteRequestStatus(true, "Vote cast");
    }

    public async Task<VoteRequestStatus> TryRemoveVoteAsync(string voterEmail, string candidateEmail)
    {
        using (var transaction = await _dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable))
        {
            // Get the candidate by email
            var candidate = await _dbContext.Users.FirstOrDefaultAsync(i => i.Email == candidateEmail);
            if (candidate == null)
                return new VoteRequestStatus(false, "Candidate Not Found"); ;

            // Check if the candidate is indeed a candidate
            if (!await _userManager.IsInRoleAsync(candidate, "Candidate"))
                return new VoteRequestStatus(false, "Cannot remove votes of users that are not candidates"); ;

            // Get voter by email
            var voter = await _dbContext.Users.FirstOrDefaultAsync(i => i.Email == voterEmail);
            if (voter == null)
                return new VoteRequestStatus(false, "User Not Found"); ;

            // Check if the voter is indeed a voter
            if (!await _userManager.IsInRoleAsync(voter, "Voter"))
                return new VoteRequestStatus(false, "User is not a voter"); ;

            // Find the vote
            var vote = await _dbContext.VoteTokens
                .FirstOrDefaultAsync(i => i.VoterId == voter.Id && i.CandidateId == candidate.Id);

            // Check if the vote exists
            if (vote == null)
                return new VoteRequestStatus(false, "Vote Not Found"); ;

            // Remove the vote
            _dbContext.VoteTokens.Remove(vote);

            // Update the vote cache per user
            voter.VoteCount--;
            candidate.VoteCount--;

            _dbContext.Update(voter);
            _dbContext.Update(candidate);

            await _dbContext.SaveChangesAsync();

            await transaction.CommitAsync();
        }
        return new VoteRequestStatus(true, "Vote removed"); ;
    }
}