using Microsoft.Extensions.Logging;

namespace LiftLedger.Mobile;

public partial class MainPage : ContentPage, IDisposable
{
    private readonly ILogger<MainPage> _logger;
    private readonly IDisposable? _loggerScope;
    int count = 0;

    public MainPage(ILogger<MainPage> logger)
    {
        _logger = logger;
        InitializeComponent();

        _loggerScope = _logger.BeginScope("Main");
    }

    private void OnCounterClicked(object sender, EventArgs e)
    {
        count++;

        if (count == 1)
            CounterBtn.Text = $"Clicked {count} time";
        else
            CounterBtn.Text = $"Clicked {count} times";
        
        _logger.LogInformation("Counter: {Count}", count);

        SemanticScreenReader.Announce(CounterBtn.Text);
    }

    public void Dispose()
    {
        _loggerScope?.Dispose();
    }
}