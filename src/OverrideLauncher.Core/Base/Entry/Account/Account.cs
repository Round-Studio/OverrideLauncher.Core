using OverrideLauncher.Core.Base.Entry.Account.Skin;
using OverrideLauncher.Core.Base.Enum.Account;

namespace OverrideLauncher.Core.Base.Entry.Account;

public class Account
{
    public string UserName { get; set; }
    public string UUID { get; set; }
    public string RefreshToken { get; set; }
    public string Token { get; set; }
    public DateTime LoginTime { get; set; }
    public AccountType AccountType { get; set; }
    public SkinInfo SkinData { get; set; } = new();
}