using System.Security.Cryptography;
using System.Text;
using OverrideLauncher.Core.Modules.Entry.AccountEntry;

namespace OverrideLauncher.Core.Modules.Classes.Account;

public class OffineAuthenticator
{
    private AccountEntry accountEntry = new();
    public OffineAuthenticator(string username)
    {
        accountEntry.UserName = username;
        accountEntry.UUID = NameToMcOfflineUUID(username).ToString();
        accountEntry.Token = NameToMcOfflineUUID(username).ToString();
        accountEntry.AccountType = "off";
    }

    public AccountEntry Authenticator()
    {
        return accountEntry;
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