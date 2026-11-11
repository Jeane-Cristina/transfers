using System.Text;
using RabbitMQ.Client;
using Transfers.Infrastructure.Outbox;

namespace Transfers.Infrastructure.Messaging;

public sealed class RabbitMqPublisher(RabbitMqConnection connection) : IAsyncDisposable
{
    private IChannel? _channel;

    public async Task PublishAsync(OutboxMessage message, CancellationToken ct)
    {
        var channel = await GetChannelAsync(ct);

        var properties = new BasicProperties
        {
            MessageId = message.Id.ToString(),
            Type = message.Type,
            ContentType = "application/json",
            DeliveryMode = DeliveryModes.Persistent,
            Timestamp = new AmqpTimestamp(message.OccurredAt.ToUnixTimeSeconds())
        };

        await channel.BasicPublishAsync(
            message.Exchange, message.RoutingKey, mandatory: true,
            basicProperties: properties, body: Encoding.UTF8.GetBytes(message.Payload),
            cancellationToken: ct);
    }

    private async ValueTask<IChannel> GetChannelAsync(CancellationToken ct)
    {
        if (_channel is { IsOpen: true })
            return _channel;

        var conn = await connection.GetAsync(ct);
        _channel = await conn.CreateChannelAsync(
            new CreateChannelOptions(publisherConfirmationsEnabled: true, publisherConfirmationTrackingEnabled: true),
            ct);
        await RabbitMqTopology.DeclareAsync(_channel, ct);
        return _channel;
    }

    public async ValueTask DisposeAsync()
    {
        if (_channel is not null)
            await _channel.DisposeAsync();
    }
}
