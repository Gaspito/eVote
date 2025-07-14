using VotingApp.Web.Models;


/// <summary>
/// RabbitMq implementation of the role service.
/// </summary>
public class RMQRoleService : IRoleService
{
    private readonly ILogger<RabbitMqConnection> _logger;
    private readonly IMessagePublisher _publisher;
    private readonly IConfiguration _config;

    public RMQRoleService(ILogger<RabbitMqConnection> logger, IMessagePublisher publisher, IConfiguration config)
    {
        _logger = logger;
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
        _logger.LogInformation($"Publishing Switch to Candidate ({userEmail}) message to RabbitMq");

        // dispatch to rabbit mq
        var queueName = _config["RabbitMq:VoteQueue"];
        var resp = await _publisher.PublishAsync(msg, queueName);
        if (resp.Status == "Success")
        {
            _logger.LogError($"Failed to Switch to Candidate ({userEmail})");
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
        _logger.LogInformation($"Publishing Switch to Voter ({userEmail}) message to RabbitMq");

        // dispatch to rabbit mq
        var queueName = _config["RabbitMq:VoteQueue"];
        var resp = await _publisher.PublishAsync(msg, queueName);
        if (resp.Status == "Success")
        {
            _logger.LogError($"Failed to Switch to Voter ({userEmail})");
            return new VoteRequestStatus(true, "Switched to Voter");
        }
        return new VoteRequestStatus(false, resp.Message);
    }
}