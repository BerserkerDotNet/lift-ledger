using OpenTelemetry.Resources;

namespace LiftLedger.Mobile;

public class LiftledgerResourceDetector : IResourceDetector{
    public OpenTelemetry.Resources.Resource Detect()
    {
        return new OpenTelemetry.Resources.Resource(new Dictionary<string, object>
        {
            { "service.name", "LiftLedger App" },
            { "service.version", AppInfo.Version.ToString()},
            { "os.type", Environment.OSVersion.Platform.ToString() },
            { "os.version", Environment.OSVersion.VersionString }
        });
    }
}