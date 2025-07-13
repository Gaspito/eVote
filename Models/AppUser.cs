using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

/// <summary>
/// Custom User Model for the web app.
/// New fields for users can be added here.
/// AppUser should be referenced everywhere instead of IdentityUser.
/// </summary>
public class AppUser : IdentityUser
{
    /// <summary>
    /// Timestamp used for optimistic concurrency strategies.
    /// </summary>
    [Timestamp]
    public byte[] RowVersion { get; set; }

    /// <summary>
    /// Number of votes cast by or cast for the User, depending on their role.
    /// For Voters, this is the number of votes they casted.
    /// For Candidates, this is the number of votes they received.
    /// Important: this field is meant as a simple cache. It may not perfectly
    /// reflect the actual vote count, which takes longer to compute as
    /// vote tokens must be counted one by one.
    /// </summary>
    public int VoteCount { get; set; } = 0;
}