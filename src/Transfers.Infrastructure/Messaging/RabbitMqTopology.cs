using RabbitMQ.Client;
using static Transfers.Infrastructure.Messaging.MessagingTopology;

namespace Transfers.Infrastructure.Messaging;

public static class RabbitMqTopology
{
    public const string AuditQueue = "transfers.events.audit";

    public static async Task DeclareAsync(IChannel channel, CancellationToken ct)
    {
        await channel.ExchangeDeclareAsync(EventsExchange, ExchangeType.Topic, durable: true, cancellationToken: ct);
        await channel.ExchangeDeclareAsync(CommandsExchange, ExchangeType.Direct, durable: true, cancellationToken: ct);
        await channel.ExchangeDeclareAsync(DeadLetterExchange, ExchangeType.Fanout, durable: true, cancellationToken: ct);

        await channel.QueueDeclareAsync(DeadLetterQueue, durable: true, exclusive: false, autoDelete: false,
            cancellationToken: ct);
        await channel.QueueBindAsync(DeadLetterQueue, DeadLetterExchange, routingKey: "", cancellationToken: ct);

        await channel.QueueDeclareAsync(ExecuteScheduledQueue, durable: true, exclusive: false, autoDelete: false,
            arguments: new Dictionary<string, object?>
            {
                ["x-queue-type"] = "quorum",
                ["x-delivery-limit"] = 5,
                ["x-dead-letter-exchange"] = DeadLetterExchange
            },
            cancellationToken: ct);
        await channel.QueueBindAsync(ExecuteScheduledQueue, CommandsExchange, ExecuteScheduledRoutingKey,
            cancellationToken: ct);

        await channel.QueueDeclareAsync(AuditQueue, durable: true, exclusive: false, autoDelete: false,
            cancellationToken: ct);
        await channel.QueueBindAsync(AuditQueue, EventsExchange, "transfer.#", cancellationToken: ct);
    }
}
