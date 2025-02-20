using System.Reflection;
using Microsoft.Extensions.Configuration;

namespace LiftLedger.Mobile.Extensions;

public static class MAUIBuilderExtensions
{
    public static void AddConfig(this ConfigurationManager manager)
    {
        var a = Assembly.GetExecutingAssembly();
        using var stream = a.GetManifestResourceStream("LiftLedger.Mobile.appsettings.json");

        var config = new ConfigurationBuilder()
            .AddJsonStream(stream)
            .Build();
        manager.AddConfiguration(config);
    }
}