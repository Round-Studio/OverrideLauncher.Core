using OverrideLauncher.Core.Base.Entry.Account;

namespace OverrideLauncher.Core.Interface;

public interface ILogin
{
    Account Authenticate();
}