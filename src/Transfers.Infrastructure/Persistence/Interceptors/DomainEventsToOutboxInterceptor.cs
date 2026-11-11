using Microsoft.EntityFrameworkCore.Diagnostics;
using Transfers.Domain.Common;
using Transfers.Infrastructure.Outbox;

namespace Transfers.Infrastructure.Persistence.Interceptors;

public sealed class DomainEventsToOutboxInterceptor : SaveChangesInterceptor
{
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is TransfersDbContext db)
            StageDomainEvents(db);

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        if (eventData.Context is TransfersDbContext db)
            StageDomainEvents(db);

        return base.SavingChanges(eventData, result);
    }

    private static void StageDomainEvents(TransfersDbContext db)
    {
        var aggregates = db.ChangeTracker
            .Entries<AggregateRoot>()
            .Select(entry => entry.Entity)
            .Where(aggregate => aggregate.DomainEvents.Count > 0)
            .ToList();

        foreach (var aggregate in aggregates)
        {
            foreach (var domainEvent in aggregate.DomainEvents)
                db.OutboxMessages.Add(OutboxMessage.FromDomainEvent(domainEvent));

            aggregate.ClearDomainEvents();
        }
    }
}
