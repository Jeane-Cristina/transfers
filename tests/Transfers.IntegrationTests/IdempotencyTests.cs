using System.Net;

namespace Transfers.IntegrationTests;

public sealed class IdempotencyTests(ApiFactory factory) : IntegrationTest(factory)
{
    [Fact]
    public async Task Mesma_chave_e_mesmo_corpo_debita_uma_vez()
    {
        var source = await CreateAccountAsync(1_000m);
        var destination = await CreateAccountAsync(0m);
        var key = Guid.NewGuid().ToString();

        var responses = await Task.WhenAll(
            TransferAsync(source.Id, destination.Id, 100m, key),
            TransferAsync(source.Id, destination.Id, 100m, key));

        var transfers = await Task.WhenAll(responses.Select(ReadTransferAsync));
        Assert.Equal(transfers[0].Id, transfers[1].Id);
        Assert.Equal(900m, await BalanceAsync(source.Id));
        Assert.Contains(responses, r => r.Headers.Contains("Idempotent-Replayed"));
    }

    [Fact]
    public async Task Mesma_chave_com_corpo_diferente_retorna_409()
    {
        var source = await CreateAccountAsync(1_000m);
        var destination = await CreateAccountAsync(0m);
        var key = Guid.NewGuid().ToString();

        await TransferAsync(source.Id, destination.Id, 100m, key);
        var reused = await TransferAsync(source.Id, destination.Id, 200m, key);

        Assert.Equal(HttpStatusCode.Conflict, reused.StatusCode);
        Assert.Equal(900m, await BalanceAsync(source.Id));
    }

    [Fact]
    public async Task Repeticao_de_transferencia_recusada_devolve_a_mesma_recusa()
    {
        var source = await CreateAccountAsync(100m);
        var destination = await CreateAccountAsync(0m);
        var key = Guid.NewGuid().ToString();

        var first = await TransferAsync(source.Id, destination.Id, 500m, key);
        var second = await TransferAsync(source.Id, destination.Id, 500m, key);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, first.StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, second.StatusCode);
        Assert.True(second.Headers.Contains("Idempotent-Replayed"));
    }
}
