using OverrideLauncher.Core.Modules.Classes.Download;
using OverrideLauncher.Core.Modules.Enum.Download;

var installversion = "1.17.1";
var version = installversion;

InstallClient ins = new InstallClient(DownloadVersionHelper.TryingFindVersion(installversion).Result, version);
ins.ProgressCallback = (DownloadStateEnum state,string logs, double progress) => { Console.WriteLine(logs+"   "+progress); };
ins.DownloadThreadsCount = 512;
Console.WriteLine(ins.GetThePreInstalledSize().Result);
ins.Install("D:/Games/.minecraft").Wait();