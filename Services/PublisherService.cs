using System.Text;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using VotingApp.Web.Models;

/// <summary>
/// Interface to a message publisher service for RabbitMq
/// </summary>
public interface IMessagePublisher
{
    /// <summary>
    /// Publishes a new message to an open queue of RabbitMq
    /// </summary>
    /// <typeparam name="T">Message Type</typeparam>
    /// <param name="message">Message Object</param>
    /// <param name="queueName">Name of the open queue</param>
    /// <returns>Response object</returns>
    Task<VoteResponseMessage> PublishAsync<T>(T message, string queueName) where T : VoteMessage;
}


/// <summary>
/// Default implementation of the message publisher to RabbitMq class
/// </summary>
public class RabbitMqPublisher : IMessagePublisher
{
    private readonly IRabbitMqConnection _connection;
    private readonly ILogger<RabbitMqPublisher> _logger;

    public RabbitMqPublisher(IRabbitMqConnection connection,
                             ILogger<RabbitMqPublisher> logger)
    {
        _connection = connection;
        _logger = logger;
    }

    public async Task<VoteResponseMessage> PublishAsync<T>(T message, string queueName) where T : VoteMessage
    {
        var correlationId = Guid.NewGuid().ToString();
        message.CorrelationId = correlationId;

        // Get channel to send messages over
        using var channel = await _connection.CreateModel();
        var replyQueue = (await channel.QueueDeclareAsync(queue: "",
            durable: false,
            exclusive: true,
            arguments: null,
            autoDelete: true)).QueueName;
        
        // Declare use of the queue
        await channel.QueueDeclareAsync(queue: queueName,
                             durable: true,
                             exclusive: false,
                             autoDelete: false,
                             arguments: null);

        // Prepare to send the message
        var body = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(message));
        var props = new BasicProperties
        {
            ReplyTo = replyQueue,
            CorrelationId = correlationId,
            ContentType = "application/json",
            DeliveryMode = DeliveryModes.Persistent
        };

        _logger.LogInformation("Published message to {Queue}", queueName);

        var tcs = new TaskCompletionSource<VoteResponseMessage>();

        // Prepare a consummer for the response
        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (ch, ea) =>
        {
            if (ea.BasicProperties.CorrelationId == correlationId)
            {
                var json = Encoding.UTF8.GetString(ea.Body.ToArray());

                _logger.LogInformation("Response consumer got message: {Json}", json);

                var response = JsonConvert.DeserializeObject<VoteResponseMessage>(json);
                tcs.TrySetResult(response);

                await channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
            }
            return;
        };

        await channel.BasicConsumeAsync(queue: replyQueue, autoAck: false, consumer: consumer);

        // Publish the message

        await channel.BasicPublishAsync(exchange: "",
                             routingKey: queueName,
                             mandatory: false,
                             basicProperties: props,
                             body: body);

        // Wait 3 seconds for a response, or issue a timeout
        var resp = await Task.WhenAny(tcs.Task, Task.Delay(3000)) == tcs.Task
            ? tcs.Task.Result
            : new VoteResponseMessage
            {
                Status = "Failure",
                Message = "Timeout",
                CorrelationId = correlationId
            };

        // Close channels after use
        await channel.CloseAsync();
        await channel.DisposeAsync();

        return resp;
    }
}