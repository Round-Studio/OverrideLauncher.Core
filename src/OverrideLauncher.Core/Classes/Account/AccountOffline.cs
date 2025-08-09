using System.Security.Cryptography;
using System.Text;
using OverrideLauncher.Core.Base.Enum.Account;
using OverrideLauncher.Core.Interface;

namespace OverrideLauncher.Core.Classes.Account;

public class AccountOffline : Base.Entry.Account.Account, ILogin
{
    public AccountOffline(string userName)
    {
        this.AccountType = AccountType.Offline;
        this.UserName = userName;
        this.LoginTime = DateTime.Now;
        this.RefreshToken = NameToMcOfflineUUID(userName).ToString();
        this.UUID = NameToMcOfflineUUID(userName).ToString();
        this.Token = NameToMcOfflineUUID(userName).ToString();
    }
    public Base.Entry.Account.Account Authenticate()
    {
        return this;
    }
    private static Guid NameToMcOfflineUUID(string name)
    {
        string input = "OfflinePlayer:" + name;

        using (MD5 md5 = MD5.Create())
        {
            byte[] inputBytes = Encoding.UTF8.GetBytes(input);
            byte[] hashBytes = md5.ComputeHash(inputBytes);

            hashBytes[6] = (byte)((hashBytes[6] & 0x0F) | 0x30);
            hashBytes[8] = (byte)((hashBytes[8] & 0x3F) | 0x80);

            return new Guid(hashBytes);
        }
    }
}