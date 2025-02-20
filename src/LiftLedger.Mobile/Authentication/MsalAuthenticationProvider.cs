using System.Diagnostics;
using LiftLedger.App;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Client;
using Microsoft.IdentityModel.Abstractions;

namespace LiftLedger.Mobile.Authentication;

public class MsalAuthenticationProvider: IAuthenticationProvider
{
    private string[] _scopes;
    private readonly IPublicClientApplication _publicClientApplication;

    public MsalAuthenticationProvider(IIdentityLogger identityLogger, IOptions<AzureAdConfig> azureAdConfigOptions, IOptions<DownStreamApiConfig> downstreamApiConfigOptions)
    {
        var azureAdConfig = azureAdConfigOptions.Value;
        _scopes = downstreamApiConfigOptions.Value.ScopesArray;

        _publicClientApplication = PublicClientApplicationBuilder.Create(azureAdConfig.ClientId)
            .WithExperimentalFeatures()
            .WithLogging(identityLogger, enablePiiLogging: false) 
            .WithAuthority(azureAdConfig.Authority)
            .WithRedirectUri($"msal{azureAdConfig.ClientId}://auth")
            .Build();
        // .WithLogging(new IdentityLogger(EventLogLevel.Warning), enablePiiLogging: false)    // This is the currently recommended way to log MSAL message. For more info refer to https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/wiki/logging. Set Identity Logging level to Warning which is a middle ground
    }

    public async Task SignOutUser()
    {
        var existingUser = await FetchSignedInUserFromCache().ConfigureAwait(false);
        await this.SignOutUser(existingUser).ConfigureAwait(false);
    }
    
    public async Task<string?> SignInUser()
    {
        var existingUser = await FetchSignedInUserFromCache().ConfigureAwait(false);
        AuthenticationResult? authResult = null;

        try
        {
            // 1. Try to sign-in the previously signed-in account
            if (existingUser != null)
            {
                authResult = await _publicClientApplication
                    .AcquireTokenSilent(_scopes, existingUser)
                    .ExecuteAsync()
                    .ConfigureAwait(false);
            }
            else
            {
                authResult = await SignInUserInteractivelyAsync(_scopes);
            }
        }
        catch (MsalUiRequiredException ex)
        {
            // A MsalUiRequiredException happened on AcquireTokenSilentAsync. This indicates you need to call AcquireTokenInteractive to acquire a token interactively
            Debug.WriteLine($"MsalUiRequiredException: {ex.Message}");

            authResult = await _publicClientApplication
                .AcquireTokenInteractive(_scopes)
                .ExecuteAsync()
                .ConfigureAwait(false);
        }
        catch (MsalException msalEx)
        {
            Debug.WriteLine($"Error Acquiring Token interactively:{Environment.NewLine}{msalEx}");
        }

        return authResult?.AccessToken;
    }

    private async Task<AuthenticationResult> SignInUserInteractivelyAsync(string[] scopes,
        IAccount existingAccount = null)
    {
        if (!_publicClientApplication.IsUserInteractive())
            throw new NotSupportedException("Interactive sign-in not supported.");

        return await _publicClientApplication.AcquireTokenInteractive(scopes)
            .WithParentActivityOrWindow(Platform.CurrentActivity)
            .ExecuteAsync()
            .ConfigureAwait(false);
    }

    public async Task SignOutUser(IAccount user)
    {
        await _publicClientApplication.RemoveAsync(user).ConfigureAwait(false);
    }

    public async Task<IAccount?> FetchSignedInUserFromCache()
    {
        // get accounts from cache
        IEnumerable<IAccount?> accounts = await _publicClientApplication.GetAccountsAsync();

        // Error corner case: we should always have 0 or 1 accounts, not expecting > 1
        // This is just an example of how to resolve this ambiguity, which can arise if more apps share a token cache.
        // Note that some apps prefer to use a random account from the cache.
        if (accounts.Count() > 1)
        {
            foreach (var acc in accounts)
            {
                await _publicClientApplication.RemoveAsync(acc);
            }

            return null;
        }

        return accounts.SingleOrDefault();
    }
}