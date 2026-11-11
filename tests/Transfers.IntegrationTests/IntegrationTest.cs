using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
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

    protected Task<HttpResponseMessage> TransferAsync(Guid from, Guid to, decimal amount, string? key = null)
        => PostAsync("/api/transfers", new { sourceAccountId = from, destinationAccountId = to, amount }, key);

    protected Task<HttpResponseMessage> ScheduleAsync(Guid from, Guid to, decimal amount, DateTimeOffset when)
        => PostAsync("/api/transfers/scheduled",
            new { sourceAccountId = from, destinationAccountId = to, amount, scheduledFor = when }, key: null);

    protected static async Task<string?> FailureReasonAsync(HttpResponseMessage response)
    {
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return doc.RootElement.TryGetProperty("failureReason", out var reason) ? reason.GetString() : null;
    }

    protected static async Task<TransferResponse> ReadTransferAsync(HttpResponseMessage response)
        => (await response.Content.ReadFromJsonAsync<TransferResponse>(Json))!;

    private Task<HttpResponseMessage> PostAsync(string url, object body, string? key)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = JsonContent.Create(body) };
        if (key is not null)
            request.Headers.Add("Idempotency-Key", key);
        return Client.SendAsync(request);
    }
}
