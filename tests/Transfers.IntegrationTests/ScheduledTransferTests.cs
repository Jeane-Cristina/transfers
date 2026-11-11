using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Transfers.Application.Transfers;
using Transfers.Domain.Accounts;
using Transfers.Domain.Transfers;

namespace Transfers.IntegrationTests;

public sealed class ScheduledTransferTests(ApiFactory factory) : IntegrationTest(factory)
{
    [Fact]
    public async Task Agendada_sem_saldo_no_momento_da_execucao_falha()
    {
        var source = await CreateAccountAsync(300m);
        var destination = await CreateAccountAsync(0m);

        var scheduled = await ReadTransferAsync(
            await ScheduleAsync(source.Id, destination.Id, 200m, Factory.Clock.GetUtcNow().AddMinutes(10)));
        Assert.Equal(TransferStatus.Scheduled, scheduled.Status);

        await TransferAsync(source.Id, destination.Id, 300m);

        Factory.Clock.Advance(TimeSpan.FromMinutes(11));
        await RunScheduledPipelineAsync(scheduled.Id);

        var final = await GetTransferAsync(scheduled.Id, source.Id);
        Assert.Equal(TransferStatus.Failed, final.Status);
        Assert.Equal(FailureReason.InsufficientFunds, final.FailureReason);
    }

    [Fact]
    public async Task Agendada_com_saldo_conclui_no_horario()
    {
        var source = await CreateAccountAsync(1_000m);
        var destination = await CreateAccountAsync(0m);

        var scheduled = await ReadTransferAsync(
            await ScheduleAsync(source.Id, destination.Id, 250m, Factory.Clock.GetUtcNow().AddMinutes(5)));

        Factory.Clock.Advance(TimeSpan.FromMinutes(6));
        await RunScheduledPipelineAsync(scheduled.Id);

        var final = await GetTransferAsync(scheduled.Id, source.Id);
        Assert.Equal(TransferStatus.Completed, final.Status);
        Assert.Equal(750m, await BalanceAsync(source.Id));
    }

    [Fact]
    public async Task Agendada_nao_executa_antes_do_horario()
    {
        var source = await CreateAccountAsync(1_000m);
        var destination = await CreateAccountAsync(0m);

        var scheduled = await ReadTransferAsync(
            await ScheduleAsync(source.Id, destination.Id, 100m, Factory.Clock.GetUtcNow().AddMinutes(30)));

        await RunScheduledPipelineAsync(scheduled.Id);

        var final = await GetTransferAsync(scheduled.Id, source.Id);
        Assert.Equal(TransferStatus.Scheduled, final.Status);
        Assert.Equal(1_000m, await BalanceAsync(source.Id));
    }

    [Fact]
    public async Task Agendamento_cancelado_nao_executa()
    {
        var source = await CreateAccountAsync(1_000m);
        var destination = await CreateAccountAsync(0m);
        var scheduled = await ReadTransferAsync(
            await ScheduleAsync(source.Id, destination.Id, 100m, Factory.Clock.GetUtcNow().AddMinutes(5)));

        var cancel = await CancelAsync(scheduled.Id, source.Id);
        var cancelAgain = await CancelAsync(scheduled.Id, source.Id);

        Factory.Clock.Advance(TimeSpan.FromMinutes(6));
        await RunScheduledPipelineAsync(scheduled.Id);

        Assert.Equal(HttpStatusCode.OK, cancel.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, cancelAgain.StatusCode);
        Assert.Equal(1_000m, await BalanceAsync(source.Id));
    }

    [Fact]
    public async Task Agendamento_no_passado_e_recusado()
    {
        var source = await CreateAccountAsync(1_000m);
        var destination = await CreateAccountAsync(0m);

        var response = await ScheduleAsync(source.Id, destination.Id, 100m, Factory.Clock.GetUtcNow().AddMinutes(-1));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Agendamento_com_conta_bloqueada_e_recusado()
    {
        var source = await CreateAccountAsync(1_000m);
        var blocked = await CreateAccountAsync(0m, status: AccountStatus.Blocked);

        var response = await ScheduleAsync(source.Id, blocked.Id, 100m, Factory.Clock.GetUtcNow().AddMinutes(5));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    private async Task RunScheduledPipelineAsync(Guid transferId)
    {
        await using (var scope = Factory.Services.CreateAsyncScope())
            await scope.ServiceProvider.GetRequiredService<ScheduledTransferDispatcher>().DispatchDueAsync(100, default);

        await using (var scope = Factory.Services.CreateAsyncScope())
            await scope.ServiceProvider.GetRequiredService<TransferExecutor>().ExecuteScheduledAsync(transferId, default);
    }
}
