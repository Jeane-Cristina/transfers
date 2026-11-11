using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Transfers.Infrastructure.Messaging;
using Transfers.Infrastructure.Persistence;

namespace Transfers.Infrastructure.BackgroundJobs;

public sealed class OutboxPublisherJob(
    IServiceScopeFactory scopes,
    RabbitMqPublisher publisher,
    TimeProvider clock,
    ILogger<OutboxPublisherJob> logger) : BackgroundService
{
    private const int BatchSize = 100;
    private const int MaxAttempts = 10;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(2));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                while (await PublishBatchAsync(stoppingToken) == BatchSize) { }
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                logger.LogError(ex, "Falha no ciclo do publisher do outbox");
            }
        }
    }

    private async Task<int> PublishBatchAsync(CancellationToken ct)
    {
        await using var scope = scopes.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<TransfersDbContext>();
        await using var transaction = await db.Database.BeginTransactionAsync(ct);

        var batch = await db.OutboxMessages
            .FromSql($"""
                SELECT * FROM outbox_messages
                WHERE published_at IS NULL AND attempts < {MaxAttempts}
                ORDER BY occurred_at
                LIMIT {BatchSize}
                FOR UPDATE SKIP LOCKED
                """)
            .ToListAsync(ct);

        foreach (var message in batch)
        {
            try
            {
                await publisher.PublishAsync(message, ct);
                message.PublishedAt = clock.GetUtcNow();
            }
            catch (Exception ex) when (!ct.IsCancellationRequested)
            {
                message.Attempts++;
                message.LastError = ex.Message.Length > 500 ? ex.Message[..500] : ex.Message;
                logger.LogWarning(ex, "Falha ao publicar {MessageId} (tentativa {Attempts})", message.Id, message.Attempts);
            }
        }

        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return batch.Count;
    }
}
