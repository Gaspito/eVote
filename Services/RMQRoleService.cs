using VotingApp.Web.Models;


/// <summary>
/// RabbitMq implementation of the role service.
/// </summary>
public class RMQRoleService : IRoleService
{
    private readonly IMessagePublisher _publisher;
    private readonly IConfiguration _config;

    public RMQRoleService(IMessagePublisher publisher, IConfiguration config)
    {
        _publisher = publisher;
        _config = config;
    }

    public async Task<VoteRequestStatus> SwitchToCandidate(string userEmail)
    {
        var msg = new VoteMessage
        {
            CandidateEmail = "",
            VoterEmail = userEmail, // main user email
            Action = "SwitchToCandidate"
        };

        // dispatch to rabbit mq
        var queueName = _config["RabbitMq:VoteQueue"];
        var resp = await _publisher.PublishAsync(msg, queueName);
        if (resp.Status == "Success")
        {
            return new VoteRequestStatus(true, "Switched to Candidate");
        }
        return new VoteRequestStatus(false, resp.Message);
    }

    public async Task<VoteRequestStatus> SwitchToVoter(string userEmail)
    {
        var msg = new VoteMessage
        {
            CandidateEmail = "",
            VoterEmail = userEmail,
            Action = "SwitchToVoter"
        };

        // dispatch to rabbit mq
        var queueName = _config["RabbitMq:VoteQueue"];
        var resp = await _publisher.PublishAsync(msg, queueName);
        if (resp.Status == "Success")
        {
            return new VoteRequestStatus(true, "Switched to Voter");
        }
        return new VoteRequestStatus(false, resp.Message);
    }
}