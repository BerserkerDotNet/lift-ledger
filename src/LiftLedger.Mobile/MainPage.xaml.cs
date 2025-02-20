using System.Net.Security;
using Grpc.Core;
using Grpc.Net.Client;
using LiftLedger.API;
using LiftLedger.Mobile.Authentication;
using LiftLedger.Mobile.Client;
using Microsoft.Extensions.Logging;

namespace LiftLedger.Mobile;

public partial class MainPage : ContentPage, IDisposable
{
    private readonly DummyAPIClient _client;
    private readonly ILogger<MainPage> _logger;
    private readonly IDisposable? _loggerScope;
    private readonly MsalAuthenticationProvider _authenticationProvider;

    public MainPage(DummyAPIClient client,  ILogger<MainPage> logger)
    {
        _client = client;
        _logger = logger;
        InitializeComponent();

        _loggerScope = _logger.BeginScope("Main");
    }

    private async void OnCounterClicked(object sender, EventArgs e)
    {
        await CallApi();

        SemanticScreenReader.Announce(CounterBtn.Text);
    }

    private async Task CallApi()
    {
        var message = await _client.GetHelloFromApi();
        _logger.LogInformation("Counter: {Count}", message);
        Dispatcher.Dispatch(() =>
        {
            CounterBtn.Text = $"Server response: {message}";
        });
    }

    public void Dispose()
    {
        _loggerScope?.Dispose();
    }
}