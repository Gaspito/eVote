namespace VotingApp.Web.Models;

/*
    Models in this file are used by decoupled services, such as RabbitMQ
    and the Web App's own services. They are the data exchange models
    between them.
*/

/// <summary>
/// Data exchange model between controllers and request handler services.
/// </summary>
public class VoteMessage
{
    /// <summary>
    /// Email of the voter, or main user for the request.
    /// </summary>
    public string? VoterEmail { get; set; }
    /// <summary>
    /// Optional email of the candidate for the request.
    /// </summary>
    public string? CandidateEmail { get; set; }
    /// <summary>
    /// Action the request targets. Should be a valid action name.
    /// </summary>
    public string? Action { get; set; }

    /// <summary>
    /// Correlation Id used to listen for response messages, 
    /// to know if the request was handled.
    /// Is automatically filled by appropriate services.
    /// </summary>
    public string? CorrelationId { get; set; }
}

/// <summary>
/// Data exchange model of responses sent back to request handler services.
/// </summary>
public class VoteResponseMessage
{
    /// <summary>
    /// Correlation Id matching the VoteMessage request's.
    /// </summary>
    public string? CorrelationId { get; set; }
    /// <summary>
    /// Status identifier name (i.e. Success, Failed).
    /// TBD: implement using Enums.
    /// </summary>
    public string? Status { get; set; }
    /// <summary>
    /// Optional status message. In case of errors,
    /// this field should contain a meaningful error message.
    /// </summary>
    public string? Message { get; set; }
}