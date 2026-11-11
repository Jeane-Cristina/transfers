using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Transfers.Application.Transfers;
using Transfers.Infrastructure.Messaging;

namespace Transfers.Infrastructure.BackgroundJobs;

public sealed class ScheduledTransferConsumer(
    RabbitMqConnection connection,
    IServiceScopeFactory scopes,
    ILogger<ScheduledTransferConsumer> logger) : BackgroundService
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private IChannel? _channel;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var conn = await connection.GetAsync(stoppingToken);
        _channel = await conn.CreateChannelAsync(cancellationToken: stoppingToken);
        await RabbitMqTopology.DeclareAsync(_channel, stoppingToken);
        await _channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 10, global: false, stoppingToken);

        var channel = _channel;
        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (_, delivery) =>
        {
            try
            {
                var command = JsonSerializer.Deserialize<ExecuteScheduledTransfer>(delivery.Body.Span, Json)
                    ?? throw new InvalidOperationException("Mensagem vazia.");

                await using var scope = scopes.CreateAsyncScope();
                var executor = scope.ServiceProvider.GetRequiredService<TransferExecutor>();
                await executor.ExecuteScheduledAsync(command.TransferId, stoppingToken);

                await channel.BasicAckAsync(delivery.DeliveryTag, multiple: false);
                logger.LogInformation("Agendamento {TransferId} executado", command.TransferId);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Falha ao executar agendamento {MessageId}", delivery.BasicProperties.MessageId);
                await channel.BasicNackAsync(delivery.DeliveryTag, multiple: false, requeue: true);
            }
        };

        await channel.BasicConsumeAsync(MessagingTopology.ExecuteScheduledQueue, autoAck: false, consumer, stoppingToken);
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_channel is { IsOpen: true })
            await _channel.CloseAsync(cancellationToken);
        await base.StopAsync(cancellationToken);
    }
}
