namespace OverrideLauncher.Core.Modules.Entry.AccountEntry;

public class OfflineAccountEntry : AccountEntry
{
    public OfflineAccountEntry(string UserName)
    {
        this.AccountType = "msa";
        this.UserName = UserName;
        this.UUID = Guid.NewGuid().ToString();
        this.Token = Guid.NewGuid().ToString();
    }
}