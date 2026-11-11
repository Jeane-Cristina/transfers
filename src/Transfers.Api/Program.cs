using System.Text.Json.Serialization;
using Transfers.Api.Endpoints;
using Transfers.Api.Errors;
using Transfers.Application;
using Transfers.Infrastructure;
using Transfers.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication(builder.Configuration);
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddOpenApi();
builder.Services.ConfigureHttpJsonOptions(o =>
    o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p
    .WithOrigins(builder.Configuration["Cors:WebOrigin"] ?? "http://localhost:5173")
    .AllowAnyHeader()
    .AllowAnyMethod()));

var app = builder.Build();

if (app.Configuration.GetValue<bool>("Database:ApplyMigrationsOnStartup"))
    await DatabaseInitializer.InitializeAsync(app.Services);

app.UseExceptionHandler();
app.UseCors();

app.MapOpenApi();
app.UseSwaggerUI(o => o.SwaggerEndpoint("/openapi/v1.json", "Transfers API v1"));

app.MapAccountEndpoints();
app.MapTransferEndpoints();

app.Run();

public partial class Program;
