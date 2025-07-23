namespace OverrideLauncher.Core.Base.Dictionary;

public class DictionaryDownloadHost
{
    // 源
    public static MirrorConfig Sources;

    // 镜像源配置 - 可以根据网络情况切换
    public static readonly Dictionary<string, MirrorConfig> MirrorSources = new()
    {
        ["official"] = new MirrorConfig
        {
            Name = "官方源",
            ManifestHost = "https://piston-meta.mojang.com/mc/game/version_manifest.json",
            ResourceHost = "https://resources.download.minecraft.net",
            LibrariesHost = "https://libraries.minecraft.net",
            
            FabricHost = "https://meta.fabricmc.net/v2",
            FabricResourceHost = "https://maven.fabricmc.net",
            FabricAPIModURL = "https://api.modrinth.com/v2/project/P7dR8mSH/version",
            
            ForgeManufestHost = "https://maven.minecraftforge.net/net/minecraftforge/forge/maven-metadata.xml",
            ForgeResourceHost = "https://maven.minecraftforge.net/net/minecraftforge/forge/{FORGE_VERSION_ID}/forge-{FORGE_VERSION_ID}-installer.jar"
        },
        ["bmclapi"] = new MirrorConfig
        {
            Name = "BMCLAPI镜像",
            ManifestHost = "https://bmclapi2.bangbang93.com/mc/game/version_manifest.json",
            ResourceHost = "https://bmclapi2.bangbang93.com/assets",
            LibrariesHost = "https://bmclapi2.bangbang93.com/maven",
            
            FabricHost = "https://bmclapi2.bangbang93.com/fabric-meta/v2",
            FabricResourceHost = "https://bmclapi2.bangbang93.com/maven",
            FabricAPIModURL = "https://api.modrinth.com/v2/project/P7dR8mSH/version",
            
            ForgeManufestHost = "https://maven.minecraftforge.net/net/minecraftforge/forge/maven-metadata.xml",
            ForgeResourceHost = "https://bmclapi2.bangbang93.com/forge/download?mcversion={CLIENT_VERSION}&version={FORGE_VERSION}&category=installer&format=jar"
        }
    };

    // 当前使用的镜像源
    public static string CurrentMirror = "official";

    /// <summary>
    /// 切换镜像源
    /// </summary>
    /// <param name="mirrorKey">镜像源键名</param>
    public static void SwitchMirror(string mirrorKey)
    {
        if (MirrorSources.ContainsKey(mirrorKey))
        {
            CurrentMirror = mirrorKey;
            Sources = MirrorSources[mirrorKey];
        }
    }

    /// <summary>
    /// 获取当前镜像配置
    /// </summary>
    public static MirrorConfig GetCurrentMirror()
    {
        return MirrorSources.GetValueOrDefault(CurrentMirror, MirrorSources["official"]);
    }
}

public class MirrorConfig
{
    public string Name { get; set; }
    public string ManifestHost { get; set; }
    public string ResourceHost { get; set; }
    public string LibrariesHost { get; set; }
    
    public string FabricHost { get; set; }
    public string FabricResourceHost { get; set; }
    public string FabricAPIModURL { get; set; }
    
    public string ForgeManufestHost { get; set; }
    public string ForgeResourceHost { get; set; }
}