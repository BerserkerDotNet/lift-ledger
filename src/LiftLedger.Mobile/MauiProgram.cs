using System.Diagnostics;
using System.Runtime.CompilerServices;
using Azure.Monitor.OpenTelemetry.Exporter;
using LiftLedger.Mobile.Authentication;
using LiftLedger.Mobile.Client;
using LiftLedger.Mobile.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Abstractions;
using Microsoft.Maui.LifecycleEvents;
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace LiftLedger.Mobile;

public static class MauiProgram
{
    private static readonly ActivitySource _activitySource = new ActivitySource(
        "LiftLedger.Mobile",
        AppInfo.Version.ToString());
    
    private static Activity? _currentActivity;
    
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureLifecycleEvents(e =>
            {
                e.AddAndroid(a => a
                    .OnResume(OnStateChange)
                    .OnStop(OnStateChange)
                    .OnDestroy(OnDisposeActivity));
            })
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        builder.Configuration.AddConfig();
        builder.Services.Configure<AzureAdConfig>(builder.Configuration.GetSection("AzureAd"));
        builder.Services.Configure<DownStreamApiConfig>(builder.Configuration.GetSection("DownstreamApi"));

        Sdk.CreateTracerProviderBuilder()
            .SetResourceBuilder(ResourceBuilder.CreateEmpty()
                .AddDetector(new LiftledgerResourceDetector())
                .AddTelemetrySdk()
                .AddEnvironmentVariableDetector())
            .AddSource("LiftLedger.Mobile")
            .AddAzureMonitorTraceExporter(o => { o.ConnectionString = "InstrumentationKey=dd3e550f-feef-4031-8b22-2b129e41b9aa;IngestionEndpoint=https://centralus-2.in.applicationinsights.azure.com/;LiveEndpoint=https://centralus.livediagnostics.monitor.azure.com/;ApplicationId=287e9055-1086-40dc-94cc-725230d254fd"; o.SamplingRatio = 1.0F; })
            .AddGrpcClientInstrumentation(opt =>
            {
                opt.SuppressDownstreamInstrumentation = true;
            })
            .AddHttpClientInstrumentation()
            .Build();
        
        builder.Logging.AddOpenTelemetry(o =>
        {
            o.IncludeScopes = true;
            o.IncludeFormattedMessage = true;
            o.SetResourceBuilder(ResourceBuilder.CreateEmpty()
                .AddDetector(new LiftledgerResourceDetector())
                .AddTelemetrySdk()
                .AddEnvironmentVariableDetector());
            o.AddAzureMonitorLogExporter(e =>
            {
                e.SamplingRatio = 1.0F;
                e.ConnectionString =
                    "InstrumentationKey=dd3e550f-feef-4031-8b22-2b129e41b9aa;IngestionEndpoint=https://centralus-2.in.applicationinsights.azure.com/;LiveEndpoint=https://centralus.livediagnostics.monitor.azure.com/;ApplicationId=287e9055-1086-40dc-94cc-725230d254fd";
            });
        });

        builder.Services.AddSingleton<MsalAuthenticationProvider>();
        builder.Services.AddSingleton<DummyAPIClient>();
        builder.Services.AddSingleton<IIdentityLogger, OpenTelemetryIdentityLogger>();

#if DEBUG
        builder.Logging.AddDebug();
#endif
        return builder.Build();
    }

    private static void OnStateChange(Android.App.Activity activity)
    {
        _currentActivity?.Dispose();
        _currentActivity = _activitySource.StartActivity();
    }
    
    private static void OnDisposeActivity(Android.App.Activity activity)
    {
        _currentActivity?.Dispose();
    }
}