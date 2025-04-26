using OverrideLauncher.Core.Modules.Entry.AccountEntry;

namespace OverrideLauncher.Core.Modules.Classes.Account;

public class OffineAuthenticator
{
    private AccountEntry accountEntry = new();
    public OffineAuthenticator(string username)
    {
        accountEntry.UserName = username;
        accountEntry.UUID = Guid.NewGuid().ToString();
        accountEntry.Token = Guid.NewGuid().ToString();
    }

    public AccountEntry Authenticator()
    {
        return accountEntry;
    }
}