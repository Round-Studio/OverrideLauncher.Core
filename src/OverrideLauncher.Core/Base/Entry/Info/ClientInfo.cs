using OverrideLauncher.Core.Base.Entry.Download.Install.Manifest;

namespace OverrideLauncher.Core.Base.Entry.Info;

public class ClientInfo
{
    public bool FilesFullRange { get; set; } = false;
    public string ClientName { get; set; } = String.Empty;
    public string ClientRootPath { get; set; } = String.Empty;
    public string ClientVersion { get; set; } = String.Empty;
    public string System { get; set; } = String.Empty;
    public List<string> ModLoaders { get; set; } = null;
    public ManifestClientJson ManifestClientJson { get; set; } = null;
    public ManifestClientAssetsJson ManifestClientAssetsJson { get; set; } = null;
}