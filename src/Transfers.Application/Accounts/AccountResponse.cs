using Transfers.Domain.Accounts;

namespace Transfers.Application.Accounts;

public sealed record AccountResponse(
    Guid Id,
    string HolderName,
    decimal Balance,
    decimal OverdraftLimit,
    decimal OverdraftInUse,
    decimal AvailableToTransfer,
    AccountStatus Status,
    TransferLimits Limits)
{
    public static AccountResponse From(Account a) => new(
        a.Id, a.HolderName, a.Balance, a.OverdraftLimit, a.OverdraftInUse,
        a.AvailableToTransfer, a.Status, a.Limits);
}
