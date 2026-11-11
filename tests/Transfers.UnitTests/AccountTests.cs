using Transfers.Domain.Accounts;
using Transfers.Domain.Common;

namespace Transfers.UnitTests;

public class AccountTests
{
    [Fact]
    public void Pode_transferir_ate_saldo_mais_cheque_especial()
    {
        var account = TestAccounts.With(balance: 500m, overdraft: 1_000m);

        Assert.True(account.CanDebit(1_500m));
        Assert.False(account.CanDebit(1_500.01m));
    }

    [Fact]
    public void Debito_pode_deixar_saldo_negativo_dentro_do_cheque_especial()
    {
        var account = TestAccounts.With(balance: 500m, overdraft: 1_000m);

        account.Debit(1_000m);

        Assert.Equal(-500m, account.Balance);
        Assert.Equal(500m, account.OverdraftInUse);
    }

    [Fact]
    public void Debito_acima_do_cheque_especial_lanca_excecao()
    {
        var account = TestAccounts.With(balance: 500m, overdraft: 1_000m);

        var ex = Assert.Throws<DomainException>(() => account.Debit(1_500.01m));

        Assert.Equal("account.insufficient_funds", ex.Code);
        Assert.Equal(500m, account.Balance);
    }

    [Fact]
    public void Credito_com_saldo_negativo_cobre_primeiro_o_cheque_especial()
    {
        var account = TestAccounts.With(balance: -800m, overdraft: 1_000m);

        account.Credit(1_000m);

        Assert.Equal(200m, account.Balance);
        Assert.Equal(0m, account.OverdraftInUse);
    }

    [Fact]
    public void Conta_bloqueada_nao_recebe_credito()
    {
        var account = TestAccounts.With(balance: 0m, status: AccountStatus.Blocked);

        var ex = Assert.Throws<DomainException>(() => account.Credit(10m));

        Assert.Equal("account.inactive", ex.Code);
    }

    [Fact]
    public void Nao_abre_conta_com_saldo_alem_do_cheque_especial()
    {
        var ex = Assert.Throws<DomainException>(
            () => Account.Open("Titular", "00000000000", -1_000.01m, 1_000m));

        Assert.Equal("account.invalid_balance", ex.Code);
    }
}
