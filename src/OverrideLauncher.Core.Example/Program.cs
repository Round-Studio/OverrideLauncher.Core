using OverrideLauncher.Core.Base.Entry.Download.Install.Client;
using OverrideLauncher.Core.Classes.Install;
using OverrideLauncher.Core.Classes.Install.Manifest;

InstallClient install = new InstallClient(await InstallHelper.TryingFindVersion("1.17.1"));
await install.Install(new InstallClientInfo()
{
    InstallName = "测试",
    InstallPath = "G:\\testmc"
});