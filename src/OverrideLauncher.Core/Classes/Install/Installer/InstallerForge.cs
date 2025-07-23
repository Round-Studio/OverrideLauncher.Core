using OverrideLauncher.Core.Base.Dictionary;
using OverrideLauncher.Core.Base.Entry.Download.Install.Client;
using OverrideLauncher.Core.Interface.Download;

namespace OverrideLauncher.Core.Classes.Install.Installer;

public class InstallerForge : IDownload
{
    public InstallerForge(string forgeVersionId)
    {
        var forgeversion = forgeVersionId.Replace($"{forgeVersionId.Split('-')[0]}-", "");
        var clientversion = forgeVersionId.Split('-')[0];

        var installerUrl = DictionaryDownloadHost.Sources.ForgeResourceHost
            .Replace("{FORGE_VERSION_ID}", forgeVersionId)
            .Replace("{FORGE_VERSION}",forgeversion)
            .Replace("{CLIENT_VERSION}",clientversion);
        
        Console.WriteLine(installerUrl);
    }

    public async Task Install(ClientRootInfo rootInfo)
    {
        
    }
}