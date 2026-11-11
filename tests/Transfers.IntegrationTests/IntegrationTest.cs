using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Transfers.Api.Auth;
using Transfers.Application.Transfers;
using Transfers.Domain.Accounts;
using Transfers.Infrastructure.Persistence;

namespace Transfers.IntegrationTests;

[Collection(nameof(IntegrationCollection))]
public abstract class IntegrationTest(ApiFactory factory)
{
    protected static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    protected static readonly TransferLimits Generous = new(1_000_000m, 1_000, 1_000_000m, 1_000);

    protected ApiFactory Factory { get; } = StartAtDaytime(factory);
    protected HttpClient Client { get; } = factory.CreateClient();

    private static ApiFactory StartAtDaytime(ApiFactory factory)
    {
        factory.Clock.AdvanceToLocal(new TimeOnly(10, 0));
        return factory;
    }

    protected async Task<Account> CreateAccountAsync(
        decimal balance, decimal overdraft = 0m, TransferLimits? limits = null,
        AccountStatus status = AccountStatus.Active)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<TransfersDbContext>();

        var document = Random.Shared.NextInt64(10_000_000_000, 99_999_999_999).ToString();
        var account = Account.Open("Conta de teste", document, balance, overdraft, limits, status);

        db.Accounts.Add(account);
        await db.SaveChangesAsync();
        return account;
    }

    protected async Task<decimal> BalanceAsync(Guid accountId)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<TransfersDbContext>();
        return await db.Accounts.AsNoTracking()
            .Where(a => a.Id == accountId)
            .Select(a => a.Balance)
            .SingleAsync();
    }

    protected string TokenFor(Guid accountId)
    {
        var tokens = Factory.Services.GetRequiredService<JwtTokenService>();
        return tokens.Issue(accountId, "Conta de teste").Token;
    }

    protected Task<HttpResponseMessage> TransferAsync(Guid from, Guid to, decimal amount, string? key = null)
        => SendAsync(HttpMethod.Post, "/api/transfers", from,
            new { sourceAccountId = from, destinationAccountId = to, amount }, key);

    protected Task<HttpResponseMessage> ScheduleAsync(Guid from, Guid to, decimal amount, DateTimeOffset when)
        => SendAsync(HttpMethod.Post, "/api/transfers/scheduled", from,
            new { sourceAccountId = from, destinationAccountId = to, amount, scheduledFor = when });

    protected Task<HttpResponseMessage> CancelAsync(Guid transferId, Guid asAccount)
        => SendAsync(HttpMethod.Post, $"/api/transfers/{transferId}/cancel", asAccount);

    protected async Task<TransferResponse> GetTransferAsync(Guid transferId, Guid asAccount)
    {
        var response = await SendAsync(HttpMethod.Get, $"/api/transfers/{transferId}", asAccount);
        response.EnsureSuccessStatusCode();
        return await ReadTransferAsync(response);
    }

    protected Task<HttpResponseMessage> SendAsync(
        HttpMethod method, string url, Guid? asAccount, object? body = null, string? key = null)
    {
        var request = new HttpRequestMessage(method, url);
        if (body is not null)
            request.Content = JsonContent.Create(body);
        if (asAccount is { } accountId)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", TokenFor(accountId));
        if (key is not null)
            request.Headers.Add("Idempotency-Key", key);
        return Client.SendAsync(request);
    }

    protected static async Task<string?> FailureReasonAsync(HttpResponseMessage response)
    {
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return doc.RootElement.TryGetProperty("failureReason", out var reason) ? reason.GetString() : null;
    }

    protected static async Task<TransferResponse> ReadTransferAsync(HttpResponseMessage response)
        => (await response.Content.ReadFromJsonAsync<TransferResponse>(Json))!;
}
