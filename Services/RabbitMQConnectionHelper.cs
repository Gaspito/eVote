using RabbitMQ.Client;

/// <summary>
/// Interface to a helper class that creates the connection to RabbitMq
/// </summary>
public interface IRabbitMqConnection : IDisposable
{
    /// <summary>
    /// Returns a new RabbitMq channel
    /// </summary>
    Task<IChannel> CreateModel();
}


/// <summary>
/// Default implementation of the connector class to RabbitMq
/// </summary>
public class RabbitMqConnection : IRabbitMqConnection
{
    private readonly ILogger<RabbitMqConnection> _logger;
    private IConnection? _connection;

    public RabbitMqConnection(ILogger<RabbitMqConnection> logger, IConfiguration config)
    {
        _logger = logger;
        SetupConnection(config).Wait();
    }

    /// <summary>
    /// Establishes a connection to RabbitMq
    /// </summary>
    private async Task<int> SetupConnection(IConfiguration config)
    {
        var factory = new ConnectionFactory
        {
            HostName = config["RabbitMq:HostName"],
            UserName = config["RabbitMq:UserName"],
            Password = config["RabbitMq:Password"],
        };
        _connection = await factory.CreateConnectionAsync();
        _logger.LogInformation("Connection to RabbitMq Established");
        return 0;
    }

    public async Task<IChannel> CreateModel() => await _connection.CreateChannelAsync();

    public void Dispose()
    {
        DisposeAsync().Wait();
    }

    /// <summary>
    /// Closes the connection to RabbitMq
    /// </summary>
    public async Task<int> DisposeAsync()
    {
        var connection = _connection;
        if (connection == null) return 1;
        await connection.CloseAsync();
        await connection.DisposeAsync();
        _logger.LogInformation("Connection to RabbitMq Closed");
        return 0;
    }
}