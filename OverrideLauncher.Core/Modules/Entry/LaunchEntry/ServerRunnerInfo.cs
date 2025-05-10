using System.Security.AccessControl;
using OverrideLauncher.Core.Modules.Classes.Version;
using OverrideLauncher.Core.Modules.Entry.GameEntry;
using OverrideLauncher.Core.Modules.Entry.JavaEntry;
using OverrideLauncher.Core.Modules.Entry.LaunchEntry.ServerEntry;
using OverrideLauncher.Core.Modules.Entry.ServerEntry;

namespace OverrideLauncher.Core.Modules.Entry.LaunchEntry;

public class ServerRunnerInfo
{
    public ServerInstancesInfo ServerInfo { get; set; }
    public JavaInfo JavaInfo { get; set; }
    public bool IsNoWindow { get; set; } = true;
    public List<string> JvmArgs { get; set; } = new();
}