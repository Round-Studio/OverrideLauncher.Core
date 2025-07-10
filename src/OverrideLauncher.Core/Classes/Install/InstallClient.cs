using System.Text.Json;
using System.Text.Json.Serialization;
using OverrideLauncher.Core.Base.Dictionary;
using OverrideLauncher.Core.Base.Entry.Download.Install.Client;
using OverrideLauncher.Core.Base.Entry.Download.Install.Manifest;
using OverrideLauncher.Core.Classes.Install.Manifest;
using OverrideLauncher.Core.Interface.Download;

namespace OverrideLauncher.Core.Classes.Install;

public class InstallClient : Download
{
    #region Public
    public ManifestClientJson ManifestClientJson { get; set; }
    public ManifestClientAssetsJson ManifestClientAssetsJson { get; set; }
    public InstallClient(ManifestMojang.ManifestVersion manifestVersion)
    {
        _installName = manifestVersion.Id;
        ManifestClientJson = InstallHelper.TryingGetClientJson(manifestVersion).Result;
        ManifestClientAssetsJson = InstallHelper.TryingGetClientAssetsJson(ManifestClientJson).Result;
    }

    public async Task Install(InstallClientInfo info)
    {
        _installClientInfo = info;
        if(!string.IsNullOrEmpty(info.InstallName)) _installName = info.InstallName;
        
        SaveJson();
    }

    #endregion
    
    #region Private
    private string _installName { get; set; }
    private InstallClientInfo _installClientInfo { get; set; }
    private void SaveJson()
    {
        var versionJsonPath = Path.Combine(_installClientInfo.InstallPath, DictionaryGameRoot.VersionsPath,
            _installName, $"{_installName}.json");
        var assetsJsonPath = Path.Combine(_installClientInfo.InstallPath, DictionaryGameRoot.AssetsIndexPath,
            $"{ManifestClientJson.AssetIndex.Id}.json");
        
        Directory.CreateDirectory(Path.GetDirectoryName(versionJsonPath));
        Directory.CreateDirectory(Path.GetDirectoryName(assetsJsonPath));
        
        File.WriteAllText(versionJsonPath,JsonSerializer.Serialize(ManifestClientJson));
        File.WriteAllText(assetsJsonPath,JsonSerializer.Serialize(ManifestClientAssetsJson));
    }
    
    #endregion
}