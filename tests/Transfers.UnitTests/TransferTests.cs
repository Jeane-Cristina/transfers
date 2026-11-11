using System.Globalization;
using Transfers.Domain.Common;
using Transfers.Domain.Transfers;

namespace Transfers.UnitTests;

public class TransferTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-11-10T15:00:00Z");
    private static readonly Guid Source = Guid.NewGuid();
    private static readonly Guid Destination = Guid.NewGuid();

    [Fact]
    public void Agendar_no_passado_lanca_excecao()
    {
        var ex = Assert.Throws<DomainException>(
            () => Transfer.Schedule(Source, Destination, 100m, Now.AddMinutes(-1), Now));

        Assert.Equal("transfer.schedule_not_in_future", ex.Code);
    }

    [Fact]
    public void Origem_igual_ao_destino_lanca_excecao()
    {
        var ex = Assert.Throws<DomainException>(
            () => Transfer.CreateImmediate(Source, Source, 100m, Now));

        Assert.Equal("transfer.same_account", ex.Code);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("10.001")]
    public void Valor_invalido_lanca_excecao(string amount)
    {
        var value = decimal.Parse(amount, CultureInfo.InvariantCulture);

        var ex = Assert.Throws<DomainException>(
            () => Transfer.CreateImmediate(Source, Destination, value, Now));

        Assert.Equal("transfer.invalid_amount", ex.Code);
    }

    [Fact]
    public void Agendamento_nasce_agendado_e_gera_evento()
    {
        var transfer = Transfer.Schedule(Source, Destination, 100m, Now.AddHours(1), Now);

        Assert.Equal(TransferStatus.Scheduled, transfer.Status);
        Assert.IsType<TransferScheduled>(Assert.Single(transfer.DomainEvents));
    }

    [Fact]
    public void Cancelar_agendamento_muda_status_e_gera_evento()
    {
        var transfer = Transfer.Schedule(Source, Destination, 100m, Now.AddHours(1), Now);
        transfer.ClearDomainEvents();

        transfer.Cancel(Now);

        Assert.Equal(TransferStatus.Cancelled, transfer.Status);
        Assert.IsType<TransferCancelled>(Assert.Single(transfer.DomainEvents));
    }

    [Fact]
    public void Nao_cancela_o_que_nao_esta_agendado()
    {
        var transfer = Transfer.CreateImmediate(Source, Destination, 100m, Now);

        var ex = Assert.Throws<DomainException>(() => transfer.Cancel(Now));

        Assert.Equal("transfer.not_cancellable", ex.Code);
    }

    [Fact]
    public void Nao_cancela_agendamento_que_ja_entrou_em_processamento()
    {
        var transfer = Transfer.Schedule(Source, Destination, 100m, Now.AddHours(1), Now);
        transfer.StartProcessing();

        Assert.Throws<DomainException>(() => transfer.Cancel(Now));
    }

    [Fact]
    public void Imediata_nao_pode_entrar_em_processamento_de_novo()
    {
        var transfer = Transfer.CreateImmediate(Source, Destination, 100m, Now);

        var ex = Assert.Throws<DomainException>(() => transfer.StartProcessing());

        Assert.Equal("transfer.invalid_transition", ex.Code);
    }

    [Fact]
    public void Concluir_gera_exatamente_um_evento_de_conclusao()
    {
        var transfer = Transfer.CreateImmediate(Source, Destination, 100m, Now);

        transfer.Complete(Now);

        Assert.Equal(TransferStatus.Completed, transfer.Status);
        Assert.Equal(Now, transfer.ProcessedAt);
        Assert.IsType<TransferCompleted>(Assert.Single(transfer.DomainEvents));
    }

    [Fact]
    public void Falhar_registra_motivo_e_evento()
    {
        var transfer = Transfer.CreateImmediate(Source, Destination, 100m, Now);

        transfer.Fail(FailureReason.InsufficientFunds, Now);

        Assert.Equal(TransferStatus.Failed, transfer.Status);
        Assert.Equal(FailureReason.InsufficientFunds, transfer.FailureReason);
        var failed = Assert.IsType<TransferFailed>(Assert.Single(transfer.DomainEvents));
        Assert.Equal(FailureReason.InsufficientFunds, failed.Reason);
    }

    [Fact]
    public void Concluida_nao_pode_falhar_depois()
    {
        var transfer = Transfer.CreateImmediate(Source, Destination, 100m, Now);
        transfer.Complete(Now);

        Assert.Throws<DomainException>(() => transfer.Fail(FailureReason.InsufficientFunds, Now));
    }
}
