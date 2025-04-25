using System.Security.AccessControl;
using OverrideLauncher.Core.Modules.Entry.GameEntry;
using OverrideLauncher.Core.Modules.Entry.JavaEntry;

namespace OverrideLauncher.Core.Modules.Entry.LaunchEntry;

public class LaunchRunnerInfo
{
    public GameInstancesInfo GameInstances { get; set; }
    public JavaInfo JavaInfo { get; set; }
    public string LauncherInfo { get; set; }
    public string LauncherVersion { get; set; }
}