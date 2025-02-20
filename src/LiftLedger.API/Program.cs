using Azure.Monitor.OpenTelemetry.AspNetCore;
using LiftLedger.API.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Web;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"));

builder.Services.AddAuthorization();

builder.Services.AddGrpc();
builder.Services.AddOpenTelemetry()
    .UseAzureMonitor();

using var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapGrpcService<GreeterService>()
    .RequireAuthorization();

app.MapGet("/", ([FromServices]ILogger<GreeterService> logger) =>
{
    using (logger.BeginScope("FOOOOO"))
    {
        logger.LogInformation("Accessing GRPC service via https. Use GRPC client");
    }

    return "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909";
});

app.Run();