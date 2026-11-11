namespace Transfers.Domain.Transfers;

public enum FailureReason
{
    InsufficientFunds = 1,
    AccountInactive = 2,
    HourlyAmountExceeded = 3,
    HourlyAttemptsExceeded = 4
}
