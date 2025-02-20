using Android.App;
using Android.Content;
using Microsoft.Identity.Client;

namespace LiftLedger.Mobile;

[Activity(Exported =true)]
[IntentFilter(new[] { Intent.ActionView },
    Categories = new[] { Intent.CategoryBrowsable, Intent.CategoryDefault },
    DataHost = "auth",
    DataScheme = "msal7430fb32-2709-4a17-94b4-11c8ac495916")]
public class MsalActivity : BrowserTabActivity
{
}