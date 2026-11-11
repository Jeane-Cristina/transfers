namespace Transfers.Infrastructure.Messaging;

public static class MessagingTopology
{
    public const string EventsExchange = "transfers.events";
    public const string CommandsExchange = "transfers.commands";
    public const string ExecuteScheduledQueue = "transfers.execute-scheduled";
    public const string ExecuteScheduledRoutingKey = "transfer.execute";
    public const string DeadLetterExchange = "transfers.dlx";
    public const string DeadLetterQueue = "transfers.dead-letter";
}
