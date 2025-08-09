using OverrideLauncher.Core.Base.Entry.Download.Install.Client;

namespace OverrideLauncher.Core.Base.Entry.Info;

public class ClientRunnerInfo
{
    public ClientRootInfo ClientRootInfo { get; set; }
    public Account.Account Account { get; set; }
    public string LauncherInfo { get; set; } = "OverrideLauncher.Core";
    public string LauncherVersion { get; set; }
    public bool IsDemo { get; set; } = false;
}