using OverrideLauncher.Core.Base.Entry.Download.Install.Fabric;
using OverrideLauncher.Core.Base.Entry.Download.Install.Manifest;

namespace OverrideLauncher.Core.Base.Entry.Download.Install;

public class InstallerCollectionEntry
{
    public ManifestMojang.ManifestVersion VanillaManifest { get; set; } = null;
    
    public FabricLoaderVersion FabricVersion { get; set; } = null;
}