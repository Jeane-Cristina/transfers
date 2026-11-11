using System.Text.Json;
using Transfers.Domain.Common;
using Transfers.Domain.Transfers;
using Transfers.Infrastructure.Messaging;

namespace Transfers.Infrastructure.Outbox;

public sealed class OutboxMessage
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public Guid Id { get; init; } = Guid.CreateVersion7();
    public string Type { get; init; } = default!;
    public string Exchange { get; init; } = default!;
    public string RoutingKey { get; init; } = default!;
    public string Payload { get; init; } = default!;
    public DateTimeOffset OccurredAt { get; init; }
    public DateTimeOffset? PublishedAt { get; set; }
    public int Attempts { get; set; }
    public string? LastError { get; set; }

    public static OutboxMessage FromDomainEvent(IDomainEvent domainEvent) => new()
    {
        Type = domainEvent.GetType().Name,
        Exchange = MessagingTopology.EventsExchange,
        RoutingKey = domainEvent switch
        {
            TransferScheduled => "transfer.scheduled",
            TransferCompleted => "transfer.completed",
            TransferFailed => "transfer.failed",
            TransferCancelled => "transfer.cancelled",
            _ => throw new InvalidOperationException($"Evento sem rota: {domainEvent.GetType().Name}")
        },
        Payload = JsonSerializer.Serialize(domainEvent, domainEvent.GetType(), Json),
        OccurredAt = domainEvent.OccurredAt
    };

    public static OutboxMessage Command<T>(string routingKey, T command, DateTimeOffset now) => new()
    {
        Type = typeof(T).Name,
        Exchange = MessagingTopology.CommandsExchange,
        RoutingKey = routingKey,
        Payload = JsonSerializer.Serialize(command, Json),
        OccurredAt = now
    };
}
