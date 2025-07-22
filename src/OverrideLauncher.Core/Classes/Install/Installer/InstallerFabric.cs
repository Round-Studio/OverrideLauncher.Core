using System.Text.Json;
using OverrideLauncher.Core.Base.Dictionary;
using OverrideLauncher.Core.Base.Entry.Download.Install.Client;
using OverrideLauncher.Core.Base.Entry.Download.Install.Fabric;
using OverrideLauncher.Core.Base.Entry.Download.Install.Manifest;
using OverrideLauncher.Core.Classes.Install.Manifest;
using OverrideLauncher.Core.Interface.Download;

namespace OverrideLauncher.Core.Classes.Install.Installer;

public class InstallerFabric : IDownload
{
    private ManifestClientJson? ManifestClientJson { get; set; }
    private FabricLoaderVersion _fabricProfile;

    public InstallerFabric(FabricLoaderVersion fabricProfile)
    {
        _fabricProfile = fabricProfile;
    }

    public async Task Install(ClientRootInfo rootInfo)
    {
        ManifestClientJson = InstallHelper.GetClientJsonEntry(rootInfo);

        ManifestClientJson.ModLoader.Add(new ModLoaderInfo()
        {
            Name = "fabric",
            Version = _fabricProfile.Loader.Version
        });

        var lst = new List<ManifestClientJson.Library>();
        
        lst.Add(new ManifestClientJson.Library()
                  {
                      Url = DictionaryDownloadHost.Sources.FabricResourceHost,
                      Name = _fabricProfile.Intermediary.Maven
                  });
        lst.Add(new ManifestClientJson.Library()
        {
            Url = DictionaryDownloadHost.Sources.FabricResourceHost,
            Name = _fabricProfile.Loader.Maven
        });
        _fabricProfile.LauncherMeta.Libraries.Common.ToList().ForEach(x =>
        {
            Console.WriteLine(x.Name);
            lst.Add(new ManifestClientJson.Library()
            {
                Url = x.Url,
                Name = x.Name,
                Size = (ulong)x.Size
            });
        });
        
        ManifestClientJson.Libraries.AddRange(lst);

        if (_fabricProfile.LauncherMeta.MainClass is string mastr)
        {
            ManifestClientJson.MainClass = mastr;
        }
        else
        {
            var mainClass = JsonSerializer.Deserialize<MainClass>(_fabricProfile.LauncherMeta.MainClass.ToString());
            ManifestClientJson.MainClass = mainClass.Client;
        }

        InstallHelper.SaveClientJson(ManifestClientJson, rootInfo);
        
        
    }
}