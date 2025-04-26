using System;
using OverrideLauncher.Core.Modules.Classes.Account;
using OverrideLauncher.Core.Modules.Classes.Download;
using OverrideLauncher.Core.Modules.Classes.Launch;
using OverrideLauncher.Core.Modules.Entry.AccountEntry;
using OverrideLauncher.Core.Modules.Entry.DownloadEntry;
using OverrideLauncher.Core.Modules.Entry.GameEntry;
using OverrideLauncher.Core.Modules.Entry.JavaEntry;
using OverrideLauncher.Core.Modules.Entry.LaunchEntry;


InstallGame ins = new InstallGame(new GameVersion()
{
    Id = "1.18.2",
    Url = "https://piston-meta.mojang.com/v1/packages/334b33fcba3c9be4b7514624c965256535bd7eba/1.18.2.json"
});
ins.ProgressCallback = (string logs, double progress) => { Console.WriteLine(logs+"   "+progress); };
ins.DownloadThreadsCount = 512;
ins.Install(@".minecraft").Wait();

var ver = new GameInstancesInfo()
{
    GameCatalog = @".minecraft",
    GameName = "1.18.2"
};
void outlog(string logs, double progress) => Console.WriteLine(logs + "  " + progress);

FileIntegrityChecker fileIntegrityChecker = new FileIntegrityChecker(ver);
GameFileCompleter fileCompleter = new GameFileCompleter();
fileCompleter.ProgressCallback = outlog;
fileCompleter.DownloadMissingFilesAsync(fileIntegrityChecker.GetMissingFiles()).Wait();

var mic = new MicrosoftAuthenticator("c06d4d68-7751-4a8a-a2ff-d1b46688f428");
LaunchRunner Runner = new LaunchRunner(new LaunchRunnerInfo()
{
    GameInstances = ver,
    JavaInfo = new JavaInfo()
    {
        JavaPath = @"C:\Program Files\Java\jdk-19\bin\java.exe",
        Version = "17.0.2",
        Is64Bit = true
    },
    Account = mic.Authenticator().Result,
    LauncherInfo = "RMCL",
    LauncherVersion = "114",
});
Runner.LogsOutput = (string logs) => { Console.WriteLine(logs); };
Runner.Start();