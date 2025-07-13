using System.Text;
using Microsoft.AspNetCore.Identity;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Data;
using Microsoft.EntityFrameworkCore;
using VotingApp.Web.Models;

/// <summary>
/// Background service that consumes RabbitMq messages,
/// and handles the appropriate requests.
/// This service should only be instantiated once for the
/// app, becoming the central source of truth, and only
/// service writing to the database (except for new user registration)
/// </summary>
public class VoteQueueConsumer : BackgroundService
{
    private readonly IRabbitMqConnection _connection;
    private readonly IServiceProvider _services;
    private IChannel? _channel;

    private readonly int _userMaxVoteCount;

    public VoteQueueConsumer(IConfiguration config, IRabbitMqConnection connection,
                             IServiceProvider services)
    {
        _userMaxVoteCount = config.GetValue<int>("ServiceConfig:CastVoteLimit");
        _connection = connection;
        _services = services;
    }

    /// <summary>
    /// Creates a new RabbitMq channel, and starts listening to the vote queue.
    /// </summary>
    private async Task SetupChannelAsync()
    {
        _channel = await _connection.CreateModel();

        await _channel.QueueDeclareAsync(
            queue: "vote-queue",
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null
        );

        // Rewire consumer every time channel is recreated
        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += async (model, ea) => await HandleVoteAsync(ea);

        await _channel.BasicConsumeAsync("vote-queue", false, consumer);
    }

    /// <summary>
    /// This method is called for every new request recieved from RabbitMq.
    /// It dispatches to other methods depending on the request.
    /// </summary>
    protected async Task HandleVoteAsync(BasicDeliverEventArgs ea)
    {
        var json = Encoding.UTF8.GetString(ea.Body.ToArray());
        var voteMsg = JsonConvert.DeserializeObject<VoteMessage>(json);
        var replyQueue = ea.BasicProperties.ReplyTo;

        // Process with a scoped DbContext/UserManager
        // These services must not be injected from the constructor
        using var scope = _services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Prepare a default response object
        var resp = new VoteResponseMessage
        {
            Status = "Unhandled",
            Message = $"The request could not be handled. Action: '{voteMsg.Action}'",
        };

        // Dispatch to other methods
        switch (voteMsg.Action)
        {
            case "Add":
                resp = await AddVote(dbContext, userManager, voteMsg);
                break;
            case "Remove":
                resp = await RemoveVote(dbContext, userManager, voteMsg);
                break;
            case "SwitchToCandidate":
                resp = await SwitchToCandidate(dbContext, userManager, voteMsg);
                break;
            case "SwitchToVoter":
                resp = await SwitchToVoter(dbContext, userManager, voteMsg);
                break;
        }

        // Prepare a reply
        resp.CorrelationId = voteMsg.CorrelationId;

        var replyProps = new BasicProperties
        {
            CorrelationId = voteMsg.CorrelationId,
            ContentType = "application/json"
        };

        // Send the reply
        var replyJson = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(resp));
        await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
        await _channel.BasicPublishAsync("", replyQueue, false, replyProps, replyJson);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Start listening to the vote queue
        await SetupChannelAsync();

        // Keep the service alive as long as it is not cancelled
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000);
        }

        // TO DO: handle cases where the connection to RabbitMq is broken, and attempt to reconnect.
    }


    public override void Dispose()
    {
        // Close the connection to RabbitMq
        _channel?.CloseAsync().Wait();
        _channel?.Dispose();
        base.Dispose();
    }


    /// <summary>
    /// Cast a vote request
    /// </summary>
    private async Task<VoteResponseMessage> AddVote(AppDbContext _dbContext, UserManager<AppUser> _userManager, VoteMessage message)
    {
        var voterEmail = message.VoterEmail;
        var candidateEmail = message.CandidateEmail;

        // ReadCommitted isolation level is set here to guard against logical errors and concurrent processing bugs
        using (var transaction = await _dbContext.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted))
        {
            // Get the candidate by email
            var candidate = await _dbContext.Users.FirstOrDefaultAsync(i => i.Email == candidateEmail);
            if (candidate == null)
                return new VoteResponseMessage { Status = "Failed", Message = "Candidate not found" };

            // Check if the candidate is indeed a candidate
            if (!await _userManager.IsInRoleAsync(candidate, "Candidate"))
                return new VoteResponseMessage { Status = "Failed", Message = "User is not a candidate" };

            // Get voter by email
            var voter = await _dbContext.Users.FirstOrDefaultAsync(i => i.Email == voterEmail);
            if (voter == null)
                return new VoteResponseMessage { Status = "Failed", Message = "Voter not found" };

            // Check if the voter is indeed a voter
            if (!await _userManager.IsInRoleAsync(voter, "Voter"))
                return new VoteResponseMessage { Status = "Failed", Message = "User is not a voter" };

            // Get the voter's votes
            var votes = await _dbContext.VoteTokens
                .Where(i => i.VoterId == voter.Id)
                .ToListAsync();

            // Check if the vote limit was reached
            if (votes.Count >= _userMaxVoteCount)
                return new VoteResponseMessage { Status = "Failed", Message = "Vote limit reached" };

            // Check if the voter already voted for that candidate
            if (votes.Any(i => i.CandidateId == candidate.Id))
                return new VoteResponseMessage { Status = "Failed", Message = "Already voted for that candidate" };

            // Add a vote
            var vote = new VoteToken();
            vote.CandidateId = candidate.Id;
            vote.VoterId = voter.Id;

            await _dbContext.VoteTokens.AddAsync(vote);

            // Update vote count cache per user
            voter.VoteCount++;
            candidate.VoteCount++;

            _dbContext.Update(voter);
            _dbContext.Update(candidate);

            await _dbContext.SaveChangesAsync();

            await transaction.CommitAsync();
        }
        return new VoteResponseMessage { Status = "Success", Message = "Vote Added" };
    }

    /// <summary>
    /// Remove a cast vote
    /// </summary>
    private async Task<VoteResponseMessage> RemoveVote(AppDbContext _dbContext, UserManager<AppUser> _userManager, VoteMessage message)
    {
        var voterEmail = message.VoterEmail;
        var candidateEmail = message.CandidateEmail;

        // ReadCommitted isolation level is set here to guard against logical errors and concurrent processing bugs
        using (var transaction = await _dbContext.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted))
        {
            // Get the candidate by email
            var candidate = await _dbContext.Users.FirstOrDefaultAsync(i => i.Email == candidateEmail);
            if (candidate == null)
                return new VoteResponseMessage { Status = "Failed", Message = "Candidate not found" };

            // Check if the candidate is indeed a candidate
            if (!await _userManager.IsInRoleAsync(candidate, "Candidate"))
                return new VoteResponseMessage { Status = "Failed", Message = "User is not a candidate" };

            // Get voter by email
            var voter = await _dbContext.Users.FirstOrDefaultAsync(i => i.Email == voterEmail);
            if (voter == null)
                return new VoteResponseMessage { Status = "Failed", Message = "Voter not found" };

            // Check if the voter is indeed a voter
            if (!await _userManager.IsInRoleAsync(voter, "Voter"))
                return new VoteResponseMessage { Status = "Failed", Message = "User is not a voter" };

            // Find the vote
            var vote = await _dbContext.VoteTokens
                .FirstOrDefaultAsync(i => i.VoterId == voter.Id && i.CandidateId == candidate.Id);

            // Check if the vote exists
            if (vote == null)
                return new VoteResponseMessage { Status = "Failed", Message = "Vote not found" };

            // Remove the vote
            _dbContext.VoteTokens.Remove(vote);

            // Update the cached vote counts per user
            voter.VoteCount--;
            candidate.VoteCount--;

            _dbContext.Update(voter);
            _dbContext.Update(candidate);

            await _dbContext.SaveChangesAsync();

            await transaction.CommitAsync();
        }
        return new VoteResponseMessage { Status = "Success", Message = "Vote removed" };
    }

    /// <summary>
    /// Switch the main user to a candidate role
    /// </summary>
    private async Task<VoteResponseMessage> SwitchToCandidate(AppDbContext _dbContext, UserManager<AppUser> _userManager, VoteMessage message)
    {
        var userEmail = message.VoterEmail;

        // ReadCommitted isolation level is set here to guard against logical errors and concurrent processing bugs
        using (var transaction = await _dbContext.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted))
        {
            // Find the user by email
            var voter = await _userManager.FindByEmailAsync(userEmail);
            if (voter == null)
                return new VoteResponseMessage { Status = "Failed", Message = "User not found" };
            var voterId = voter.Id;

            // Check if the user has any cast votes
            var anyVotes = await _dbContext.VoteTokens
                .AnyAsync(i => i.VoterId == voterId);

            if (anyVotes)
                return new VoteResponseMessage { Status = "Failed", Message = "User has active votes. Remove them before attempting to switch to a candidate." };

            // Reset the voter's cached vote count
            voter.VoteCount = 0;

            // Change roles
            await _userManager.RemoveFromRoleAsync(voter, "Voter");
            await _userManager.AddToRoleAsync(voter, "Candidate");

            await _dbContext.SaveChangesAsync();

            await transaction.CommitAsync();

            Console.WriteLine($"Switched user {voter.Email} to Candidate");
        }
        return new VoteResponseMessage { Status = "Success", Message = "User became a Candidate" };
    }

    /// <summary>
    /// Change user to a voter role
    /// </summary>
    private async Task<VoteResponseMessage> SwitchToVoter(AppDbContext _dbContext, UserManager<AppUser> _userManager, VoteMessage message)
    {
        // voter email is used as main user email, regardless of role
        var userEmail = message.VoterEmail;

        // ReadCommitted isolation level is set here to guard against logical errors and concurrent processing bugs
        using (var transaction = await _dbContext.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted))
        {
            // Find candidate by email
            var candidate = await _userManager.FindByEmailAsync(userEmail);
            if (candidate == null)
                return new VoteResponseMessage { Status = "Failed", Message = "User not found" };
            var candidateId = candidate.Id;

            // Check if the candidate has any votes for them
            var anyVotes = await _dbContext.VoteTokens
                .AnyAsync(i => i.CandidateId == candidateId);

            if (anyVotes)
                return new VoteResponseMessage { Status = "Failed", Message = "User has votes for them" };

            // Update the cached vote count
            candidate.VoteCount = 0;

            // Change roles
            await _userManager.RemoveFromRoleAsync(candidate, "Candidate");
            await _userManager.AddToRoleAsync(candidate, "Voter");

            await _dbContext.SaveChangesAsync();

            await transaction.CommitAsync();
        }
        return new VoteResponseMessage { Status = "Success", Message = "User became a Voter" };
    }
}