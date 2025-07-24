using System.Collections;
using OverrideLauncher.Core.Base.Entry.Download.Install;
using OverrideLauncher.Core.Base.Entry.Download.Install.Client;
using OverrideLauncher.Core.Interface.Download;

namespace OverrideLauncher.Core.Classes.Install.Installer;

public class InstallerCollection
{
    public EventHandler<DownloadStatusChangedEntry> DownloadStatusChanged;
    
    public InstallerVanilla VanillaInstaller { get; private set; }
    public InstallerFabric FabricInstaller { get; private set; }
    public InstallerForge ForgeInstaller { get; private set; }
    
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

        if (installerCE.FabricVersion != null)
        {
            FabricInstaller = new InstallerFabric(installerCE.FabricVersion);
            FabricInstaller.DownloadStatusChanged += (sender, entry) =>
            {
                DownloadStatusChanged?.Invoke(this, entry);
            };

            if (installerCE.FabricApiVersion != null)
            {
                FabricInstaller.FabricApiVersion = installerCE.FabricApiVersion;
            }
        }

        if (!string.IsNullOrEmpty(installerCE.ForgeVersion))
        {
            ForgeInstaller = new InstallerForge(installerCE.ForgeVersion);
            ForgeInstaller.DownloadStatusChanged += (sender, entry) =>
            {
                DownloadStatusChanged?.Invoke(this, entry);
            };
        }
    }
    
    public void Install(ClientRootInfo rootInfo)
    {
        VanillaInstaller.Install(rootInfo).Wait();
        var ien = new List<Task>();
        if (FabricInstaller != null) ien.Add(FabricInstaller.Install(rootInfo));
        if (ForgeInstaller != null) ien.Add(ForgeInstaller.Install(rootInfo));

        Task.WaitAll(ien.ToArray());
    }
}