using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Transfers.Application.Abstractions;
using Transfers.Infrastructure.Identity;
using Transfers.Infrastructure.Messaging;
using Transfers.Infrastructure.Outbox;
using Transfers.Infrastructure.Persistence;
using Transfers.Infrastructure.Persistence.Interceptors;
using Transfers.Infrastructure.Persistence.Repositories;

namespace Transfers.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<DomainEventsToOutboxInterceptor>();

        services.AddDbContext<TransfersDbContext>((sp, options) => options
            .UseNpgsql(configuration.GetConnectionString("Postgres"))
            .UseSnakeCaseNamingConvention()
            .AddInterceptors(sp.GetRequiredService<DomainEventsToOutboxInterceptor>()));

        services.AddScoped<IAccountRepository, AccountRepository>();
        services.AddScoped<ITransferRepository, TransferRepository>();
        services.AddScoped<IUnitOfWork, EfUnitOfWork>();
        services.AddScoped<ICommandOutbox, EfCommandOutbox>();
        services.AddScoped<IPasswordHasher<AccountCredential>, PasswordHasher<AccountCredential>>();
        services.AddScoped<IAuthenticationService, PasswordAuthenticationService>();
        services.Configure<NewAccountOptions>(configuration.GetSection("NewAccounts"));
        services.AddScoped<IAccountRegistrationService, AccountRegistrationService>();

        return services;
    }

    public static IServiceCollection AddMessaging(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<RabbitMqOptions>(configuration.GetSection("RabbitMq"));
        services.AddSingleton<RabbitMqConnection>();
        services.AddSingleton<RabbitMqPublisher>();
        return services;
    }
}
