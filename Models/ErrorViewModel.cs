namespace VotingApp.Web.Models;


/// <summary>
/// Standard Error model in ASP.
/// </summary>
public class ErrorViewModel
{
    public string? RequestId { get; set; }

    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
}
