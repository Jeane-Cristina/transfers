using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Transfers.Application.Transfers;

namespace Transfers.Infrastructure.BackgroundJobs;

public sealed class ScheduledTransferDispatcherJob(
    IServiceScopeFactory scopes,
    ILogger<ScheduledTransferDispatcherJob> logger) : BackgroundService
{
    private const int BatchSize = 50;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(5));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                int dispatched;
                do
                {
                    await using var scope = scopes.CreateAsyncScope();
                    var dispatcher = scope.ServiceProvider.GetRequiredService<ScheduledTransferDispatcher>();
                    dispatched = await dispatcher.DispatchDueAsync(BatchSize, stoppingToken);

                    if (dispatched > 0)
                        logger.LogInformation("{Count} agendamento(s) enviados para execução", dispatched);
                }
                while (dispatched == BatchSize);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                logger.LogError(ex, "Falha no ciclo do dispatcher");
            }
        }
    }
}
