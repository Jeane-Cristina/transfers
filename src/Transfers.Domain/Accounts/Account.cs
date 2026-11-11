using Transfers.Domain.Common;

namespace Transfers.Domain.Accounts;

public sealed class Account : AggregateRoot
{
    public string HolderName { get; private set; } = default!;
    public string HolderDocument { get; private set; } = default!;
    public decimal Balance { get; private set; }
    public decimal OverdraftLimit { get; private set; }
    public AccountStatus Status { get; private set; }
    public TransferLimits Limits { get; private set; } = default!;

    private Account() { }

    public static Account Open(
        string holderName,
        string holderDocument,
        decimal initialBalance,
        decimal overdraftLimit,
        TransferLimits? limits = null,
        AccountStatus status = AccountStatus.Active)
    {
        if (string.IsNullOrWhiteSpace(holderName))
            throw new DomainException("account.holder_required", "O titular é obrigatório.");
        if (overdraftLimit < 0)
            throw new DomainException("account.invalid_overdraft", "O cheque especial não pode ser negativo.");
        if (initialBalance < -overdraftLimit)
            throw new DomainException("account.invalid_balance", "O saldo inicial ultrapassa o cheque especial.");

        return new Account
        {
            Id = Guid.CreateVersion7(),
            HolderName = holderName.Trim(),
            HolderDocument = holderDocument.Trim(),
            Balance = initialBalance,
            OverdraftLimit = overdraftLimit,
            Status = status,
            Limits = limits ?? TransferLimits.Default
        };
    }

    public bool IsActive => Status == AccountStatus.Active;

    public decimal AvailableToTransfer => Balance + OverdraftLimit;

    public decimal OverdraftInUse => Balance < 0 ? -Balance : 0m;

    public bool CanDebit(decimal amount) => amount <= AvailableToTransfer;

    public void Debit(decimal amount)
    {
        EnsurePositive(amount);
        EnsureActive();
        if (!CanDebit(amount))
            throw new DomainException("account.insufficient_funds", "Saldo e cheque especial insuficientes.");

        Balance -= amount;
    }

    public void Credit(decimal amount)
    {
        EnsurePositive(amount);
        EnsureActive();
        Balance += amount;
    }

    public void Block() => Status = AccountStatus.Blocked;

    private static void EnsurePositive(decimal amount)
    {
        if (amount <= 0)
            throw new DomainException("account.invalid_amount", "O valor deve ser maior que zero.");
    }

    private void EnsureActive()
    {
        if (!IsActive)
            throw new DomainException("account.inactive", "Conta bloqueada ou inativa.");
    }
}
