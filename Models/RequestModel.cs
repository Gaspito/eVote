namespace VotingApp.Web.Models;

/// <summary>
/// Expected data model of vote requests in JSON bodies.
/// </summary>
public class VoteRequestModel
{
    /// <summary>
    /// Email of the candidate the request targets.
    /// </summary>
    public string? Email { get; set; }
}


/// <summary>
/// Exchange data model between the controller and the request handler service.
/// </summary>
public class VoteRequestStatus
{
    /// <summary>
    /// True if the request was successful.
    /// </summary>
    public bool Success { get; set; } = false;
    /// <summary>
    /// Optional message on the status of the request. 
    /// When not the request failed, this should contain a meaningful error message.
    /// </summary>
    public string Message { get; set; } = "";

    // Constructors:

    public VoteRequestStatus(bool success)
    {
        Success = success;
    }

    public VoteRequestStatus(bool success, string message)
    {
        Success = success;
        Message = message;
    }
}