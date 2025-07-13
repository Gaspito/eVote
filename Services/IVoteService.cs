using VotingApp.Web.Models;


/// <summary>
/// Interface for the Vote Service backends
/// </summary>
public interface IVoteService
{
    /// <summary>
    /// Tries to cast a vote from the given voter (by email) to the given candidate (by email).
    /// </summary>
    Task<VoteRequestStatus> TryAddVoteAsync(string voterEmail, string candidateEmail);

    /// <summary>
    /// Tries to remove a vote from the given voter (by email) to the given candidate (by email).
    /// </summary>
    Task<VoteRequestStatus> TryRemoveVoteAsync(string voterEmail, string candidateEmail);
}