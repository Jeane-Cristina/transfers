using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Npgsql;
using Transfers.Application.Abstractions;
using Transfers.Application.Common;
using Transfers.Domain.Accounts;
using Transfers.Domain.Common;
using Transfers.Infrastructure.Persistence;

namespace Transfers.Infrastructure.Identity;

public sealed class NewAccountOptions
{
    public decimal InitialBalance { get; init; }
    public decimal OverdraftLimit { get; init; } = 500m;
}

public sealed class AccountRegistrationService(
    TransfersDbContext db,
    IPasswordHasher<AccountCredential> hasher,
    IOptions<NewAccountOptions> options) : IAccountRegistrationService
{
    public async Task<Account> RegisterAsync(string holderName, string document, string password, CancellationToken ct)
    {
        var name = string.Join(' ', (holderName ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries));
        var digits = Cpf.Normalize(document);
        password ??= "";

        if (name.Length < 3 || !name.Contains(' '))
            throw new DomainException("register.invalid_name", "Informe nome e sobrenome.");
        if (!Cpf.IsValid(digits))
            throw new DomainException("register.invalid_document", "CPF inválido. Confira os números digitados.");
        if (password.Length < 8 || !password.Any(char.IsLetter) || !password.Any(char.IsDigit))
            throw new DomainException("register.weak_password", "A senha precisa ter ao menos 8 caracteres, com letras e números.");
        if (await db.Accounts.AnyAsync(a => a.HolderDocument == digits, ct))
            throw new ConflictException("register.document_taken", "Já existe uma conta com este CPF.");

        var settings = options.Value;
        var account = Account.Open(name, digits, settings.InitialBalance, settings.OverdraftLimit);
        var credential = new AccountCredential(account.Id);
        credential.SetPasswordHash(hasher.HashPassword(credential, password));

        db.Accounts.Add(account);
        db.AccountCredentials.Add(credential);

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException e) when (e.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            throw new ConflictException("register.document_taken", "Já existe uma conta com este CPF.");
        }

        return account;
    }
}
