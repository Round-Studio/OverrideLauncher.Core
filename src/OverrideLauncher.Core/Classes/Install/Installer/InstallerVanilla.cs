using OverrideLauncher.Core.Base.Entry.Download.Install.Client;
using OverrideLauncher.Core.Base.Entry.Download.Install.Manifest;
using OverrideLauncher.Core.Interface.Download;

namespace OverrideLauncher.Core.Classes.Install.Installer;

public class InstallerVanilla : IDownload
{
    private InstallClient _installClient;
    public InstallerVanilla(ManifestMojang.ManifestVersion manifestVersion)
    {
        _installClient = new InstallClient(manifestVersion);
        _installClient.DownloadStatusChanged += (sender, entry) =>
        {
            DownloadStatusChanged?.Invoke(this, entry);
        };
    }

    public async Task Install(InstallClientInfo info)
    {
        await _installClient.Install(info);
    }
}