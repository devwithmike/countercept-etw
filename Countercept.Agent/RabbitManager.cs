using RabbitMQ.Client;
using Google.Protobuf;
using Countercept.Shared.Protobuf;

namespace Countercept.Agent;

public class RabbitManager : IAsyncDisposable
{
    private readonly string _queueName = "countercept_events";
    private IConnection? _connection;
    private IChannel? _channel;
    private readonly string _hostName;

    public RabbitManager(string hostName)
    {
        _hostName = hostName;
    }

    public async Task InitializeAsync()
    {
        var factory = new ConnectionFactory() { HostName = _hostName };

        _connection = await factory.CreateConnectionAsync();
        _channel = await _connection.CreateChannelAsync();

        await _channel.QueueDeclareAsync(queue: _queueName,
                                        durable: true,
                                        exclusive: false,
                                        autoDelete: false,
                                        arguments: null);
    }

    public async Task PublishEventAsync(EtwEvent protoEvent)
    {
        if (_channel == null) return;

        byte[] body = protoEvent.ToByteArray();

        await _channel.BasicPublishAsync(
            exchange: string.Empty,
            routingKey: _queueName,
            body: body);
    }

    public async ValueTask DisposeAsync()
    {
        if (_channel != null) await _channel.CloseAsync();
        if (_connection != null) await _connection.CloseAsync();
    }
}