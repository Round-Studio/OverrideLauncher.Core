﻿using System.Security.AccessControl;
using OverrideLauncher.Core.Modules.Classes.Version;
using OverrideLauncher.Core.Modules.Entry.GameEntry;
using OverrideLauncher.Core.Modules.Entry.JavaEntry;
using OverrideLauncher.Core.Modules.Entry.LaunchEntry.ServerEntry;
using OverrideLauncher.Core.Modules.Enum.Launch;

namespace OverrideLauncher.Core.Modules.Entry.LaunchEntry;

public class ClientRunnerInfo
{
    public VersionParse GameInstances { get; set; }
    public JavaInfo JavaInfo { get; set; }
    public AccountEntry.AccountEntry Account { get; set; }
    public string LauncherInfo { get; set; }
    public string LauncherVersion { get; set; }
    public QuicklyServerInfo QuicklyQuicklyServer { get; set; }
    public GameWindowInfo WindowInfo { get; set; } = ClientWindowSizeEnum.Default;
    public bool IsDemo { get; set; } = false;
    public List<string> JvmArgs { get; set; } = new();
}