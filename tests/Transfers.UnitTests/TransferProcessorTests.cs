using Transfers.Domain.Accounts;
using Transfers.Domain.Rules;
using Transfers.Domain.Transfers;

namespace Transfers.UnitTests;

public class TransferProcessorTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-11-10T15:00:00Z");
    private static readonly HourlyUsage NoUsage = new(0, 0m);

    private static (Transfer Transfer, Account Source, Account Destination) Arrange(
        decimal amount, decimal balance, decimal overdraft = 0m)
    {
        var source = TestAccounts.With(balance, overdraft);
        var destination = TestAccounts.With(0m);
        var transfer = Transfer.CreateImmediate(source.Id, destination.Id, amount, Now);
        return (transfer, source, destination);
    }

    [Fact]
    public void Conclui_com_saldo_suficiente()
    {
        var (transfer, source, destination) = Arrange(amount: 300m, balance: 500m);

        TransferProcessor.Process(transfer, source, destination, NoUsage, TransferPeriod.Day, Now);

        Assert.Equal(TransferStatus.Completed, transfer.Status);
        Assert.Equal(200m, source.Balance);
        Assert.Equal(300m, destination.Balance);
    }

    [Fact]
    public void Conclui_usando_cheque_especial()
    {
        var (transfer, source, _) = Arrange(amount: 1_000m, balance: 500m, overdraft: 1_000m);

        TransferProcessor.Process(transfer, source, TestAccounts.With(0m), NoUsage, TransferPeriod.Day, Now);

        Assert.Equal(TransferStatus.Completed, transfer.Status);
        Assert.Equal(-500m, source.Balance);
    }

    [Fact]
    public void Falha_quando_ultrapassa_o_cheque_especial()
    {
        var (transfer, source, destination) = Arrange(amount: 1_600m, balance: 500m, overdraft: 1_000m);

        TransferProcessor.Process(transfer, source, destination, NoUsage, TransferPeriod.Day, Now);

        Assert.Equal(TransferStatus.Failed, transfer.Status);
        Assert.Equal(FailureReason.InsufficientFunds, transfer.FailureReason);
        Assert.Equal(500m, source.Balance);
        Assert.Equal(0m, destination.Balance);
    }

    [Fact]
    public void Falha_quando_excede_valor_da_hora_de_dia()
    {
        var (transfer, source, destination) = Arrange(amount: 2_000.01m, balance: 20_000m);
        var usage = new HourlyUsage(Attempts: 1, CompletedAmount: 3_000m);

        TransferProcessor.Process(transfer, source, destination, usage, TransferPeriod.Day, Now);

        Assert.Equal(FailureReason.HourlyAmountExceeded, transfer.FailureReason);
    }

    [Fact]
    public void Falha_quando_excede_valor_da_hora_no_periodo_noturno()
    {
        var (transfer, source, destination) = Arrange(amount: 300m, balance: 5_000m);
        var usage = new HourlyUsage(Attempts: 1, CompletedAmount: 800m);

        TransferProcessor.Process(transfer, source, destination, usage, TransferPeriod.Night, Now);

        Assert.Equal(FailureReason.HourlyAmountExceeded, transfer.FailureReason);
    }

    [Fact]
    public void Mesmo_valor_passa_de_dia_e_falha_de_noite()
    {
        var (dayTransfer, daySource, dayDestination) = Arrange(amount: 1_500m, balance: 5_000m);
        var (nightTransfer, nightSource, nightDestination) = Arrange(amount: 1_500m, balance: 5_000m);

        TransferProcessor.Process(dayTransfer, daySource, dayDestination, NoUsage, TransferPeriod.Day, Now);
        TransferProcessor.Process(nightTransfer, nightSource, nightDestination, NoUsage, TransferPeriod.Night, Now);

        Assert.Equal(TransferStatus.Completed, dayTransfer.Status);
        Assert.Equal(FailureReason.HourlyAmountExceeded, nightTransfer.FailureReason);
    }

    [Fact]
    public void Falha_quando_esgota_tentativas_mesmo_com_saldo()
    {
        var (transfer, source, destination) = Arrange(amount: 10m, balance: 5_000m);
        var usage = new HourlyUsage(Attempts: 5, CompletedAmount: 0m);

        TransferProcessor.Process(transfer, source, destination, usage, TransferPeriod.Day, Now);

        Assert.Equal(FailureReason.HourlyAttemptsExceeded, transfer.FailureReason);
    }

    [Fact]
    public void Quinta_tentativa_ainda_e_permitida()
    {
        var (transfer, source, destination) = Arrange(amount: 10m, balance: 5_000m);
        var usage = new HourlyUsage(Attempts: 4, CompletedAmount: 0m);

        TransferProcessor.Process(transfer, source, destination, usage, TransferPeriod.Day, Now);

        Assert.Equal(TransferStatus.Completed, transfer.Status);
    }

    [Fact]
    public void Falha_quando_destino_esta_bloqueado()
    {
        var source = TestAccounts.With(1_000m);
        var destination = TestAccounts.With(0m, status: AccountStatus.Blocked);
        var transfer = Transfer.CreateImmediate(source.Id, destination.Id, 100m, Now);

        TransferProcessor.Process(transfer, source, destination, NoUsage, TransferPeriod.Day, Now);

        Assert.Equal(FailureReason.AccountInactive, transfer.FailureReason);
        Assert.Equal(1_000m, source.Balance);
    }
}
