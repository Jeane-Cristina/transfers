using Transfers.Domain.Accounts;

namespace Transfers.Application.Accounts;

public sealed record AccountResponse(
    Guid Id,
    string HolderName,
    string MaskedDocument,
    decimal Balance,
    decimal OverdraftLimit,
    decimal OverdraftInUse,
    decimal AvailableToTransfer,
    AccountStatus Status,
    TransferLimits Limits)
{
    public static AccountResponse From(Account a) => new(
        a.Id, a.HolderName, Mask(a.HolderDocument), a.Balance, a.OverdraftLimit, a.OverdraftInUse,
        a.AvailableToTransfer, a.Status, a.Limits);

    private static string Mask(string document)
        => document.Length == 11 ? $"***.{document[3..6]}.{document[6..9]}-**" : "***";
}
