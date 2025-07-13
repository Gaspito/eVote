using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace VotingApp.Web.Models;

/// <summary>
/// Represents a Vote cast from a Voter to a Candidate (ballot).
/// </summary>
public class VoteToken
{
    [Key]
    public long Id { get; set; }

    /// <summary>
    /// Id (not email) of the user that cast the vote.
    /// </summary>
    [ForeignKey("Voter")]
    [Required]
    public string VoterId { get; set; }
    /// <summary>
    /// Id (not email) of the candidate that received the vote.
    /// </summary>
    [ForeignKey("Candidate")]
    [Required]
    public string CandidateId { get; set; }

    // Relationships:
    
    public AppUser Voter { get; set; }
    public AppUser Candidate { get; set; }
}
