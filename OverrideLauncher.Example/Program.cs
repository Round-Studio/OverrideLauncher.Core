using System;
using OverrideLauncher.Core.Modules.Classes.Download;
using OverrideLauncher.Core.Modules.Classes.Launch;
using OverrideLauncher.Core.Modules.Entry.AccountEntry;
using OverrideLauncher.Core.Modules.Entry.DownloadEntry;
using OverrideLauncher.Core.Modules.Entry.GameEntry;
using OverrideLauncher.Core.Modules.Entry.JavaEntry;
using OverrideLauncher.Core.Modules.Entry.LaunchEntry;

/*InstallGame ins = new InstallGame(new GameVersion()
{
    Id = "1.14.3",
    Url = "https://piston-meta.mojang.com/v1/packages/e21618620e02be5a14543d1d17ffdba941d09aa8/1.14.3.json"
});
ins.ProgressCallback = (string logs, double progress) => { Console.WriteLine(logs+"   "+progress); };
ins.DownloadThreadsCount = 512;
ins.Install(@".minecraft").Wait();*/
var ver = new GameInstancesInfo()
{
    GameCatalog = @".minecraft",
    GameName = "1.14.3"
};

FileIntegrityChecker fileIntegrityChecker = new FileIntegrityChecker(ver);
GameFileCompleter fileCompleter = new GameFileCompleter();
fileCompleter.ProgressCallback = (string logs, double progress) => { Console.WriteLine(logs + "  " + progress); };
fileCompleter.CompleteFilesAsync(fileIntegrityChecker.GetMissingFiles()).Wait();

LaunchRunner Runner = new LaunchRunner(new LaunchRunnerInfo()
{
    GameInstances = ver,
    JavaInfo = new JavaInfo()
    {
        JavaPath = @"C:\Program Files\Java\jdk-22\bin\java.exe",
        Version = "17.0.2",
        Is64Bit = true
    },
    Account = new OfflineAccountEntry("MinecraftYJQ"),
    LauncherInfo = "RMCL",
    LauncherVersion = "114",
});
Runner.LogsOutput = (string logs) => { Console.WriteLine(logs); };
Runner.Start();