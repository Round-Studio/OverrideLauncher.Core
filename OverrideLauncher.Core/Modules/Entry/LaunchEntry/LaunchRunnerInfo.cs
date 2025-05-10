using System.Security.AccessControl;
using OverrideLauncher.Core.Modules.Classes.Version;
using OverrideLauncher.Core.Modules.Entry.GameEntry;
using OverrideLauncher.Core.Modules.Entry.JavaEntry;
using OverrideLauncher.Core.Modules.Entry.LaunchEntry.ServerEntry;

namespace OverrideLauncher.Core.Modules.Entry.LaunchEntry;

public class LaunchRunnerInfo
{
    public VersionParse GameInstances { get; set; }
    public JavaInfo JavaInfo { get; set; }
    public AccountEntry.AccountEntry Account { get; set; }
    public string LauncherInfo { get; set; }
    public string LauncherVersion { get; set; }
    public ServerInfo QuicklyServer { get; set; }
    public GameWindowInfo WindowInfo { get; set; } = new GameWindowInfo();
    public bool IsDemo { get; set; } = false;
    public List<string> JvmArgs { get; set; } = new();
}