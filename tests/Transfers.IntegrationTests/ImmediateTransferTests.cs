using System.Net;
using Transfers.Domain.Accounts;

namespace Transfers.IntegrationTests;

public sealed class ImmediateTransferTests(ApiFactory factory) : IntegrationTest(factory)
{
    [Fact]
    public async Task Transferencia_com_saldo_suficiente()
    {
        var source = await CreateAccountAsync(1_000m);
        var destination = await CreateAccountAsync(0m);

        var response = await TransferAsync(source.Id, destination.Id, 300m);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(700m, await BalanceAsync(source.Id));
        Assert.Equal(300m, await BalanceAsync(destination.Id));
    }

    [Fact]
    public async Task Transferencia_utilizando_cheque_especial()
    {
        var source = await CreateAccountAsync(500m, overdraft: 1_000m);
        var destination = await CreateAccountAsync(0m);

        var response = await TransferAsync(source.Id, destination.Id, 1_000m);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(-500m, await BalanceAsync(source.Id));
    }

    [Fact]
    public async Task Transferencia_que_ultrapassa_o_cheque_especial()
    {
        var source = await CreateAccountAsync(500m, overdraft: 1_000m);
        var destination = await CreateAccountAsync(0m);

        var response = await TransferAsync(source.Id, destination.Id, 1_500.01m);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Equal("InsufficientFunds", await FailureReasonAsync(response));
        Assert.Equal(500m, await BalanceAsync(source.Id));
    }

    [Fact]
    public async Task Recebimento_enquanto_a_conta_utiliza_cheque_especial()
    {
        var payer = await CreateAccountAsync(5_000m);
        var receiver = await CreateAccountAsync(-800m, overdraft: 1_000m);

        var response = await TransferAsync(payer.Id, receiver.Id, 1_000m);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(200m, await BalanceAsync(receiver.Id));
    }

    [Fact]
    public async Task Transferencia_acima_do_limite_da_hora()
    {
        var source = await CreateAccountAsync(20_000m);
        var destination = await CreateAccountAsync(0m);

        var first = await TransferAsync(source.Id, destination.Id, 3_000m);
        var second = await TransferAsync(source.Id, destination.Id, 2_000.01m);

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, second.StatusCode);
        Assert.Equal("HourlyAmountExceeded", await FailureReasonAsync(second));
    }

    [Fact]
    public async Task Excesso_de_tentativas_conta_inclusive_as_rejeitadas()
    {
        var source = await CreateAccountAsync(100m);
        var destination = await CreateAccountAsync(0m);

        await TransferAsync(source.Id, destination.Id, 500m);
        await TransferAsync(source.Id, destination.Id, 500m);
        for (var i = 0; i < 3; i++)
            Assert.Equal(HttpStatusCode.Created, (await TransferAsync(source.Id, destination.Id, 10m)).StatusCode);

        var sixth = await TransferAsync(source.Id, destination.Id, 10m);

        Assert.Equal("HourlyAttemptsExceeded", await FailureReasonAsync(sixth));
    }

    [Fact]
    public async Task Transferencia_durante_o_periodo_noturno()
    {
        Factory.Clock.AdvanceToLocal(new TimeOnly(21, 0));
        var source = await CreateAccountAsync(10_000m);
        var destination = await CreateAccountAsync(0m);

        var atLimit = await TransferAsync(source.Id, destination.Id, 1_000m);
        var overLimit = await TransferAsync(source.Id, destination.Id, 0.01m);

        Assert.Equal(HttpStatusCode.Created, atLimit.StatusCode);
        Assert.Equal("HourlyAmountExceeded", await FailureReasonAsync(overLimit));
    }

    [Fact]
    public async Task Janela_de_uma_hora_libera_o_limite_depois_de_60_minutos()
    {
        var source = await CreateAccountAsync(20_000m);
        var destination = await CreateAccountAsync(0m);

        await TransferAsync(source.Id, destination.Id, 5_000m);
        Factory.Clock.Advance(TimeSpan.FromMinutes(61));
        var afterWindow = await TransferAsync(source.Id, destination.Id, 5_000m);

        Assert.Equal(HttpStatusCode.Created, afterWindow.StatusCode);
    }

    [Fact]
    public async Task Conta_bloqueada_nao_recebe()
    {
        var source = await CreateAccountAsync(1_000m);
        var blocked = await CreateAccountAsync(0m, status: AccountStatus.Blocked);

        var response = await TransferAsync(source.Id, blocked.Id, 10m);

        Assert.Equal("AccountInactive", await FailureReasonAsync(response));
        Assert.Equal(1_000m, await BalanceAsync(source.Id));
    }

    [Fact]
    public async Task Origem_igual_ao_destino_retorna_400()
    {
        var source = await CreateAccountAsync(1_000m);

        var response = await TransferAsync(source.Id, source.Id, 10m);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Conta_inexistente_retorna_404()
    {
        var source = await CreateAccountAsync(1_000m);

        var response = await TransferAsync(source.Id, Guid.NewGuid(), 10m);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
