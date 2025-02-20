namespace LiftLedger.Mobile.Authentication;

public class DownStreamApiConfig
{
    public string? Scopes { get; set; }

    public string[] ScopesArray => Scopes?.Split(' ') ?? [];
}