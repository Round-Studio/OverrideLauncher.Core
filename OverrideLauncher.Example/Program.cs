using OverrideLauncher.Core.Modules.Classes.Account;
using OverrideLauncher.Core.Modules.Classes.Download;
using OverrideLauncher.Core.Modules.Classes.Launch.Client;
using OverrideLauncher.Core.Modules.Classes.Version;
using OverrideLauncher.Core.Modules.Entry.GameEntry;
using OverrideLauncher.Core.Modules.Entry.JavaEntry;
using OverrideLauncher.Core.Modules.Entry.LaunchEntry;
using OverrideLauncher.Core.Modules.Enum.Download;

var installversion = "1.20.5";
var version = installversion;

/*InstallClient ins = new InstallClient(DownloadVersionHelper.TryingFindVersion(installversion).Result, version);
ins.ProgressCallback = (DownloadStateEnum state,string logs, double progress) => { Console.WriteLine(logs+"   "+progress); };
ins.DownloadThreadsCount = 512;
Console.WriteLine(ins.GetThePreInstalledSize().Result);
ins.Install("D:/.minecraft").Wait();*/

/*FileIntegrityChecker fileIntegrityChecker = new FileIntegrityChecker(new VersionParse(new ClientInstancesInfo()
{
    GameCatalog = "D:/Games/.minecraft",
    GameName = version
})); // ver 参数是先前读取的游戏
GameFileCompleter fileCompleter = new GameFileCompleter();
fileCompleter.ProgressCallback = (@enum,logs, progress) => { Console.WriteLine($"{@enum} {logs} {progress}"); }; // 进度
fileCompleter.DownloadMissingFilesAsync(fileIntegrityChecker.GetMissingFiles()).Wait();*/

ClientRunner run = new ClientRunner(new ClientRunnerInfo()
{
    Account = new OffineAuthenticator("test").Authenticator(),
    GameInstances = new VersionParse(new ClientInstancesInfo()
    {
        GameCatalog = "D:/Games/.minecraft",
        GameName = version
    }),
    JavaInfo = new JavaInfo()
    {
        JavaPath = "C:\\Program Files\\Java\\jdk-22\\bin\\java.exe"
    },
    LauncherInfo = "RMCL"
});
run.LogsOutput = s => Console.WriteLine(s);
run.Start();

Console.ReadKey();