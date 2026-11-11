using System.Net;

namespace Transfers.IntegrationTests;

public sealed class ConcurrencyTests(ApiFactory factory) : IntegrationTest(factory)
{
    [Fact]
    public async Task Transferencias_simultaneas_nao_gastam_o_mesmo_saldo()
    {
        var source = await CreateAccountAsync(500m, overdraft: 1_000m, limits: Generous);
        var destination = await CreateAccountAsync(0m);

        var responses = await Task.WhenAll(
            Enumerable.Range(0, 10).Select(_ => TransferAsync(source.Id, destination.Id, 400m)));

        Assert.Equal(3, responses.Count(r => r.StatusCode == HttpStatusCode.Created));
        Assert.Equal(7, responses.Count(r => r.StatusCode == HttpStatusCode.UnprocessableEntity));
        Assert.Equal(-700m, await BalanceAsync(source.Id));
        Assert.Equal(1_200m, await BalanceAsync(destination.Id));
    }

    [Fact]
    public async Task Transferencias_cruzadas_simultaneas_nao_causam_deadlock()
    {
        var a = await CreateAccountAsync(10_000m, limits: Generous);
        var b = await CreateAccountAsync(10_000m, limits: Generous);

        var responses = await Task.WhenAll(Enumerable.Range(0, 20).Select(i =>
            i % 2 == 0 ? TransferAsync(a.Id, b.Id, 10m) : TransferAsync(b.Id, a.Id, 10m)));

        Assert.All(responses, r => Assert.Equal(HttpStatusCode.Created, r.StatusCode));
        Assert.Equal(10_000m, await BalanceAsync(a.Id));
        Assert.Equal(10_000m, await BalanceAsync(b.Id));
    }
}
