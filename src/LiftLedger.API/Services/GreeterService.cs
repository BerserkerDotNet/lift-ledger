using Grpc.Core;
using LiftLedger.API;
using Microsoft.Identity.Web;

namespace LiftLedger.API.Services;

public class GreeterService : Greeter.GreeterBase
{
    private readonly ILogger<GreeterService> _logger;
    public GreeterService(ILogger<GreeterService> logger)
    {
        _logger = logger;
    }

    public override Task<HelloReply> SayHello(HelloRequest request, ServerCallContext context)
    {
        return Task.FromResult(new HelloReply
        {
            Message = $"Hello {context.GetHttpContext().User.GetDisplayName()}"
        });
    }
}
