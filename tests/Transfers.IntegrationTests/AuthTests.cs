using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Transfers.Domain.Accounts;
using Transfers.Infrastructure.Identity;
using Transfers.Infrastructure.Persistence;

namespace Transfers.IntegrationTests;

public sealed class AuthTests(ApiFactory factory) : IntegrationTest(factory)
{
    [Fact]
    public async Task Sem_token_retorna_401()
    {
        var source = await CreateAccountAsync(1_000m);
        var destination = await CreateAccountAsync(0m);

        var response = await SendAsync(HttpMethod.Post, "/api/transfers", asAccount: null,
            new { sourceAccountId = source.Id, destinationAccountId = destination.Id, amount = 10m });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(1_000m, await BalanceAsync(source.Id));
    }

    [Fact]
    public async Task Nao_transfere_a_partir_de_conta_alheia()
    {
        var intruder = await CreateAccountAsync(0m);
        var victim = await CreateAccountAsync(1_000m);

        var response = await SendAsync(HttpMethod.Post, "/api/transfers", intruder.Id,
            new { sourceAccountId = victim.Id, destinationAccountId = intruder.Id, amount = 500m });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(1_000m, await BalanceAsync(victim.Id));
    }

    [Fact]
    public async Task Nao_consulta_conta_alheia()
    {
        var me = await CreateAccountAsync(0m);
        var other = await CreateAccountAsync(1_000m);

        var response = await SendAsync(HttpMethod.Get, $"/api/accounts/{other.Id}", me.Id);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Destino_consegue_ver_transferencia_recebida_e_terceiro_nao()
    {
        var source = await CreateAccountAsync(1_000m);
        var destination = await CreateAccountAsync(0m);
        var stranger = await CreateAccountAsync(0m);
        var transfer = await ReadTransferAsync(await TransferAsync(source.Id, destination.Id, 100m));

        var asDestination = await SendAsync(HttpMethod.Get, $"/api/transfers/{transfer.Id}", destination.Id);
        var asStranger = await SendAsync(HttpMethod.Get, $"/api/transfers/{transfer.Id}", stranger.Id);

        Assert.Equal(HttpStatusCode.OK, asDestination.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, asStranger.StatusCode);
    }

    [Fact]
    public async Task Destino_nao_pode_cancelar_agendamento_de_outra_pessoa()
    {
        var source = await CreateAccountAsync(1_000m);
        var destination = await CreateAccountAsync(0m);
        var scheduled = await ReadTransferAsync(
            await ScheduleAsync(source.Id, destination.Id, 100m, Factory.Clock.GetUtcNow().AddMinutes(10)));

        var response = await CancelAsync(scheduled.Id, destination.Id);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Login_com_senha_correta_retorna_token_valido()
    {
        var (account, document) = await CreateAccountWithPasswordAsync("Senha@123");

        var login = await Client.PostAsJsonAsync("/api/auth/login", new { document, password = "Senha@123" });
        login.EnsureSuccessStatusCode();
        using var body = JsonDocument.Parse(await login.Content.ReadAsStringAsync());
        var token = body.RootElement.GetProperty("token").GetString();

        var me = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me");
        me.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        var meResponse = await Client.SendAsync(me);

        Assert.Equal(HttpStatusCode.OK, meResponse.StatusCode);
        using var meBody = JsonDocument.Parse(await meResponse.Content.ReadAsStringAsync());
        Assert.Equal(account.Id, meBody.RootElement.GetProperty("id").GetGuid());
    }

    [Fact]
    public async Task Login_com_senha_errada_retorna_401()
    {
        var (_, document) = await CreateAccountWithPasswordAsync("Senha@123");

        var login = await Client.PostAsJsonAsync("/api/auth/login", new { document, password = "errada" });

        Assert.Equal(HttpStatusCode.Unauthorized, login.StatusCode);
    }

    [Fact]
    public async Task Cadastro_cria_conta_e_permite_entrar()
    {
        var document = NewValidCpf();

        var register = await Client.PostAsJsonAsync("/api/auth/register",
            new { holderName = "Maria Teste", document, password = "segura123" });
        var login = await Client.PostAsJsonAsync("/api/auth/login", new { document, password = "segura123" });

        Assert.Equal(HttpStatusCode.Created, register.StatusCode);
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        using var body = JsonDocument.Parse(await register.Content.ReadAsStringAsync());
        Assert.Equal(500m, body.RootElement.GetProperty("account").GetProperty("overdraftLimit").GetDecimal());
    }

    [Fact]
    public async Task Cadastro_com_cpf_ja_usado_retorna_409()
    {
        var document = NewValidCpf();
        await Client.PostAsJsonAsync("/api/auth/register", new { holderName = "Maria Teste", document, password = "segura123" });

        var again = await Client.PostAsJsonAsync("/api/auth/register",
            new { holderName = "Outra Pessoa", document, password = "segura123" });

        Assert.Equal(HttpStatusCode.Conflict, again.StatusCode);
    }

    [Theory]
    [InlineData("12345678900", "segura123")]
    [InlineData("11111111111", "segura123")]
    [InlineData(null, "curta1")]
    public async Task Cadastro_invalido_retorna_400(string? document, string password)
    {
        var response = await Client.PostAsJsonAsync("/api/auth/register",
            new { holderName = "Maria Teste", document = document ?? NewValidCpf(), password });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private static string NewValidCpf()
    {
        var digits = string.Concat(Enumerable.Range(0, 9).Select(_ => Random.Shared.Next(0, 10)));
        digits += Transfers.Application.Common.Cpf.CheckDigit(digits, 9);
        digits += Transfers.Application.Common.Cpf.CheckDigit(digits, 10);
        return digits;
    }

    private async Task<(Account Account, string Document)> CreateAccountWithPasswordAsync(string password)
    {
        var account = await CreateAccountAsync(100m);

        await using var scope = Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<TransfersDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<AccountCredential>>();

        var credential = new AccountCredential(account.Id);
        credential.SetPasswordHash(hasher.HashPassword(credential, password));
        db.AccountCredentials.Add(credential);
        await db.SaveChangesAsync();

        return (account, account.HolderDocument);
    }
}
