using OverrideLauncher.Core.Modules.Classes.Account;
using OverrideLauncher.Core.Modules.Classes.Download;
using OverrideLauncher.Core.Modules.Classes.Launch.Client;
using OverrideLauncher.Core.Modules.Classes.Version;
using OverrideLauncher.Core.Modules.Entry.GameEntry;
using OverrideLauncher.Core.Modules.Entry.JavaEntry;
using OverrideLauncher.Core.Modules.Entry.LaunchEntry;
using OverrideLauncher.Core.Modules.Enum.Download;

var installversion = "1.21.5";
var version = installversion;

/*InstallClient ins = new InstallClient(DownloadVersionHelper.TryingFindVersion(installversion).Result, version);
ins.ProgressCallback = (DownloadStateEnum state,string logs, double progress) => { Console.WriteLine(logs+"   "+progress); };
ins.DownloadThreadsCount = 512;
Console.WriteLine(ins.GetThePreInstalledSize().Result);
ins.Install("D:/.minecraft").Wait();*/

ClientRunner run = new ClientRunner(new ClientRunnerInfo()
{
    Account = new OffineAuthenticator("test").Authenticator(),
    GameInstances = new VersionParse(new ClientInstancesInfo()
    {
        GameCatalog = "D:/.minecraft",
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