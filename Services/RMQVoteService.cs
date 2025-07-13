using VotingApp.Web.Models;


/// <summary>
/// RabbitMq implementation of the vote service.
/// </summary>
public class RMQVoteService : IVoteService
{
    private readonly IMessagePublisher _publisher;
    private readonly IConfiguration _config;

    public RMQVoteService(IMessagePublisher publisher, IConfiguration config)
    {
        _publisher = publisher;
        _config = config;
    }


    public async Task<VoteRequestStatus> TryAddVoteAsync(string voterEmail, string candidateEmail)
    {
        var msg = new VoteMessage
        {
            CandidateEmail = candidateEmail,
            VoterEmail = voterEmail,
            Action = "Add"
        };

        // dispatch the request through rabbit mq
        var queueName = _config["RabbitMq:VoteQueue"];
        var resp = await _publisher.PublishAsync(msg, queueName);
        if (resp.Status == "Success")
        {
            return new VoteRequestStatus(true, "Vote cast");
        }
        return new VoteRequestStatus(false, resp.Message);
    }

    public async Task<VoteRequestStatus> TryRemoveVoteAsync(string voterEmail, string candidateEmail)
    {
        var msg = new VoteMessage
        {
            CandidateEmail = candidateEmail,
            VoterEmail = voterEmail,
            Action = "Remove"
        };

        // dispatch the request through rabbit mq
        var queueName = _config["RabbitMq:VoteQueue"];
        var resp = await _publisher.PublishAsync(msg, queueName);
        if (resp.Status == "Success")
        {
            return new VoteRequestStatus(true, "Vote removed");
        }
        return new VoteRequestStatus(false, resp.Message);
    }
}