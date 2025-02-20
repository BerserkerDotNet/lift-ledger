namespace LiftLedger.Mobile.Authentication;

public class AzureAdConfig
{
    public string? Authority { get; set; }

    public string? Instance { get; set; }
    public string? ClientId { get; set; }

    public string? TenantId { get; set; }

    public string? AndroidRedirectUri { get; set; }
}