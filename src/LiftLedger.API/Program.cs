using Azure.Monitor.OpenTelemetry.AspNetCore;
using LiftLedger.API.Services;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddGrpc();
builder.Services.AddOpenTelemetry()
    .UseAzureMonitor();

using var app = builder.Build();

// Configure the HTTP request pipeline.
app.MapGrpcService<GreeterService>();
app.MapGet("/", ([FromServices]ILogger<GreeterService> logger) =>
{
    using (logger.BeginScope("FOOOOO"))
    {
        logger.LogInformation("Accessing GRPC service via https. Use GRPC client");
    }

    return "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909";
});

app.Run();