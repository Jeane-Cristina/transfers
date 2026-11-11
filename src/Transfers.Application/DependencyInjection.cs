using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Transfers.Application.Common;
using Transfers.Application.Transfers;
using Transfers.Domain.Rules;

namespace Transfers.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<TransferRulesOptions>(configuration.GetSection("TransferRules"));

        services.AddSingleton(sp =>
        {
            var options = sp.GetRequiredService<IOptions<TransferRulesOptions>>().Value;
            return new TransferPeriodResolver(
                TimeZoneInfo.FindSystemTimeZoneById(options.TimeZoneId),
                TimeOnly.Parse(options.NightStart),
                TimeOnly.Parse(options.NightEnd));
        });

        services.AddScoped<IdempotencyGuard>();
        services.AddScoped<TransferExecutor>();
        services.AddScoped<ScheduleTransferHandler>();
        services.AddScoped<CancelTransferHandler>();
        services.AddScoped<ScheduledTransferDispatcher>();
        return services;
    }
}
