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

var version = "1.12.2";
var installversion = version;
var GameCatalog = ".minecraft";
#region 安装游戏

if (!Directory.Exists($"{GameCatalog}/versions/{installversion}"))
{
    InstallGame ins = new InstallGame(InstallGame.TryingFindVersion(installversion).Result, version);
    ins.ProgressCallback = (string logs, double progress) => { Console.WriteLine(logs + "   " + progress); };
    ins.DownloadThreadsCount = 512;
    ins.Install(@".minecraft").Wait();
}

#endregion
#region 读取游戏

var ver = new VersionParse(new GameInstancesInfo()
{
    GameCatalog = GameCatalog,
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
        JavaPath = "C:\\Program Files\\Java\\jdk-22\\bin\\java.exe",
        Version = "17.0.2",
        Is64Bit = true
    },
    Account = new OffineAuthenticator("MinecraftYJQ_").Authenticator(),
    LauncherInfo = "RMCL",
    LauncherVersion = "114",
});
Runner.LogsOutput = (string logs) => { Console.WriteLine(logs); };
Runner.Start();

#endregion