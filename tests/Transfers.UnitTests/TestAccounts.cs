using Transfers.Domain.Accounts;

namespace Transfers.UnitTests;

internal static class TestAccounts
{
    public static Account With(decimal balance, decimal overdraft = 0m, AccountStatus status = AccountStatus.Active)
        => Account.Open("Titular", "00000000000", balance, overdraft, TransferLimits.Default, status);
}
