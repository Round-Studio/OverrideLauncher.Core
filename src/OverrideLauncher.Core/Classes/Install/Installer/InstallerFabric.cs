using OverrideLauncher.Core.Base.Entry.Download.Install.Fabric;
using OverrideLauncher.Core.Interface.Download;

namespace OverrideLauncher.Core.Classes.Install.Installer;

public class InstallerFabric : IDownload
{
    private FabricLoaderVersion _fabricProfile;
    public InstallerFabric(FabricLoaderVersion fabricProfile)
    {
        _fabricProfile = fabricProfile;
    }
}