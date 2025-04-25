using OverrideLauncher.Core.Modules.Classes.Launch;
using OverrideLauncher.Core.Modules.Entry.GameEntry;
using OverrideLauncher.Core.Modules.Entry.JavaEntry;
using OverrideLauncher.Core.Modules.Entry.LaunchEntry;

LaunchRunner Runner = new LaunchRunner(new LaunchRunnerInfo()
{
    GameInstances = new GameInstancesInfo()
    {
        GameCatalog = @"D:\Games\.minecraft",
        GameName = "25w14craftmine"
    },
    JavaInfo = new JavaInfo()
    {
        JavaPath = @"C:\Program Files\Java\jdk-22\bin\java.exe",
        Version = "17.0.2",
        Is64Bit = true
    },
    LauncherInfo = "RMCL",
    LauncherVersion = "114"
});