using System;
using OverrideLauncher.Core.Modules.Classes.Account;
using OverrideLauncher.Core.Modules.Classes.Download;
using OverrideLauncher.Core.Modules.Classes.Launch;
using OverrideLauncher.Core.Modules.Classes.Launch.Client;
using OverrideLauncher.Core.Modules.Classes.Version;
using OverrideLauncher.Core.Modules.Entry.AccountEntry;
using OverrideLauncher.Core.Modules.Entry.DownloadEntry;
using OverrideLauncher.Core.Modules.Entry.GameEntry;
using OverrideLauncher.Core.Modules.Entry.JavaEntry;
using OverrideLauncher.Core.Modules.Entry.LaunchEntry;
using OverrideLauncher.Core.Modules.Entry.LaunchEntry.ServerEntry;
using OverrideLauncher.Core.Modules.Entry.ServerEntry;

var version = "1.12.2";
var installversion = version;

#region 安装服务端

InstallServer installServer = new InstallServer(DownloadVersionHelper.TryingFindVersion(installversion).Result);
installServer.ProgressCallback = (string logs, double progress) => { Console.WriteLine(logs + "  " + progress); };
installServer.Install(new ServerInstancesInfo()
{
    InstallPath = installversion
}).Wait();

#endregion

#region 启动服务端

ServerRunner serverRunner = new ServerRunner(new ServerRunnerInfo()
{
    IsNoWindow = true,
    JavaInfo = new JavaInfo()
    {
        JavaPath = @"D:\MCLDownload\ext\jre-v64-220420\jdk17\bin\java.exe",
        Version = "17.0.2",
        Is64Bit = true
    },
    ServerInfo = new ServerInstancesInfo()
    {
        InstallPath = installversion
    }
});
serverRunner.LogsOutput = (string logs) => { Console.WriteLine(logs); };
serverRunner.Start();

#endregion

#region 安装游戏

/*InstallClient ins = new InstallClient(DownloadVersionHelper.TryingFindVersion(installversion).Result, version);
ins.ProgressCallback = (string logs, double progress) => { Console.WriteLine(logs+"   "+progress); };
ins.DownloadThreadsCount = 512;
ins.Install(@"D:/.minecraft").Wait();*/

#endregion
#region 读取游戏

/*var ver = new VersionParse(new ClientInstancesInfo()
{
    GameCatalog = @"D:/.minecraft",
    GameName = version
});*/

#endregion
#region 补全文件

/*FileIntegrityChecker fileIntegrityChecker = new FileIntegrityChecker(ver);
GameFileCompleter fileCompleter = new GameFileCompleter();
fileCompleter.ProgressCallback = (string logs, double progress) => { Console.WriteLine(logs + "  " + progress); };
fileCompleter.DownloadMissingFilesAsync(fileIntegrityChecker.GetMissingFiles()).Wait();*/

#endregion
#region 启动

/*ClientRunner Runner = new ClientRunner(new ClientRunnerInfo()
{
    GameInstances = ver,
    JavaInfo = new JavaInfo()
    {
        JavaPath = @"D:\MCLDownload\ext\jre-v64-220420\jdk17\bin\java.exe",
        Version = "17.0.2",
        Is64Bit = true
    },
    Account = new OffineAuthenticator("MinecraftYJQ_").Authenticator(),
    LauncherInfo = "RMCL",
    LauncherVersion = "114"
});
Runner.LogsOutput = (string logs) => { Console.WriteLine(logs); };
Runner.Start();*/

#endregion