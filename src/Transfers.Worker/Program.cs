using Transfers.Application;
using Transfers.Infrastructure;
using Transfers.Infrastructure.BackgroundJobs;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddApplication(builder.Configuration);
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddMessaging(builder.Configuration);

builder.Services.AddHostedService<ScheduledTransferDispatcherJob>();
builder.Services.AddHostedService<OutboxPublisherJob>();
builder.Services.AddHostedService<ScheduledTransferConsumer>();

builder.Build().Run();
