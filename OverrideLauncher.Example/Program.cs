using System;
using OverrideLauncher.Core.Modules.Classes.Account;
using OverrideLauncher.Core.Modules.Classes.Download;
using OverrideLauncher.Core.Modules.Classes.Launch;
using OverrideLauncher.Core.Modules.Classes.Version;
using OverrideLauncher.Core.Modules.Entry.AccountEntry;
using OverrideLauncher.Core.Modules.Entry.DownloadEntry;
using OverrideLauncher.Core.Modules.Entry.GameEntry;
using OverrideLauncher.Core.Modules.Entry.JavaEntry;
using OverrideLauncher.Core.Modules.Entry.LaunchEntry;

var version = "绿色版红石生电优化";
var installversion = "1.21.1";
#region 安装游戏

/*InstallGame ins = new InstallGame(InstallGame.TryingFindVersion(installversion).Result, version);
ins.ProgressCallback = (string logs, double progress) => { Console.WriteLine(logs+"   "+progress); };
ins.DownloadThreadsCount = 512;
ins.Install(@"D:/.minecraft").Wait();*/

#endregion
#region 读取游戏

var ver = new VersionParse(new GameInstancesInfo()
{
    GameCatalog = @"D:/.minecraft",
    GameName = version
});

#endregion
#region 补全文件

FileIntegrityChecker fileIntegrityChecker = new FileIntegrityChecker(ver);
GameFileCompleter fileCompleter = new GameFileCompleter();
fileCompleter.ProgressCallback = (string logs, double progress) => { Console.WriteLine(logs + "  " + progress); };
fileCompleter.DownloadMissingFilesAsync(fileIntegrityChecker.GetMissingFiles()).Wait();

#endregion
#region 启动

LaunchRunner Runner = new LaunchRunner(new LaunchRunnerInfo()
{
    GameInstances = ver,
    JavaInfo = new JavaInfo()
    {
        JavaPath = @"D:\MCLDownload\ext\jre-v64-220420\jdk17\bin\java.exe",
        Version = "17.0.2",
        Is64Bit = true
    },
    Account = new OfflineAccountEntry("MinecraftYJQ"),
    LauncherInfo = "RMCL",
    LauncherVersion = "114",
});
Runner.LogsOutput = (string logs) => { Console.WriteLine(logs); };
Runner.Start();

#endregion