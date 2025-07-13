using VotingApp.Web.Models;


/// <summary>
/// Interface for the Role service backends
/// </summary>
public interface IRoleService
{
    /// <summary>
    /// Changes the role of the given user (by email) to 'Candidate'
    /// </summary>
    Task<VoteRequestStatus> SwitchToCandidate(string userEmail);
    /// <summary>
    /// Changes the role of the given user (by email) to 'Voter'
    /// </summary>
    Task<VoteRequestStatus> SwitchToVoter(string userEmail);
}
