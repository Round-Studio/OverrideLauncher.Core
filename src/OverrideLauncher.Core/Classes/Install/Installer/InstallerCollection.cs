using System.Collections;
using OverrideLauncher.Core.Base.Entry.Download.Install;
using OverrideLauncher.Core.Base.Entry.Download.Install.Client;
using OverrideLauncher.Core.Interface.Download;

namespace OverrideLauncher.Core.Classes.Install.Installer;

public class InstallerCollection
{
    public EventHandler<DownloadStatusChangedEntry> DownloadStatusChanged;
    
    public InstallerVanilla VanillaInstaller { get; private set; }
    
    public InstallerCollection(InstallerCollectionEntry installerCE)
    {
        if (installerCE.VanillaManifest == null) throw new NullReferenceException("VanillaManifest is null");

        if (installerCE.VanillaManifest != null)
        {
            VanillaInstaller = new InstallerVanilla(installerCE.VanillaManifest);
            VanillaInstaller.DownloadStatusChanged += (sender, entry) =>
            {
                DownloadStatusChanged?.Invoke(this, entry);
            };
        }
    }
    
    public void Install(InstallClientInfo info)
    {
        var ien = new List<Task>();
        ien.Add(VanillaInstaller.Install(info));

        Task.WaitAll(ien.ToArray());
    }
}