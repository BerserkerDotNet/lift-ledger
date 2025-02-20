using System.Net.Security;
using Grpc.Core;
using Grpc.Net.Client;
using LiftLedger.API;
using LiftLedger.Mobile.Authentication;

namespace LiftLedger.Mobile.Client;

public sealed class DummyAPIClient
{
    private readonly MsalAuthenticationProvider _authProvider;

    public DummyAPIClient(MsalAuthenticationProvider authProvider)
    {
        _authProvider = authProvider;
    }

    public async Task<string> GetHelloFromApi()
    {
        var token = await _authProvider.SignInUser();

        var headers = new Metadata();
        headers.Add("Authorization", $"Bearer {token}");
        var handler = new SocketsHttpHandler();
        handler.SslOptions = new SslClientAuthenticationOptions
        {
            RemoteCertificateValidationCallback = (sender, certificate, chain, errors) => true
        };
        
        var channel = GrpcChannel.ForAddress("https://10.0.2.2:7284", new GrpcChannelOptions { HttpHandler = handler });
        var client = new Greeter.GreeterClient(channel);
        var reply = await client.SayHelloAsync(new HelloRequest() { Name = "Foo" }, headers);

        return reply.Message;
    }
}