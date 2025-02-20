namespace LiftLedger.App;

public interface IAuthenticationProvider
{
    Task<string?> SignInUser();

    Task SignOutUser();
}