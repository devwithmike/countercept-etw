using RabbitMQ.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace App.RabbitMQ;

public interface IRabbitManager : IDisposable
{
    Task PublishEventAsync(byte[] messageBody);
    Task ConnectAsync();
}

public class RabbitManager : IRabbitManager
{
    private readonly RabbitMQSettings _settings;
    private readonly ILogger<RabbitManager> _logger;
    private IConnection? _connection;
    private IChannel? _channel;

    public RabbitManager(IOptions<RabbitMQSettings> options, ILogger<RabbitManager> logger)
    {
        _settings = options.Value;
        _logger = logger;
    }

    public async Task ConnectAsync()
    {
        try
        {
            var factory = new ConnectionFactory()
            {
                HostName = _settings.HostName,
                Port = _settings.Port,
                UserName = _settings.UserName,
                Password = _settings.Password,
                VirtualHost = _settings.VirtualHost
            };

            _connection = await factory.CreateConnectionAsync();
            _channel = await _connection.CreateChannelAsync();

            // Declare the queue
            await _channel.QueueDeclareAsync(
                queue: _settings.QueueName,
                durable: true,
                exclusive: false,
                autoDelete: false
            );

            _logger.LogInformation("Successfully connected to RabbitMQ at {HostName}:{Port}", 
                _settings.HostName, _settings.Port);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to RabbitMQ");
            throw;
        }
    }

    public async Task PublishEventAsync(byte[] messageBody)
    {
        if (_channel == null || _channel.IsClosed)
        {
            throw new InvalidOperationException("RabbitMQ channel is not connected. Call ConnectAsync first.");
        }

        try
        {
            var properties = new BasicProperties
            {
                ContentType = "application/protobuf",
                DeliveryMode = DeliveryModes.Persistent,
                Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds())
            };

            await _channel.BasicPublishAsync(
                exchange: "",
                routingKey: _settings.QueueName,
                mandatory: false,
                basicProperties: properties,
                body: messageBody
            );

            _logger.LogDebug("Published event to RabbitMQ");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish event to RabbitMQ");
            throw;
        }
    }

    public void Dispose()
    {
        try
        {
            _channel?.CloseAsync().GetAwaiter().GetResult();
            _channel?.Dispose();
            _connection?.CloseAsync().GetAwaiter().GetResult();
            _connection?.Dispose();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disposing RabbitMQ resources");
        }
    }
}
